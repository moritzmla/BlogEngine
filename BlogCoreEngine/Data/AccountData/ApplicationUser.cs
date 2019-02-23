using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BlogCoreEngine.Data.AccountData
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public byte[] Image { get; set; }
    }
}
