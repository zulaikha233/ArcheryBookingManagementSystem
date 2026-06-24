using System;

namespace ArcheryAlley.Models
{
    public class ArcherViewModel
    {
        public int StudentId { get; set; }
        public string FullName { get; set; }
        public string ICNumber { get; set; }
        public DateTime? Birthday { get; set; }
        public int? Age { get; set; }
        public ClassRegistrations ClassReg { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsExpired { get; set; }
    }
}
