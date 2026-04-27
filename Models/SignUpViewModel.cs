using System.ComponentModel.DataAnnotations;

namespace pms.Models
{
    public class SignUpViewModel
    {
        [Required, MaxLength(50)]
        public string Username { get; set; }

        [Required, MaxLength(100), EmailAddress]
        public string Email { get; set; }

        [Required, MinLength(6), MaxLength(255)]
        public string Password { get; set; }

        [Required, Compare("Password", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string Role { get; set; } // "Student" or "Admin"
    }
}
