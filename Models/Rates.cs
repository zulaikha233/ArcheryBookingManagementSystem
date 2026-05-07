using System;

namespace ArcheryAlley.Models
{
    public class Rates
    {
        public int RateId { get; set; }

        /// <summary>
        /// Rate type code: e.g. RATE-SN, RATE-MN, RATE-SD, RATE-MD, RATE-SS, RATE-MS, RATE-FOC
        /// </summary>
        public string RateCode { get; set; }

        /// <summary>
        /// Human-readable rate name: e.g. "Siang (Normal)"
        /// </summary>
        public string RateName { get; set; }

        /// <summary>
        /// Rate category: 1 = Normal, 2 = Discount, 3 = Special, 4 = FOC
        /// </summary>
        public int RateCategory { get; set; }

        /// <summary>
        /// Session type: 1 = Siang (Day), 2 = Malam (Night), 0 = All (FOC)
        /// </summary>
        public int SessionType { get; set; }

        /// <summary>
        /// Base price per hour (RM)
        /// </summary>
        public decimal BasePrice { get; set; }

        /// <summary>
        /// Discount percentage (0-100). Only applicable for discount rates.
        /// </summary>
        public decimal? DiscountPercentage { get; set; }

        /// <summary>
        /// Computed final price after discount. Stored for quick lookup.
        /// </summary>
        public decimal FinalPrice { get; set; }

        /// <summary>
        /// Whether this rate is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Last updated timestamp
        /// </summary>
        public DateTime UpdatedOn { get; set; } = DateTime.Now;

        /// <summary>
        /// Promo validity start date. Only applicable for Special rates (RateCategory = 3).
        /// </summary>
        public DateTime? ValidFrom { get; set; }

        /// <summary>
        /// Promo validity end date. Only applicable for Special rates (RateCategory = 3).
        /// </summary>
        public DateTime? ValidTo { get; set; }

        /// <summary>
        /// Updated by which admin EmpId
        /// </summary>
        public string? UpdatedBy { get; set; }

    }
}
