using System;
using System.ComponentModel.DataAnnotations;

namespace ArcheryAlley.Models
{
    public class CoachAttendance
    {
        [Key]
        public int AttendanceId { get; set; }
        public string EmpId { get; set; }
        public DateTime Date { get; set; }
        public DateTime? ClockInTime { get; set; }
        public DateTime? ClockOutTime { get; set; }
        public string Status { get; set; } = "Present";
    }
}