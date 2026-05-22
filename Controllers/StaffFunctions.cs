using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ArcheryAlley.Controllers
{
    public class StaffFunctions : Controller
    {
        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role))
                return RedirectToAction("Login", "Account");

            ViewBag.StaffName = HttpContext.Session.GetString("UserName");
            return View("~/Views/Staff/StudentAttendance.cshtml");
        }
    }
}
