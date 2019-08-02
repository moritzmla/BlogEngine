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
using BlogCoreEngine.DataAccess.Extensions;
using BlogCoreEngine.Web.Extensions;
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

        public PostController(ApplicationDbContext applicationContext, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole> roleManager)
        {
            this.applicationContext = applicationContext;
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.roleManager = roleManager;
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

            if (!(User.Identity.GetAuthorId() == target.AuthorId || this.User.IsInRole("Administrator")))
            {
                return RedirectToAction("NoAccess", "Home");
            }

            return View(target);
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public IActionResult Edit(Guid id, PostDataModel postDataModel, IFormFile formFile)
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

                    if (formFile != null)
                    {
                        post.Cover = formFile.ToByteArray();
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
                    AuthorId = User.Identity.GetAuthorId(),
                    BlogId = id,
                    Created = DateTime.Now,
                    Modified = DateTime.Now,
                    Archieved = false,
                    Pinned = false,
                    Preview = post.Preview,
                    Link = post.Link,
                    Title = post.Title,
                    Views = 0,
                    Cover = post.Cover.ToByteArray(),
                    Content = post.Text
                });

                await this.applicationContext.SaveChangesAsync();

                return RedirectToAction("Details", "Post", new { id = postId });
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

            HttpWebRequest request = WebRequest.Create(WebsiteUrl) as HttpWebRequest;
            HttpWebResponse response = request.GetResponse() as HttpWebResponse;

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream receiveStream = response.GetResponseStream();
                StreamReader readStream = StreamReader.Null;

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

            if (!((User.Identity.GetAuthorId() == target.Author.Id) || this.User.IsInRole("Administrator")))
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

            if (!((User.Identity.GetAuthorId() == target.Author.Id) || this.User.IsInRole("Administrator")))
            {
                return RedirectToAction("NoAccess", "Home");
            }

            if (target != null)
            {
                this.applicationContext.Comments.RemoveRange(target.Comments);
                await this.applicationContext.SaveChangesAsync();
            }

            return RedirectToAction("View", "Blog", new { id = target.BlogId });
        }

        #endregion
    }
}