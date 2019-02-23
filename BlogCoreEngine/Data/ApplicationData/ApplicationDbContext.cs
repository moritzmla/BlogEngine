using BlogCoreEngine.Models.DataModels;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlogCoreEngine.Data.ApplicationData
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<SettingDataModel> Settings { get; set; }

        public DbSet<BlogPostDataModel> BlogPosts { get; set; }

        public DbSet<CommentDataModel> Comments { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
