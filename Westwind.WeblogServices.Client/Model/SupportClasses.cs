using System;
using System.Collections.Generic;
using System.Text;

namespace Westwind.WeblogPostService.Model
{

    /// <summary>
    /// Result from a Post Listing which includes only 
    /// a few fields from a post.
    /// </summary>
    public class WeblogMinimalPost
    {
        public string PostId { get; set; }

        public string Title { get; set; }

        public string Abstract { get; set; }
        public DateTime Created { get; set; }
        public string Url { get; set; }

        public string ImageUrl { get; set; }

        public string ThumbnailUrl { get; set; }

        public int CommentCount { get; set; }
    }


    /// <summary>
    /// A filter used to retrieve a Post list
    /// </summary>
    public class PostListFilter
    {
        public string BlogId { get; set; }

        public int NumberOfPosts { get; set; } = 25;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }


    public class GetPostRequest
    {
        public string BlogId { get; set; }
        public string PostId { get; set; }
    }


    /// <summary>
    /// Input for the Authenticate method.
    /// </summary>
    public class AuthenticateRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }

        public string BlogId { get; set; }
    }
}
