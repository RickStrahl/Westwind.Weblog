
using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Westwind.Utilities;

namespace Westwind.Weblog.PostServiceTests
{
    [TestFixture]
    public class ServiceTests
    {
        [Test]
        public void UploadMediaImageTest()
        {
            var file = @"c:\sailbig.jpg";
            var bytes = File.ReadAllBytes(file);

            var media = new
            {
                Name = "sailbig.jpg",
                ContentType = "text/jpeg",
                Data = bytes
            };

            var settings = new HttpRequestSettings
            {
                Content = media,
                Url = "http://localhost:5004/api/posts/image",
                HttpVerb = "POST"
            };
            settings.Headers.Add("Authorization", "Bearer peucqep08vafjjcz");
            var url = HttpUtils.JsonRequest<string>(settings);

            Assert.IsNotEmpty(url);
            Console.WriteLine(url);


            // Define other methods and classes here

        }

        [Test]
        public async Task UploadMediaImageTestAsync()
        {
            var file = @"c:\sailbig.jpg";
            var bytes = File.ReadAllBytes(file);

            var media = new
            {
                Name = "sailbig.jpg",
                ContentType = "text/jpeg",
                Data = bytes
            };

            var settings = new HttpRequestSettings
            {
                Content = media,
                Url = "http://localhost:5004/api/posts/image",
                HttpVerb = "POST"
            };
            settings.Headers.Add("Authorization", "Bearer peucqep08vafjjcz");
            var url = await HttpUtils.JsonRequestAsync<string>(settings);

            Assert.IsNotEmpty(url);
            Console.WriteLine(url);


            // Define other methods and classes here

        }
    }
}
