using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
using Newtonsoft.Json;
using Westwind.AspNetCore;
using Westwind.AspNetCore.Errors;
using Westwind.AspNetCore.Markdown;
using Westwind.Utilities;
using Westwind.Weblog.Business.Utilities;


var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var env = builder.Environment;

wlApp.IsDevelopment = builder.Environment.IsDevelopment();
wlApp.EnvironmentName = builder.Environment.EnvironmentName;
wlApp.Constants.AppStartedOn = DateTime.Now;


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

services.AddScoped<PostBusiness>();
services.AddScoped<AdminBusiness>();
services.AddScoped<UserBusiness>();

if (Environment.CommandLine.Contains("-createdb",StringComparison.OrdinalIgnoreCase))
{
    CreateDb();
    RequestLogger.EnsureTablesExist();
    return;
}

RequestLogger.EnsureTablesExist();

services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/account/login";
        o.LogoutPath = "/account/logout";
        o.SlidingExpiration = true;
        o.ExpireTimeSpan = new TimeSpan(30, 0, 0, 0);
        o.Cookie.Name = "ww_wl";
    });

// disable user state authentication - just use plain cookie Auth
UserStateWebSettings.Current.IsUserStateEnabled = false;


//UserStateWebSettings.Current = new UserStateWebSettings()
//{
//    IsUserStateEnabled = true,
//    PersistanceMode = UserStatePersistanceModes.IdentityClaims,
//    CookieEncryptionKey = "wljad3ad4ad9Qd4W3td#2pI0o@--",
//    CookieTimeoutDays = 5
//};

services.AddControllersWithViews()
    .AddNewtonsoftJson(opt =>
    {
        if (builder.Environment.IsDevelopment())
            opt.SerializerSettings.Formatting = Formatting.Indented;
    })
    .AddRazorRuntimeCompilation();


services.AddMarkdown(config =>
{
    config.MarkdownRenderExtensions.Add(new FontAwesomeRenderExtension());
    config.MarkdownRenderExtensions.Add(new PlantUmlMarkdownRenderExtension());
});

// ***  BUILD ***
var app = builder.Build();

wlApp.IsDevelopment = env.IsDevelopment();
wlApp.ServiceProvider = app.Services;

// pre-load model async
Task.Run(() =>
{
    // can't inject configuration here :-( So we use explict
    string connectionString = wlApp.Configuration.ConnectionString; // Configuration["Data:SqlServerConnectionString"];
    var context = WeblogContext.CreateContext(connectionString);
    context.Posts.Any(p => p.Id == "@!");
}).FireAndForget();

wlApp.Cache = app.Services.GetService<IMemoryCache>();


if (wlApp.Configuration.System.LiveReloadEnabled)
    app.UseLiveReload();

if (config.System.ErrorDisplayMode == ErrorDisplayModes.Developer)
{
    app.UseDeveloperExceptionPage();
    ApiExceptionFilterAttribute.ShowExceptionDetail = true;
}
else
{
    app.UseExceptionHandler("/Home/Error");
    ApiExceptionFilterAttribute.ShowExceptionDetail = config.System.ErrorDisplayMode != ErrorDisplayModes.Application;
}


//app.UseStatusCodePages(new StatusCodePagesOptions
//{
//    HandleAsync = (ctx) =>
//    {
//        if (ctx.HttpContext.Response.StatusCode == 404)
//        {
//            ctx.HttpContext.Response.Redirect("/home/missingpage?url=" + ctx.HttpContext.Request.GetUrl());
//            return Task.FromResult(0);
//            //// throw an exception so it shows as an error page
//            ////  404 has special handling in `/home/error`
//            ////throw new HttpRequestException("Page not  found: " + ctx.HttpContext.Request.Path, null, statusCode: System.Net.HttpStatusCode.NotFound);
//            //var ctxAccessor = ctx.HttpContext.RequestServices.GetService<IHttpContextAccessor>();
//            //var factory = ctx.HttpContext.RequestServices.GetService<BusinessFactory>();
//            //var logger = ctx.HttpContext.RequestServices.GetService<ILogger<HomeController>>();


//            //var controller = new HomeController(logger,factory, ctxAccessor );
//            //var result = controller.Error(new HttpRequestException(
//            //    "Page not  found: " + ctx.HttpContext.Request.Path,
//            //    null,
//            //    statusCode: System.Net.HttpStatusCode.NotFound));
//        }
//        else if (ctx.HttpContext.Response.StatusCode == 401)
//        {
//            throw new HttpRequestException("Unauthorized: " + ctx.HttpContext.Request.Path, null, statusCode: System.Net.HttpStatusCode.Unauthorized);
//        }

//        return Task.FromResult(0);
//    }
//});


app.UseAuthentication();

app.UseRouting();

app.UseAuthorization();

app.UseStatusCodePages();


app.UseStaticFiles();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");    
//app.MapRazorPages();

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


void CreateDb()
{
    try
    {
        Console.WriteLine("Creating Db: " + wlApp.Configuration.ConnectionString + " user: " + Environment.UserName);

        var wlContext = WeblogContext.CreateContext(wlApp.Configuration.ConnectionString);

        try
        {
            if (wlContext.Posts.Any())
            {
                Console.WriteLine("Database already has data - skipping creation");
                return;
            }
            
        }
        catch { }

        if(WeblogDataImporter.EnsureWeblogData(wlContext, wlApp.Configuration.OldWeblogConnectionString))
        {
            Console.WriteLine("Database created and data imported successfully.");
        }
        else
        {
            Console.WriteLine("Database import failed.");
        }        
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error: " + ex.Message + "\n" + ex.GetBaseException().Message);
    }

    return;
}