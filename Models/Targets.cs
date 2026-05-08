using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArcheryAlley.Models
{
    public class Targets
    {
        [Key]
        public int TargetId { get; set; }

        [Required]
        public int TargetNumber { get; set; }

        public int LaneId { get; set; }

        [ForeignKey("LaneId")]
        public virtual Lanes Lane { get; set; }

        public int MaxCapacity { get; set; } = 4; // Default 4 pax per target

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } // Available, Maintenance
    }
}
