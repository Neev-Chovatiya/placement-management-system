using System;
using System.ComponentModel.DataAnnotations;

namespace pms.Models
{
    public class AdminSecretKey
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(64)]
        public string KeyHash { get; set; } // SHA256 hash of the plain key

        public DateTime CreatedAt { get; set; }

        public bool IsActive { get; set; }
    }
}
