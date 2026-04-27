using System.ComponentModel.DataAnnotations;

namespace pms.Models
{
    public class StudentSignUpViewModel
    {
        // ── Step-1 carry-over (hidden fields) ──
        [Required] public string Username { get; set; }
        [Required] public string Email { get; set; }
        [Required] public string Password { get; set; }

        // ── Student personal / academic details ──
        [Required, MaxLength(100), Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required, MaxLength(20), Display(Name = "Enrollment No")]
        public string EnrollmentNo { get; set; }

        [Required, MaxLength(50)]
        public string Branch { get; set; }

        [Required, Range(0.0, 10.0)]
        public decimal CGPA { get; set; }

        [Required, Display(Name = "Passing Year")]
        public int PassingYear { get; set; }

        [Phone, RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number must be 10 digits.")]
        [MaxLength(15)]
        public string Phone { get; set; }

        [Url]
        [MaxLength(255), Display(Name = "Resume URL")]
        public string ResumeURL { get; set; }
    }
}
