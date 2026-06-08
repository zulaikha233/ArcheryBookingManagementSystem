using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArcheryAlley.Models
{
    public class Students
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int StudentId { get; set; }

        [Required]
        public int ParentCustomerId { get; set; }

        [ForeignKey("ParentCustomerId")]
        public Customers Parent { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [StringLength(20)]
        public string? ICNumber { get; set; }

        public DateTime? Birthday { get; set; }
        
        public int? Age { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string LevelCategory { get; set; }
    }
}
