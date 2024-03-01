using Microsoft.EntityFrameworkCore;
using SMTPServer.Data.Entities;

namespace SMTPServer.Data
{
    public class OneSourceContext : DbContext
    {
        public DbSet<SMTPLog> SMTPLog { get; set; }

        public DbSet<MappingSMTPReceiver> MappingSMTPReceiver { get; set; }

        public OneSourceContext(DbContextOptions<OneSourceContext> options)
            : base(options)
        {
        }
    }
}
