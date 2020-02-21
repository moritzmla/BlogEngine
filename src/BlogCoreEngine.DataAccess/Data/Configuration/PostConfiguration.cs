using BlogCoreEngine.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlogCoreEngine.DataAccess.Data.Configuration
{
    class PostConfiguration : IEntityTypeConfiguration<PostDataModel>
    {
        public void Configure(EntityTypeBuilder<PostDataModel> builder)
        {
            builder.HasMany(c => c.Comments)
                .WithOne(b => b.Post)
                .HasForeignKey(x => x.PostId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Property(x => x.Pinned)
                .HasDefaultValue(false);

            builder.Property(x => x.Archieved)
                .HasDefaultValue(false);

            builder.Property(x => x.Cover)
                .HasDefaultValue(System.IO.File.ReadAllBytes(".//wwwroot//images//Default.png"));

            builder.Property(x => x.Views)
                .HasDefaultValue(0);
        }
    }
}
