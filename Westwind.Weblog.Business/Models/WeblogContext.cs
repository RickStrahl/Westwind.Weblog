using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Westwind.Weblog.Business.Configuration;

namespace Westwind.Weblog.Business.Models
{
    public class WeblogContext : DbContext
    {
        
        public string ConnectionString
        {
            get
            {
                if (_connectionString == null)
                {
                    var conn = Database.GetDbConnection();
                    _connectionString = conn?.ConnectionString;
                    conn = null;
                }
                return _connectionString;
            }
            set { _connectionString = value; }
        }
        private string _connectionString = "server=.;database=WeblogCore;integrated security=true;encrypt=false";


        public WeblogContext() : base(CreateDbContextOptions())
        {

        }
        public WeblogContext(DbContextOptions options) : base(options)
        {
        }



        public static DbContextOptions CreateDbContextOptions(DbContextOptionsBuilder builder = null,
            string connectionString = null, ILoggerFactory loggerFactory = null)
        {
            if (string.IsNullOrEmpty(connectionString))
                connectionString =  wlApp.Configuration.ConnectionString;

            if (string.IsNullOrEmpty(connectionString))
                connectionString = wlApp.Constants.DefaultConnectionString;

            if (builder == null)
                builder = new DbContextOptionsBuilder();

            builder
                .UseLazyLoadingProxies()
                .UseSqlServer(connectionString,
                    opt =>
                    {
                        opt.EnableRetryOnFailure()
                            .CommandTimeout(15)
                            .MigrationsAssembly("Westwind.Weblog.Business");
                    });
            if (wlApp.Configuration.System.ShowConsoleDbCommands)
                builder.LogTo(Console.WriteLine, new[] { RelationalEventId.CommandExecuted })
                    .EnableSensitiveDataLogging();

            if (loggerFactory != null)
                builder.UseLoggerFactory(loggerFactory);

            return builder.Options;
        }

        /// <summary>
        /// Allows creating a new WebStore Context outside of DI for a few
        /// edge case scenarios (like app startup) where DI may not be
        /// available.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static WeblogContext CreateContext(string connectionString = null)
        {
            var builder = new DbContextOptionsBuilder<WeblogContext>();
            builder.UseSqlServer(connectionString ?? wlApp.Configuration.ConnectionString);

            var options = CreateDbContextOptions(builder);
            var context = new WeblogContext(options);
            
            return context;
        }


        public DbSet<Post> Posts { get; set; }

        public DbSet<Comment> Comments { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<Weblog> Weblogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Post>()
                .HasIndex(b => b.Created);
        }

        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    base.OnConfiguring(optionsBuilder);
        //    if (optionsBuilder.IsConfigured)
        //        return;

        //    // Auto configuration
        //    ConnectionString = Configuration.GetValue<string>("Data:Weblog:ConnectionString");
        //    optionsBuilder.UseSqlServer(ConnectionString);
        //}

    }
}