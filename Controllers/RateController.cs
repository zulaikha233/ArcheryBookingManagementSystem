using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ArcheryAlley.Models;
using System;
using System.Linq;

namespace ArcheryAlley.Controllers
{
    public class RateController : Controller
    {
        private readonly IArcheryAlleyRepository _repository;

        public RateController(IArcheryAlleyRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public IActionResult ManageRates()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin") return RedirectToAction("Login", "Account");
            _repository.SeedDefaultRates();
            var rates = _repository.GetAllRates();
            return View(rates);
        }

        [HttpGet]
        public IActionResult AddRate()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin") return RedirectToAction("Login", "Account");
            return View();
        }

        [HttpPost]
        public IActionResult AddRate(string RateCode, string RateName, int RateCategory, int SessionType, decimal BasePrice, decimal? DiscountPercentage, DateTime? ValidFrom, DateTime? ValidTo)
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin") return RedirectToAction("Login", "Account");

            var rate = new ArcheryAlley.Models.Rates
            {
                RateCode           = RateCode?.Trim().ToUpper() ?? "RATE-NEW",
                RateName           = RateName?.Trim() ?? "New Rate",
                RateCategory       = RateCategory,
                SessionType        = SessionType,
                BasePrice          = BasePrice,
                DiscountPercentage = DiscountPercentage,
                IsActive           = true,
                ValidFrom          = RateCategory == 3 ? ValidFrom : null,
                ValidTo            = RateCategory == 3 ? ValidTo   : null
            };

            string empId = HttpContext.Session.GetString("EmpId") ?? "SYSTEM";
            _repository.AddRate(rate, empId);

            TempData["SuccessTitle"]   = "RATE CREATED";
            TempData["SuccessMessage"] = $"New rate [{rate.RateCode}] \"{rate.RateName}\" has been successfully added.";
            return RedirectToAction("Success", "Home");
        }

        [HttpPost]
        public IActionResult DeleteRate(int id)
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return Json(new { success = false, message = "Unauthorized" });

            var rate = _repository.GetRateById(id);
            if (rate == null)
                return Json(new { success = false, message = "Rate not found" });

            string name = rate.RateName;
            _repository.DeleteRate(id);
            return Json(new { success = true, message = $"Rate \"{name}\" deleted." });
        }

        [HttpGet]
        public IActionResult EditRate(int id)
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin") return RedirectToAction("Login", "Account");
            var rate = _repository.GetRateById(id);
            if (rate == null) return RedirectToAction("ManageRates");
            return View(rate);
        }

        [HttpPost]
        public IActionResult EditRate(int RateId, string RateCode, string RateName, decimal BasePrice, decimal? DiscountPercentage, bool IsActive, DateTime? ValidFrom, DateTime? ValidTo)
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin") return RedirectToAction("Login", "Account");

            var rate = _repository.GetRateById(RateId);
            if (rate == null) return RedirectToAction("ManageRates");

            rate.RateCode          = RateCode?.Trim().ToUpper() ?? rate.RateCode;
            rate.RateName          = RateName?.Trim() ?? rate.RateName;
            rate.BasePrice         = BasePrice;
            rate.DiscountPercentage = DiscountPercentage;
            rate.IsActive          = IsActive;
            rate.ValidFrom         = rate.RateCategory == 3 ? ValidFrom : null;
            rate.ValidTo           = rate.RateCategory == 3 ? ValidTo   : null;

            string empId = HttpContext.Session.GetString("EmpId") ?? "SYSTEM";
            _repository.SaveRate(rate, empId);

            TempData["SuccessTitle"]   = "RATE UPDATED";
            TempData["SuccessMessage"] = $"Rate \"{rate.RateName}\" [{rate.RateCode}] has been successfully updated.";
            return RedirectToAction("Success", "Home");
        }

        [HttpGet]
        public JsonResult GetActiveRates()
        {
            _repository.SeedDefaultRates();
            var today = DateTime.Today;

            var rates = _repository.GetAllRates()
                .Where(r => r.IsActive)
                .Where(r =>
                    r.RateCategory != 3 ||
                    (
                        (!r.ValidFrom.HasValue || today >= r.ValidFrom.Value.Date) &&
                        (!r.ValidTo.HasValue   || today <= r.ValidTo.Value.Date)
                    )
                )
                .Select(r => new
                {
                    rateId       = r.RateId,
                    rateCode     = r.RateCode,
                    rateName     = r.RateName,
                    rateCategory = r.RateCategory,
                    sessionType  = r.SessionType, 
                    finalPrice   = r.FinalPrice,
                    discountPct  = r.DiscountPercentage
                })
                .ToList();

            return Json(rates);
        }

        [HttpGet]
        public IActionResult ValidatePromoCode(string code, string date, int? slotId)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Json(new { valid = false, message = "Please enter a promo code." });

            var today = DateTime.Today;

            var rate = _repository.GetAllRates()
                .FirstOrDefault(r =>
                    r.RateCode.ToUpper() == code.Trim().ToUpper() &&
                    r.RateCategory == 3 &&   
                    r.IsActive);

            if (rate == null)
                return Json(new { valid = false, message = "Invalid promo code. Please try again." });

            if (rate.ValidFrom.HasValue && today < rate.ValidFrom.Value.Date)
                return Json(new { valid = false, message = $"This promo is not yet active. Valid from {rate.ValidFrom:dd MMM yyyy}." });

            if (rate.ValidTo.HasValue && today > rate.ValidTo.Value.Date)
                return Json(new { valid = false, message = $"This promo has expired on {rate.ValidTo:dd MMM yyyy}." });

            return Json(new {
                valid        = true,
                rateCode     = rate.RateCode,
                rateName     = rate.RateName,
                sessionType  = rate.SessionType,
                finalPrice   = rate.FinalPrice,
                discountPct  = rate.DiscountPercentage,
                message      = $"{rate.RateName} applied! RM {rate.FinalPrice:0.00}/hr"
            });
        }

        [HttpPost]
        public IActionResult ToggleRate(int id)
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return Json(new { success = false, message = "Unauthorized" });

            string empId = HttpContext.Session.GetString("EmpId") ?? "SYSTEM";
            _repository.ToggleRateActive(id, empId);
            var rate = _repository.GetRateById(id);
            return Json(new { success = true, isActive = rate?.IsActive ?? false });
        }
    }
}
