using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BlogCoreEngine.Core.Entities;
using BlogCoreEngine.DataAccess.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlogCoreEngine.Controllers
{
    public class CommentController : Controller
    {
        protected ApplicationDbContext applicationContext;
        protected UserManager<ApplicationUser> userManager;
        protected SignInManager<ApplicationUser> signInManager;
        protected RoleManager<IdentityRole> roleManager;

        public CommentController(ApplicationDbContext applicationContext, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole> roleManager)
        {
            this.applicationContext = applicationContext;
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.roleManager = roleManager;
        }

        #region New

        [Authorize, HttpPost]
        public async Task<IActionResult> New(Guid id, string CommentText)
        {
            if (string.IsNullOrWhiteSpace(CommentText))
            {
                ModelState.AddModelError("", "Text field is required!");
                return RedirectToAction("Details", "Post", new { id });
            }

            PostDataModel postDataModel = this.applicationContext.Posts.FirstOrDefault(c => c.Id == id);
            ApplicationUser currentUser = this.applicationContext.Users.FirstOrDefault(u => u.Id == this.User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (postDataModel.Archieved)
            {
                return RedirectToAction("Details", "Post", new { id });
            }

            CommentDataModel comment = new CommentDataModel
            {
                Content = CommentText,
                Created = DateTime.Now,
                Modified = DateTime.Now,
                AuthorId = currentUser.AuthorId,
                Post = postDataModel
            };

            this.applicationContext.Comments.Add(comment);
            await this.applicationContext.SaveChangesAsync();

            return RedirectToAction("Details", "Post", new { id });
        }

        #endregion

        #region Delete

        [Authorize]
        public async Task<IActionResult> Delete(Guid id)
        {
            CommentDataModel commentDataModel = this.applicationContext.Comments.FirstOrDefault(c => c.Id == id);

            if (!(this.User.FindFirstValue(ClaimTypes.NameIdentifier).Equals(commentDataModel.Author.Id) || this.User.IsInRole("Administrator")))
            {
                return RedirectToAction("NoAccess", "Home");
            }

            this.applicationContext.Comments.Remove(commentDataModel);
            await this.applicationContext.SaveChangesAsync();

            return RedirectToAction("Details", "Post", new { id = commentDataModel.PostId });
        }

        #endregion

        #region Edit

        [Authorize]
        public IActionResult Edit(Guid id)
        {
            CommentDataModel comment = this.applicationContext.Comments.FirstOrDefault(c => c.Id == id);

            if (!(this.User.FindFirstValue(ClaimTypes.NameIdentifier).Equals(comment.Author.Id) || this.User.IsInRole("Administrator")))
            {
                return RedirectToAction("NoAccess", "Home");
            }

            return View(comment);
        }

        [Authorize, HttpPost]
        public async Task<IActionResult> Edit(Guid id, CommentDataModel comment)
        {
            if (ModelState.IsValid)
            {
                CommentDataModel commentDataModel = this.applicationContext.Comments.FirstOrDefault(c => c.Id == id);
                commentDataModel.Content = comment.Content;

                this.applicationContext.Update(commentDataModel);
                await this.applicationContext.SaveChangesAsync();

                return RedirectToAction("Details", "Post", new { id = commentDataModel.PostId });
            }

            return View(comment);
        }

        #endregion
    }
}