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
    }
}