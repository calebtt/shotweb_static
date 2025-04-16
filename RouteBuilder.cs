using Microsoft.Extensions.FileProviders;
using Microsoft.Playwright;
using System.IO.Compression;
using System.Net.WebSockets;
using System.Text.Json;

namespace shotweb;

public static class ShotWebRouteBuilder
{
    public static void AddStaticImageRoutes(WebApplication app, string apiKey)
    {
        app.MapGet("/screenshot/multi", async (HttpRequest request) =>
        {
            var query = request.Query;
            var url = query["url"].ToString();

            if (string.IsNullOrWhiteSpace(url) || !Uri.IsWellFormedUriString(url, UriKind.Absolute))
                return Results.BadRequest("Missing or invalid URL.");

            (bool isSafe, string reason) = await SafeBrowsing.IsUrlSafeWithReason(url, apiKey);
            if (!isSafe)
            {
                Console.WriteLine($"Blocked URL: {url} - Reason: {reason}");
                return Results.BadRequest($"URL rejected: {reason}");
            }

            bool fullPage = bool.TryParse(query["fullpage"], out var fp) && fp;

            var resolutions = new (int width, int height)[]
            {
                (1920, 1080),
                (2560, 1440),
                (3840, 2160)
            };

            var zipStream = new MemoryStream();
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var screenshots = await WebScreenshotHelper.CaptureMultipleDesktopScreenshotsAsync(url, resolutions, fullPage);

                foreach (var (name, bytes) in screenshots)
                {
                    var entry = archive.CreateEntry($"{name}.png", CompressionLevel.Fastest);
                    using var entryStream = entry.Open();
                    await entryStream.WriteAsync(bytes);
                }
            }

            zipStream.Position = 0;
            return Results.File(zipStream, "application/zip", "screenshots.zip");
        });

        app.MapGet("/screenshot/mobile", async (HttpRequest request) =>
        {
            var query = request.Query;
            var url = query["url"].ToString();

            if (string.IsNullOrWhiteSpace(url) || !Uri.IsWellFormedUriString(url, UriKind.Absolute))
                return Results.BadRequest("Missing or invalid URL.");

            (bool isSafe, string reason) = await SafeBrowsing.IsUrlSafeWithReason(url, apiKey);
            if (!isSafe)
            {
                Console.WriteLine($"Blocked URL: {url} - Reason: {reason}");
                return Results.BadRequest($"URL rejected: {reason}");
            }

            bool fullPage = bool.TryParse(query["fullpage"], out var fp) && fp;

            var viewports = new (int width, int height, string name)[]
            {
                (375, 667, "iPhone8"),
                (390, 844, "iPhone13"),
                (412, 915, "Pixel6"),
                (428, 926, "iPhone14ProMax"),
                (810, 1080, "iPad9"),
                (834, 1194, "iPadPro11"),
                (800, 1280, "GalaxyTabS8")
            };

            try
            {
                var images = await WebScreenshotHelper.CaptureMultipleScreenshotsAsync(url, viewports, fullPage, emulateMobile: true);

                var zipStream = new MemoryStream();
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
                {
                    foreach (var (name, bytes) in images)
                    {
                        var entry = archive.CreateEntry($"{name}.png", CompressionLevel.Fastest);
                        using var entryStream = entry.Open();
                        await entryStream.WriteAsync(bytes);
                    }
                }

                zipStream.Position = 0;
                return Results.File(zipStream, "application/zip", "mobile_screenshots.zip");
            }
            catch (Exception ex)
            {
                return Results.Problem($"Screenshot failed: {ex.Message}");
            }
        });
    }

}

