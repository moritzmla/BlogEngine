using BlogCoreEngine.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlogCoreEngine.DataAccess.Data.Configuration
{
    class AuthorConfiguration : IEntityTypeConfiguration<Author>
    {
        public void Configure(EntityTypeBuilder<Author> builder)
        {
            builder.HasMany(c => c.Posts)
                .WithOne(a => a.Author)
                .HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(c => c.Comments)
                .WithOne(a => a.Author)
                .HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Property(x => x.Image)
                .HasDefaultValue(System.IO.File.ReadAllBytes(".//wwwroot//images//Profile.png"));
        }
    }
}
