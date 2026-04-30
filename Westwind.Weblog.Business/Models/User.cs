using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Westwind.Data.EfCore;
using Westwind.Weblog.Business.Configuration;

namespace Westwind.Weblog.Business.Models
{
    public class User
    {
        public string Id { get; set; } = wlApp.NewId();

        [Required]
        public string Username { get; set; }

        [JsonIgnore]
        [Required]
        public string Password
        {
            get { return _password; }
            set
            {
                if (!string.IsNullOrEmpty(Id))
                    _password = UserBusiness.HashPassword(value, Id.ToString());
                else
                    _password = value;
            }
        }

        [XmlIgnore]
        private string _password;

        [Required]
        public string Fullname { get; set; }


        public bool IsAdmin { get; set; }

        public bool IsActive { get; set; }
    }
}