using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BlogCoreEngine.Core.Entities;
using BlogCoreEngine.DataAccess.Data;
using BlogCoreEngine.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.EntityFrameworkCore;

namespace BlogCoreEngine.Controllers
{
    public class PostController : Controller
    {
        private readonly ApplicationDbContext applicationContext;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly RoleManager<IdentityRole> roleManager;

        private ApplicationUser currentUser { get; set; }

        public PostController(ApplicationDbContext applicationContext, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole> roleManager)
        {
            this.applicationContext = applicationContext;
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.roleManager = roleManager;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            this.currentUser = this.userManager.FindByIdAsync(this.User.FindFirstValue(ClaimTypes.NameIdentifier)).Result;
        }

        #region Pin

        [Authorize(Roles = "Administrator")]
        public IActionResult Pin(Guid id)
        {
            var post = this.applicationContext.Posts.FirstOrDefault(p => p.Id == id);

            post.Pinned = !post.Pinned;

            this.applicationContext.Posts.Update(post);
            this.applicationContext.SaveChanges();

            return RedirectToAction("View", "Blog", new { id = post.BlogId });
        }

        #endregion

        #region Details

        public IActionResult Details(Guid id)
        {
            PostDataModel post = this.applicationContext.Posts.FirstOrDefault(p => p.Id == id);

            if (post != null)
            {
                post.Views += 1;
                this.applicationContext.Posts.Update(post);
                this.applicationContext.SaveChanges();
            }

            return View(post);
        }

        #endregion

        #region Edit

        [Authorize]
        public IActionResult Edit(Guid id)
        {
            PostDataModel target = this.applicationContext.Posts.FirstOrDefault(x => x.Id == id);

            if (!(currentUser.AuthorId.Equals(target.AuthorId) || this.User.IsInRole("Administrator")))
            {
                return RedirectToAction("NoAccess", "Home");
            }

            return View(target);
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public IActionResult Edit(Guid id, PostDataModel postDataModel, IFormFile _formFile)
        {
            PostDataModel post = this.applicationContext.Posts.FirstOrDefault(x => x.Id == id);

            if (ModelState.IsValid)
            {
                if (post != null)
                {
                    post.Title = postDataModel.Title;
                    post.Preview = postDataModel.Preview;
                    post.Content = postDataModel.Content;
                    post.Modified = DateTime.Now;
                    post.Link = postDataModel.Link;

                    if (_formFile != null)
                    {
                        post.Cover = FileToByteArray(_formFile);
                    }
                }

                this.applicationContext.Posts.Update(post);
                this.applicationContext.SaveChanges();

                return RedirectToAction("View", "Blog", new { id = post.BlogId });
            }

            postDataModel.Title = post.Title;
            postDataModel.Content = post.Content;
            postDataModel.Cover = post.Cover;

            return View(postDataModel);
        }

        #endregion

        #region New

        [Authorize]
        public IActionResult New(Guid id)
        {
            return View();
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> New(Guid id, PostViewModel post)
        {
            if (ModelState.IsValid)
            {
                var postId = Guid.NewGuid();

                await this.applicationContext.Posts.AddAsync(new PostDataModel
                {
                    Id = postId,
                    AuthorId = this.currentUser.AuthorId,
                    BlogId = id,
                    Created = DateTime.Now,
                    Modified = DateTime.Now
                });

                await this.applicationContext.SaveChangesAsync();

                return RedirectToAction("View", "Blog", new { id = postId });
            }

            return View(post);
        }

        [Authorize(Roles = "Administrator"), HttpPost]
        public IActionResult Steal(Guid id, string WebsiteUrl)
        {
            ApplicationUser currentUser = this.applicationContext.Users.FirstOrDefault(u => u.Id == this.User.FindFirstValue(ClaimTypes.NameIdentifier));

            PostDataModel postDataModel = new PostDataModel
            {

                Title = WebsiteUrl,
                Preview = WebsiteUrl,
                Link = WebsiteUrl,
                BlogId = id,
                AuthorId = currentUser.AuthorId
            };

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(WebsiteUrl);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream receiveStream = response.GetResponseStream();
                StreamReader readStream = null;

                if (response.CharacterSet == null)
                {
                    readStream = new StreamReader(receiveStream);
                }
                else
                {
                    readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
                }

                postDataModel.Content = readStream.ReadToEnd();

                response.Close();
                readStream.Close();
            }

            this.applicationContext.Posts.Add(postDataModel);
            this.applicationContext.SaveChanges();

            return RedirectToAction("View", "Blog", new { id });
        }

        #endregion

        #region Archiv

        [Authorize]
        public async Task<IActionResult> Archiv(Guid id)
        {
            PostDataModel target = this.applicationContext.Posts.FirstOrDefault(x => x.Id == id);

            if (!(currentUser.AuthorId.Equals(target.Author.Id) || this.User.IsInRole("Administrator")))
            {
                return RedirectToAction("NoAccess", "Home");
            }

            target.Archieved = !target.Archieved;

            this.applicationContext.Posts.Update(target);
            await this.applicationContext.SaveChangesAsync();

            return RedirectToAction("Details", "Post", new { id });
        }

        #endregion

        #region Delete

        [Authorize]
        public async Task<IActionResult> Delete(Guid id)
        {
            PostDataModel target = this.applicationContext.Posts.FirstOrDefault(x => x.Id == id);
            ApplicationUser author = this.applicationContext.Users.FirstOrDefault(x => x.AuthorId == target.AuthorId);

            if (!(currentUser.AuthorId.Equals(target.Author.Id) || this.User.IsInRole("Administrator")))
            {
                return RedirectToAction("NoAccess", "Home");
            }

            if (target != null)
            {
                this.applicationContext.Comments.RemoveRange(target.Comments);
                this.applicationContext.Users.Update(author);
                this.applicationContext.Posts.Remove(target);

                await this.applicationContext.SaveChangesAsync();
            }

            return RedirectToAction("View", "Blog", new { id = target.BlogId });
        }

        #endregion

        #region Methods

        private byte[] FileToByteArray(IFormFile _formFile)
        {
            using(MemoryStream _memoryStream = new MemoryStream())
            {
                _formFile.CopyTo(_memoryStream);
                return _memoryStream.ToArray();
            }
        }

        #endregion
    }
}