using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BlogCoreEngine.Core.Entities;
using BlogCoreEngine.DataAccess.Data;
using BlogCoreEngine.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace BlogCoreEngine.Controllers
{
    public class HomeController : Controller
    {
        protected ApplicationDbContext applicationContext;
        protected UserManager<ApplicationUser> userManager;
        protected SignInManager<ApplicationUser> signInManager;
        protected RoleManager<IdentityRole> roleManager;

        public HomeController(ApplicationDbContext applicationContext, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole> roleManager)
        {
            this.applicationContext = applicationContext;
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.roleManager = roleManager;
        }

        #region Index

        public IActionResult Index()
        {
            SearchViewModel search = new SearchViewModel
            {
                Blogs = this.applicationContext.Blogs.ToList(),
                Posts = this.applicationContext.Posts.ToList()
            };

            return View(search);
        }

        #endregion

        #region Search

        [HttpPost]
        public IActionResult Search(string searchString)
        {
            if(string.IsNullOrWhiteSpace(searchString))
            {
                return RedirectToAction("Index", "Home");
            }

            var posts = this.applicationContext.Posts;
            var blogs = this.applicationContext.Blogs;
            var users = this.applicationContext.Authors;

            List<PostDataModel> searchedPost = new List<PostDataModel>();
            foreach (PostDataModel post in posts)
            {
                if(post.Title.ToLower().Contains(searchString.ToLower()) || post.Preview.ToLower().Contains(searchString.ToLower()))
                {
                    if(!searchedPost.Contains(post))
                    {
                        searchedPost.Add(post);
                    }
                }
            }

            List<BlogDataModel> searchedBlogs = new List<BlogDataModel>();
            foreach (BlogDataModel blog in blogs)
            {
                if (blog.Name.ToLower().Contains(searchString.ToLower()) || blog.Description.ToLower().Contains(searchString.ToLower()))
                {
                    if (!searchedBlogs.Contains(blog))
                    {
                        searchedBlogs.Add(blog);
                    }
                }
            }

            List<Author> searchedUsers = new List<Author>();
            foreach (Author user in users)
            {
                if (user.Name.ToLower().Contains(searchString.ToLower()))
                {
                    if (!searchedUsers.Contains(user))
                    {
                        searchedUsers.Add(user);
                    }
                }
            }

            SearchViewModel result = new SearchViewModel
            {
                Posts = searchedPost,
                Blogs = searchedBlogs,
                Users = searchedUsers
            };

            return View(result);
        }

        #endregion

        #region NoAccess

        public IActionResult NoAccess()
        {
            return View();
        }

        #endregion

        #region AdminPanel

        [Authorize(Roles = "Administrator")]
        public IActionResult AdminPanel()
        {
            return View();
        }

        [Authorize(Roles = "Administrator")]
        public IActionResult Users()
        {
            return View(applicationContext.Users.ToList());
        }

        [Authorize(Roles = "Administrator")]
        public IActionResult Blogs()
        {
            return View(applicationContext.Blogs.ToList());
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> SetAdmin(string id)
        {
            await this.userManager.AddToRoleAsync(await userManager.FindByIdAsync(id), "Administrator");
            this.applicationContext.SaveChanges();

            return RedirectToAction("Users");
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            await this.userManager.DeleteAsync(await userManager.FindByIdAsync(id));
            this.applicationContext.SaveChanges();

            return RedirectToAction("Users");
        }

        #endregion

        #region Settings

        [Authorize(Roles = "Administrator")]
        public IActionResult Settings()
        {
            OptionDataModel options = applicationContext
                .Options
                .First();

            SettingViewModel settingViewModel = new SettingViewModel();
            settingViewModel.Title = options.Title;

            ViewBag.LogoToBytes = options.Logo;

            return View(settingViewModel);
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Settings(SettingViewModel settingViewModel)
        {
            OptionDataModel options = applicationContext
                .Options
                .First();

            if (ModelState.IsValid)
            {
                if(!(settingViewModel.Logo == null || settingViewModel.Logo.Length <= 0))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        settingViewModel.Logo.CopyTo(memoryStream);
                        options.Logo = memoryStream.ToArray();
                    }
                }

                if (!(settingViewModel.Background == null || settingViewModel.Background.Length <= 0))
                {
                    System.IO.File.Delete(".//wwwroot//images//Background.png");
                    using (var fileStream = new FileStream(".//wwwroot//images//Background.png", FileMode.Create))
                    {
                        settingViewModel.Background.CopyTo(fileStream);
                    }
                }

                options.Title = settingViewModel.Title;

                this.applicationContext.Update(options);
                this.applicationContext.SaveChanges();
            }

            ViewBag.LogoToBytes = options.Logo;

            return View(settingViewModel);
        }

        #endregion
    }
}