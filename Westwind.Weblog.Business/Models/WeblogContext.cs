using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.Extensions.Configuration;

namespace Westwind.Weblog.Business.Models
{
    public class WeblogContext : DbContext
    {
        public string ConnectionString { get; set; }
     
        public WeblogContext(DbContextOptions options) : base(options)
        {
        }

        public static WeblogContext GetWeblogContext(string connectionString)
        {
            var options = new DbContextOptionsBuilder<WeblogContext>()
                    .UseSqlServer(connectionString)
                    .Options;

            return new WeblogContext(options);                                                    
        }

        public DbSet<Post> Posts { get; set; }

        public DbSet<Comment> Comments { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<Weblog> Weblogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Post>()
                .HasIndex(b => b.Entered);
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