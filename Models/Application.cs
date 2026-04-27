using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pms.Models
{
    public class Application
    {
        [Key]
        public int ApplicationID { get; set; }
        [ForeignKey("Student")]
        public int StudentID { get; set; }
        public Student? Student { get; set; }
        [ForeignKey("Job")]
        public int JobID { get; set; }
        public Job? Job { get; set; }
        public DateTime AppliedDate { get; set; }
        [MaxLength(30)]
        public string Status { get; set; } // Applied/Shortlisted/Rejected/Selected
    }
}
