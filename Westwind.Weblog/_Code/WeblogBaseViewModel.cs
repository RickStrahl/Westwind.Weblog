using Westwind.AspNetCore;
using Westwind.Weblog.Business.Configuration;

namespace Westwind.Weblog
{
    public class WeblogBaseViewModel : BaseViewModel
    {
        public WeblogConfiguration Configuration { get; set; } = wlApp.Configuration;

        
    }
}
