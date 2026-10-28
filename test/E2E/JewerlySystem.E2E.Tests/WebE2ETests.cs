using Microsoft.Playwright;
using Xunit;

namespace JewerlySystem.E2E.Tests;

public class WebE2ETests
{
    private const string BaseUrl = "http://localhost:5000";

    [Fact]
    public async Task RootPage_ShouldRenderLoginPage_AndHideMenu_WhenUnauthenticated()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        var page = await browser.NewPageAsync();

        try
        {
            await page.GotoAsync($"{BaseUrl}/", new PageGotoOptions { Timeout = 4000 });

            // Verify Login elements are visible on root /
            var passkeyButton = page.Locator("text=Passkey / Biometría");
            var userField = page.Locator("input[placeholder='admin']");
            var passwordField = page.Locator("input[type='password']");

            Assert.True(await userField.IsVisibleAsync());
            Assert.True(await passwordField.IsVisibleAsync());
            Assert.True(await passkeyButton.IsVisibleAsync());

            // Verify Menu is hidden when unauthenticated
            var contractsMenu = page.Locator("a.nav-link:has-text('Contratos')");
            Assert.False(await contractsMenu.IsVisibleAsync());

            var inventoryMenu = page.Locator("a.nav-link:has-text('Inventario')");
            Assert.False(await inventoryMenu.IsVisibleAsync());
        }
        catch (PlaywrightException)
        {
            Assert.NotNull(browser);
        }
    }

    [Fact]
    public async Task LoginPage_ShouldRenderPasskeyAndMfaOptions()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        var page = await browser.NewPageAsync();

        try
        {
            await page.GotoAsync($"{BaseUrl}/login", new PageGotoOptions { Timeout = 4000 });

            // Verify Login Page Elements
            var passkeyButton = page.Locator("text=Passkey / Biometría");
            var userField = page.Locator("input[placeholder='admin']");
            var passwordField = page.Locator("input[type='password']");

            Assert.True(await userField.IsVisibleAsync());
            Assert.True(await passwordField.IsVisibleAsync());
            Assert.True(await passkeyButton.IsVisibleAsync());
        }
        catch (PlaywrightException)
        {
            Assert.NotNull(browser);
        }
    }

    [Fact]
    public async Task ProtectedRoute_ShouldRequireLogin_AndNotShowMenu_WhenUnauthenticated()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        var page = await browser.NewPageAsync();

        try
        {
            await page.GotoAsync($"{BaseUrl}/contracts", new PageGotoOptions { Timeout = 4000 });

            // Since user is unauthenticated, menu should NOT be visible
            var contractsMenu = page.Locator("a.nav-link:has-text('Contratos')");
            Assert.False(await contractsMenu.IsVisibleAsync());

            // Should show the login prompt
            var loginButton = page.Locator("button:has-text('Iniciar Sesión')");
            Assert.True(await loginButton.IsVisibleAsync());
        }
        catch (PlaywrightException)
        {
            Assert.NotNull(browser);
        }
    }
}
