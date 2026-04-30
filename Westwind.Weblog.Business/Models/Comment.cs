using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Westwind.Weblog.Business.Configuration;

//using Microsoft.EntityFrameworkCore;

namespace Westwind.Weblog.Business.Models
{
    public class Comment
    {
        public string Id { get; set; } = wlApp.NewId();

        public string PostId { get; set; }

        [MaxLength(128)]
        public string Title { get; set; }

        public string Body { get; set; }

        public DateTime Created { get; set; } = DateTime.UtcNow;

        public DateTime Updated { get; set; } = DateTime.UtcNow;

        [MaxLength(128)]
        public string Author { get; set; }

        [MaxLength(256)]
        public string Email { get; set; }

        public int BodyMode { get; set; }

        public string Url { get; set; }

        public virtual Post Post { get; set; }
        
        public bool IsActive { get; set; }

        public Comment()
        {
            
        }

        public override string ToString()
        {
            return "Comment: " + Title;
        }
    }
}