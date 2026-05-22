using System.Data;
using System;
using System.Collections.Generic;

namespace ArcheryAlley.Models
{
    public class Reservations
    {
        public int ReservationId { get; set; }
        public string? ReservedBy { get; set; } // EmpId or Customer Email
        public string CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public DateTime ReservedOn { get; set; }
        public int NumberOfPax { get; set; } // Max 4 per target
        public int SlotId { get; set; }
        public int TargetNo { get; set; } // 1-20
        public int RangeNo { get; set; } // 1-8
        public int DurationHours { get; set; } // 1-4
        public decimal TotalPrice { get; set; }
        public string? RateCode { get; set; }   // e.g. RATE-SN, RATE-FOC
        public int? Status { get; set; }
        
        public BookingSlots Slot { get; set; }
        public bool Attended { get; set; } = false;

    }
}
