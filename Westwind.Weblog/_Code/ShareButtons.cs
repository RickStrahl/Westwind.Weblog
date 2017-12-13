﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Westwind.Weblog.Business.Configuration;

namespace Westwind.Weblog
{
    /// <summary>
    /// Class that holds a few static string definitions for
    /// embedding Share on Google+ and Twitter
    /// </summary>
    public static class ShareButtons
    {
        /// <summary>
        /// Places a Google+  +1 and Share button in the page
        /// </summary>
        /// <param name="url">The Url to share. If not provided the current page is used</param>
        /// <param name="buttonSize">small,medium,standard,tall</param>
        /// <param name="width"></param>
        /// <returns></returns>
        public static HtmlString GooglePlusPlusOneButton(string url = null,
                                                     string buttonSize = "medium",
                                                     int width = -1)
        {
            if (!string.IsNullOrEmpty(url))
                url = " href=\"" + url + "\"";
            else
                url = string.Empty;

            string linkWidth = string.Empty;
            if (width != -1)
                linkWidth = " width=\"" + width.ToString() + "\"";

            var link =
                @"
<g:plusone size=""" + buttonSize.ToString() + "\"" + url + " " + linkWidth + @"""></g:plusone>
<script type=""text/javascript"">
  (function() {
    var po = document.createElement('script'); po.type = 'text/javascript'; po.async = true;
    po.src = 'https://apis.google.com/js/plusone.js';
    var s = document.getElementsByTagName('script')[0]; s.parentNode.insertBefore(po, s);
  })();
</script>
";
            return new HtmlString(link);
        }


        /// <summary>
        /// Inserts a Tweet button to share tweet on Twitter with an image link
        /// </summary>
        /// <param name="text">The text to present</param>
        /// <param name="twitterShareAccount"></param>
        /// <returns></returns>
        public static HtmlString ShareOnTwitter(string text, string twitterShareAccount = null, string url = null, string hashTag = null)
        {
            string format =
@"<a href=""https://twitter.com/share"" class=""twitter-share-button"" data-text=""{0}"" data-via=""{1}"" data-lang=""en"" data-hashtags=""{3}"" data-url=""{2}"">Tweet</a>
<script>!function(d,s,id){{var js,fjs=d.getElementsByTagName(s)[0];if(!d.getElementById(id)){{js=d.createElement(s);js.id=id;js.src=""//platform.twitter.com/widgets.js"";fjs.parentNode.insertBefore(js,fjs);}}}}(document,""script"",""twitter-wjs"");</script>";

            return new HtmlString(string.Format(format, text, twitterShareAccount, url, hashTag));

        }

        public static HtmlString ShareOnFacebook(string url)
        {
            var baseUrl = WeblogConfiguration.Current.ApplicationBasePath;
            string link =
$@"<a href=""https://www.facebook.com/sharer/sharer.php?u={url}&display=popup"" target=""_blank"">
      <img src=""{baseUrl}images/shareonfacebook.png"" style='height: 20px;padding: 0;' />
</a>";

            return new HtmlString(link);
        }

        public static string ShareOnFacebookFull(string url)
        {
            return null;

        }
    }

    public enum GooglePlusOneButtonSizes
    {
        Small15,
        Medium20,
        Standard24,
        Tall60
    }
}
