using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BlogCoreEngine.Core.Entities;
using BlogCoreEngine.DataAccess.Data;
using BlogCoreEngine.DataAccess.Extensions;
using BlogCoreEngine.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace BlogCoreEngine.Controllers
{
    public class AccountController : Controller
    {
        protected readonly ApplicationDbContext applicationContext;

        protected UserManager<ApplicationUser> userManager;
        protected SignInManager<ApplicationUser> signInManager;

        public AccountController(ApplicationDbContext applicationContext, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            this.applicationContext = applicationContext;
            this.userManager = userManager;
            this.signInManager = signInManager;
        }

        #region Options

        [Authorize]
        public IActionResult Settings(string id)
        {
            if (id != this.User.Identity.GetAuthorName())
            {
                return RedirectToAction("NoAccess", "Home");
            }

            ApplicationUser target = this.userManager.Users.FirstOrDefault(u => u.UserName == id);
            ViewBag.CurrentUserPicture = target.Author.Image;

            return View();
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(string id, ProfileViewModel profileViewModel)
        {
            ApplicationUser target = this.userManager.Users.FirstOrDefault(u => u.UserName == id);

            if (ModelState.IsValid)
            {
                if (!(profileViewModel.ProfilePicture == null || profileViewModel.ProfilePicture.Length <= 0))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        profileViewModel.ProfilePicture.CopyTo(memoryStream);
                        target.Author.Image = memoryStream.ToArray();
                    }
                }

                await this.userManager.UpdateAsync(target);
                return RedirectToAction("Profile", "Account", new { id });
            }

            ViewBag.CurrentUserPicture = target.Author.Image;

            return View(profileViewModel);
        }

        #endregion

        #region Profile

        public IActionResult Profile(string id)
        {
            ApplicationUser profile = this.applicationContext.Users.FirstOrDefault(u => u.UserName == id);
            return View(profile.Author);
        }

        #endregion

        #region Register

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel registerViewModel)
        {
            if (ModelState.IsValid)
            {
                if(registerViewModel.Password.Equals(registerViewModel.ConfirmPassword))
                {
                    Author author = new Author
                    {
                        Id = Guid.NewGuid(),
                        Created = DateTime.Now,
                        Modified = DateTime.Now,
                        Name = registerViewModel.UserName,
                        Image = System.IO.File.ReadAllBytes(".//wwwroot//images//ProfilPicture.png")
                    };

                    applicationContext.Authors.Add(author);
                    applicationContext.SaveChanges();

                    ApplicationUser applicationUser = new ApplicationUser
                    {
                        UserName = registerViewModel.UserName,
                        Email = registerViewModel.Email,
                        Author = author
                    };

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

        #endregion

        #region Login

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel loginViewModel)
        {
            if (ModelState.IsValid)
            {
                string userName = loginViewModel.UserName;

                if (userName.IndexOf("@") > -1)
                {
                    var user = await this.userManager.FindByEmailAsync(userName);

                    if (user == null)
                    {
                        ModelState.AddModelError("", "Invalid login attempt.");
                        return View(loginViewModel);
                    } else
                    {
                        userName = user.UserName;
                    }
                }

                var result = await signInManager.PasswordSignInAsync(userName, loginViewModel.Password, loginViewModel.RememberMe, false);

                if(result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Password or Username is not right.");
                }
            }

            return View(loginViewModel);
        }

        #endregion

        #region LogOut

        [Authorize]
        [Route("Logout")]
        public async Task<IActionResult> LogOutAsync()
        {
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return RedirectToAction("Index", "Home");
        }

        #endregion

        #region Methods

        private byte[] FileToByteArray(IFormFile _formFile)
        {
            using (MemoryStream _memoryStream = new MemoryStream())
            {
                _formFile.CopyTo(_memoryStream);
                return _memoryStream.ToArray();
            }
        }

        #endregion
    }
}