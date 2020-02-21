using BlogCoreEngine.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlogCoreEngine.DataAccess.Data.Configuration
{
    class ReplyConfiguration : IEntityTypeConfiguration<CommentDataModel>
    {
        public void Configure(EntityTypeBuilder<CommentDataModel> builder)
        {
        }
    }
}
