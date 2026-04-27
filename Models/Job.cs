using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pms.Models
{
    public class Job
    {
        [Key]
        public int JobID { get; set; }
        [ForeignKey("Company")]
        public int CompanyID { get; set; }
        public Company? Company { get; set; }
        [Required, MaxLength(100)]
        public string JobRole { get; set; }
        [Required, Range(0, 500)]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Package { get; set; }
        [Required, Range(0, 10)]
        [Column(TypeName = "decimal(3,2)")]
        public decimal MinCGPA { get; set; }
        /// <summary>
        /// Comma-separated list of eligible branches (e.g., "Computer Engineering,Information Technology").
        /// This is synchronized with the 9 standard branches across the system.
        /// </summary>
        [MaxLength(100)]
        public string? EligibleBranch { get; set; }
        [Required, Range(2020, 2035)]
        public int PassingYear { get; set; }
        public DateTime LastApplyDate { get; set; }
        public ICollection<Application>? Applications { get; set; }
        public ICollection<Placement>? Placements { get; set; }
    }
}
