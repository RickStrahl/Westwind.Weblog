using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

//using Microsoft.EntityFrameworkCore;

namespace Westwind.Weblog.Business.Models
{
    public class Post
    {        
        public int Id { get; set; }
        
        [MaxLength(128)]
        public string Title { get; set; }

        public string Body { get; set; }

        [MaxLength(2048)]
        public string Abstract { get; set; }

        public DateTime Entered { get; set; }

        public DateTime Updated { get; set; }

        public bool Active { get; set; }

        [MaxLength(256)]
        public string Url { get; set; }

        [MaxLength(256)]
        public string TitleUrl { get; set; }


        [MaxLength(128)]
        public string Author { get; set; }

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

        [MaxLength(256)]
        public string FeaturedImageUrl { get; set; }
        

        public List<Comment> Comments { get; set; }


        public Post()
        {
            Comments = new List<Comment>();
        }
    }
}