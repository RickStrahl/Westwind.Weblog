using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Westwind.Weblog.Business.Configuration;

//using Microsoft.EntityFrameworkCore;

namespace Westwind.Weblog.Business.Models
{
    public class Post
    {
        [MaxLength(50)]
        public string Id { get; set; } = wlApp.NewId();

        [MaxLength(50)]
        public string BlogId { get; set; } 
                
        [MaxLength(150)]
        public string Title { get; set; }

        public string Body { get; set; }

        [MaxLength(2048)]
        public string Abstract { get; set; }

        public DateTime Created { get; set; }

        public DateTime Updated { get; set; }

        public bool Active { get; set; }

        [MaxLength(128)]
        public string Author { get; set; }


        /// <summary>
        /// 0 - Html,  2 - Markdown
        /// </summary>
        public int BodyMode { get; set; }


        [DefaultValue(0)]
        public int CommentCount { get; set; }

        public bool CommentsClosed { get; set; }

        [MaxLength(256)]
        public string Categories { get; set; }

        [MaxLength(256)]
        public string Keywords { get; set; }

        [MaxLength(128)]
        public string Location { get; set; }

        [MaxLength(256)]
        public string RedirectUrl { get; set; }
        

        [MaxLength(150)]
        public string SafeTitle { get; set; }
        
        public bool IsFeatured { get; set; }
        
       
        public string Markdown { get; set; }

        /// <summary>
        /// The permanent Url assigned when the post is created.
        /// </summary>
        [MaxLength(256)]
        public string PermanentUrl { get; set; }

        [MaxLength(320)]
        public string FeaturedImageUrl { get; set; }
        

        public virtual List<Comment> Comments { get; set; }

        [MaxLength(256)]
        public string GithubUrl { get; set; }

        public bool IsArticle { get; set;  }


        public Post()
        {
            Comments = new List<Comment>();
        }

        public override string ToString()
        {
            return "Post: " + Title;
        }

        public string GetPostUrl(bool fullyQualified = false)
        {
            DateTime date = Created;
            string url = $"{wlApp.Configuration.ApplicationBasePath}posts/{date.Year}/{date:MMM}/{date:dd}/{SafeTitle}";

            if (!fullyQualified)
                return url;

            return wlApp.Configuration.WeblogHomeUrl + url;            
        }
    }
}