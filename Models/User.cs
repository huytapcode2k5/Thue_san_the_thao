using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Thue_san_the_thao.Models.Data;

namespace Thue_san_the_thao.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; }

        [StringLength(15)]
        public string Phone { get; set; }

        public int RoleID { get; set; }

        [StringLength(50)]
        public string Provider { get; set; }

        [StringLength(255)]
        public string ProviderID { get; set; }

        public bool Status { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Property
        [ForeignKey("RoleID")]
        public virtual Role Role { get; set; }
    }
}