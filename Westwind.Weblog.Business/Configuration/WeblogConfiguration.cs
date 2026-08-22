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
        /// Cookie name used for authentication.
        /// </summary>
        public string CookieName { get; set; } = "ww_wl";

        /// <summary>
        /// Sql Server ConnectionString for this application
        /// </summary>
        public string ConnectionString { get; set; }

        
        /// <summary>
        /// REMOVE after migration
        /// </summary>
        public string OldWeblogConnectionString { get; set; } = "server=.;database=Weblog;integrated security=true;encrypt=false";

        /// <summary>
        /// The server relative root path for this application.
        /// 
        /// This should include a fully qualified root for the
        /// application, including a virtual folder if specified.
        /// 
        /// Examples:
        /// * https://weblog.west-wind.com/
        /// * https://weblog.west-wind.com/blog/
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
        /// If the Application Path includes a virtual path
        /// segment (ie. `/blog/` or `/weblog/`) specify here
        ///
        /// Stored without leading and trailing slashes.         
        /// </summary>
        public string VirtualPath {
            get
            {
                if (string.IsNullOrEmpty(field))
                    return field;

                return field.Trim();
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

        /// <summary>
        /// This is the image Url used for the RSS feed
        /// </summary>
        public string WeblogRssImageUrl { get; set; } = "https://www.west-wind.com/images/WestwindSiteLogo.jpg";

        public bool DisableComments { get; set;  }

        public EmailConfiguration Email { get; set; } = new EmailConfiguration();

        public SystemConfiguration System { get; set; } = new SystemConfiguration();
        
        /// <summary>
        /// Optional full or partial name  part that is used to auto approve comments.
        /// Leave empty for no auto-validation. Matches the email address entered.
        /// 
        /// Example: Rick Strahl
        /// </summary>
        public string CommentAutoApproveNamePart { get; set; }  // = "Henry Rollins";
        


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

        public bool UseTls { get; set; }

        public string SenderName { get; set; }
        public string SenderEmail { get; set; }

        public bool SendEmails { get; set; }
        public bool SendAdminEmails { get; set; }
    }

    public class SystemConfiguration
    { 
        public bool LiveReloadEnabled { get; set; } 
        public bool UseRateLimiting { get; set; }
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
