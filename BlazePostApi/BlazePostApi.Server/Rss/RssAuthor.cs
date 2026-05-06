using System;
using System.Diagnostics;

namespace BlazePostApi.Rss
{

    [DebuggerDisplay("{Name}")]
    public class RssAuthor
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }
    
}