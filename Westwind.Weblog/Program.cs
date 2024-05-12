using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Castle.Core.Configuration;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Westwind.AspNetCore.LiveReload;
using Westwind.Weblog.Business.Configuration;
using Westwind.Weblog.Business.Models;
using Westwind.Weblog.Business;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http;
using Microsoft.Extensions.Hosting;
using Westwind.AspNetCore.Extensions;
using System.Runtime.InteropServices;

//namespace Westwind.Weblog
//{
//    public class Program
//    {
//        public static void Main(string[] args)
//        {
//            BuildWebHost(args).Run();
//        }

//        public static IWebHost BuildWebHost(string[] args) =>
//            WebHost.CreateDefaultBuilder(args)
//                .UseStartup<Startup>()
//                .Build();
//    }
//}



var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var env = builder.Environment;


// constants            
wlApp.StartupFolder = Environment.CurrentDirectory;
wlApp.WebRootFolder = System.IO.Path.Combine(wlApp.StartupFolder, "wwwroot");

services.AddMemoryCache();

// initial read from disk
var config = wlApp.Configuration;

// read configuration overrides
builder.Configuration.Bind("Weblog", config);
services.AddSingleton(config);


// write out to disk full configuration
wlApp.Configuration.Write();


services.AddLiveReload(config =>
{
    config.LiveReloadEnabled = wlApp.Configuration.System.LiveReloadEnabled;
    config.RefreshInclusionFilter = path =>
    {
        if (path.Contains("/LocalizationAdmin", StringComparison.OrdinalIgnoreCase))
            return RefreshInclusionModes.DontRefresh;

        return RefreshInclusionModes.ContinueProcessing;
    };
});

// logging
var logConfig = new LoggerConfiguration()
    .MinimumLevel.Warning()
    .Enrich.FromLogContext()
    // .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}---{NewLine}")
    .WriteTo.File(
        System.IO.Path.Combine(wlApp.WebRootFolder, "admin", "applicationlog.txt"),
        fileSizeLimitBytes: 3_000_000,
        retainedFileCountLimit: 5,
        rollOnFileSizeLimit: true,
        shared: true,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}{NewLine}---",
        flushToDiskInterval: TimeSpan.FromSeconds(20));
Log.Logger = logConfig.CreateLogger();
services.AddSerilog(Log.Logger);

Log.Logger.Information("Application Started.");

services.AddScoped<PostBusiness>();
services.AddScoped<AdminBusiness>();
services.AddScoped<UserBusiness>();

services.AddDbContext<WeblogContext>(builder =>
{
    var connStr = config.ConnectionString;
    if (string.IsNullOrEmpty(connStr))
        connStr = "server=.;database=WeblogCore; integrated security=true;MultipleActiveResultSets=true";

    builder.UseSqlServer(connStr, opt =>
    {
        opt.EnableRetryOnFailure();
        opt.CommandTimeout(15);
    });
});



// set up and configure Authentication - make sure to call .UseAuthentication()
services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/account/login";
        o.LogoutPath = "/account/logout";
    });

services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();




// ***  BUILD ***
var app = builder.Build();




wlApp.IsDevelopment = env.IsDevelopment();
wlApp.ServiceProvider = app.Services;

// pre-load model async
Task.Run(() =>
{
    // can't inject configuration here :-( So we use explict
    string connectionString = wlApp.Configuration.ConnectionString; // Configuration["Data:SqlServerConnectionString"];

    var context = WeblogContext.GetWeblogContext(connectionString);
    context.Posts.Any(p => p.Id == -1);
});

wlApp.Cache = app.Services.GetService<IMemoryCache>();

if (env.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStatusCodePages(new StatusCodePagesOptions
{
    HandleAsync = (ctx) =>
    {
        if (ctx.HttpContext.Response.StatusCode == 404)
        {
            ctx.HttpContext.Response.Redirect("/home/missingpage?url=" + ctx.HttpContext.Request.GetUrl());
            return Task.FromResult(0);
            //// throw an exception so it shows as an error page
            ////  404 has special handling in `/home/error`
            ////throw new HttpRequestException("Page not  found: " + ctx.HttpContext.Request.Path, null, statusCode: System.Net.HttpStatusCode.NotFound);
            //var ctxAccessor = ctx.HttpContext.RequestServices.GetService<IHttpContextAccessor>();
            //var factory = ctx.HttpContext.RequestServices.GetService<BusinessFactory>();
            //var logger = ctx.HttpContext.RequestServices.GetService<ILogger<HomeController>>();


            //var controller = new HomeController(logger,factory, ctxAccessor );
            //var result = controller.Error(new HttpRequestException(
            //    "Page not  found: " + ctx.HttpContext.Request.Path,
            //    null,
            //    statusCode: System.Net.HttpStatusCode.NotFound));
        }
        else if (ctx.HttpContext.Response.StatusCode == 401)
        {
            throw new HttpRequestException("Unauthorized: " + ctx.HttpContext.Request.Path, null, statusCode: System.Net.HttpStatusCode.Unauthorized);
        }

        return Task.FromResult(0);
    }
});

app.UseAuthentication();

app.UseRouting();

app.UseAuthorization();

app.UseStatusCodePages();

app.UseLiveReload();
app.UseStaticFiles();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
    //endpoints.MapRazorPages();
});



Console.ForegroundColor = ConsoleColor.DarkYellow;
Console.WriteLine($@"-----------------
West Wind Web Log
-----------------");
Console.ResetColor();


var urls = builder.WebHost.GetSetting(WebHostDefaults.ServerUrlsKey)?.Replace(";", " ");
Console.Write($"    Urls: ");
Console.ForegroundColor = ConsoleColor.DarkCyan;
Console.WriteLine($"{urls}", ConsoleColor.DarkCyan);
Console.ResetColor();

Console.WriteLine($" Runtime: {RuntimeInformation.FrameworkDescription} - {builder.Environment.EnvironmentName}");
Console.WriteLine($"Platform: {RuntimeInformation.OSDescription}");
Console.WriteLine();


if (!File.Exists("_weblog-configuration.json"))
{
    try
    {
        wlApp.Configuration.Write(); // write out full configuration
    }
    catch { }
}

wlApp.AppStartedOn = DateTime.Now;

app.Run();
