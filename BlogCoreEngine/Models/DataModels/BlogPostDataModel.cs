using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BlogCoreEngine.Models.DataModels
{
    public class BlogPostDataModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        public byte[] Cover { get; set; }

        [Required]
        public string Preview { get; set; }

        [Required]
        public string Content { get; set; }

        [Required]
        public int Comments { get; set; }

        [Required]
        public int Views { get; set; }

        [Required]
        public DateTime UploadDate { get; set; }

        [Required]
        public DateTime LastChangeDate { get; set; }

        [Required]
        public string CreatorId { get; set; }

        [Required]
        public string CreatorName { get; set; }
    }
}
