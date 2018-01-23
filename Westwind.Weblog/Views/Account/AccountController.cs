using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Westwind.AspNetCore.Errors;
using Westwind.Weblog.Business.Models;


namespace Westwind.Weblog.Views.Account
{
    public class AccountController : AppBaseController
    {
        private readonly UserBusiness _userBus;

        public AccountController(UserBusiness UserBus)
        {
            _userBus = UserBus;
        }
        
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Login()
        {
            var model = CreateViewModel<LoginViewModel>();
            model.RedirectUrl = Request.Query["ReturnUrl"].FirstOrDefault();
            return View(model);
        }


        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult> Login(LoginViewModel model)
        {
            InitializeViewModel(model);

            if (!ModelState.IsValid)
            {
                model.ErrorDisplay.AddMessages(ModelState);
                model.ErrorDisplay.ShowError("","Please correct the following");
                return View(model);
            }

            var user = _userBus.AuthenticateAndRetrieveUser(model.Username, model.Password);
            if (user == null)
            {
                model.ErrorDisplay.ShowError(_userBus.ErrorMessage);
                return View(model);
            }            

            var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
            identity.AddClaim(new Claim(ClaimTypes.Name, user.Username));
            identity.AddClaim(new Claim(ClaimTypes.PrimarySid, user.Id.ToString()));

            UserState.Email = user.Username;
            UserState.UserId = user.Id.ToString();
            UserState.IsAdmin = user.IsAdmin;
            
            identity.AddClaim(new Claim("UserState", UserState.ToString()));
            
            // Set cookie and attach claims
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));


            if (!string.IsNullOrEmpty(model.RedirectUrl))            
                return Redirect(model.RedirectUrl);

            return Redirect("~/");            
        }

       
        [AllowAnonymous]
        [HttpGet]        
        public async Task<IActionResult> Logout()
        {
            UserState = new Web.UserState();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return Redirect("~/");
        }

        [HttpGet]
        [Route("api/isAuthenticated")]
        public bool IsAuthenthenticated()
        {
            return User.Identity.IsAuthenticated;
        }

    }
}