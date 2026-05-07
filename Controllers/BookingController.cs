using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ArcheryAlley.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ArcheryAlley.Controllers
{
    public class BookingController : Controller
    {
        private readonly IArcheryAlleyRepository _repository;

        public BookingController(IArcheryAlleyRepository repository)
        {
            _repository = repository;
        }

        public IActionResult GetFreeSlots()
        {
            ViewBag.CustomerEmail = HttpContext.Session.GetString("CustomerEmail");
            ViewBag.CustomerName = HttpContext.Session.GetString("CustomerName");
            return View();
        }

        [HttpGet]
        public IActionResult SelectionLane(int slotId, string name, string email, string date, int duration, string ageGroup)
        {
            ViewBag.SlotId = slotId;
            ViewBag.Name = name;
            ViewBag.Email = email;
            ViewBag.Date = date;
            ViewBag.Duration = duration;
            ViewBag.AgeGroup = ageGroup;
            return View();
        }

        [HttpGet]
        public JsonResult GetAvailableSlotsAjax(DateTime date)
        {
            var slots = _repository.GetBookingSlots();
            
            var cutoff = new TimeSpan(18, 0, 0);

            var morningSlots = slots
                .Where(s => s.SlotStartTime < cutoff)
                .OrderBy(s => s.SlotStartTime)
                .Select((s, index) => new {
                    slotId = s.SlotId,
                    slotStartTime = s.SlotStartTime.ToString(@"hh\:mm"),
                    slotEndTime = s.SlotEndTime.ToString(@"hh\:mm"),
                    isNight = false,
                    label = $"Slot {index + 1}"
                }).ToList();

            var nightSlots = slots
                .Where(s => s.SlotStartTime >= cutoff)
                .OrderBy(s => s.SlotStartTime)
                .Select((s, index) => new {
                    slotId = s.SlotId,
                    slotStartTime = s.SlotStartTime.ToString(@"hh\:mm"),
                    slotEndTime = s.SlotEndTime.ToString(@"hh\:mm"),
                    isNight = true,
                    label = $"Slot {index + 1}"
                }).ToList();

            return Json(morningSlots.Concat(nightSlots));
        }

        [HttpGet]
        public JsonResult GetAvailableTargetsAjax(int slotId, DateTime date, int duration = 1)
        {
            var targets = _repository.GetAvailableTargets(slotId, date, duration);
            return Json(targets);
        }

        [HttpGet]
        public IActionResult BookSlots(int SlotId)
        {
            var slot = _repository.GetBookingSlots().Find(s => s.SlotId == SlotId);

            if (slot == null)
            {
                return RedirectToAction("Exception", "Home");
            }

            return View(slot);
        }

        [HttpPost]
        public IActionResult ConfirmBooking(int SlotId, string CustomerName, string EmpId)
        {
            var existingReservation = _repository.GetReservations()
                .FirstOrDefault(r => r.SlotId == SlotId && r.Status == 1 && r.ReservedOn.Date == DateTime.Today.Date);

            if (existingReservation != null)
            {
                return RedirectToAction("Exception", "Home");
            }
            else
            {
                _repository.BookSlots(SlotId, EmpId, CustomerName);

                return RedirectToAction("Success", "Home");
            }
        }

        [HttpPost]
        public IActionResult Payment(ArcheryAlley.Models.PaymentViewModel model)
        {
            // Populate Time string from SlotId
            if (model.SlotId > 0)
            {
                var slot = _repository.GetBookingSlots().FirstOrDefault(s => s.SlotId == model.SlotId);
                if (slot != null)
                {
                    model.Time = $"{slot.SlotStartTime.ToString(@"hh\:mm")} - {slot.SlotEndTime.ToString(@"hh\:mm")}";
                }
            }

            // Populate TargetDetails for summary
            if (!string.IsNullOrEmpty(model.SelectedLanes))
            {
                model.TargetDetails = $"Lane(s): {model.SelectedLanes}";
            }

            // Default pax to 1 if not provided (since we removed pax selection)
            if (model.NumberOfPax <= 0) model.NumberOfPax = 1;

            return View(model);
        }

        [HttpPost]
        public IActionResult CreateCustomerBooking(int SlotId, string CustomerName, string CustomerEmail, int TargetNo, int RangeNo, int Duration, decimal TotalPrice, string SelectedLanes, string SelectedLaneRanges, int NumberOfPax, string RateCode)
        {
            try
            {
                var laneNumbers = SelectedLanes.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
                
                var laneRangeMap = new Dictionary<int, int>();
                if (!string.IsNullOrEmpty(SelectedLaneRanges))
                {
                    var pairs = SelectedLaneRanges.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach(var pair in pairs)
                    {
                        var parts = pair.Split(':');
                        if(parts.Length == 2 && int.TryParse(parts[0], out int laneId) && int.TryParse(parts[1], out int rId))
                        {
                            laneRangeMap[laneId] = rId;
                        }
                    }
                }
                
                decimal splitPrice = TotalPrice / laneNumbers.Count; 

                string reservedBy = HttpContext.Session.GetString("EmpId") ?? CustomerEmail;

                foreach (var lane in laneNumbers)
                {
                    int specificRange = laneRangeMap.ContainsKey(lane) ? laneRangeMap[lane] : RangeNo;

                    var reservation = new Reservations
                    {
                        SlotId        = SlotId,
                        CustomerName  = CustomerName,
                        CustomerEmail = CustomerEmail,
                        ReservedBy    = reservedBy,
                        TargetNo      = lane,
                        RangeNo       = specificRange,
                        DurationHours = Duration,
                        TotalPrice    = splitPrice,
                        RateCode      = string.IsNullOrWhiteSpace(RateCode) ? null : RateCode,
                        ReservedOn    = DateTime.Now,
                        NumberOfPax   = NumberOfPax,
                        Status        = 1
                    };
                    _repository.CreateReservation(reservation);
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.InnerException?.Message
                         ?? ex.InnerException?.Message
                         ?? ex.Message;
                return Json(new { success = false, message = inner });
            }
        }

        public IActionResult BookingHistory()
        {
            var history = _repository.GetReservations();
            var allSlots = _repository.GetBookingSlots();

            foreach (var res in history)
            {
                res.Slot = allSlots.FirstOrDefault(s => s.SlotId == res.SlotId);
            }

            var grouped = history
                .GroupBy(r => new
                {
                    r.CustomerName,
                    r.SlotId,
                    Date = r.ReservedOn.Date,
                    r.ReservedBy,
                    r.DurationHours,
                    r.RateCode,
                    r.Status
                })
                .Select(g => new ArcheryAlley.Models.BookingHistoryViewModel
                {
                    GroupId       = g.Min(r => r.ReservationId),
                    CustomerName  = g.Key.CustomerName,
                    CustomerEmail = g.First().CustomerEmail,
                    ReservedBy    = g.Key.ReservedBy,
                    ReservedOn    = g.First().ReservedOn,
                    Slot          = g.First().Slot,
                    TargetNos     = g.Select(r => r.TargetNo).OrderBy(t => t).ToList(),
                    RangeNos      = g.Select(r => r.RangeNo).Distinct().OrderBy(r => r).ToList(),
                    DurationHours = g.Key.DurationHours,
                    TotalPrice    = g.Sum(r => r.TotalPrice),
                    RateCode      = g.Key.RateCode,
                    NumberOfPax   = g.Sum(r => r.NumberOfPax),
                    Status        = g.Key.Status
                })
                .OrderByDescending(g => g.ReservedOn)
                .ToList();

            return View(grouped);
        }

        [HttpGet]
        public IActionResult StaffBooking()
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(role))
                return RedirectToAction("Login", "Account");

            if (role == "Admin")
                return RedirectToAction("ManageSlots", "Slot");

            ViewBag.StaffName = HttpContext.Session.GetString("UserName");
            return View();
        }
    }
}
