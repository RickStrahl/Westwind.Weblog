using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Westwind.Utilities;

namespace BlazePostApi.Client
{
    public class BlazePostApiClient
    {
        public WeblogTokenInfo AuthenticationToken { get; set; }


        /// <summary>
        /// An Api base url such as https://site.com/blazepostapi or https://publish.site.com
        /// 
        /// The relative Url (ie. authenticate or recent or `{{postId}} '
        /// is added to the base url when making requests.
        /// </summary>
        public string ApiBaseUrl
        {
            get => field?.TrimEnd('/');
            set;
        }

        /// <summary>
        /// Captures the entire last request content
        /// </summary>

        public string LastRequestContent { get; set; }

        /// <summary>
        /// Captures the entire last response content
        /// </summary>
        public string LastResponseContent { get; set; }


        /// <summary>
        /// Authenticates a user and returns a Bearer token
        /// Url: posts/authenticate
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="blogId">Optional blog Id</param>
        /// <param name="relativeUrl">Optional relative URL for the authentication endpoint</param>
        /// <returns></returns>
        public async Task<WeblogTokenInfo> Authenticate(string username, string password, string blogId = null,string relativeUrl = "authenticate")
        {
            var data = new AuthenticateRequest
            {
                Username = username,
                Password = password,
                BlogId = blogId
            };

            var settings = new HttpClientRequestSettings
            {                 
                RequestContent = data,
                RequestContentType = "application/json",
                Url = ApiBaseUrl + "/" + relativeUrl.Trim('/'),
                HttpVerb = "POST"          ,
                CaptureRequestAndResponse = true
            };

            WeblogTokenInfo token = null;
            try
            {
                token = await HttpClientUtils.DownloadJsonAsync<WeblogTokenInfo>(settings);
                if (token == null)
                {
                    SetError("Failed to authenticate: " + settings.CapturedResponseContent);
                    ParseJsonError(settings, "Failed to authenticate.");
                }

            }
            catch (Exception ex)
            {
                
                ParseJsonError(settings, "Failed to authenticate",ex);
            }

            AuthenticationToken = token;

            LastRequestContent = settings.CapturedRequestContent;
            LastResponseContent = settings.CapturedResponseContent;

            return token;
        }


        /// <summary>
        /// Checks to see if there's an active valid Token
        /// set on this instance.
        /// 
        /// Checks for a token and whether it's expired.
        /// </summary>
        /// <returns>true or false</returns>
        public bool IsTokenValid()
        {
            if (AuthenticationToken == null || string.IsNullOrEmpty(AuthenticationToken.Token))
                return false;
            if (AuthenticationToken.ExpirationUtc < DateTime.UtcNow)
                return false;
            return true;
        }


        public async Task<WeblogPost> GetPost(string postId, string blogId = null, string relativeUrl = "")
        {
            if (!EnsureAuthToken())
                return null;


            var url = ("/" + relativeUrl + postId).Replace("//","/");
            url = ApiBaseUrl + url.Trim() ;
            if (!string.IsNullOrWhiteSpace(blogId))
                url += "/" + blogId;


            var settings = new HttpClientRequestSettings
            {
                Url = url,
                HttpVerb = "POST",
                CaptureRequestAndResponse = true,
            };
            settings.Headers.Add("Authorization", $"Bearer {AuthenticationToken?.Token}");

            WeblogPost post = null;

            try
            {
                post = await HttpClientUtils.DownloadJsonAsync<WeblogPost>(settings);
                if (post == null)
                {
                    ParseJsonError(settings, "Couldn't retrieve post.");
                }

            }
            catch (Exception ex)
            {
                ParseJsonError(settings, "Couldn't retrieve post.", ex);
            }

            LastRequestContent = settings.CapturedRequestContent;
            LastResponseContent = settings.CapturedResponseContent;

            return post;
        }

        public async Task<WeblogPost> GetLastPost(string blogId = null, string relativeUrl = "last")
        {
            if (!EnsureAuthToken())
                return null;

            var settings = new HttpClientRequestSettings
            {
                Url = ApiBaseUrl + "/" + relativeUrl.Trim('/'),
                HttpVerb = "GET",
                CaptureRequestAndResponse = true
            };
            settings.Headers.Add("Authorization", $"Bearer {AuthenticationToken?.Token}");

            WeblogPost post = null;
            try
            {
                post = await HttpClientUtils.DownloadJsonAsync<WeblogPost>(settings);
                if (post == null)
                {
                    ParseJsonError(settings, "Failed to retrieve last post.");
                }
            }
            catch (Exception ex)
            {
                ParseJsonError(settings, "Failed to retrieve last post.", ex);
            }

            LastRequestContent = settings.CapturedRequestContent;
            LastResponseContent = settings.CapturedResponseContent;

            return post;
        }


        /// <summary>
        /// Checks to see if a post exists by post id
        /// </summary>
        /// <param name="postId"></param>
        /// <param name="blogId"></param>
        /// <returns></returns>
        public async Task<bool> PostExists(string postId)
        {
            if (!EnsureAuthToken())
                return false;

            var settings = new HttpClientRequestSettings
            {
                Url = ApiBaseUrl + "/exists/" + postId,
                HttpVerb = "GET",
                CaptureRequestAndResponse = true
            };
            settings.Headers.Add("Authorization", $"Bearer {AuthenticationToken?.Token}");

            bool exists = false;
            try
            {
                exists = await HttpClientUtils.DownloadJsonAsync<bool>(settings);

                if (!exists)
                    SetError("Post does not exist.");
            }
            catch (Exception ex)
            {
                ParseJsonError(settings, "Failed to check if post exists.", ex);
            }

            LastRequestContent = settings.CapturedRequestContent;
            LastResponseContent = settings.CapturedResponseContent;

            return exists;
        }

        public async Task<IList<WeblogMinimalPost>> GetRecentPosts(PostListFilter listFilter = null, string relativeUrl = "recent")
        {
            if (!EnsureAuthToken())
                return null;

            listFilter ??= new PostListFilter();

            var settings = new HttpClientRequestSettings
            {
                RequestContent = listFilter,
                RequestContentType = "application/json",
                Url = ApiBaseUrl + "/" + relativeUrl.Trim('/'),
                HttpVerb = "POST",
                CaptureRequestAndResponse = true
            };
            settings.Headers.Add("Authorization", $"Bearer {AuthenticationToken?.Token}");

            IList<WeblogMinimalPost> posts = null;
            try
            {
                posts = await HttpClientUtils.DownloadJsonAsync<List<WeblogMinimalPost>>(settings);
                if (posts == null)
                {
                    ParseJsonError(settings, "Failed to retrieve recent posts.");
                }
            }
            catch (Exception ex)
            {
                ParseJsonError(settings, "Failed to retrieve recent posts.", ex);
            }

            LastRequestContent = settings.CapturedRequestContent;
            LastResponseContent = settings.CapturedResponseContent;

            return posts;
        }


        public async Task<WeblogPost> UploadPost(WeblogPost post, string relativeUrl = "")
        {
            if (!EnsureAuthToken())
                return null;

            var settings = new HttpClientRequestSettings
            {
                RequestContent = post,
                RequestContentType = "application/json",
                Url = ApiBaseUrl + "/" + relativeUrl.Trim('/'),
                HttpVerb = "POST",
                CaptureRequestAndResponse = true
            };
            settings.Headers.Add("Authorization", $"Bearer {AuthenticationToken?.Token}");

            post = null;
            try
            {
                post = await HttpClientUtils.DownloadJsonAsync<WeblogPost>(settings);
                if (post == null)
                {
                    ParseJsonError(settings, "Failed to publish new post.");
                }

            }
            catch (Exception ex)
            {
                ParseJsonError(settings, "Failed to publish new post.", ex);
            }

            LastRequestContent = settings.CapturedRequestContent;
            LastResponseContent = settings.CapturedResponseContent;

            return post;
        }

        public async Task<string> UploadMediaObject(WeblogMediaObject image, string relativeUrl = "media")
        {
            if (!EnsureAuthToken())
                return null;

            var settings = new HttpClientRequestSettings
            {
                RequestContent = image,
                RequestContentType = "application/json",
                Url = ApiBaseUrl + "/" + relativeUrl.Trim('/'),
                HttpVerb = "POST",
                CaptureRequestAndResponse = true
            };
            settings.Headers.Add("Authorization", $"Bearer {AuthenticationToken?.Token}");

            string imageUrl = null;
            try
            {
                // Returns a plain text string - not JSON!
                imageUrl = await HttpClientUtils.DownloadStringAsync(settings);
                if (string.IsNullOrEmpty(imageUrl))
                {
                    ParseJsonError(settings, "Failed to upload media object.");
                }
            }
            catch (Exception ex)
            {
                ParseJsonError(settings, "Failed to upload media object.", ex);
            }

            LastRequestContent = settings.CapturedRequestContent;
            LastResponseContent = settings.CapturedResponseContent;

            return imageUrl;
        }

        #region Helpers

        /// <summary>
        /// Checks to see if the Authorization token has been set is not expired.
        /// If not throws an exception.
        /// </summary>
        /// <exception cref="UnauthorizedAccessException"></exception>
        protected bool EnsureAuthToken()
        {
            if (string.IsNullOrEmpty(AuthenticationToken?.Token))
            {
                ErrorMessage = "Please make sure to call Authenticate before making this request.";
                return false;
            }
            if (AuthenticationToken.ExpirationUtc < DateTime.UtcNow)
            {
                ErrorMessage = "Authentication token has expired, please call Authenticate again.";
                return false;
            }
            return true;
        }

        /// <summary>
        /// Parses out an error message from Json if it exists.
        /// 
        /// Looks for `message` property in the returned JSON content.
        /// </summary>
        /// <param name="settings">HttpClientSettings instance from re quest</param>
        /// <param name="baseMessage">A base error message that if provided is pre-pended to any retrieve error message.</param>
        /// <param name="ex">Optional exception that is used if there's no error content</param>
        protected void ParseJsonError(HttpClientRequestSettings settings, string baseMessage = null, Exception ex = null )
        {
            
            if (settings == null)
                return;

            SetError();

            string errorMessage = null;

            if (!string.IsNullOrEmpty(settings.CapturedResponseContent) &&
                settings.Response?.Content != null && settings.CapturedResponseContent.Trim().StartsWith("{"))
            {
                var error = JsonSerializationUtils.Deserialize<JObject>(settings.CapturedResponseContent);
                if (error != null)
                {
                     errorMessage = error["message"]?.ToString();
                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        SetError((!string.IsNullOrEmpty(baseMessage) ? $"{baseMessage}: " : "") +  errorMessage);
                    }
                }
            }

            if (string.IsNullOrEmpty(errorMessage))            
            {                
                if (ex != null)
                    baseMessage += $": {ex.Message}";
                SetError(baseMessage);
            }
        }

        #endregion

        #region Errors

        public string ErrorMessage { get; set; }

        protected void SetError()
        {
            this.SetError("CLEAR");
        }

        protected void SetError(string message)
        {
            if (message == null || message == "CLEAR")
            {
                this.ErrorMessage = string.Empty;
                return;
            }
            this.ErrorMessage += message;
        }

        protected void SetError(Exception ex, bool checkInner = false)
        {
            if (ex == null)
                this.ErrorMessage = string.Empty;

            Exception e = ex;
            if (checkInner)
                e = e.GetBaseException();

            ErrorMessage = e.Message;
        }
        #endregion
    }



    public class WeblogTokenInfo
    {
        public static int TokenTimeoutSeconds = 30 * 60;

        public string Token { get; set; }
        public DateTime ExpirationUtc { get; set; } = DateTime.UtcNow.AddSeconds(TokenTimeoutSeconds);


        public override string ToString()
        {
            return Token;
        }
    }

}
