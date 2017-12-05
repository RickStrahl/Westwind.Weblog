using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

//using Microsoft.EntityFrameworkCore;

namespace Westwind.Weblog.Business.Models
{
    public class Comment
    {
        public int Id { get; set; }

        public int PostId { get; set; }

        [MaxLength(128)]
        public string Title { get; set; }

        public string Body { get; set; }

        public DateTime Entered { get; set; }

        public DateTime Updated { get; set; }

        [MaxLength(128)]
        public string Author { get; set; }

        [MaxLength(256)]
        public string Email { get; set; }

        public int BodyMode { get; set; }
        
        
        

        public Comment()
        {

        }
    }
}