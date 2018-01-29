using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Westwind.WeblogServices.Server.Rss
{
    [DebuggerDisplay("{Title} - {Link}")]
    public class RssFeed
    {
        public string Title { get; set; }

        public string Link { get; set; }

        public string ImageUrl { get; set; }
        
        public string Description { get; set; }
        
        public string Copyright { get; set; }

        public string Generator { get; set; }

        public string Language { get; set; } = "en-us";

        public IList<RssItem> Items { get; set; } = new List<RssItem>();


        public DateTime PubDate { get; set; } = DateTime.UtcNow;

        public DateTime LastUpdate { get; set; } = DateTime.UtcNow;


        /// <summary>
        /// Returns the XML feeds as a Linq XML Document object
        /// </summary>
        /// <returns></returns>
        public XDocument Serialize()
        {
            
            var doc = new XDocument(new XElement("rss"));
            doc.Root.Add(new XAttribute("version", "2.0"));
            
            var channel = new XElement("channel");
            channel.Add(new XElement("title", Title));
            channel.Add(new XElement("link", Link));


            if (!string.IsNullOrEmpty(ImageUrl))
            {
                var el = new XElement("image");
                el.Add(new XElement("url", "ImageUrl"));
                el.Add(new XElement("title", Title));
                el.Add(new XElement("link", Link));
                channel.Add(el);
            }
                            
            channel.Add(new XElement("description", Description));
            channel.Add(new XElement("copyright", Copyright));
            channel.Add(new XElement("pubDate", PubDate));
            if (LastUpdate > new DateTime(2000, 1, 1))
                channel.Add(new XElement("lastBuildDate"), LastUpdate.ToUniversalTime());
            
            if (!string.IsNullOrEmpty(Generator))
                channel.Add(new XElement("generator", Generator));

            doc.Root.Add(channel);

            foreach (var item in Items)
            {
                var itemElement = new XElement("item");
                itemElement.Add(new XElement("title", item.Title));
                itemElement.Add(new XElement("description", item.Body));
                itemElement.Add(new XElement("link", item.Link));

                if (!string.IsNullOrEmpty(item.Guid))
                {
                    var el = new XElement("guid", item.Guid);
                    el.Add(new XAttribute("isPermaLink", "false"));
                    itemElement.Add(el);
                }
                
                if (item.Author != null)
                    itemElement.Add(new XElement("author", $"{item.Author.Email} ({item.Author.Name})"));

                foreach (var c in item.Categories)
                {
                    itemElement.Add(new XElement("category", c));
                }

                if (item.CommentsUrl != null)
                    itemElement.Add(new XElement("comments", item.CommentsUrl));
                //if (item.CommentCount > 0)
                //    itemElement.Add(new XElement("slash:comments"), item.CommentCount);

                if (!string.IsNullOrWhiteSpace(item.Permalink))
                    itemElement.Add(new XElement("guid", item.Permalink));
                
                if (item.PublishDate > DateTime.MinValue)
                    itemElement.Add(new XElement("pubDate", item.PublishDate.ToString("r")));

                channel.Add(itemElement);
            }

            return doc;
        }

        /// <summary>
        /// Serializes the RSS Feed to a string
        /// </summary>
        /// <returns></returns>
        public string SerializeToString()
        {
            return Serialize().ToString();
        }
    }
}
