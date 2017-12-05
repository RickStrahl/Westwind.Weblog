using System.ComponentModel.DataAnnotations;

namespace Westwind.Weblog.Business.Models
{
    public class Weblog
    {        
        public int Id { get; set; }

        [MaxLength(256)]
        public int WeblogName { get; set;  }

        [MaxLength(256)]
        public string Url { get; set; }
    }
}