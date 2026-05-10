using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Westwind.AspNetCore;
using Westwind.AspNetCore.Security;
using Westwind.Weblog.Business.Configuration;
using Westwind.Weblog.Business.Models;

namespace Westwind.Weblog
{
    public class WeblogBaseViewModel : BaseViewModel
    {
        public WeblogConfiguration Configuration { get; set; } = wlApp.Configuration;

    }
}
