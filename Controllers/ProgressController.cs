using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using ArcheryAlley.Models;
using System.Linq;

namespace ArcheryAlley.Controllers
{
    public class ProgressController : Controller
    {
        private readonly IArcheryAlleyRepository _repository;

        public ProgressController(IArcheryAlleyRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public IActionResult MemberProgress()
        {
            string email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("CustomerLogin", "Account");

            ViewBag.CustomerName = HttpContext.Session.GetString("CustomerName");

            var parent = _repository.GetCustomerByEmail(email);
            if (parent != null)
            {
                var students = _repository.GetStudentsByParentId(parent.CustomerId).ToList();
                ViewBag.Students = students;
            }

            return View();
        }
    }
}
