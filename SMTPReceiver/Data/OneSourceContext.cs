using Microsoft.EntityFrameworkCore;
using SMTPReceiver.Data.Entities;

namespace SMTPReceiver.Data
{
    public class OneSourceContext : DbContext
    {
        public DbSet<SMTPLog> SMTPLog { get; set; }

        public OneSourceContext(DbContextOptions<OneSourceContext> options)
            : base(options)
        {
        }
    }
}
