using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace pms.Models
{
    public class Company
    {
        [Key]
        public int CompanyID { get; set; }
        [Required, MaxLength(100)]
        public string CompanyName { get; set; }
        [Required, MaxLength(100)]
        public string Location { get; set; }
        public DateTime? VisitDate { get; set; }
        [Required, MaxLength(255)]
        public string Description { get; set; }
        [Required, MaxLength(100)]
        public string ContactPerson { get; set; }
        [Required, MaxLength(100)]
        [EmailAddress]
        public string ContactEmail { get; set; }
        public ICollection<Job> Jobs { get; set; }

        /// <summary>
        /// Binary data for the company's logo.
        /// Stored as a byte array to be displayed using Base64 encoding in the UI.
        /// </summary>
        public byte[]? CompanyLogo { get; set; }
    }
}
