using System;

namespace Westwind.Weblog.Views.Home
{
    public class ErrorViewModel : WeblogBaseViewModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public Exception Error { get; set; }

        public int StatusCode { get; set; } = 500;
        public string HttpVerb { get; set; }

        public string PostData { get; set; }

        public string Path { get; set; }
    }
}