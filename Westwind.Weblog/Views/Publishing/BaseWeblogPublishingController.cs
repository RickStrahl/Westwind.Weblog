//using Westwind.AspNetCore;
//using Westwind.AspNetCore.Messages;
//using Westwind.WeblogPostService.Model;

//namespace Westwind.Weblog.Views.Publishing;

//public abstract class BaseWeblogPublishingController : IBaseWeblogPublishingController,  BaseApiController
//{

//    /// <summary>
//    /// Validates the user and throws exception on failure which will throw
//    /// us out of any service method and return the error to the client.
//    /// </summary>
//    /// <param name="Username"></param>
//    /// <param name="Password"></param>
//    /// <returns></returns>
//    public virtual bool ValidateUser(string Username, string Password)
//    {
//        return true;
//    }

//    public virtual string[] GetCategories(object blogid, string username, string password)
//    {
//        return null;
//    }

//    public virtual WeblogPost[] GetRecentPosts(string blogid, int numberOfPostes, string username, string password)
//    {
//        return null;
//    }

//    public virtual WeblogPost GetPost(string postid, string username, string password)
//    {
//        return null;
//    }

//    public virtual string NewPost(string blogid, WeblogPost post, bool publish, string username, string password)
//    {
//        return null;
//    }
//    public virtual bool EditPost(string postid, WeblogPost post, bool publish, string username, string password)
//    {
//        return false;
//    }


//    public virtual MediaObjectInfo NewMediaObject(object blogid, MediaObject mediaobject, string username, string password)
//    {
//        string url = null;
//        var mediaResponse = new MediaObjectInfo
//        {
//            Url = url
//        };
//        return mediaResponse;
//    }

//    public virtual bool DeletePost(string appKey, string postid, string username, string password)
//    {
//        return false;
//    }

//    public virtual WeblogInfo GetWeblogInfo(string blogId, string username, string password)
//    {
//        return null;
//    }

//}


