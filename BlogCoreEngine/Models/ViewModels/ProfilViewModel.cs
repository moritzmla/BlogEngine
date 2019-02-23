using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BlogCoreEngine.Models.ViewModels
{
    public class ProfilViewModel
    {
        public IFormFile ProfilPicture { get; set; }
    }
}
