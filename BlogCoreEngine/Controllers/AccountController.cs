using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BlogCoreEngine.Data.AccountData;
using BlogCoreEngine.Data.ApplicationData;
using BlogCoreEngine.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BlogCoreEngine.Controllers
{
    public class AccountController : Controller
    {
        protected readonly AccountDbContext accountDbContext;
        protected readonly ApplicationDbContext applicationDbContext;

        protected UserManager<ApplicationUser> userManager;
        protected SignInManager<ApplicationUser> signInManager;

        public AccountController(ApplicationDbContext _applicationDbContext, AccountDbContext _accountDbContext, UserManager<ApplicationUser> _userManager, SignInManager<ApplicationUser> _signInManager)
        {
            this.applicationDbContext = _applicationDbContext;
            this.accountDbContext = _accountDbContext;
            this.userManager = _userManager;
            this.signInManager = _signInManager;
        }

        public IActionResult Login()
        {
            SetViewBags();

            return View();
        }

        public IActionResult Register()
        {
            SetViewBags();

            return View();
        }

        public IActionResult Profil(string id)
        {
            SetViewBags();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel registerViewModel)
        {
            if(ModelState.IsValid)
            {
                if(registerViewModel.Password.Equals(registerViewModel.ConfirmPassword))
                {
                    ApplicationUser applicationUser = new ApplicationUser();
                    applicationUser.UserName = registerViewModel.UserName;
                    applicationUser.Email = registerViewModel.Email;

                    IdentityResult result = userManager.CreateAsync(applicationUser, registerViewModel.Password).Result;

                    if(result.Succeeded)
                    {
                        this.userManager.AddToRoleAsync(applicationUser, "Writer");

                        this.signInManager.PasswordSignInAsync(registerViewModel.UserName, registerViewModel.Password, registerViewModel.RememberMe, false);

                        return RedirectToAction("Index", "Home");
                    } else
                    {
                        ModelState.AddModelError("", "Something dosen´t work.");
                    }
                } else
                {
                    ModelState.AddModelError("", "Passwords dosen´t are the same.");
                }
            }

            return View(registerViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel loginViewModel)
        {
            if(ModelState.IsValid)
            {
                var result = signInManager.PasswordSignInAsync(loginViewModel.UserName, loginViewModel.Password, loginViewModel.RememberMe, false).Result;

                if(result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                } else
                {
                    ModelState.AddModelError("", "Password or Username is not right.");
                }
            }

            return View(loginViewModel);
        }

        [Authorize]
        [Route("Logout")]
        public async Task<IActionResult> LogOutAsync()
        {
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return RedirectToAction("Index", "Home");
        }

        private void SetViewBags()
        {
            ViewBag.Title = this.applicationDbContext.Settings.FirstOrDefault(o => o.Id == 1).Title;
            ViewBag.Logo = this.applicationDbContext.Settings.FirstOrDefault(o => o.Id == 1).Logo;

            ViewBag.CurrentUserId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.UserBlogPostCount = this.applicationDbContext.BlogPosts.Where(bp => bp.CreatorId == this.User.FindFirstValue(ClaimTypes.NameIdentifier)).Count();
            ViewBag.UserCommentCount = this.applicationDbContext.Comments.Where(c => c.CreatorId == this.User.FindFirstValue(ClaimTypes.NameIdentifier)).Count();
        }
    }
}