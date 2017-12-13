using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.EntityFrameworkCore;
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
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddOptions();
            
            var config = new WeblogConfiguration();
            Configuration.Bind("Weblog", config);
            services.AddSingleton(config);
                        
            services.AddScoped<PostRepository>();
            services.AddScoped<AdminRepository>();

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

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
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
