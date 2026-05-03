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
        /// <summary>
        /// The Id of the post
        /// </summary>
        public string PostId { get; set; }


        /// <summary>
        /// Title for the post
        /// </summary>
        public string Title { get; set; }


        /// <summary>
        /// The short text abstract for the post
        /// </summary>
        public string Abstract { get; set; }

        /// <summary>
        /// Optional - and not included unless explicitly asked for
        /// to reduce amount of data returned.
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// Date the post was created
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Optional location where the post was created
        /// if set
        /// </summary>
        public string Location { get; set; }


        /// <summary>
        /// The fully qualified Url to the post
        /// </summary>
        public string Url { get; set; }


        /// <summary>
        /// The Featured Image Url
        /// </summary>
        public string FeaturedImageUrl { get; set; }


        /// <summary>
        /// A small image Url
        /// </summary>
        public string ThumbnailUrl { get; set; }


        /// <summary>
        /// Number of comments for this post. This is optional and not included unless explicitly asked for
        /// in the filter
        /// </summary>
        public int CommentCount { get; set; }
        
    }


    /// <summary>
    /// A filter used to retrieve a Post list
    /// </summary>
    public class PostListFilter
    {
        public string BlogId { get; set; }

        public int NumberOfPosts { get; set; } = 20;

        public bool IncludeBody { get; set; } = false;

        public string Body { get; set; }

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
