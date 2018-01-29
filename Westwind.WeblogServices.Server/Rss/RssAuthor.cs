using System;
using System.Diagnostics;

namespace Westwind.WeblogServices.Server
{

    [DebuggerDisplay("{Name}")]
    public class RssAuthor
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }
    
}