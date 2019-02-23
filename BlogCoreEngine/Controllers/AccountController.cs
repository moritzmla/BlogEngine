using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BlogCoreEngine.Data.AccountData;
using BlogCoreEngine.Data.ApplicationData;
using BlogCoreEngine.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

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

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);
            SetViewBags();
        }

        public IActionResult Login()
        {
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }

        [Authorize]
        public IActionResult ProfilSettings(string id)
        {
            if(id != this.User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return RedirectToAction("NoAccess", "Home");
            }

            return View();
        }

        [Authorize]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ProfilSettings(string id, ProfilViewModel profilViewModel)
        {
            ApplicationUser target = this.userManager.Users.FirstOrDefault(u => u.Id == id);

            if (ModelState.IsValid)
            {
                if (!(profilViewModel.ProfilPicture == null || profilViewModel.ProfilPicture.Length <= 0))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        profilViewModel.ProfilPicture.CopyTo(memoryStream);
                        target.Image = memoryStream.ToArray();
                    }
                }

                await this.userManager.UpdateAsync(target);
            }

            return View(profilViewModel);
        }

        public IActionResult Profil(string id)
        {
            ApplicationUser applicationUser = this.userManager.Users.FirstOrDefault(u => u.Id == id);

            ViewBag.ProfilName = applicationUser.UserName;
            ViewBag.ProfilBlogPostCount = this.applicationDbContext.BlogPosts.Where(bp => bp.CreatorId == id).Count();
            ViewBag.ProfilCommentCount = this.applicationDbContext.Comments.Where(c => c.CreatorId == id).Count();
            ViewBag.ProfilPicture = applicationUser.Image;
            ViewBag.ProfilId = id;

            return View(this.applicationDbContext.BlogPosts.Where(bp => bp.CreatorId == id).ToList());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel registerViewModel)
        {
            if (ModelState.IsValid)
            {
                if(registerViewModel.Password.Equals(registerViewModel.ConfirmPassword))
                {
                    ApplicationUser applicationUser = new ApplicationUser();
                    applicationUser.UserName = registerViewModel.UserName;
                    applicationUser.Email = registerViewModel.Email;
                    applicationUser.Image = System.IO.File.ReadAllBytes(".//wwwroot//images//ProfilPicture.png");

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

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel loginViewModel)
        {
            if (ModelState.IsValid)
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

        // Private

        private byte[] FileToByteArray(IFormFile _formFile)
        {
            using (MemoryStream _memoryStream = new MemoryStream())
            {
                _formFile.CopyTo(_memoryStream);
                return _memoryStream.ToArray();
            }
        }

        private void SetViewBags()
        {
            ViewBag.Title = this.applicationDbContext.Settings.FirstOrDefault(o => o.Id == 1).Title;
            ViewBag.Logo = this.applicationDbContext.Settings.FirstOrDefault(o => o.Id == 1).Logo;

            if (this.User.Identity.IsAuthenticated)
            {
                ViewBag.CurrentUserId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
                ViewBag.CurrentUserPicture = this.userManager.Users.FirstOrDefault(u => u.Id == this.User.FindFirstValue(ClaimTypes.NameIdentifier)).Image;
                ViewBag.UserBlogPostCount = this.applicationDbContext.BlogPosts.Where(bp => bp.CreatorId == this.User.FindFirstValue(ClaimTypes.NameIdentifier)).Count();
                ViewBag.UserCommentCount = this.applicationDbContext.Comments.Where(c => c.CreatorId == this.User.FindFirstValue(ClaimTypes.NameIdentifier)).Count();
            }
        }
    }
}