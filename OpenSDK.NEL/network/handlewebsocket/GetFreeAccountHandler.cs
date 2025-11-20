namespace OpenSDK.NEL.HandleWebSocket;

using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager.Helpers;
using Serilog;

internal class GetFreeAccountHandler : IWsHandler
{
    public async Task ProcessAsync(WebSocket ws, JsonElement root)
    {
        Log.Information("正在获取4399小号...");
        await SendJsonAsync(ws, new { type = "get_free_account_status", status = "processing", message = "获取小号中, 这可能需要点时间..." });

        await Task.Run(async () =>
        {
            IWebDriver? driver = null;
            try
            {
                new DriverManager().SetUpDriver(new EdgeConfig(), VersionResolveStrategy.MatchingBrowser);

                var options = new EdgeOptions();
                options.AddArgument("--headless"); 
                options.AddArgument("--disable-gpu");
                options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.0.0");
                driver = new EdgeDriver(options);
                string targetUrl = "https://freecookie.studio";
                driver.Navigate().GoToUrl(targetUrl);
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));
                var button = wait.Until(d => d.FindElement(By.XPath("//button[contains(text(),'获取4399') or contains(text(),'领取')]")));
                ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", button);
                wait.Until(d => d.FindElement(By.Id("account")).GetAttribute("value").Length > 5);
                wait.Until(d => d.FindElement(By.Id("password")).GetAttribute("value").Length > 5);
                string account = driver.FindElement(By.Id("account")).GetAttribute("value").Trim();
                string password = driver.FindElement(By.Id("password")).GetAttribute("value").Trim();
                Log.Information("获取成功: {Account} {Password}", account, password);
                await SendJsonAsync(ws, new 
                { 
                    type = "get_free_account_result", 
                    success = true, 
                    account = account, 
                    password = password,
                    message = "获取成功！"
                });
            }
            catch (Exception e)
            {
                Log.Error(e, "错误: {Message}", e.Message);
                await SendJsonAsync(ws, new { type = "get_free_account_result", success = false, message = "错误: " + e.Message });
            }
            finally
            {
                driver?.Quit();
            }
        });
    }
    
    private async Task SendJsonAsync(WebSocket ws, object data)
    {
        if (ws.State == WebSocketState.Open)
        {
            var json = JsonSerializer.Serialize(data);
            var buffer = Encoding.UTF8.GetBytes(json);
            await ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}