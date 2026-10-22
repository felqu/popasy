using Microsoft.EntityFrameworkCore;

namespace APItests.Infrastructrure
{
    public class Context : DbContext
    {
        public Context(DbContextOptions<Context> options) : base(options)
        {
        }
    }
}
