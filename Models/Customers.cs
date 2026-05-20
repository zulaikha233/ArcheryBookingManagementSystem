using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArcheryAlley.Models
{
    public class Customers
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CustomerId { get; set; }

        [StringLength(100)]
        public string? FullName { get; set; }

        [Required]
        [StringLength(150)]
        public string Email { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Required]
        [StringLength(50)]
        public string Password { get; set; }

        public DateTime? Birthday { get; set; }

        [StringLength(250)]
        public string? Address { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Inactive";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
