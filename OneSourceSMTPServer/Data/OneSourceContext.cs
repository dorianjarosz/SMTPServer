using Microsoft.EntityFrameworkCore;
using OneSourceSMTPServer.Data.Entities;

namespace OneSourceSMTPServer.Data
{
    public class OneSourceContext : DbContext
    {
        public DbSet<SMTPLog> SMTPLog { get; set; }

        public DbSet<MappingSMTPReceiver> MappingSMTPReceiver { get; set; }

        public OneSourceContext(DbContextOptions<OneSourceContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<SMTPLog>()
               .Property(b => b.LastUpdate)
               .HasDefaultValueSql("getdate()");

            builder.Entity<MappingSMTPReceiver>()
               .Property(b => b.LastUpdate)
               .HasDefaultValueSql("getdate()");
        }
    }
}
