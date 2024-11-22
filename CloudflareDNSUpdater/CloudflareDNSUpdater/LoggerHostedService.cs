using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace DSaladin.CloudflareDnsUpdater
{
    internal class LoggerHostedService : IHostedService, IDisposable
    {
        private readonly ILogger<LoggerHostedService> logger;
        private Timer? _timer;

        private TimeSpan period;
        private string? bearerToken = "";
        private List<string> zoneIdentifiers = [];

        public LoggerHostedService(ILogger<LoggerHostedService> logger)
        {
            this.logger = logger;

            bearerToken = Environment.GetEnvironmentVariable("Token");
            string? zoneIdentifiersText = Environment.GetEnvironmentVariable("ZoneIdentifiers");

            if (!string.IsNullOrEmpty(zoneIdentifiersText))
                zoneIdentifiers = [.. zoneIdentifiersText.Split(",")];

            if (bearerToken is null)
            {
                logger.LogError("The bearer token is empty. Please set the environment variable Token");
                throw new NullReferenceException("The bearer token is empty");
            }

            if (zoneIdentifiers.Count == 0)
            {
                logger.LogError("No zone identifiers defined. Please set the environment variable ZoneIdentifiers");
                throw new NullReferenceException("No zones found");
            }

            string? periodText = Environment.GetEnvironmentVariable("Period");
            period = periodText is not null ? TimeSpan.Parse(periodText) : TimeSpan.FromMinutes(5);


        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Starting Cloudflare DNS Update service.");
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
            return Task.CompletedTask;
        }

        private void DoWork(object? state)
        {
            HttpClient client = new();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);

            // Get current public/external ip
            HttpResponseMessage ipResponseMessage = new HttpClient().Send(new(HttpMethod.Get, "http://icanhazip.com"));
            string ip = ipResponseMessage.Content.ReadAsStringAsync().Result.Trim();

            logger.LogInformation("Sending current ip to Cloudflare: {ip}", ip);

            foreach (string zoneIdentifier in zoneIdentifiers)
            {
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

                    logger.LogInformation("Updating IP for DNS record {dnsName}", dnsName);
                    HttpRequestMessage requestMessage = new(HttpMethod.Put, $"https://api.cloudflare.com/client/v4/zones/{zoneIdentifier}/dns_records/{identifier}")
                    {
                        Content = new StringContent(bodyJson.ToJsonString(), Encoding.UTF8, MediaTypeNames.Application.Json)
                    };
                    HttpResponseMessage responseMessage = client.Send(requestMessage);

                    logger.LogInformation("Update result: {statusCode} - {reasonPhrase}", responseMessage.StatusCode, responseMessage.ReasonPhrase);

                    if (!responseMessage.IsSuccessStatusCode)
                        logger.LogError("Error Response Content: {content}", responseMessage.Content.ReadAsStringAsync().Result);
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Stopping Cloudflare DNS Update service.");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
