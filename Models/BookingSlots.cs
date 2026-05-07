using System.Collections.Generic;
using System;

namespace ArcheryAlley.Models
{
    public class BookingSlots
    {
        public BookingSlots()
        {
            Reservations = new HashSet<Reservations>();
        }

        public int SlotId { get; set; }
        public TimeSpan SlotStartTime { get; set; }
        public TimeSpan SlotEndTime { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<Reservations> Reservations { get; set; }
    }
}
