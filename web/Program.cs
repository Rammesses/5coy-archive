using System;
using System.Net.Http;
using System.Threading.Tasks;

using Blazor.Extensions.Logging;

using Ganss.Xss;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Services;

namespace web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            builder.Services.AddLogging(builder => builder
                .AddBrowserConsole()
                .SetMinimumLevel(LogLevel.Information)
            );

            builder.RootComponents.Add<App>("#app");

            builder.Services.AddBlazorBootstrap();

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddScoped<IArticlesService, ArticlesService>();
            builder.Services.AddScoped<IMissionsService, MissionsService>();
            builder.Services.AddScoped<IVideoService, VideoService>();
            builder.Services.AddScoped<IMarkdownService, MarkdownService>();
            builder.Services.AddScoped<IHtmlSanitizer, HtmlSanitizer>();

            await builder.Build().RunAsync();
        }
    }
}
