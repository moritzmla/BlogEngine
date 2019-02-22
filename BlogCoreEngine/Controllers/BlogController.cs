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

namespace BlogCoreEngine.Controllers
{
    public class BlogController : Controller
    {
        protected readonly ApplicationDbContext applicationDbContext;
        protected readonly AccountDbContext accountDbContext;

        protected UserManager<ApplicationUser> userManager;
        protected SignInManager<ApplicationUser> signInManager;
        protected RoleManager<IdentityRole> roleManager;

        public BlogController(ApplicationDbContext _applicationDbContext, AccountDbContext _accountDbContext, UserManager<ApplicationUser> _userManager, SignInManager<ApplicationUser> _signInManager, RoleManager<IdentityRole> _roleManager)
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

        public IActionResult DetailsBlogPost(int id, BigViewModel bigViewModel)
        {
            bigViewModel.BlogPostDataModel = this.applicationDbContext.BlogPosts.FirstOrDefault(bp => bp.Id == id);

            if (bigViewModel.BlogPostDataModel != null)
            {
                bigViewModel.BlogPostDataModel.Views += 1;
                this.applicationDbContext.BlogPosts.Update(bigViewModel.BlogPostDataModel);
                this.applicationDbContext.SaveChanges();
            }

            bigViewModel.CommentDataModels = this.applicationDbContext.Comments.Where(c => c.BlogPostId == id).ToList();

            return View(bigViewModel);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> NewComment(int id, BigViewModel bigViewModel)
        {
            if (bigViewModel.CommentViewModel.Content == null || bigViewModel.CommentViewModel.Content.Length <= 0)
            {
                ModelState.AddModelError("", "Text field is required!");

                return RedirectToAction("DetailsBlogPost", "Blog", new
                {
                    id,
                    bigViewModel
                });
            }

            BlogPostDataModel blogPostDataModel = this.applicationDbContext.BlogPosts.FirstOrDefault(c => c.Id == id);
            blogPostDataModel.Comments += 1;
            this.applicationDbContext.BlogPosts.Update(blogPostDataModel);

            this.applicationDbContext.Comments.Add(new CommentDataModel
            {
                BlogPostId = id,
                Content = bigViewModel.CommentViewModel.Content,
                UploadDate = DateTime.Now,
                CreatorId = this.User.FindFirstValue(ClaimTypes.NameIdentifier),
                CreatorName = HttpContext.User.Identity.Name
            });

            await this.applicationDbContext.SaveChangesAsync();

            return RedirectToAction("DetailsBlogPost", "Blog", new {
                blogPostDataModel.Id,
                bigViewModel
            });
        }

        [Authorize]
        public async Task<IActionResult> DeleteComment(int id)
        {
            CommentDataModel commentDataModel = this.applicationDbContext.Comments.FirstOrDefault(c => c.Id == id);

            if (!(this.User.FindFirstValue(ClaimTypes.NameIdentifier).Equals(commentDataModel.CreatorId) || this.User.IsInRole("Administrator")))
            {
                return RedirectToAction("NoAccess", "Home");
            }

            BlogPostDataModel blogPostDataModel = this.applicationDbContext.BlogPosts.FirstOrDefault(c => c.Id == commentDataModel.BlogPostId);
            blogPostDataModel.Comments -= 1;
            this.applicationDbContext.BlogPosts.Update(blogPostDataModel);

            this.applicationDbContext.Remove(commentDataModel);
            await this.applicationDbContext.SaveChangesAsync();

            BigViewModel bigViewModel = new BigViewModel();
            bigViewModel.BlogPostDataModel = blogPostDataModel;

            return RedirectToAction("DetailsBlogPost", "Blog", new {
                bigViewModel.BlogPostDataModel.Id,
                bigViewModel
            });
        }

        [Authorize]
        public IActionResult EditComment(int id)
        {
            CommentDataModel commentDataModel = this.applicationDbContext.Comments.FirstOrDefault(c => c.Id == id);

            if (!(this.User.FindFirstValue(ClaimTypes.NameIdentifier).Equals(commentDataModel.CreatorId) || this.User.IsInRole("Administrator")))
            {
                return RedirectToAction("NoAccess", "Home");
            }

            return View(new CommentViewModel { Content = commentDataModel.Content });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> EditComment(int id, CommentViewModel editViewModel)
        {
            if (ModelState.IsValid)
            {
                CommentDataModel commentDataModel = this.applicationDbContext.Comments.FirstOrDefault(c => c.Id == id);
                commentDataModel.Content = editViewModel.Content;
                this.applicationDbContext.Update(commentDataModel);
                await this.applicationDbContext.SaveChangesAsync();

                return RedirectToAction("Index", "Home");
            }

            return View(editViewModel);
        }

        [Authorize]
        public IActionResult EditBlogPost(int id)
        {
            BlogPostDataModel target = this.applicationDbContext.BlogPosts.FirstOrDefault(bp => bp.Id == id);

            if (!(this.User.FindFirstValue(ClaimTypes.NameIdentifier).Equals(target.CreatorId) || this.User.IsInRole("Administrator")))
            {
                return RedirectToAction("NoAccess", "Home");
            }

            BlogPostViewModel blogPostViewModel = new BlogPostViewModel();
            blogPostViewModel.Title = target.Title;
            blogPostViewModel.Preview = target.Preview;
            blogPostViewModel.Content = target.Content;

            ViewBag.CoverToBytes = target.Cover;

            return View(blogPostViewModel);
        }

        [Authorize]
        public IActionResult NewBlogPost()
        {
            return View();
        }

        [Authorize]
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult EditBlogPost(int id, BlogPostViewModel blogPostViewModel, IFormFile _formFile)
        {
            BlogPostDataModel target = this.applicationDbContext.BlogPosts.FirstOrDefault(bp => bp.Id == id);

            if (ModelState.IsValid)
            {
                if (target != null)
                {
                    target.Title = blogPostViewModel.Title;
                    target.Preview = blogPostViewModel.Preview;
                    target.Content = blogPostViewModel.Content;
                    target.LastChangeDate = DateTime.Now;

                    if(_formFile != null)
                    {
                        target.Cover = FileToByteArray(_formFile);
                    }
                }

                this.applicationDbContext.BlogPosts.Update(target);
                this.applicationDbContext.SaveChanges();

                return RedirectToAction("Index", "Home");
            }

            blogPostViewModel.Title = target.Title;
            blogPostViewModel.Content = target.Content;

            return View(blogPostViewModel);
        }

        [Authorize]
        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult NewBlogPost(BlogPostViewModel blogPostViewModel, IFormFile _formFile)
        {
            if (ModelState.IsValid)
            {
                if (_formFile == null)
                {
                    ModelState.AddModelError("", "You need a Cover");
                    return View(blogPostViewModel);
                }

                this.applicationDbContext.BlogPosts.Add(new BlogPostDataModel
                { 
                    Title = blogPostViewModel.Title,
                    Preview = blogPostViewModel.Preview,
                    Content = blogPostViewModel.Content,
                    UploadDate = DateTime.Now,
                    Views = 1,
                    Comments = 0,
                    Cover = FileToByteArray(_formFile),
                    LastChangeDate = DateTime.Now,
                    CreatorId = this.User.FindFirstValue(ClaimTypes.NameIdentifier),
                    CreatorName = HttpContext.User.Identity.Name
                });

                this.applicationDbContext.SaveChanges();
                return RedirectToAction("Index", "Home");
            }

            return View(blogPostViewModel);
        }

        [Authorize]
        public async Task<IActionResult> DeleteBlogPost(int id)
        {
            BlogPostDataModel target = this.applicationDbContext.BlogPosts.FirstOrDefault(bp => bp.Id == id);

            if (!(this.User.FindFirstValue(ClaimTypes.NameIdentifier).Equals(target.CreatorId) || this.User.IsInRole("Administrator")))
            {
                return RedirectToAction("NoAccess", "Home");
            }

            foreach (CommentDataModel commentDataModel in this.applicationDbContext.Comments.Where(c => c.BlogPostId == target.Id))
            {
                this.applicationDbContext.Comments.Remove(commentDataModel);
            }

            if (target != null)
            {
                this.applicationDbContext.BlogPosts.Remove(target);
            }

            await this.applicationDbContext.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }

        // Private

        private byte[] FileToByteArray(IFormFile _formFile)
        {
            using(MemoryStream _memoryStream = new MemoryStream())
            {
                _formFile.CopyTo(_memoryStream);
                return _memoryStream.ToArray();
            }
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