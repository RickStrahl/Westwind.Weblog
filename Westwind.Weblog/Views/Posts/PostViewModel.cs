using System.Collections.Generic;
using Westwind.Weblog.Business;
using Westwind.Weblog.Business.Models;

namespace Westwind.Weblog
{
    public class PostViewModel
    {
        public List<Post> Posts { get; set; }

        public Post Post { get; set; }

        public List<Comment> Comments { get; set; }

        public PostBusiness PostRepo { get; set; }

    }
}