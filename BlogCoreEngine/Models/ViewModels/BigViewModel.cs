using BlogCoreEngine.Models.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlogCoreEngine.Models.ViewModels
{
    public class BigViewModel
    {
        public BlogPostDataModel BlogPostDataModel { get; set; }

        public CommentViewModel CommentViewModel { get; set; }

        public List<CommentDataModel> CommentDataModels { get; set; }
    }
}
