using CloudflareDNSUpdater;
using System.Net.Mime;
using System.Text;
using System.Text.Json.Nodes;

Logger.Start("Starting updating DNS records with the current ip");
string configFile = Path.GetFullPath("cloudflare.config");

if (!File.Exists(configFile))
{
    Logger.LogError("The config file is missing");
    throw new FileNotFoundException("The config file is missing", "cloudflare.config");
}

string json = File.ReadAllText(configFile, Encoding.UTF8);
Configuration? configuration = System.Text.Json.JsonSerializer.Deserialize<Configuration>(json);

if (configuration is null)
{
    Logger.LogError("Could not serialize the configuration");
    throw new NullReferenceException("Could not serialize the configuration");
}

if (string.IsNullOrEmpty(configuration.BearerToken))
{
    Logger.LogError("The bearer token is empty");
    throw new NullReferenceException("The bearer token is empty");
}

if (configuration.ZoneIdentifiers is null || configuration.ZoneIdentifiers.Count == 0)
{
    Logger.LogError("No zones found");
    throw new NullReferenceException("No zones found");
}

HttpClient client = new();
client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", configuration.BearerToken);

// Get current public/external ip
HttpResponseMessage ipResponseMessage = new HttpClient().Send(new(HttpMethod.Get, "http://icanhazip.com"));
string ip = ipResponseMessage.Content.ReadAsStringAsync().Result.Trim();
Logger.LogInfo($"Current ip address: {ip}");

foreach (string zoneIdentifier in configuration.ZoneIdentifiers)
{
    Logger.LogInfo($"Fetching zone dns entries (Identifier: {zoneIdentifier})");

    HttpResponseMessage dnsRecordsResponseMessage = client.Send(new(HttpMethod.Get, $"https://api.cloudflare.com/client/v4/zones/{zoneIdentifier}/dns_records?type=A"));
    JsonObject dnsRecords = (JsonObject)JsonNode.Parse(dnsRecordsResponseMessage.Content.ReadAsStringAsync().Result)!;

    foreach (JsonObject? dnsRecord in dnsRecords.First().Value!.AsArray().Cast<JsonObject?>())
    {
        if (dnsRecord is null)
            continue;

        string identifier = dnsRecord["id"]!.GetValue<string>();
        string dnsName = dnsRecord["name"]!.GetValue<string>();

        JsonObject bodyJson = new()
        {
            { "type", dnsRecord["type"]!.GetValue<string>() },
            { "name", dnsName },
            { "content", ip },
            { "ttl", dnsRecord["ttl"]!.GetValue<int>() },
            { "proxied", dnsRecord["proxied"]!.GetValue<bool>() }
        };

        Logger.LogInfo($"Sending request for DNS {dnsName} and identifier {identifier}");
        HttpRequestMessage requestMessage = new(HttpMethod.Put, $"https://api.cloudflare.com/client/v4/zones/{zoneIdentifier}/dns_records/{identifier}")
        {
            Content = new StringContent(bodyJson.ToJsonString(), Encoding.UTF8, MediaTypeNames.Application.Json)
        };
        HttpResponseMessage responseMessage = client.Send(requestMessage);

        Logger.LogInfo($"DNS identifier change: {responseMessage.StatusCode}");
        if (!responseMessage.IsSuccessStatusCode)
            Logger.LogError(responseMessage.Content.ReadAsStringAsync().Result);
    }
}