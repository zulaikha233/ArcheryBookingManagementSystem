using System;
using System.Linq;
using ArcheryAlley.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ArcheryAlley.Controllers
{
    public class ReportController : Controller
    {
        private readonly IArcheryAlleyRepository _repository;

        public ReportController(IArcheryAlleyRepository repository)
        {
            _repository = repository;
        }

        
        
        [HttpGet]
        public IActionResult StudentPerformance()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role))
                return RedirectToAction("Login", "Account");

            ViewBag.StaffName = HttpContext.Session.GetString("UserName");
            return View("~/Views/Staff/StudentPerformanceReport.cshtml");
        }
        [HttpGet]
        public JsonResult GetStudentReports(string name, string level)
        {
            var reports = _repository.GetReportsByStudent(name, level);

            var result = reports.Select(r => new {
                reportId = r.ReportId,
                date = r.ReportDate.ToString("yyyy-MM-dd"),
                time = r.ReportDate.ToString("HH:mm"),
                coach = r.CoachName,
                text = r.ReportText
            });

            return Json(result);
        }

        [HttpPost]
        public JsonResult AddReport(string studentName, string level, string reportText)
        {
            try
            {
                var report = new PerformanceReports
                {
                    StudentName = studentName,
                    LevelCategory = level,
                    ReportText = reportText,
                    CoachName = HttpContext.Session.GetString("UserName") ?? "Coach",
                    EmpId = HttpContext.Session.GetString("EmpId") ?? "",
                    ReportDate = DateTime.Now
                };

                _repository.AddPerformanceReport(report);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetAllArchers()
        {
            var archers = _repository.GetAllArchers();

            var result = archers.Select(a => new {
                name = a.FullName,
                level = a.LevelCategory ?? "Unassigned",
                age = a.Age ?? 0
            });
            return Json(result);
        }

        [HttpGet]
        public JsonResult GetStudentByName(string name, string level)
        {
            var archers = _repository.GetAllArchers()
                .Where(a => a.FullName.ToLower().Contains(name.ToLower())
                         && a.LevelCategory == level)
                .Select(a => new {
                    name = a.FullName,
                    level = a.LevelCategory ?? "Unassigned",
                    age = a.Age ?? 0
                })
                .ToList();

            return Json(archers);
        }
    }
}