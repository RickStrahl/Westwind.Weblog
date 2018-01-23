using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Westwind.Weblog.Business;
using Westwind.Weblog.Business.Configuration;
using Westwind.Weblog.Business.Models;

namespace Westwind.Weblog
{
    public class Startup
    {
        public static Startup Current;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            Current = this;
        }

        public IConfiguration Configuration { get; }
        public IMemoryCache Cache { get; private set; }
        public IServiceProvider ServiceProvider { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMemoryCache();

            var config = new WeblogConfiguration();
            Configuration.Bind("Weblog", config);
            services.AddSingleton(config);
                        
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

            // pre-load model async
            Task.Run(() =>
            {
                var context = WeblogContext.GetWeblogContext(config.ConnectionString);
                context.Posts.Any(p => p.Id == -1);
            });

            // set up and configure Authentication - make sure to call .UseAuthentication()
            services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(o =>
                {
                    o.LoginPath = "/account/login";
                    o.LogoutPath = "/account/logout";
                });

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IMemoryCache cache, IServiceProvider serviceProvider)
        {
            Cache = cache;
            ServiceProvider = serviceProvider;            

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseDatabaseErrorPage();
            app.UseStatusCodePages();
            app.UseStaticFiles();

            app.UseMvcWithDefaultRoute();
        }
    }
}
