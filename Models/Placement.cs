using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pms.Models
{
    public class Placement
    {
        [Key]
        public int PlacementID { get; set; }
        [ForeignKey("Student")]
        public int StudentID { get; set; }
        public Student Student { get; set; }
        [ForeignKey("Company")]
        public int CompanyID { get; set; }
        public Company Company { get; set; }
        [ForeignKey("Job")]
        public int JobID { get; set; }
        public Job Job { get; set; }
        [Column(TypeName = "decimal(10,2)")]
        public decimal Package { get; set; }
        public DateTime PlacementDate { get; set; }
    }
}
