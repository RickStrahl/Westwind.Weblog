using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace BlazePostApi.Client
{

    /// <summary>
    /// An individual Weblog post that is posted or 
    /// retrieved from the target site
    /// </summary>
    public class WeblogPost
    {
        public WeblogPost()
        {
            DateCreated = DateTime.Now;
            
            CustomFields = new Dictionary<string, string>();            
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
        /// Actual content of the post - usually ready to render HTML 
        /// that is used to display the post online but it could also
        /// be Markdown that is then formatted into HTMl by the app.
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
        /// Optional type of the raw post text. This is typically
        /// markdown or html or plain text.
        /// </summary>
        public string RawPostType { get; set; }


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
        public List<string> Keywords { get; set; } = [];


        /// <summary>
        /// Post categories for this post as a comma delimited list
        /// </summary>
        public List<string> Categories { get; set; } = [];

        

        /// <summary>
        /// Optional type you can attach to a post. Example: Blog, Article, Advert etc.
        /// </summary>
        public string PostType { get; set; } = "Blog";


        /// <summary>
        /// Status of this post whether Published or Draft.
        /// </summary>
        public PostStatuses PostStatus { get; set; }

        
        /// <summary>
        /// Image associated with this post
        /// </summary>
        public string ImageUrl { get; set; }

        /// <summary>
        /// Optional a smaller thumbnail URL associated with this post.
        /// </summary>
        public string FeaturedImageUrl { get; set; }


        /// <summary>
        /// Attach an Author to the post
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Optional location where the post was created
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// If the post data is stored on GitHub or some
        /// other online location you can specify the URL
        /// here.
        /// </summary>
        public string SourceEditUrl { get; set; }


        /// <summary>
        /// Any custom fields using a string key and value
        /// </summary>
        public Dictionary<string, string> CustomFields { get; set; } = new Dictionary<string, string>();


        /// <summary>
        /// Optionally allows attaching media objects directly to the post.
        /// Alternately you can post it separately and then fix up the document
        /// after the fact.
        /// </summary>
        public List<WeblogMediaObject> MediaObjects { get; set; } = new List<WeblogMediaObject>();

        /// <summary>
        /// Comments for post when returning a single post
        /// </summary>
        public List<WeblogComment> Comments { get; set; } = [];


        public int CommentCount
        {
            get
            {
                if (Comments.Count > 0) return Comments.Count;
                return field;
            }
            set => field = value;
        }

        /// <summary>
        /// The slug URL for the title part of the URL. Doesn't include the date
        /// which might be adjusted.
        /// </summary>
        public string SafeTitle { get; set; }


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

    public class WeblogComment
    {
        public int BodyMode { get; set; }
        public bool IsActive { get; set; }
        public string Url { get; set; }
        public string Email { get; set; }
        public DateTime Created { get; set; }
        public string Body { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string PostId { get; set; }
        public string Id { get; set; }
    }


    public enum PostStatuses
    {
        Published,
        Draft,
        Pending,
        Future,
    }

    public class WeblogMediaObject
    {

        /// <summary>
        /// Optional Blog Id. 
        /// 
        /// If passed the BlogId can be an additional identifier
        /// for uniqueness of the media object.
        /// </summary>
        public string BlogId { get; set; }


        /// <summary>
        /// Optional PostId to which this media object is attached.
        /// 
        /// If passed the PostId can be an additional identifier
        /// for uniqueness of the media object.
        /// </summary>
        public string PostId { get; set; }

        /// <summary>
        /// The name of the Media Object. Can optionally include a path
        /// prefix that can be used to construct a save path on disk or
        /// prefix for data stored in a database.
        /// 
        /// Examples:
        /// SomeImage.png
        /// Weblog-Post-1234/SomeImage.png
        /// 
        /// I like to store posts on disk and use the folder prefix
        /// in a year subfolder to store media objects. If stored in
        /// this fashion they can be overridden.        
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The type of the Media Object.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// The byte array of the Media Object itself.
        /// 
        /// </summary>
        public byte[] Data { get; set; }


        public bool LoadDataFromFile(string file)
        {
            // implement 
            try
            {
                Data = System.IO.File.ReadAllBytes(file);
            }
            catch
            {
                return false;
            }
            return true;
        }


        

    }

    public class WeblogInfo
    {
        public string BlogId { get; set; }
        public string Url { get; set; }

        public string Title { get; set; }        
    }
}