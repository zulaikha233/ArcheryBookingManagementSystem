using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ArcheryAlley.Models;
using System;
using System.Linq;

namespace ArcheryAlley.Controllers
{
    public class SlotController : Controller
    {
        private readonly IArcheryAlleyRepository _repository;

        public SlotController(IArcheryAlleyRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public IActionResult ManageSlots()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin") return RedirectToAction("Login", "Account");
            var slots = _repository.GetBookingSlots();
            return View(slots);
        }

        [HttpGet]
        public IActionResult AddSlot()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin") return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost]
        public IActionResult AddSlot(string SST, string SET)
        {
            try
            {
                TimeSpan startTime = TimeSpan.Parse(SST);
                TimeSpan endTime = TimeSpan.Parse(SET);

                var existingSlot = _repository.GetBookingSlots()
                    .FirstOrDefault(s => s.SlotStartTime == startTime && s.SlotEndTime == endTime);

                if (existingSlot != null)
                {
                    ViewBag.ErrorMessage = "This slot already exists.";
                    return View();
                }

                _repository.AddSlot(startTime, endTime);
                TempData["SuccessTitle"] = "SLOT ADDED";
                TempData["SuccessMessage"] = "The new arena slot has been successfully created and is now available for booking.";
                return RedirectToAction("Success", "Home");
            }
            catch (Exception ex)
            {
                return RedirectToAction("Exception", "Home");
            }
        }

        [HttpPost]
        public IActionResult DeleteSlot(int SlotId)
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return Json(new { success = false, message = "Unauthorized" });
            try
            {
                var slot = _repository.GetBookingSlots().FirstOrDefault(s => s.SlotId == SlotId);
                if (slot == null)
                    return Json(new { success = false, message = "Slot not found." });

                _repository.DeleteSlot(SlotId);
                return Json(new { success = true });
            }
            catch
            {
                return Json(new { success = false, message = "An error occurred." });
            }
        }

        [HttpPost]
        public IActionResult EditSlot(int SlotId, string SST, string SET)
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return Json(new { success = false, message = "Unauthorized" });
            try
            {
                if (!TimeSpan.TryParse(SST, out TimeSpan newStart) || !TimeSpan.TryParse(SET, out TimeSpan newEnd))
                    return Json(new { success = false, message = "Invalid time format." });

                _repository.UpdateSlot(SlotId, newStart, newEnd);
                return Json(new {
                    success  = true,
                    start    = newStart.ToString(@"hh\:mm"),
                    end      = newEnd.ToString(@"hh\:mm")
                });
            }
            catch
            {
                return Json(new { success = false, message = "An error occurred." });
            }

        }

        [HttpGet]
        public IActionResult AdminDashboard()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role))
                return RedirectToAction("Login", "Account");

            if (role != "Admin")
                return RedirectToAction("StaffDashboard", "Booking");

            ViewBag.AdminName = HttpContext.Session.GetString("UserName");
            return View("~/Views/Staff_Admin/AdminDashboard.cshtml");
        }

        [HttpGet]
        public IActionResult AdminProfile()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role))
                return RedirectToAction("Login", "Account");

            var empId = HttpContext.Session.GetString("EmpId");
            var profile = _repository.GetStaffProfile(empId);

            ViewBag.StaffName = profile?.EmpName ?? "";
            ViewBag.EmpId = empId;
            ViewBag.Gender = profile?.Gender ?? "";
            ViewBag.Email = profile?.Email ?? "";
            ViewBag.Phone = profile?.PhoneNumber ?? "";
            ViewBag.EContactName = profile?.EContactName ?? "";
            ViewBag.EContactPhone = profile?.EContactNumber ?? "";
            ViewBag.Picture = profile?.ProfilePicture ?? "";

            return View("~/Views/Staff_Admin/AdminProfile.cshtml");
        }
    }
}
