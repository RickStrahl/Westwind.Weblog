//using Microsoft.EntityFrameworkCore;
//using System;

//namespace Westwind.Weblog.Business.Models
//{
//    public class WeblogContext : DbContext
//    {        
//        public string ConnectionString { get; set; }

//        public WeblogContext(DbContextOptions options) : base(options)
//        {         
//        }

//        public DbSet<Post> Posts { get; set; }
//        public DbSet<Comment> Comments { get; set; }

//        public DbSet<User> Users { get; set; }
        
//        protected override void OnModelCreating(ModelBuilder builder)
//        {         
//            base.OnModelCreating(builder);
//        }
        
//        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//        //{
//        //    base.OnConfiguring(optionsBuilder);

//        //    if (optionsBuilder.IsConfigured)
//        //        return;

//        //    // Auto configuration
//        //    ConnectionString = Configuration.GetValue<string>("Data:AlbumViewer:ConnectionString");
//        //    optionsBuilder.UseSqlServer(ConnectionString);
//        //}

//    }
//}