using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BlogCoreEngine.Models.ViewModels
{
    public class CommentViewModel
    {
        [Required]
        public string Content { get; set; }
    }
}
