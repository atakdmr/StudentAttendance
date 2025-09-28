using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Yoklama.Services.Sms
{
    public interface ISmsService
    {
        Task SendBulkAsync(IEnumerable<(string phone, string message)> messages);
    }

    public sealed class NetGsmSmsService : ISmsService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUser;
        private readonly string _apiPass;
        private readonly string _header;

        public NetGsmSmsService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            var section = configuration.GetSection("NetGSM");
            _apiUser = section["User"] ?? string.Empty;     // fill in prod
            _apiPass = section["Password"] ?? string.Empty;  // fill in prod
            _header = section["Header"] ?? "DERSHANE";
        }

        public async Task SendBulkAsync(IEnumerable<(string phone, string message)> messages)
        {
            // NetGSM expected JSON (simplified): one message to multiple recipients or per message entries
            var entries = new List<Dictionary<string, string>>();
            foreach (var (phone, message) in messages)
            {
                if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(message)) continue;
                entries.Add(new Dictionary<string, string>
                {
                    { "gsmno", phone },
                    { "msg", message }
                });
            }

            var payload = new
            {
                usercode = _apiUser,
                password = _apiPass,
                msgheader = _header,
                messages = entries
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.netgsm.com.tr/sms/send/json");
            req.Content = content;
            using var resp = await _httpClient.SendAsync(req);
            resp.EnsureSuccessStatusCode();
        }
    }
}


