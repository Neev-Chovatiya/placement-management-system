using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace pms.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }
        [Required, MaxLength(50)]
        public string Username { get; set; }
        [Required, MaxLength(100)]
        public string Email { get; set; }
        [Required, MaxLength(255)]
        public string PasswordHash { get; set; }
        [Required, MaxLength(20)]
        public string Role { get; set; } // Student/Admin
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public Student Student { get; set; }
    }
}
