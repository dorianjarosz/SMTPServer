using System.ComponentModel.DataAnnotations.Schema;

namespace SMTPServer.Data.Entities
{
    [Table("sys_SMTPLog")]
    public class SMTPLog
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("emlPath")]
        public string EmlPath { get; set; }

        [Column("from")]
        public string From { get; set; }

        [Column("to")]
        public string To { get; set; }

        [Column("subject")]
        public string Subject { get; set; }

        [Column("mode")]
        public string Mode { get; set; }

        [Column("ruleName")]
        public string RuleName { get; set; }

        [Column("isEnabled")]
        public bool IsEnabled { get; set; }

        [Column("lastUpdate")]
        public DateTime? LastUpdate { get; set; }
    }
}
