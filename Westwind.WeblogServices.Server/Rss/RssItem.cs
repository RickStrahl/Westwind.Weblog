using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Westwind.WeblogServices.Server
{
    [DebuggerDisplay("{Title} - {Link}")]
    public class RssItem
    {
        public RssItem()
        {
            Author = new RssAuthor();
        }

        public string Title { get; set; }

        public DateTime PublishDate { get; set; }

        public RssAuthor Author { get; set; }

        public string Body { get; set; }

        public string Link { get; set; }

        public string Permalink { get; set; }

        /// <summary>
        /// Unique identifier: Example: Post Pk + date and time
        /// </summary>
        public string Guid { get; set; }

        public IList<string> Categories { get; set; } = new List<string>();

        public string CommentsUrl { get; set; }

        public int CommentCount { get; set; }  
    }
}