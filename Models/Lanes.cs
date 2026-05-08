using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ArcheryAlley.Models
{
    public class Lanes
    {
        [Key]
        public int LaneId { get; set; }

        [Required]
        public int LaneNumber { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } // Active, Maintenance, Closed

        // Relationship to Targets
        public virtual ICollection<Targets> Targets { get; set; }
    }
}
