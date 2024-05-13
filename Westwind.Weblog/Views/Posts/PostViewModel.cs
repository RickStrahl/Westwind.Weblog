using System.Collections.Generic;
using Westwind.Weblog.Business;
using Westwind.Weblog.Business.Models;

namespace Westwind.Weblog
{
    public class PostViewModel : WeblogBaseViewModel
    {
        public List<Post> Posts { get; set; }


        public Post Post { get; set; }

        public List<Comment> Comments { get; set; }

        public PostBusiness PostRepo { get; set; }

        public int PageToDisplay { get; set; } = 1;

        public int TotalPages { get; set; } = 1;

        public CommentViewModel ActiveComment { get; set; } 

        public PostViewModel()
        {
            ActiveComment = new CommentViewModel(this);
        }
    }

    public class CommentViewModel
   {
        public CommentViewModel(PostViewModel post)
        {
            Post = post.Post;
        }

        public bool IsCommentDialogVisible { get; set; }

        public string CommentAuthor { get; set; }

        public string CommentWebSite { get; set; }

        public string CommentEmail { get; set; }

        public string CommentText { get; set; }

        public string CommentErrorMessage { get; set; }

        public string CommentErrorIcon { get; set; } = "warning";

        public Post Post { get; set;  }
    }
}