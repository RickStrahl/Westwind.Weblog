using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Westwind.AspNetCore;
using Westwind.Utilities;
using Westwind.Weblog;

namespace Westwind.Weblog
{
    public class AppUser : AppUserBase
    {
        public AppUser(ClaimsPrincipal user) : base(user)
        { }

        public string Email => GetClaim("Username");

        public string Fullname => GetClaim("Fullname");

        public string UserId => GetClaim("UserId");

        public bool IsAdmin => HasRole("Admin");
        
    }
}

namespace System.Security.Claims
{
    public static class ClaimsPrincipalExtensions
    {
        public static AppUser GetAppUser(this ClaimsPrincipal user)
        {
            return new AppUser(user);
        }
    }
}
