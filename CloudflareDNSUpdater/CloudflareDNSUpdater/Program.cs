using CloudflareDNSUpdater;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;

string basePath = args[0];
string configFile = Path.Combine(basePath, "cloudflare.config");
string logFilePath = Path.Combine(basePath, "cloudflare.log");

Logger.LogInfo(logFilePath, "Starting updating DNS records with the current ip");

if (!File.Exists(configFile))
{
    Logger.LogError(logFilePath, "The config file is missing");
    throw new FileNotFoundException("The config file is missing", configFile);
}

string json = File.ReadAllText(configFile, Encoding.UTF8);
Configuration? configuration = System.Text.Json.JsonSerializer.Deserialize<Configuration>(json);

if (configuration is null)
{
    Logger.LogError(logFilePath, "Could not serialize the configuration");
    throw new NullReferenceException("Could not serialize the configuration");
}

if (string.IsNullOrEmpty(configuration.BearerToken))
{
    Logger.LogError(logFilePath, "The bearer token is empty");
    throw new NullReferenceException("The bearer token is empty");
}

if (configuration.ZoneIdentifiers is null || configuration.ZoneIdentifiers.Count == 0)
{
    Logger.LogError(logFilePath, "No zones found");
    throw new NullReferenceException("No zones found");
}

HttpClient client = new();
client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", configuration.BearerToken);

// Get current public/external ip
HttpResponseMessage ipResponseMessage = new HttpClient().Send(new(HttpMethod.Get, "http://icanhazip.com"));
string ip = ipResponseMessage.Content.ReadAsStringAsync().Result.Trim();
Logger.LogInfo(logFilePath, $"Current ip address: {ip}");

foreach (string zoneIdentifier in configuration.ZoneIdentifiers)
{
    Logger.LogInfo(logFilePath, $"Fetching zone dns entries (Identifier: {zoneIdentifier})");

    HttpResponseMessage dnsRecordsResponseMessage = client.Send(new(HttpMethod.Get, $"https://api.cloudflare.com/client/v4/zones/{zoneIdentifier}/dns_records?type=A"));
    JsonObject? dnsRecords = (JsonObject)JsonNode.Parse(dnsRecordsResponseMessage.Content.ReadAsStringAsync().Result)!;

    if (dnsRecords == null || dnsRecords.Count == 0 || dnsRecords.First().Value == null || dnsRecords.First().Value!.GetType() != typeof(JsonArray))
        continue;

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

        Logger.LogInfo(logFilePath, $"Sending request for DNS {dnsName} and identifier {identifier}");
        HttpRequestMessage requestMessage = new(HttpMethod.Put, $"https://api.cloudflare.com/client/v4/zones/{zoneIdentifier}/dns_records/{identifier}")
        {
            Content = new StringContent(bodyJson.ToJsonString(), Encoding.UTF8, MediaTypeNames.Application.Json)
        };
        HttpResponseMessage responseMessage = client.Send(requestMessage);

        Logger.LogInfo(logFilePath, $"DNS identifier change: {responseMessage.StatusCode}");
        if (!responseMessage.IsSuccessStatusCode)
            Logger.LogError(logFilePath, responseMessage.Content.ReadAsStringAsync().Result);
    }
}