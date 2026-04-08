using ApiMonkey.Models;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ApiMonkey.Services;

internal class ApiClient
{
    private readonly HttpClient _client;

    internal ApiClient(HttpClient? client = null)
    {
        _client = client ?? new HttpClient();
    }

    internal async Task<ApiResponse> SendRequest(string url, string method, string body, Header[] headers)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = new StringContent(body),
        };

        foreach (var header in headers)
        {
            if (header.Name != null && header.Name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(header.Value ?? "");
                continue;
            }

            request.Headers.Add(header.Name ?? "", header.Value);
        }

        var response = await _client.SendAsync(request);

        return new ApiResponse(
            body: await response.Content.ReadAsStringAsync(),
            headers: response.Headers.Select(h => new Header
            {
                Name = h.Key,
                Value = string.Join(", ", h.Value)
            }).ToList()
        );
    }
}