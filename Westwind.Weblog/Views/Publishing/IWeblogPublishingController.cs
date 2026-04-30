using System.Threading.Tasks;
using Westwind.AspNetCore.Messages;
using Westwind.WeblogPostService.Model;

namespace Westwind.Weblog.Views.Publishing;

public interface IWeblogPublishingController 
{
    /// <summary>
    /// Validates the user and throws exception on failure which will throw
    /// us out of any service method and return the error to the client.
    /// </summary>
    /// <param name="Username"></param>
    /// <param name="Password"></param>
    /// <returns></returns>
    bool ValidateUser(string Username, string Password);

    string[] GetCategories(object blogid, string username, string password);

    Task<WeblogPost[]> GetRecentPosts(string blogid, int numberOfPostes, string username, string password);

    WeblogPost GetPost(string postid, string username, string password);

    string NewPost(string blogid, WeblogPost post, bool publish, string username, string password);

    bool EditPost(string postid, WeblogPost post, bool publish, string username, string password);

    MediaObjectInfo NewMediaObject(object blogid, MediaObject mediaobject, string username, string password);

    bool DeletePost(string appKey, string postid, string username, string password);

    WeblogInfo GetWeblogInfo(string blogId, string username, string password);
}


public class MediaObjectInfo : ApiResponse
{
    public string Url { get; set; }
}