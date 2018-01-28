namespace Westwind.Weblog.PostService.Model
{
    public class MediaObject
    {

        public string BlogId { get; set; }

        /// <summary>
        /// The name of the Media Object.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The type of the Media Object.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// The byte array of the Media Object itself.
        /// 
        /// </summary>
        public byte[] Data { get; set; }        
    }
}