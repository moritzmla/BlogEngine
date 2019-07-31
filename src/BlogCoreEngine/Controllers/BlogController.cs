using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BlogCoreEngine.Core.Entities;
using BlogCoreEngine.DataAccess.Data;
using BlogCoreEngine.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlogCoreEngine.Controllers
{
    public class BlogController : Controller
    {
        protected ApplicationDbContext applicationContext;
        protected UserManager<ApplicationUser> userManager;
        protected SignInManager<ApplicationUser> signInManager;
        protected RoleManager<IdentityRole> roleManager;

        public BlogController(ApplicationDbContext applicationContext, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole> roleManager)
        {
            this.applicationContext = applicationContext;
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.roleManager = roleManager;
        }

        #region View

        public IActionResult View(Guid id)
        {
            var blog = this.applicationContext.Blogs.FirstOrDefault(x => x.Id == id);
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
                var blogId = Guid.NewGuid();

                await this.applicationContext.Blogs.AddAsync(new BlogDataModel
                {
                    Id = blogId,
                    Name = blog.Name,
                    Description = blog.Description,
                    Cover = FileToByteArray(blog.Cover),
                    Created = DateTime.Now,
                    Modified = DateTime.Now
                });

                await this.applicationContext.SaveChangesAsync();

                return RedirectToAction("View", "Blog", new { id = blogId });
            }
            return View(blog);
        }

        #endregion

        #region Edit

        [Authorize(Roles = "Administrator")]
        public IActionResult Edit(Guid id)
        {
            return View(this.applicationContext.Blogs.FirstOrDefault(x => x.Id == id));
        }

        [Authorize(Roles = "Administrator"), HttpPost, ValidateAntiForgeryToken]
        public IActionResult Edit(Guid id, BlogDataModel blog, IFormFile formFile)
        {
            BlogDataModel target = this.applicationContext.Blogs.FirstOrDefault(x => x.Id == id);

            if (ModelState.IsValid)
            {
                target.Name = blog.Name;
                target.Description = blog.Description;

                if (!(formFile == null || formFile.Length <= 0))
                {
                    target.Cover = FileToByteArray(formFile);
                }

                this.applicationContext.Blogs.Update(target);
                this.applicationContext.SaveChanges();

                return RedirectToAction("View", "Blog", new { id });
            }

            blog.Cover = target.Cover;

            return View(blog);
        }

        #endregion

        #region Delete

        [Authorize(Roles = "Administrator")]
        public IActionResult Delete(Guid id)
        {
            BlogDataModel blog = this.applicationContext.Blogs.Include(x => x.Posts).FirstOrDefault(x => x.Id == id);
            this.applicationContext.Posts.RemoveRange(blog.Posts);
            this.applicationContext.Blogs.Remove(blog);
            this.applicationContext.SaveChanges();

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