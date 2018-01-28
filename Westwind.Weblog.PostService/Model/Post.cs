using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Westwind.Weblog.PostService.Model
{
    public class Post
    {
        public Post()
        {
            DateCreated = DateTime.Now;
            
            CustomFields = new Dictionary<string, string>();
            Categories = new List<string>();
            Tags = new List<string>();
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

        public List<string> Categories { get; set; }

        public List<string> Tags { get; set; }

        public Dictionary<string,string> CustomFields { get; set; }

        public string PostType { get; set; }

        public string PostStatus { get; set; }


        /// <summary>
        /// Optional image URL to an image that is associated with this post.        
        /// </summary>
        public string PostImageUrl { get; set; }

        /// <summary>
        /// Optional a smaller thumbnail URL associated with this post.
        /// </summary>
        public string ThumbnailUrl { get; set; }
    

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

    public class MediaObject
    {
        /// <summary>
        /// The name of the Media Object.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The type of the Media Object.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The byte array of the Media Object itself.
        /// 
        /// </summary>
        public byte[] Bits { get; set; }        
    }

    public enum PostStatus
    {
        published,
        draft,
        pending,
        future,
        

    }
}
