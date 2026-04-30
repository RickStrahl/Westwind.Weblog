using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Westwind.AspNetCore.Errors;
using Westwind.Weblog.Business;
using Westwind.Weblog.Business.Models;
using Westwind.WeblogPostService.Model;
using Post = Westwind.WeblogPostService.Model.WeblogPost;

namespace Westwind.Weblog.Views.Publishing;


[Route("api/publishing")]
public class WeblogPublishingController : IWeblogPublishingController
{
    public PostBusiness PostBus { get; }
    public UserBusiness UserBus { get; }

    public WeblogPublishingController(PostBusiness postBus, UserBusiness userBus)
    {
        PostBus = postBus;
        UserBus = userBus;
    }

    public bool ValidateUser(string username, string password)
    {
        if (!UserBus.AuthenticateUser(username, password))
            throw new ApiException(UserBus.ErrorMessage, 401);

        return true;
    }

    public string[] GetCategories(object blogid, string username, string password)
    {
        throw new System.NotImplementedException();
    }

    [HttpGet]
    [Route("post/{postId}")]
    public Post GetPost(string postid, string username, string password)
    {
        ValidateUser(username, password);
            

        if (string.IsNullOrEmpty(postid))
            throw new ApiException("Invalid post Id passed in.");

        return GetPost(postid, username, password);
    }

    [HttpGet]
    [Route("posts")]
    public async Task<WeblogPost[]> GetRecentPosts(string blogid, int numberOfPostes, string username, string password)
    {
        ValidateUser(username, password);

        var posts = await PostBus.GetLastPostsAsync();


        return posts.Select((p) =>
            new WeblogPost
            {
                PostId = p.Id.ToString(),
                BlogId = p.Id.ToString(),
                Abstract = p.Abstract,
                Body = p.Body,
                Title = p.Title,
                DateCreated = p.Created,
                ImageUrl = p.FeaturedImageUrl,
                Location = p.Location,
                Categories = p.Categories,
                Keywords = p.Keywords,
                PermaLink = p.PermanentUrl,
                PostStatus = p.Active ? PostStatuses.Published : PostStatuses.Draft,
                SourceEditUrl = p.GithubUrl,
                SafeTitle = p.SafeTitle,
                RawPostText = p.Markdown,
                RawPostType = "markdown",
            }).ToArray();                
    }


    public string NewPost(string blogid, Post post, bool publish, string username, string password)
    {
        throw new System.NotImplementedException();
    }

    public bool EditPost(string postid, Post post, bool publish, string username, string password)
    {
        throw new System.NotImplementedException();
    }

    public MediaObjectInfo NewMediaObject(object blogid, MediaObject mediaobject, string username, string password)
    {
        throw new System.NotImplementedException();
    }

    public bool DeletePost(string appKey, string postid, string username, string password)
    {
        throw new System.NotImplementedException();
    }

    public WeblogInfo GetWeblogInfo(string blogId, string username, string password)
    {
        throw new System.NotImplementedException();
    }
}
