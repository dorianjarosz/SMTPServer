using System.ComponentModel.DataAnnotations.Schema;

namespace OneSourceSMTPServer.Data.Entities
{
    [Table("sys_MappingSMTPReceiver")]
    public class MappingSMTPReceiver
    {
        [Column("id")]
        public int Id { get; set; }

        public string MenuEntryName { get; set; }

        [Column("section")]
        public string Section { get; set; }

        [Column("category")]
        public string Category { get; set; }

        [Column("dataAccess")]
        public string DataAccess { get; set; }

        [Column("toEmail")]
        public string ToEmail { get; set; }

        [Column("destinationInstance")]
        public string DestinationInstance { get; set; }

        [Column("destinationInstanceVersion")]
        public string DestinationInstanceVersion { get; set; }

        [Column("containsString")]
        public string ContainsString { get; set; }

        [Column("discardInternal")]
        public bool? DiscardInternal { get; set; }

        [Column("isEnabled")]
        public bool IsEnabled { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("mode")]
        public string Mode { get; set; }

        [Column("lastUpdate")]
        public DateTime? LastUpdate { get; set; }
    }
}
