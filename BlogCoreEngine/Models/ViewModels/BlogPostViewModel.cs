using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BlogCoreEngine.Models.ViewModels
{
    public class BlogPostViewModel
    {
        [Required]
        public string Title { get; set; }

        [Required]
        public string Preview { get; set; }

        [Required]
        public string Content { get; set; }

        public byte[] Cover { get; set; }
    }
}
