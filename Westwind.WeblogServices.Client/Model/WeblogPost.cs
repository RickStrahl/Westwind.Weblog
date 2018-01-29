using System;
using System.Collections.Generic;

namespace Westwind.WeblogPostService.Model
{
    public class WeblogPost
    {
        public WeblogPost()
        {
            DateCreated = DateTime.Now;
            
            CustomFields = new Dictionary<string, string>();
            Categories = new List<string>();            
            PostStatus = PostStatuses.Published;            
        }

        /// <summary>
        /// A unique Id that identifies the post
        /// </summary>
        public string PostId { get; set; }


        /// <summary>
        /// Optional ID for the Blog that receives this post
        /// </summary>
        public string BlogId { get; set; }
        
        /// <summary>
        /// Date of the post
        /// </summary>
        public DateTime DateCreated { get; set; }

        /// <summary>
        /// Actual content of the post - usually HTML that is
        /// used to display the post online.
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// A short paragraph that describes the contents of the post
        /// </summary>
        public string Abstract { get; set; }

        /// <summary>
        /// Optional raw post text that holds the text from
        /// which the post is generated. Typically holds
        /// Markdown or other original text prior to 
        /// HTML generation.
        /// </summary>
        public string RawPostText { get; set; }


        /// <summary>
        /// The headline title of the post.
        /// </summary>
        public string Title { get; set; }

        private string _permalink;

        /// <summary>
        /// Url of the post 
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Permanent link to the post. Usually the same as the URL, but
        /// an option if 
        /// </summary>
        public string PermaLink
        {
            get
            {
                if (_permalink == null)
                    return Url;
                return _permalink;
            }
            set { _permalink = value; }
        }

        /// <summary>
        /// Comma delimited list of keywords.
        /// </summary>
        public string Keywords { get; set; }


        /// <summary>
        /// Post categories for this post
        /// </summary>
        public List<string> Categories { get; set; }

        
        /// <summary>
        /// Any custom fields using a string key and value
        /// </summary>
        public Dictionary<string,string> CustomFields { get; set; }


        /// <summary>
        /// Optional type you can attach to a post. Example: Blog, Article, Advert etc.
        /// </summary>
        public string PostType { get; set; } = "Blog";


        /// <summary>
        /// Status of this post whether Published or Draft.
        /// </summary>
        public PostStatuses PostStatus { get; set; }


        /// <summary>
        /// Optional image URL to an image that is associated with this post.        
        /// </summary>
        public string PostImageUrl { get; set; }

        /// <summary>
        /// Optional a smaller thumbnail URL associated with this post.
        /// </summary>
        public string ImageUrl { get; set; }


        /// <summary>
        /// Attach an Author to the post
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Attach a location to the p
        /// </summary>
        public string Location { get; set; }
        


        /// <summary>
        /// Returns the Title of the topic
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Title;
        }

        /// <summary>
        /// Gets the custom field for a given key and returns null
        /// if is not available.
        /// </summary>
        /// <param name="key">key to retrieve value for</param>
        /// <returns>string value or null</returns>
        public string GetCustomField(string key)
        {
            if (!CustomFields.TryGetValue(key, out string value))
                return null;
            return value;
        }        
    }



}