using System.ComponentModel.DataAnnotations;
using Westwind.Weblog.Business.Configuration;

namespace Westwind.Weblog.Business.Models
{
    public class Weblog
    {        
        public string Id { get; set; } = wlApp.NewId();

        [MaxLength(256)]
        public int WeblogName { get; set;  }

        [MaxLength(256)]
        public string Url { get; set; }
    }
}