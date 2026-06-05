using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Westwind.Utilities;

namespace Westwind.Weblog.Business.Configuration
{

    public class wlApp
    {
        public static WeblogConfiguration Configuration { get; set; }

        public static WeblogConstants Constants { get; set; } = new WeblogConstants();

        /// <summary>
        /// Global Memory Cache
        /// </summary>
        public static IMemoryCache Cache { get; set; }

        public static IServiceProvider ServiceProvider { get; set; }

        public static string WebRootFolder { get; set; }

        public static string StartupFolder { get; set; }

        public static string EnvironmentName { get; set; }

        public static bool IsDevelopment { get; set; }

        public static DateTime AppStartedOn { get; set; }


        static wlApp()
        {
            wlApp.Configuration = new WeblogConfiguration();
            wlApp.Configuration.Initialize();
            
            WeblogConfiguration.Current = wlApp.Configuration;
        }

        

        /// <summary>
        /// Generates a new Unique ID for posts and blogs etc.
        /// </summary>
        /// <returns></returns>
        public static string NewId()
        {
            return DataUtils.GenerateUniqueId(12);
        }

        /// <summary>
        /// Generic application encryption
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string Encrypt(string text)
        {
            return Encryption.EncryptString(text, _enc, true);
        }

        /// <summary>
        /// Generic application decryption
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string Decrypt(string text)
        {
            return Encryption.DecryptString(text, _enc, true);
        }
        // Encryption key - should be 16 characters for AES encryption
        const string _enc = "4#2!5689qe@1!x23";
    }

    public class WeblogConstants
    {
        public string DefaultConnectionString { get; set; } = "server=.;database=WeblogCore; integrated security=true;MultipleActiveResultSets=true;encrypt=false";
        public DateTime AppStartedOn { get; set; }
    }
}
