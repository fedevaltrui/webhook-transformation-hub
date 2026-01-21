using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Hub.Api.Ingest;

public static class RequestCapture
{


    public static string CaptureHeadersJson(HttpRequest req)
    {
        

        //Serializar headers a JSON plain
        var dict = req.Headers.ToDictionary(
            h => h.Key,
            h => string.Join(",", h.Value.ToArray())
        );

        return JsonSerializer.Serialize(dict);
    }

    public static async Task<string> CaptureBodyJsonAsync(HttpRequest req, CancellationToken ct = default)
    {
        //Leemos body
        req.EnableBuffering();

        using var reader = new StreamReader(req.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var raw = await reader.ReadToEndAsync(ct);
        req.Body.Position = 0;

        var contentType = req.ContentType ?? "application/octet-stream";
        if (string.IsNullOrWhiteSpace(raw))
        {
            return JsonSerializer.Serialize(new{empty = true, contentType});
        }
        
        //Si es JSON
        if (contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);
                return doc.RootElement.GetRawText();
            } catch
            {
                //cae al raw wrapper
            }
        }
            //Fallback
            var rawBytes = Encoding.UTF8.GetBytes(raw);
            var b64 = Convert.ToBase64String(rawBytes);

            return JsonSerializer.Serialize(new {rawBase64 = b64, contentType});
        }

        public static string GenerateEndpointKey(int bytes = 18)
    {
        var data = RandomNumberGenerator.GetBytes(bytes);

        //base64url sin padding 
        return Convert.ToBase64String(data)
        .TrimEnd('=')
        .Replace('+','-')
        .Replace('/','_');
    }

}
