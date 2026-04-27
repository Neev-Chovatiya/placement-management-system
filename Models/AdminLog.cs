using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pms.Models
{
    public class AdminLog
    {
        [Key]
        public int LogID { get; set; }
        [ForeignKey("User")]
        public int AdminID { get; set; }
        public User Admin { get; set; }
        [MaxLength(255)]
        public string Action { get; set; }
        public DateTime ActionDate { get; set; }
    }
}
