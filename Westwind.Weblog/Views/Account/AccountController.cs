using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;


namespace Westwind.Weblog.Views.Account
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public ActionResult Login()
        {
            
            var model = new LoginViewModel();
            model.ErrorDisplay.ShowInfo("Hello World");

            return View(model);
        }
    }
}