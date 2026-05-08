using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ArcheryAlley.Models;
using System.Linq;

namespace ArcheryAlley.Controllers
{
    public class ManagementController : Controller
    {
        private readonly IArcheryAlleyRepository _repository;

        public ManagementController(IArcheryAlleyRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public IActionResult ManageLanes()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin") return RedirectToAction("Login", "Account");
            
            // Seed if empty
            _repository.SeedDefaultLanesAndTargets();
            
            var lanes = _repository.GetAllLanes();
            var targets = _repository.GetAllTargets();
            
            ViewBag.Targets = targets;
            return View(lanes);
        }

        [HttpPost]
        public IActionResult ToggleLane(int laneId)
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin") return Json(new { success = false });
            _repository.ToggleLaneStatus(laneId);
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult ToggleTarget(int targetId)
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin") return Json(new { success = false });
            _repository.ToggleTargetStatus(targetId);
            return Json(new { success = true });
        }
    }
}
