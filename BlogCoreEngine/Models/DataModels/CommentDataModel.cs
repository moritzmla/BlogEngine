using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BlogCoreEngine.Models.DataModels
{
    public class CommentDataModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BlogPostId { get; set; }

        [Required]
        public string Content { get; set; }

        [Required]
        public DateTime UploadDate { get; set; }

        [Required]
        public string CreatorName { get; set; }

        [Required]
        public string CreatorId { get; set; }
    }
}
