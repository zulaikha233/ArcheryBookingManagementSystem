using System;

namespace ArcheryAlley.Models
{
    public class PerformanceReports 
    {
        public int ReportId { get; set; }
        public string StudentName { get; set; }
        public string LevelCategory { get; set; }
        public string ReportText { get; set; }
        public string CoachName { get; set; }
        public string EmpId { get; set; }
        public DateTime ReportDate { get; set; }



    }
}
