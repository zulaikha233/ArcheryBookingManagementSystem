using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArcheryAlley.Models
{
    public class Payments
    {
        [Key]
        public int PaymentId { get; set; }

        public int ReservationId { get; set; }
        
        [ForeignKey("ReservationId")]
        public virtual Reservations Reservation { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(20)]
        public string PaymentMethod { get; set; } // FPX, Card, Cash

        [MaxLength(50)]
        public string TransactionId { get; set; }

        public DateTime PaymentDate { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } // Pending, Success, Failed, Refunded
    }
}
