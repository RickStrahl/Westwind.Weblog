using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Westwind.Data.EfCore;

namespace Westwind.Weblog.Business.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string Username { get; set; }

        [JsonIgnore]
        [Required]
        public string Password
        {
            get { return _password; }
            set => _password = UserBusiness.HashPassword(value, Id.ToString());
        }
        [XmlIgnore]
        private string _password;

        [Required]
        public string Fullname { get; set; }


        public bool IsAdmin { get; set; }

        public bool IsActive { get; set; }
    }
}