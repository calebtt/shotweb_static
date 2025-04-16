using System.Text.Json;

namespace shotweb;

public static class SafeBrowsing
{
    public static async Task<(bool isSafe, string reason)> IsUrlSafeWithReason(string url, string apiKey)
    {
        var payload = new
        {
            client = new { clientId = "your-screenshot-service", clientVersion = "1.0" },
            threatInfo = new
            {
                threatTypes = new[] {
                "MALWARE", "SOCIAL_ENGINEERING",
                "UNWANTED_SOFTWARE", "POTENTIALLY_HARMFUL_APPLICATION"
            },
                platformTypes = new[] { "ANY_PLATFORM" },
                threatEntryTypes = new[] { "URL" },
                threatEntries = new[] { new { url } }
            }
        };

        using var client = new HttpClient();
        var response = await client.PostAsync(
            $"https://safebrowsing.googleapis.com/v4/threatMatches:find?key={apiKey}",
            new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")
        );

        var json = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(json) || json.Trim() == "{}")
            return (true, "Clean");

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("matches", out var matches) && matches.GetArrayLength() > 0)
            {
                var first = matches[0];

                string threatType = first.TryGetProperty("threatType", out var tt) ? tt.GetString() ?? "UNKNOWN" : "UNKNOWN";
                string platformType = first.TryGetProperty("platformType", out var pt) ? pt.GetString() ?? "UNKNOWN" : "UNKNOWN";
                string matchedUrl = first.TryGetProperty("threat", out var threatObj) && threatObj.TryGetProperty("url", out var u)
                    ? u.GetString() ?? "unknown"
                    : "unknown";

                string reason = $"Flagged as {threatType} on {platformType}: {matchedUrl}";
                return (false, reason);
            }

            return (true, "Clean"); // No matches present
        }
        catch (Exception ex)
        {
            return (false, $"Blocked due to parsing error: {ex.Message}");
        }
    }


    //public static async Task<bool> IsUrlSafe(string url, string apiKey)
    //{
    //    var payload = new
    //    {
    //        client = new { clientId = "your-screenshot-service", clientVersion = "1.0" },
    //        threatInfo = new
    //        {
    //            threatTypes = new[] {
    //            "MALWARE", "SOCIAL_ENGINEERING",
    //            "UNWANTED_SOFTWARE", "POTENTIALLY_HARMFUL_APPLICATION"
    //        },
    //            platformTypes = new[] { "ANY_PLATFORM" },
    //            threatEntryTypes = new[] { "URL" },
    //            threatEntries = new[] { new { url } }
    //        }
    //    };

    //    using var client = new HttpClient();
    //    var response = await client.PostAsync(
    //        $"https://safebrowsing.googleapis.com/v4/threatMatches:find?key={apiKey}",
    //        new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json")
    //    );

    //    var json = await response.Content.ReadAsStringAsync();
    //    return string.IsNullOrWhiteSpace(json) || json == "{}"; // No matches = safe
    //}

}