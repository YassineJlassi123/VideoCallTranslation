using Microsoft.EntityFrameworkCore;

namespace VideoCallTranslation.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
       : base(options)
        {
        }
        public DbSet<VideoCallRoom> VideoCallRooms { get; set; }

    }
}
