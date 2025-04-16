using Microsoft.Playwright;
namespace shotweb;

public static class WebScreenshotHelper
{
    public static async Task<byte[]> CaptureScreenshotAsync(string url, int width = 1280, int height = 720, bool useFullPage = false, bool emulateMobile = false)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        if (!emulateMobile)
        {
            // Subtract estimated browser chrome (e.g. 85px)
            height = Math.Max(height - 85, 200);
        }

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            IsMobile = emulateMobile,
            UserAgent = emulateMobile
                ? "Mozilla/5.0 (iPhone; CPU iPhone OS 15_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/15.0 Mobile/15E148 Safari/604.1"
                : null
        });

        var page = await context.NewPageAsync();
        await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        return await page.ScreenshotAsync(new PageScreenshotOptions
        {
            FullPage = useFullPage
        });
    }

    public static async Task<Dictionary<string, byte[]>> CaptureMultipleScreenshotsAsync(string url, (int width, int height, string name)[] viewports, bool fullPage = false, bool emulateMobile = false)
    {
        var result = new Dictionary<string, byte[]>();

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            IsMobile = emulateMobile,
            UserAgent = emulateMobile
                ? "Mozilla/5.0 (iPhone; CPU iPhone OS 15_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/15.0 Mobile/15E148 Safari/604.1"
                : null
        });

        var page = await context.NewPageAsync();

        await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        foreach (var (width, height, name) in viewports)
        {
            try
            {
                await page.SetViewportSizeAsync(width, height);
                var screenshot = await page.ScreenshotAsync(new PageScreenshotOptions
                {
                    FullPage = fullPage
                });

                result[name] = screenshot;
            }
            catch
            {
                // Skip failed viewport
            }
        }

        return result;
    }

    public static async Task<Dictionary<string, byte[]>> CaptureMultipleDesktopScreenshotsAsync(string url, (int width, int height)[] viewports, bool fullPage = false)
    {
        var result = new Dictionary<string, byte[]>();

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        var context = await browser.NewContextAsync(); // default desktop profile
        var page = await context.NewPageAsync();

        await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        foreach (var (width, height) in viewports)
        {
            try
            {
                await page.SetViewportSizeAsync(width, height);
                var screenshot = await page.ScreenshotAsync(new PageScreenshotOptions
                {
                    FullPage = fullPage
                });

                result[$"screenshot_{width}x{height}"] = screenshot;
            }
            catch
            {
                // log or skip silently
            }
        }

        return result;
    }


}
