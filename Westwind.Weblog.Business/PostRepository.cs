using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Westwind.Data.EfCore;
using Westwind.Weblog.Business.Models;

namespace Westwind.Weblog.Business
{
    public class PostRepository : EntityFrameworkRepository<WeblogContext,Post>
    {
        public PostRepository(WeblogContext context) : base(context)
        {
        
        }

        public async Task<Post> GetPost(string slug)
        {            
            return await Context.Posts
                .Include("Comments")            
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.SafeTitle == slug);
        }

        public async Task<Post> GetPost(int id)
        {
            return await Context.Posts
                    .Include("Comments")                    
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == id);
        }



        
    }
}
