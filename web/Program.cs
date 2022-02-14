using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;

using Blazor.Extensions.Logging;
using Ganss.XSS;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Services;
using BlazorApplicationInsights;

namespace web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            var config = builder.Build().Configuration;

            builder.Services.AddLogging(builder => builder
                .AddBrowserConsole()
                .SetMinimumLevel(LogLevel.Information)
            );

            builder.Services.AddBlazorApplicationInsights(async applicationInsights =>
            {
                // var appInsightsKey = config["ApplicationInsights:InstrumentationKey"] ?? Guid.Empty.ToString();
                var appInsightsKey = "181e8816-8264-4b24-9b0e-0951727e5c76";
                await applicationInsights.SetInstrumentationKey(appInsightsKey);        
                await applicationInsights.TrackPageView();
            });

            builder.RootComponents.Add<App>("#app");

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddScoped<IArticlesService, ArticlesService>();
            builder.Services.AddScoped<IMarkdownService, MarkdownService>();
            builder.Services.AddScoped<IHtmlSanitizer, HtmlSanitizer>();
            await builder.Build().RunAsync();
        }
    }
}
