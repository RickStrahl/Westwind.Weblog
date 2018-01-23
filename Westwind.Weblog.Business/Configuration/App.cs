using System;
using System.Collections.Generic;
using System.Text;

namespace Westwind.Weblog.Business.Configuration
{

    public class App
    {
        public static WeblogConfiguration Configuration { get; set; }

        public static string WeblogConnectionString { get; set; }
    }
}
