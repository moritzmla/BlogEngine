using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BlogCoreEngine.Core.Entities;
using BlogCoreEngine.Core.Interfaces;
using BlogCoreEngine.DataAccess.Data;
using BlogCoreEngine.DataAccess.Extensions;
using BlogCoreEngine.Web.Extensions;
using BlogCoreEngine.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BlogCoreEngine.Controllers
{
    public class PostController : Controller
    {
        private readonly IAsyncRepository<PostDataModel> postRepository;
        private readonly UserManager<ApplicationUser> userManager;

        public PostController(IAsyncRepository<PostDataModel> postRepository, UserManager<ApplicationUser> userManager)
        {
            this.postRepository = postRepository;
            this.userManager = userManager;
        }

        #region Pin

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Pin(Guid id)
        {
            var post = await this.postRepository.GetById(id);

            post.Pinned = !post.Pinned;

            await this.postRepository.Update(post);

            return RedirectToAction("View", "Blog", new { id = post.BlogId });
        }

        #endregion

        #region Details

        public async Task<IActionResult> Details(Guid id)
        {
            var post = await this.postRepository.GetById(id);

            if (post != null)
            {
                post.Views += 1;
                await this.postRepository.Update(post);
            }

            return View(post);
        }

        #endregion

        #region Edit

        [Authorize]
        public async Task<IActionResult> Edit(Guid id)
        {
            var post = await this.postRepository.GetById(id);

            if (!(User.Identity.GetAuthorId() == post.AuthorId || this.User.IsInRole("Administrator")))
            {
                return RedirectToAction("NoAccess", "Home");
            }

            return View(post);
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, PostDataModel postDataModel, IFormFile formFile)
        {
            var post = await this.postRepository.GetById(id);

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

                await this.postRepository.Update(post);

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
                var newPost = await this.postRepository.Add(new PostDataModel
                {
                    Id = Guid.NewGuid(),
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

                return RedirectToAction("Details", "Post", new { id = newPost.Id });
            }

            return View(post);
        }

        [Authorize(Roles = "Administrator"), HttpPost]
        public async Task<IActionResult> Steal(Guid id, string WebsiteUrl)
        {
            PostDataModel postDataModel = new PostDataModel
            {
                Title = WebsiteUrl,
                Preview = WebsiteUrl,
                Link = WebsiteUrl,
                BlogId = id,
                AuthorId = User.Identity.GetAuthorId()
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

            await this.postRepository.Add(postDataModel);

            return RedirectToAction("View", "Blog", new { id });
        }

        #endregion

        #region Archiv

        [Authorize]
        public async Task<IActionResult> Archiv(Guid id)
        {
            var post = await this.postRepository.GetById(id);

            if (!((User.Identity.GetAuthorId() == post.AuthorId) || this.User.IsInRole("Administrator")))
            {
                return RedirectToAction("NoAccess", "Home");
            }

            post.Archieved = !post.Archieved;

            await this.postRepository.Update(post);

            return RedirectToAction("Details", "Post", new { id });
        }

        #endregion

        #region Delete

        [Authorize]
        public async Task<IActionResult> Delete(Guid id)
        {
            var post = await this.postRepository.GetById(id);

            if (!((User.Identity.GetAuthorId() == post.AuthorId) || this.User.IsInRole("Administrator")))
            {
                return RedirectToAction("NoAccess", "Home");
            }

            await this.postRepository.Remove(id);

            return RedirectToAction("View", "Blog", new { id = post.BlogId });
        }

        #endregion
    }
}