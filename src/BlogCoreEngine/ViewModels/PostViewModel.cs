using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace BlogCoreEngine.Web.ViewModels
{
    public class PostViewModel
    {
        [Required]
        public string Title { get; set; }

        [Required]
        public string Text { get; set; }

        [Required]
        public string Preview { get; set; }

        [DataType(DataType.Url)]
        public string Link { get; set; }

        [Required]
        public IFormFile Cover { get; set; }
    }
}
