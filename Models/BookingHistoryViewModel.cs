using System;
using System.Collections.Generic;

namespace ArcheryAlley.Models
{
    /// <summary>
    /// Groups multiple reservations (same customer, same slot, same date) into a single display row.
    /// One booking session can span multiple targets (lanes).
    /// </summary>
    public class BookingHistoryViewModel
    {
        // The lowest ReservationId in the group (used as the group reference ID)
        public int GroupId { get; set; }

        public string CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? ReservedBy { get; set; }

        public DateTime ReservedOn { get; set; }

        public BookingSlots Slot { get; set; }

        // All target/lane numbers in this booking group (e.g. [13, 14])
        public List<int> TargetNos { get; set; } = new();

        // All range numbers assigned
        public List<int> RangeNos { get; set; } = new();

        public int DurationHours { get; set; }

        // Sum of all TotalPrice within the group
        public decimal TotalPrice { get; set; }

        public string? RateCode { get; set; }

        public int NumberOfPax { get; set; }

        public int? Status { get; set; }
        public string? ShooterName { get; set; }
        public bool Attended { get; set; }
        public string? AbsentReason { get; set; }

        // Helper: display lanes as comma-separated string e.g. "Lane 13, Lane 14"
        public string LanesDisplay => string.Join(", ", TargetNos.ConvertAll(t => $"Lane {t}"));

        // Helper: display ranges as comma-separated string e.g. "Range 1, Range 2"
        public string RangesDisplay => string.Join(", ", RangeNos.ConvertAll(r => $"Range {r}"));

        // Helper: is this an online booking or staff booking?
        public bool IsOnline => ReservedBy != null && ReservedBy.Contains("@");
    }
}
