using BlogCoreEngine.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlogCoreEngine.DataAccess.Data.Configuration
{
    class BlogConfiguration : IEntityTypeConfiguration<BlogDataModel>
    {
        public void Configure(EntityTypeBuilder<BlogDataModel> builder)
        {
            builder.HasMany(x => x.Posts)
                .WithOne(x => x.Blog)
                .HasForeignKey(x => x.BlogId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Property(x => x.Cover)
                .HasDefaultValue(System.IO.File.ReadAllBytes(".//wwwroot//images//Default.png"));
        }
    }
}
