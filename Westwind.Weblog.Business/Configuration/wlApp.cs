using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Text;

namespace Westwind.Weblog.Business.Configuration
{

    public class wlApp
    {
        public static WeblogConfiguration Configuration { get; set; }

        public static WeblogConstants Constants { get; set; } = new WeblogConstants();


        static wlApp()
        {
            wlApp.Configuration = new WeblogConfiguration();
            wlApp.Configuration.Initialize();
            WeblogConfiguration.Current = wlApp.Configuration;
        }
    }

    public class WeblogConstants
    {
        public string DefaultConnectionString { get; set; } = "server=.;database=WeblogCore; integrated security=true;MultipleActiveResultSets=true;encrypt=false";

        public string WebRootFolder { get; set; } 
        public string StartupFolder { get; set;  }

        public bool IsDevelopment { get; set; }
    }
}
