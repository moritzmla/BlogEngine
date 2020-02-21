using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace BlogCoreEngine.Web.ViewModels
{
    public class BlogViewModel
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public IFormFile Cover { get; set; }
    }
}
