using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArcheryAlley.Models
{
    public class ClassRegistrations
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RegistrationId { get; set; }

        [Required]
        [StringLength(150)]
        public string CustomerEmail { get; set; }

        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; }

        [Required]
        [StringLength(50)]
        public string PackageType { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PackagePrice { get; set; }
        [Required]
        [StringLength(100)]
        public string LearningMethod { get; set; }
        public int LearningMethodPax { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal LearningMethodPrice { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal AnnualFee { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentStatus { get; set; }

        [StringLength(100)]
        public string TransactionId { get; set; }

        // Optional FK — null = parent registered for themselves, value = registered for a child
        public int? StudentId { get; set; }

        [ForeignKey("StudentId")]
        public Students? Student { get; set; }
    }
}
