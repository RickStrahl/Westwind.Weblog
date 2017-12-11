using System;
using System.Collections.Generic;
using System.Text;
using Westwind.Utilities.Configuration;

namespace Westwind.Weblog.Business.Configuration
{
    public class WeblogConfiguration
    {
        public static WeblogConfiguration Current;

        public WeblogConfiguration()
        {
            Current = this;
        }

        /// <summary>
        /// Display name for this application/blog
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// The server relative root path for this application
        /// </summary>
        public string ApplicationBasePath { get; set; } = "/";

        /// <summary>
        /// The page size of an individual post
        /// </summary>
        public int PostPageSize { get; set; } = 7500;

        /// <summary>
        /// Number of post abstracts that show on the home page
        /// </summary>
        public int HomePagePostCount { get; set; } = 30;
    }
}
