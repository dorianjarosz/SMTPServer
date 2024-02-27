﻿using System.ComponentModel.DataAnnotations.Schema;

namespace SMTPReceiver.Data.Entities
{
    [Table("sys_SMTPLog")]
    public class SMTPLog
    {
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
    }
}
