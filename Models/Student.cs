using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pms.Models
{
    public class Student
    {
        [Key]
        public int StudentID { get; set; }
        [ForeignKey("User")]
        public int? UserID { get; set; }
        public User User { get; set; }
        [Required, MaxLength(20)]
        public string EnrollmentNo { get; set; }
        [Required, MaxLength(100)]
        public string FullName { get; set; }
        [MaxLength(50)]
        public string Branch { get; set; }
        [Required, Range(0, 10)]
        public double CGPA { get; set; }
        [Required, Range(2020, 2035)]
        public int PassingYear { get; set; }
        [Phone, RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number must be 10 digits.")]
        [MaxLength(15)]
        public string Phone { get; set; }
        [Url]
        [MaxLength(255)]
        public string ResumeURL { get; set; }
        public ICollection<Application> Applications { get; set; }
        public ICollection<Notification> Notifications { get; set; }
        /// <summary>
        /// Navigation property to the Placement record if the student is placed.
        /// </summary>
        public Placement Placement { get; set; }

        /// <summary>
        /// Binary data for the student's profile picture. 
        /// Stored as a byte array to be displayed using Base64 encoding in the UI.
        /// </summary>
        public byte[]? ProfilePicture { get; set; }
    }
}
