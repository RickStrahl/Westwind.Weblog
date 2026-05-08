using System;
using System.Collections.Generic;
using System.Text;
using Westwind.Utilities;
using Westwind.Utilities.Configuration;

namespace Westwind.Weblog.Business.Configuration
{
    public class WeblogConfiguration : Westwind.Utilities.Configuration.AppConfiguration
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

        public string ApplicationByLineHtml { get; set; } =
            """
            Wind, waves, code and everything in between...<br />
            .NET • C# • Markdown • WPF • All things Web
            """;

        /// <summary>
        /// Sql Server ConnectionString for this application
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// REMOVE after migration
        /// </summary>
        public string OldWeblogConnectionString => "server=.;database=Weblog;integrated security=true;encrypt=true";

        /// <summary>
        /// The server relative root path for this application
        /// </summary>
        public string ApplicationBasePath
        {
            get
            {
                if (string.IsNullOrEmpty(field))
                    return "/";


                return StringUtils.TerminateString(field, "/");
            }
            set;
        }

        /// <summary>
        /// The page size of an individual post
        /// </summary>
        public int PostPageSize { get; set; } = 7500;

        /// <summary>
        /// Number of post abstracts that show on the home page
        /// </summary>
        public int HomePagePostCount { get; set; } = 30;

        /// <summary>
        /// Number of Hero Images used by the hero banner in /images/HeroImages
        /// </summary>
        public int HeroImageCount { get; set; } = 16;

        public string PayPalEmail { get; set; }

        public string WeblogAuthor { get; set; } = "Rick Strahl";

        public string WeblogHomeUrl { get; set; } = "https://weblog.west-wind.com";
        public string WeblogImageUrl { get; set; } = "http://www.west-wind.com/images/WebLogBannerLogo.jpg";

        public EmailConfiguration Email { get; set; } = new EmailConfiguration();

        public SystemConfiguration System { get; set; } = new SystemConfiguration();



        protected override IConfigurationProvider OnCreateDefaultProvider(string sectionName, object configData)
        {
            var provider = new JsonFileConfigurationProvider<WeblogConfiguration>()
            {
                JsonConfigurationFile = "_weblog-configuration.json"
            };
            return provider;
        }
    }

    public class EmailConfiguration
    {
        public string MailServer { get; set; }
        public string MailServerUsername { get; set; }

        public string MailServerPassword { get; set; }

        public bool MailServerUseSsl { get; set; }

        public bool SendAdminEmails { get; set; }

        public string SenderName { get; set; }
        public string SenderEmail { get; set; }
        public string AdminSenderEmail { get; set; }
        
    }

    public class SystemConfiguration
    { 
        public bool LiveReloadEnabled { get; set; } 

        public bool ShowConsoleDbCommands { get; set; }
        public ErrorDisplayModes ErrorDisplayMode { get; set; }
    }

    public enum ErrorDisplayModes
    {
        Application,
        ApplicationPlusDetail,
        Developer
    }

}
