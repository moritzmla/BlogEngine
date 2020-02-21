using BlogCoreEngine.Core.Entities;
using BlogCoreEngine.Core.Interfaces;
using BlogCoreEngine.Web.Extensions;
using BlogCoreEngine.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BlogCoreEngine.Controllers
{
    public class BlogController : Controller
    {
        private readonly IAsyncRepository<BlogDataModel> blogRepository;

        public BlogController(IAsyncRepository<BlogDataModel> blogRepository)
        {
            this.blogRepository = blogRepository;
        }

        #region View

        public async Task<IActionResult> View(Guid id)
        {
            var blog = await this.blogRepository.GetById(id);
            return View(blog);
        }

        #endregion

        #region New

        [Authorize(Roles = "Administrator")]
        public IActionResult New()
        {
            return View();
        }

        [Authorize(Roles = "Administrator"), HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> New(BlogViewModel blog)
        {
            if (ModelState.IsValid)
            {
                var newBlog = await this.blogRepository.Add(new BlogDataModel
                {
                    Id = Guid.NewGuid(),
                    Name = blog.Name,
                    Description = blog.Description,
                    Cover = blog.Cover.ToByteArray(),
                    Created = DateTime.Now,
                    Modified = DateTime.Now
                });

                return this.RedirectToAsync<BlogController>(x => x.View(newBlog.Id));
            }
            return View(blog);
        }

        #endregion

        #region Edit

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Edit(Guid id)
        {
            return View(await this.blogRepository.GetById(id));
        }

        [Authorize(Roles = "Administrator"), HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, BlogDataModel blog, IFormFile formFile)
        {
            var targetBlog = await this.blogRepository.GetById(id);

            if (ModelState.IsValid)
            {
                targetBlog.Name = blog.Name;
                targetBlog.Description = blog.Description;

                if (!(formFile == null || formFile.Length <= 0))
                {
                    targetBlog.Cover = formFile.ToByteArray();
                }

                await this.blogRepository.Update(targetBlog);

                return this.RedirectToAsync<BlogController>(x => x.View(id));
            }

            blog.Cover = targetBlog.Cover;

            return View(blog);
        }

        #endregion

        #region Delete

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await this.blogRepository.Remove(id);
            return this.RedirectToAsync<HomeController>(x => x.Index());
        }

        #endregion
    }
}