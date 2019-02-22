using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BlogCoreEngine.Data.AccountData;
using BlogCoreEngine.Data.ApplicationData;
using BlogCoreEngine.Models.DataModels;
using BlogCoreEngine.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using static System.Net.Mime.MediaTypeNames;

namespace BlogCoreEngine.Controllers
{
    public class HomeController : Controller
    {
        protected readonly ApplicationDbContext applicationDbContext;
        protected readonly AccountDbContext accountDbContext;

        protected UserManager<ApplicationUser> userManager;
        protected SignInManager<ApplicationUser> signInManager;
        protected RoleManager<IdentityRole> roleManager;

        public HomeController(ApplicationDbContext _applicationDbContext, AccountDbContext _accountDbContext, UserManager<ApplicationUser> _userManager, SignInManager<ApplicationUser> _signInManager, RoleManager<IdentityRole> _roleManager)
        {
            this.applicationDbContext = _applicationDbContext;
            this.accountDbContext = _accountDbContext;
            this.userManager = _userManager;
            this.signInManager = _signInManager;
            this.roleManager = _roleManager;
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            base.OnActionExecuted(context);
            SetViewBags();
        }

        public IActionResult Index()
        {
            return View(this.applicationDbContext.BlogPosts.ToList());
        }

        public IActionResult NoAccess()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(string searchString)
        {
            if(searchString == null || searchString.Length <= 0)
            {
                return RedirectToAction("Index");
            }

            List<BlogPostDataModel> blogPostDataModels = new List<BlogPostDataModel>();

            foreach(BlogPostDataModel bpdm in this.applicationDbContext.BlogPosts.Where(bp => bp.Title.ToLower().Contains(searchString.ToLower())).ToList())
            {
                if(!blogPostDataModels.Contains(bpdm))
                {
                    blogPostDataModels.Add(bpdm);
                }
            }

            foreach (BlogPostDataModel bpdm in this.applicationDbContext.BlogPosts.Where(bp => bp.Content.ToLower().Contains(searchString.ToLower())).ToList())
            {
                if (!blogPostDataModels.Contains(bpdm))
                {
                    blogPostDataModels.Add(bpdm);
                }
            }

            return View(blogPostDataModels);
        }

        [Authorize(Roles = "Administrator")]
        public IActionResult Users()
        {
            return View(accountDbContext.Users.ToList());
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> SetAdmin(string id)
        {
            await this.userManager.AddToRoleAsync(await userManager.FindByIdAsync(id), "Administrator");
            this.accountDbContext.SaveChanges();

            return RedirectToAction("Users");
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            await this.userManager.DeleteAsync(await userManager.FindByIdAsync(id));
            this.accountDbContext.SaveChanges();

            return RedirectToAction("Users");
        }

        [Authorize(Roles = "Administrator")]
        public IActionResult AdminPanel()
        {
            return View();
        }

        [Authorize(Roles = "Administrator")]
        public IActionResult Settings()
        {
            SettingDataModel settingDataModel = applicationDbContext.Settings.FirstOrDefault(o => o.Id == 1);

            SettingViewModel settingViewModel = new SettingViewModel();
            settingViewModel.Title = settingDataModel.Title;

            return View(settingViewModel);
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Settings(SettingViewModel settingViewModel)
        {
            SettingDataModel settingDataModel = applicationDbContext.Settings.FirstOrDefault(o => o.Id == 1);

            if (ModelState.IsValid)
            {
                if(!(settingViewModel.Logo == null || settingViewModel.Logo.Length <= 0))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        settingViewModel.Logo.CopyTo(memoryStream);
                        settingDataModel.Logo = memoryStream.ToArray();
                    }
                }

                settingDataModel.Title = settingViewModel.Title;

                this.applicationDbContext.Update(settingDataModel);
                this.applicationDbContext.SaveChanges();
            }

            return View(settingViewModel);
        }

        // Private

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