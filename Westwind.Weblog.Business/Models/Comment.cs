using System;
using System.ComponentModel.DataAnnotations;

namespace Westwind.Weblog.Business.Models
{
    public class Comment
    {
        public int Id { get; set; }

        [MaxLength(128)]
        public string Title { get; set; }

        public string Body { get; set; }

        public DateTime Entered { get; set; }

        public DateTime Updated { get; set; }

        [MaxLength(128)]
        public string Author { get; set; }

        [MaxLength(256)]
        public string Email { get; set; }   
    }
}