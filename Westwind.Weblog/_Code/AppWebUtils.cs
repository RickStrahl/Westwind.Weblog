using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Westwind.AspNetCore.Services;
using Westwind.Weblog.Business;
using Westwind.Weblog.Business.Configuration;
using Westwind.Weblog.Business.Models;

namespace Westwind.Weblog
{
    public class AppWebUtils
    {

        /// <summary>
        /// Formats a date in friendly format
        /// </summary>
        /// <param name="date"></param>
        /// <param name="showTime"></param>
        /// <returns></returns>
        public static string WebLogDateString(DateTime date, bool showTime)
        {
            string FormattedDate = "";
            if (date.Date == DateTime.Today)
                FormattedDate = "Today"; //Resources.Resources.Today; 
            else if (date.Date == DateTime.Today.AddDays(-1))
                FormattedDate = "Yesterday"; //Resources.Resources.Yesterday;
            else if (date.Date > DateTime.Today.AddDays(-6))
                // *** Show the Day of the week
                FormattedDate = date.ToString("dddd");
            else
                FormattedDate = date.ToString("MMMM dd, yyyy");

            if (showTime)
                FormattedDate += " @ " + date.ToString("h:mmtt").ToLower();

            return FormattedDate;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url"></param>
        /// <param name="text"></param>
        /// <param name="newWindow"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public static string WebLogShortenedUrl(string url, string text, bool newWindow, int maxLength)
        {
            if (text.Length > maxLength)
            {
                text = text.Substring(0, maxLength / 2 - 3) + "..." + text.Substring(text.Length - maxLength / 2);
            }

            string Output = "<a href=\"" + url + "\"";

            if (newWindow)
                Output += " target=\"_top\"";

            Output += ">" + text + "</a>";

            return Output;
        }

        /// <summary>
        /// Displays the Category links and KickIt Urls
        /// </summary>
        /// <param name="Entry"></param>
        /// <returns></returns>
        public static HtmlString CategoryLinks(PostBusiness postRepo)
        {
            StringBuilder sb = new StringBuilder("");
            string Url = HttpUtility.UrlEncode(postRepo.GetPostUrl(postRepo.Entity));

            string[] Categories = postRepo.Entity.Categories.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (Categories.Length > 0)
            {
                sb.Append("<div>Posted in <b>");
                foreach (string Category in Categories)
                {
                    sb.Append("<a href='" + postRepo.WeblogConfiguration.ApplicationBasePath +
                        "ShowPosts.aspx?Category=" + Category + "'>" + 
                        Category + "</a>&nbsp;&nbsp;");
                }
                sb.Append("</b></div>\r\n");
            }
            
            sb.Append("</small>");
            return new HtmlString(sb.ToString());
        }


        /// <summary>
        /// Generate a gravatar link
        /// </summary>
        /// <param name="Email">Email address</param>
        /// <param name="size">Image size</param>
        /// <returns></returns>
        public static HtmlString GravatarLink(Comment comment, int size = 100)
        {
            string Email = comment.Email as string;
            if (string.IsNullOrEmpty(Email))
                Email = "";// return "";
            
            return new HtmlString(Gravatar.GetGravatarImage(Email, size, "R", "style='border-radius: 4px;box-shadow: 2px 2px 4px #5353535; '", null));
        }

        /// <summary>
        /// Load the Blog and Comment count. Store in cache so we don't have to 
        /// redo this code every time.
        /// </summary>
        /// <returns></returns>
        public static HtmlString GetBlogStats()
        {
            var cache = Startup.Current.Cache;
            string blogStats = cache.Get<string>("BlogStats");          
            if (blogStats != null)
                return new HtmlString(blogStats);

            var config = WeblogConfiguration.Current;
            var context = WeblogContext.GetWeblogContext(config.ConnectionString);            
            var postRepo = new PostBusiness(context,config);
            var counts = postRepo.GetPostStats();

            StringBuilder sb = new StringBuilder();
            int CommentCount = counts.commentCount;
            int PostCount = counts.postCount;
            
            sb.Append($"<div><a href='{config.ApplicationBasePath}posts' >Posts - {PostCount:n0}</a></div>\r\n");
            sb.Append($"<div><a href='{config.ApplicationBasePath}comments' >Comments - {CommentCount:n0}</a></div>\r\n");
            string stats = sb.ToString();

            cache.Set("BlogStats", stats, new TimeSpan(0, 15, 0));

            return new HtmlString(stats);
        }


    }
}
