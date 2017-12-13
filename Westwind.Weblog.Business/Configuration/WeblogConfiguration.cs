using System;
using System.Collections.Generic;
using System.Text;
using Westwind.Utilities.Configuration;

namespace Westwind.Weblog.Business.Configuration
{
    public class WeblogConfiguration
    {
        public static WeblogConfiguration Current { get; set; }

        public WeblogConfiguration()
        {
            Current = this;
        }

        /// <summary>
        /// Display name for this application/blog
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Sql Server ConnectionString for this application
        /// </summary>
        public string ConnectionString { get; set; }

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

        public string PayPalEmail { get; set; }

        public EmailConfiguration Email { get; set; } = new EmailConfiguration();
    }

    public class EmailConfiguration
    {
        public string MailServer { get; set; }
        public string MailServerUsername { get; set; }

        public string MailServerPassword { get; set; }

        public string SenderName { get; set; }
        public string SenderEmail { get; set; }
        public string AdminSenderEmail { get; set; }
    }
}
