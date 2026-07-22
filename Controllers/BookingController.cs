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
            string email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("CustomerLogin", "Account");

            var parent = _repository.GetCustomerByEmail(email);
            var students = parent != null ? _repository.GetStudentsByParentId(parent.CustomerId).Where(s => {
                if (s.Status != "Registered") return false;
                
                // Exclude students who have a class registration with a pending payment status
                var classReg = _repository.GetClassRegistrationByStudentId(s.StudentId);
                if (classReg != null && (classReg.PaymentStatus == "Pending" || classReg.PaymentStatus == "Pending Payment"))
                {
                    return false;
                }
                return true;
            }).ToList() : new List<Students>();

            bool isParentActive = HttpContext.Session.GetString("CustomerStatus") == "Active";
            
            if (!isParentActive)
            {
                TempData["ErrorMessage"] = "Your account is Inactive. Please pay your Annual Membership fee (RM 80.00) in the Fee/Payment section to activate your account and access target bookings.";
                return RedirectToAction("MemberDashboard", "Account");
            }

            _repository.SeedFixedSlots();
            HttpContext.Session.SetString("IsGuest", "false");
            ViewBag.CustomerEmail = email;
            ViewBag.CustomerName = HttpContext.Session.GetString("CustomerName");
            ViewBag.CustomerPhone = HttpContext.Session.GetString("CustomerPhone");
            ViewBag.Students = students;
            ViewBag.ParentActive = isParentActive;

            return View("~/Views/Booking/MemberTargetBooking.cshtml");
        }

        public IActionResult ClassBooking()
        {
            string email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("CustomerLogin", "Account");

            if (HttpContext.Session.GetString("CustomerStatus") != "Active")
            {
                TempData["ErrorMessage"] = "Your account is Inactive. Please pay your Annual Membership fee (RM 80.00) in the Fee/Payment section to activate your account and access class bookings.";
                return RedirectToAction("MemberDashboard", "Account");
            }

            var classRegs = _repository.GetClassRegistrationsByEmail(email);
            bool hasClassReg = classRegs.Any(cr => cr.PaymentStatus == "Success" && cr.PackageType != "Annual Membership");
            if (!hasClassReg)
            {
                TempData["ErrorMessage"] = "You must register for a class before you can book class sessions.";
                return RedirectToAction("MemberDashboard", "Account");
            }

            var parent = _repository.GetCustomerByEmail(email);
            int? selfStudentId = null;
            if (parent != null)
            {
                var selfStudent = _repository.GetStudentsByParentId(parent.CustomerId).FirstOrDefault(s => s.FullName.ToLower() == parent.FullName?.ToLower());
                selfStudentId = selfStudent?.StudentId;
            }

            var students = parent != null ? _repository.GetStudentsByParentId(parent.CustomerId).Where(s => {
                var classReg = _repository.GetClassRegistrationByStudentId(s.StudentId);
                return s.StudentId != selfStudentId && classReg != null && classReg.PaymentStatus == "Success" && classReg.PackageType != "Annual Membership";
            }).ToList() : new List<Students>();

            bool parentHasClass = classRegs.Any(cr => cr.PaymentStatus == "Success" && cr.PackageType != "Annual Membership" && (cr.StudentId == null || cr.StudentId == selfStudentId));

            _repository.SeedFixedSlots();
            HttpContext.Session.SetString("IsGuest", "false");
            ViewBag.CustomerEmail = email;
            ViewBag.CustomerName = HttpContext.Session.GetString("CustomerName");
            ViewBag.CustomerPhone = HttpContext.Session.GetString("CustomerPhone");
            ViewBag.ParentHasClass = parentHasClass;
            ViewBag.Students = students;
            return View("~/Views/Booking/MemberClassBooking.cshtml");
        }

        [HttpPost]
        public IActionResult CreateClassBooking([FromForm] List<int> slotIds, string date, [FromForm] List<int> studentIds = null)
        {
            try
            {
                string email = HttpContext.Session.GetString("CustomerEmail");
                string name = HttpContext.Session.GetString("CustomerName");

                if (string.IsNullOrEmpty(email))
                    return Json(new { success = false, message = "User not logged in." });

                if (studentIds == null || !studentIds.Any())
                    return Json(new { success = false, message = "No archers selected." });

                if (slotIds == null || !slotIds.Any())
                    return Json(new { success = false, message = "No slots selected." });

                DateTime bookingDate;
                if (!DateTime.TryParse(date, out bookingDate))
                    return Json(new { success = false, message = "Invalid date format." });

                var slots = _repository.GetBookingSlots();
                string transactionId = "CLASS-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

                foreach (var sId in slotIds)
                {
                    var slot = slots.FirstOrDefault(s => s.SlotId == sId);
                    if (slot == null) continue;

                    var slotTime = slot.SlotStartTime;
                    DateTime finalReservedOn = bookingDate.Date.Add(slotTime);

                    var existingReservations = _repository.GetReservationsByEmail(email);

                    foreach (var studentId in studentIds)
                    {
                        int? targetStudentId = studentId == 0 ? (int?)null : studentId;
                        
                        bool hasConflict = existingReservations.Any(r => 
                            r.ReservedOn.Date == finalReservedOn.Date && 
                            r.SlotId == sId && 
                            r.StudentId == targetStudentId &&
                            (r.RateCode == "Class Session" || r.RateCode == "CLASS"));

                        if (hasConflict)
                        {
                            string archerName = targetStudentId == null ? name : _repository.GetStudentById(targetStudentId.Value)?.FullName ?? "This archer";
                            return Json(new { success = false, message = $"{archerName} has already booked." });
                        }
                        
                        var reservation = new Reservations
                        {
                            SlotId = sId,
                            CustomerName = name,
                            CustomerEmail = email,
                            ReservedBy = email,
                            DurationHours = 2, // Classes are always 2-hour slots
                            TotalPrice = 0, // Classes are pre-paid
                            ReservedOn = finalReservedOn,
                            NumberOfPax = 1,
                            Status = 1,
                            RateCode = "Class Session",
                            StudentId = targetStudentId
                        };

                        _repository.CreateReservation(reservation);

                        var payment = new Payments
                        {
                            ReservationId = reservation.ReservationId,
                            Amount = 0,
                            PaymentMethod = "Class Package",
                            PaymentDate = DateTime.Now,
                            Status = "Success",
                            TransactionId = transactionId
                        };
                        _repository.AddPayment(payment);
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult MemberCalendar()
        {
            _repository.SeedFixedSlots();
            string email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("CustomerLogin", "Account");

            ViewBag.CustomerName = HttpContext.Session.GetString("CustomerName");
            
            var rawReservations = _repository.GetReservationsByEmail(email);
            ViewBag.Reservations = rawReservations.Select(r => new {
                ReservationId = r.ReservationId,
                CustomerName = r.CustomerName,
                CustomerEmail = r.CustomerEmail,
                ReservedOn = r.ReservedOn,
                TargetNo = r.TargetNo,
                RangeNo = r.RangeNo,
                DurationHours = r.DurationHours,
                TotalPrice = r.TotalPrice,
                RateCode = r.RateCode,
                Status = r.Status,
                Student = r.Student != null ? new { FullName = r.Student.FullName } : null,
                Slot = r.Slot != null ? new {
                    SlotId = r.Slot.SlotId,
                    SlotStartTime = r.Slot.SlotStartTime.ToString(@"hh\:mm"),
                    SlotEndTime = r.Slot.SlotEndTime.ToString(@"hh\:mm"),
                    IsNight = r.Slot.SlotStartTime.Hours >= 18
                } : null
            }).ToList();

            return View("~/Views/Calendar/MemberCalendar.cshtml");
        }


        public IActionResult GuestBooking()
        {
            _repository.SeedFixedSlots();
            HttpContext.Session.SetString("IsGuest", "true");
            return View("~/Views/Guest/GuestBooking.cshtml");
        }

        [HttpGet]
        public IActionResult SelectTarget(int slotId, string name, string email, string phone, string date, int duration, int numberOfPax, string sessionType = "", int? studentId = null)
        {
            if (!string.IsNullOrEmpty(sessionType))
            {
                HttpContext.Session.SetString("SessionType", sessionType);
            }
            else if (HttpContext.Session.GetString("IsGuest") == "true")
            {
                HttpContext.Session.SetString("SessionType", "class_trial");
            }

            ViewBag.IsGuest  = HttpContext.Session.GetString("IsGuest") == "true";
            ViewBag.SlotId   = slotId;
            ViewBag.Name     = name;
            ViewBag.Email    = email;
            ViewBag.Phone    = phone;
            ViewBag.Date     = date;
            ViewBag.Duration = duration;
            ViewBag.NumberOfPax = numberOfPax;
            ViewBag.StudentId = studentId;

            if (ViewBag.IsGuest)
                return View("~/Views/Guest/SelectTarget.cshtml");

            return View();
        }

        [HttpPost]
        public IActionResult SelectLane(int slotId, string name, string email, string phone, string date, int duration, string targetSize, int targetAmount, int numberOfPax, int? studentId = null)
        {
            ViewBag.IsGuest      = HttpContext.Session.GetString("IsGuest") == "true";
            ViewBag.SlotId       = slotId;
            ViewBag.Name         = name;
            ViewBag.Email        = email;
            ViewBag.Phone        = phone;
            ViewBag.Date         = date;
            ViewBag.Duration     = duration;
            ViewBag.TargetSize   = targetSize;
            ViewBag.TargetAmount = targetAmount;
            ViewBag.NumberOfPax  = numberOfPax;
            ViewBag.StudentId    = studentId;
            ViewBag.SessionType  = HttpContext.Session.GetString("SessionType") ?? "game";

            ViewBag.EnableLaneSelection = SystemSettings.EnableLaneSelection;

            if (ViewBag.IsGuest)
                return View("~/Views/Guest/SelectLane.cshtml");

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
                    label = $"Slot {index + 1}",
                    slotDurationHours = s.SlotEndTime >= s.SlotStartTime ? (s.SlotEndTime - s.SlotStartTime).TotalHours : (s.SlotEndTime - s.SlotStartTime).TotalHours + 24
                }).ToList();

            var nightSlots = slots
                .Where(s => s.SlotStartTime >= cutoff)
                .OrderBy(s => s.SlotStartTime)
                .Select((s, index) => new {
                    slotId = s.SlotId,
                    slotStartTime = s.SlotStartTime.ToString(@"hh\:mm"),
                    slotEndTime = s.SlotEndTime.ToString(@"hh\:mm"),
                    isNight = true,
                    label = $"Slot {index + 1}",
                    slotDurationHours = s.SlotEndTime >= s.SlotStartTime ? (s.SlotEndTime - s.SlotStartTime).TotalHours : (s.SlotEndTime - s.SlotStartTime).TotalHours + 24
                }).ToList();

            return Json(morningSlots.Concat(nightSlots));
        }

        [HttpGet]
        public JsonResult GetAvailableTargetsAjax(int slotId, DateTime date, int duration = 1)
        {
            // Get targets that are physically available (Active & Available)
            var availableTargetNumbers = _repository.GetAvailableTargets(slotId, date, duration);
            
            // Get ALL targets to check their maintenance status
            var allTargets = _repository.GetAllTargets();
            
            var result = allTargets.Select(t => new {
                targetNumber = t.TargetNumber,
                status = t.Status == "Maintenance" || t.Lane.Status == "Maintenance" ? "Maintenance" : 
                         (availableTargetNumbers.Contains(t.TargetNumber) ? "Available" : "Taken")
            }).ToList();

            return Json(result);
        }

        [HttpPost]
        public IActionResult Payment(ArcheryAlley.Models.PaymentViewModel model)
        {
            if (model.SlotId > 0)
            {
                var slots = _repository.GetBookingSlots().OrderBy(s => s.SlotStartTime).ToList();
                var slot = slots.FirstOrDefault(s => s.SlotId == model.SlotId);
                if (slot != null)
                {
                    var cutoff = new TimeSpan(18, 0, 0);
                    
                    string GetSlotLabel(BookingSlots s)
                    {
                        if (s.SlotStartTime < cutoff)
                        {
                            var morningList = slots.Where(x => x.SlotStartTime < cutoff).ToList();
                            var idx = morningList.FindIndex(x => x.SlotId == s.SlotId);
                            return idx >= 0 ? $"Slot {idx + 1}" : "Slot";
                        }
                        else
                        {
                            var nightList = slots.Where(x => x.SlotStartTime >= cutoff).ToList();
                            var idx = nightList.FindIndex(x => x.SlotId == s.SlotId);
                            return idx >= 0 ? $"Slot {idx + 1}" : "Slot";
                        }
                    }

                    var primaryLabel = GetSlotLabel(slot);

                    if (model.Duration == 4)
                    {
                        var isNight = slot.SlotStartTime >= cutoff;
                        var sessionSlots = slots.Where(s => (s.SlotStartTime >= cutoff) == isNight).ToList();
                        var primaryIndex = sessionSlots.FindIndex(s => s.SlotId == slot.SlotId);
                        
                        var nextSlot = (primaryIndex >= 0 && primaryIndex + 1 < sessionSlots.Count) 
                            ? sessionSlots[primaryIndex + 1] 
                            : null;

                        if (nextSlot != null)
                        {
                            var secondaryLabel = GetSlotLabel(nextSlot);
                            model.Time = $"{primaryLabel} & {secondaryLabel} ({slot.SlotStartTime.ToString(@"hh\:mm")} - {nextSlot.SlotEndTime.ToString(@"hh\:mm")})";
                        }
                        else
                        {
                            model.Time = $"{primaryLabel} ({slot.SlotStartTime.ToString(@"hh\:mm")} - {slot.SlotEndTime.ToString(@"hh\:mm")})";
                        }
                    }
                    else
                    {
                        model.Time = $"{primaryLabel} ({slot.SlotStartTime.ToString(@"hh\:mm")} - {slot.SlotEndTime.ToString(@"hh\:mm")})";
                    }
                }
            }

            // Populate TargetDetails for summary
            if (!string.IsNullOrEmpty(model.SelectedLanes))
            {
                model.TargetDetails = $"Lane(s): {model.SelectedLanes}";
            }

            // Default pax to 1 if not provided (since we removed pax selection)
            if (model.NumberOfPax <= 0) model.NumberOfPax = 1;

            var sessionType = HttpContext.Session.GetString("SessionType") ?? "game";
            string serviceName = "Archery Game Session";
            if (sessionType == "class") serviceName = "Archery Class Session";
            else if (sessionType == "self_training") serviceName = "Archery Self Training";
            
            ViewBag.ServiceName = serviceName;
            ViewBag.EnableLaneSelection = SystemSettings.EnableLaneSelection;

            if (model.StudentId.HasValue)
            {
                var student = _repository.GetStudentById(model.StudentId.Value);
                if (student != null)
                {
                    ViewBag.ShooterName = student.FullName;
                }
            }

            return View("~/Views/Payment/MemberBookingPayment.cshtml", model);
        }

        public IActionResult PaymentGuest(int SlotId, string CustomerName, string CustomerEmail, string Date, int Duration, decimal TotalPrice, string SelectedLanes, string TargetSize, int TargetAmount, int NumberOfPax)
        {
            var model = new ArcheryAlley.Models.PaymentViewModel
            {
                SlotId = SlotId,
                CustomerName = CustomerName,
                CustomerEmail = CustomerEmail,
                Date = Date,
                Duration = Duration,
                TotalPrice = TotalPrice,
                SelectedLanes = SelectedLanes,
                TargetSize = TargetSize,
                TargetAmount = TargetAmount,
                NumberOfPax = NumberOfPax
            };

            if (model.SlotId > 0)
            {
                var slots = _repository.GetBookingSlots().OrderBy(s => s.SlotStartTime).ToList();
                var slot = slots.FirstOrDefault(s => s.SlotId == model.SlotId);
                if (slot != null)
                {
                    var cutoff = new TimeSpan(18, 0, 0);
                    
                    string GetSlotLabel(BookingSlots s)
                    {
                        if (s.SlotStartTime < cutoff)
                        {
                            var morningList = slots.Where(x => x.SlotStartTime < cutoff).ToList();
                            var idx = morningList.FindIndex(x => x.SlotId == s.SlotId);
                            return idx >= 0 ? $"Slot {idx + 1}" : "Slot";
                        }
                        else
                        {
                            var nightList = slots.Where(x => x.SlotStartTime >= cutoff).ToList();
                            var idx = nightList.FindIndex(x => x.SlotId == s.SlotId);
                            return idx >= 0 ? $"Slot {idx + 1}" : "Slot";
                        }
                    }

                    var primaryLabel = GetSlotLabel(slot);

                    if (model.Duration == 4)
                    {
                        var isNight = slot.SlotStartTime >= cutoff;
                        var sessionSlots = slots.Where(s => (s.SlotStartTime >= cutoff) == isNight).ToList();
                        var primaryIndex = sessionSlots.FindIndex(s => s.SlotId == slot.SlotId);
                        
                        var nextSlot = (primaryIndex >= 0 && primaryIndex + 1 < sessionSlots.Count) 
                            ? sessionSlots[primaryIndex + 1] 
                            : null;

                        if (nextSlot != null)
                        {
                            var secondaryLabel = GetSlotLabel(nextSlot);
                            model.Time = $"{primaryLabel} & {secondaryLabel} ({slot.SlotStartTime.ToString(@"hh\:mm")} - {nextSlot.SlotEndTime.ToString(@"hh\:mm")})";
                        }
                        else
                        {
                            model.Time = $"{primaryLabel} ({slot.SlotStartTime.ToString(@"hh\:mm")} - {slot.SlotEndTime.ToString(@"hh\:mm")})";
                        }
                    }
                    else
                    {
                        model.Time = $"{primaryLabel} ({slot.SlotStartTime.ToString(@"hh\:mm")} - {slot.SlotEndTime.ToString(@"hh\:mm")})";
                    }
                }
            }

            ViewBag.ServiceName = "Archery Game Session";
            ViewBag.EnableLaneSelection = SystemSettings.EnableLaneSelection;
            return View("~/Views/Guest/PaymentGuest.cshtml", model);
        }

        [HttpPost]
        public IActionResult CreateCustomerBooking(int SlotId, string CustomerName, string CustomerEmail, int TargetNo, int RangeNo, int Duration, decimal TotalPrice, string SelectedLanes, string SelectedLaneRanges, int NumberOfPax, string RateCode, string PaymentMethod, string Date = "", int? StudentId = null)
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

                DateTime bookingDate = DateTime.Now;
                if (!string.IsNullOrEmpty(Date))
                {
                    DateTime.TryParse(Date, out bookingDate);
                }

                // Retrieve slot start time for precision
                var slots = _repository.GetBookingSlots();
                var slot = slots.FirstOrDefault(s => s.SlotId == SlotId);
                var slotTime = slot != null ? slot.SlotStartTime : TimeSpan.Zero;
                DateTime finalReservedOn = bookingDate.Date.Add(slotTime);

                string sharedTxnId = (PaymentMethod == "counter") 
                    ? "PENDING-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper() 
                    : "TXN-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

                int? firstId = null;
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
                        ReservedOn    = finalReservedOn,
                        NumberOfPax   = NumberOfPax,
                        Status        = 1,
                        StudentId     = StudentId
                    };
                    _repository.CreateReservation(reservation);
                    if (firstId == null) firstId = reservation.ReservationId;

                    // Create Payment record for each reservation
                    string actualMethod = PaymentMethod switch {
                        "fpx" => "Online (FPX)",
                        "card" => "Credit/Debit Card",
                        "counter" => "Cash/Counter",
                        _ => "Cash/Counter"
                    };

                    string paymentStatus = (PaymentMethod == "counter") ? "Pending" : "Success";

                    var payment = new Payments
                    {
                        ReservationId = reservation.ReservationId,
                        Amount = splitPrice,
                        PaymentMethod = actualMethod,
                        PaymentDate = DateTime.Now,
                        Status = paymentStatus,
                        TransactionId = sharedTxnId
                    };
                    _repository.AddPayment(payment);
                }

                return Json(new { success = true, bookingCode = firstId?.ToString("D4") });
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



        public IActionResult MemberHistory()
        {
            string email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("CustomerLogin", "Account");

            var allSlots = _repository.GetBookingSlots();
            var history  = _repository.GetReservationsByEmail(email);

            foreach (var res in history)
                res.Slot = allSlots.FirstOrDefault(s => s.SlotId == res.SlotId);

            var grouped = history
                .GroupBy(r => new
                {
                    r.CustomerName,
                    r.SlotId,
                    Date = r.ReservedOn.Date,
                    r.ReservedBy,
                    r.DurationHours,
                    r.RateCode,
                    r.Status,
                    r.StudentId
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
                    Status        = g.Key.Status,
                    ShooterName   = g.First().Student != null ? g.First().Student.FullName : g.Key.CustomerName
                })
                .OrderByDescending(g => g.ReservedOn)
                .ToList();

            return View("~/Views/History/MemberHistory.cshtml", grouped);
        }

        public IActionResult MemberAttendance(string archerName = "")
        {
            string email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("CustomerLogin", "Account");

            var allSlots = _repository.GetBookingSlots();
            var history  = _repository.GetReservationsByEmail(email)
                                      .Where(r => r.RateCode == "CLASS" || r.RateCode == "Class Session").ToList();
            var customer = _repository.GetCustomerByEmail(email);
            var students = _repository.GetStudentsByParentId(customer.CustomerId);

            var classRegs = _repository.GetClassRegistrationsByEmail(email)
                                       .Where(cr => (cr.PaymentStatus == "Success" || cr.PaymentStatus == "Paid") && cr.PackageType != "Annual Membership").ToList();

            int? selfStudentId = null;
            if (customer != null)
            {
                var selfStudent = students.FirstOrDefault(s => s.FullName.ToLower() == customer.FullName?.ToLower());
                selfStudentId = selfStudent?.StudentId;
            }

            if (string.IsNullOrEmpty(archerName))
            {
                bool mainHasReg = classRegs.Any(cr => cr.StudentId == null || cr.StudentId == selfStudentId);
                if (!mainHasReg && students.Any())
                {
                    var firstRegisteredChild = students.FirstOrDefault(s => classRegs.Any(cr => cr.StudentId == s.StudentId));
                    if (firstRegisteredChild != null)
                    {
                        archerName = firstRegisteredChild.FullName;
                    }
                    else
                    {
                        archerName = students.First().FullName;
                    }
                }
                else
                {
                    archerName = customer.FullName;
                }
            }

            foreach (var res in history)
                res.Slot = allSlots.FirstOrDefault(s => s.SlotId == res.SlotId);

            var grouped = history
                .GroupBy(r => new
                {
                    r.CustomerName,
                    r.SlotId,
                    Date = r.ReservedOn.Date,
                    r.ReservedBy,
                    r.DurationHours,
                    r.RateCode,
                    r.Status,
                    r.StudentId
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
                    Status        = g.Key.Status,
                    ShooterName   = g.First().Student != null ? g.First().Student.FullName : g.Key.CustomerName,
                    Attended      = g.First().Attended,
                    AbsentReason  = g.First().AbsentReason
                })
                .OrderByDescending(g => g.ReservedOn)
                .ToList();

            // Populate Archer List from their actual children + themselves, BUT only if they have a Success/Paid class registration
            var archerList = new List<string>();
            if (classRegs.Any(cr => cr.StudentId == null || cr.StudentId == selfStudentId))
            {
                archerList.Add(customer.FullName);
            }
            archerList.AddRange(students.Where(s => classRegs.Any(cr => cr.StudentId == s.StudentId)).Select(s => s.FullName));
            
            // Fallback if no one is registered (just to prevent empty dropdown errors, though UI can hide it)
            if (!archerList.Any())
            {
                archerList.Add(archerName);
            }

            ViewBag.ArcherList = archerList.Distinct().ToList();
            ViewBag.SelectedArcher = archerName;

            // Filter
            var selectedStudent = students.FirstOrDefault(s => s.FullName == archerName);
            ViewBag.SelectedStudentId = selectedStudent?.StudentId;
            
            grouped = grouped.Where(g => g.ShooterName == archerName).ToList();

            classRegs = classRegs.Where(cr => {
                string crA = customer.FullName;
                if (cr.StudentId.HasValue) {
                    var stu = students.FirstOrDefault(s => s.StudentId == cr.StudentId.Value);
                    if (stu != null) crA = stu.FullName;
                }
                return crA == archerName;
            }).ToList();

            var latestReg = classRegs.OrderByDescending(cr => cr.RegistrationDate).FirstOrDefault();
            int totalSlots = 4;
            int attendedSlots = 0;
            bool isExpired = false;

            if (latestReg != null)
            {
                if (DateTime.Now >= latestReg.RegistrationDate.AddMonths(1))
                {
                    isExpired = true;
                }
                
                if (!string.IsNullOrEmpty(latestReg.PackageType))
                {
                    if (latestReg.PackageType.Contains("4 Slot", StringComparison.OrdinalIgnoreCase)) totalSlots = 4;
                    else if (latestReg.PackageType.Contains("6 Slot", StringComparison.OrdinalIgnoreCase)) totalSlots = 6;
                    else if (latestReg.PackageType.Contains("Progressive", StringComparison.OrdinalIgnoreCase)) totalSlots = 6;
                }

                int previousCapacity = 0;
                var previousRegs = classRegs.Where(cr => cr.RegistrationId != latestReg.RegistrationId).ToList();
                foreach(var pr in previousRegs)
                {
                    if (!string.IsNullOrEmpty(pr.PackageType))
                    {
                        if (pr.PackageType.Contains("4 Slot", StringComparison.OrdinalIgnoreCase)) previousCapacity += 4;
                        else if (pr.PackageType.Contains("6 Slot", StringComparison.OrdinalIgnoreCase)) previousCapacity += 6;
                        else if (pr.PackageType.Contains("Progressive", StringComparison.OrdinalIgnoreCase)) previousCapacity += 6;
                        else previousCapacity += 4;
                    }
                    else
                    {
                        previousCapacity += 4;
                    }
                }

                int totalAttendedEver = 0;
                foreach(var r in grouped.Where(r => r.RateCode == "CLASS" || r.RateCode == "Class Session"))
                {
                    if (r.Attended) 
                    {
                        totalAttendedEver += Math.Max(1, r.DurationHours / 2);
                    }
                }

                attendedSlots = Math.Max(0, totalAttendedEver - previousCapacity);
            }

            var upcomingClass = grouped.Where(g => (g.RateCode == "CLASS" || g.RateCode == "Class Session") && g.ReservedOn.Date >= DateTime.Now.Date && !g.Attended && g.Status == 1)
                                       .OrderBy(g => g.ReservedOn)
                                       .FirstOrDefault();

            ViewBag.TotalClassSlots = totalSlots;
            ViewBag.AttendedClassSlots = attendedSlots;
            ViewBag.UpcomingClass = upcomingClass;
            ViewBag.IsExpired = isExpired;

            return View("~/Views/Attendance/MemberAttendance.cshtml", grouped);
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
            return View("~/Views/Staff/StaffDashboard.cshtml");
        }
        [HttpGet]
        public JsonResult GetAttendanceByDate(DateTime date)
        {
            var reservations = _repository.GetReservationsByDate(date);

            var result = reservations.Select(r => new {
                id = r.ReservationId,
                name = r.Student != null ? r.Student.FullName : r.CustomerName,
                email = r.CustomerEmail,
                packageType = r.RateCode ?? "General",
                time = r.Slot != null
                                ? $"{r.Slot.SlotStartTime.ToString(@"hh\:mm")} - {r.Slot.SlotEndTime.ToString(@"hh\:mm")}"
                                : "�",
                attended = r.Attended  // the column you added earlier
            });

            return Json(result);
        }

        [HttpPost]
        public JsonResult ToggleAttendance(int reservationId, bool attended)
        {
            try
            {
                var reservation = _repository.GetReservations()
                    .FirstOrDefault(r => r.ReservationId == reservationId);

                if (reservation == null)
                    return Json(new { success = false, message = "Reservation not found" });

                reservation.Attended = attended;
                _repository.UpdateAttendance(reservationId, attended);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult StudentAttendance()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role))
                return RedirectToAction("Login", "Account");

            ViewBag.StaffName = HttpContext.Session.GetString("UserName");
            return View("~/Views/Staff/StudentAttendance.cshtml");
        }

        [HttpGet]
        public IActionResult StaffDashBoard()
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(role))
                return RedirectToAction("Login", "Account");

            if (role == "Admin")
                return RedirectToAction("ManageSlots", "Slot");

            ViewBag.StaffName = HttpContext.Session.GetString("UserName");
            return View("~/Views/Staff/StaffDashboard.cshtml");
        }
        [HttpPost]
        public JsonResult SubmitAbsentReason(int groupId, string reason)
        {
            try
            {
                _repository.UpdateAbsentReason(groupId, reason);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetClockStatus()
        {
            var empId = HttpContext.Session.GetString("EmpId");
            if (string.IsNullOrEmpty(empId))
                return Json(new { status = "none" });

            var attendance = _repository.GetTodayAttendance(empId);

            if (attendance == null)
                return Json(new { status = "none" });

            if (attendance.ClockOutTime.HasValue)
                return Json(new
                {
                    status = "completed",
                    clockIn = attendance.ClockInTime?.ToString("hh:mm tt"),
                    clockOut = attendance.ClockOutTime?.ToString("hh:mm tt")
                });

            return Json(new
            {
                status = "clocked-in",
                clockIn = attendance.ClockInTime?.ToString("hh:mm tt")
            });
        }

        [HttpPost]
        public JsonResult ClockIn()
        {
            var empId = HttpContext.Session.GetString("EmpId");
            if (string.IsNullOrEmpty(empId))
                return Json(new { success = false, message = "Session expired." });

            try
            {
                _repository.ClockIn(empId);
                var time = DateTime.Now.ToString("hh:mm tt");
                return Json(new { success = true, time });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult ClockOut()
        {
            var empId = HttpContext.Session.GetString("EmpId");
            if (string.IsNullOrEmpty(empId))
                return Json(new { success = false, message = "Session expired." });

            try
            {
                _repository.ClockOut(empId);
                var time = DateTime.Now.ToString("hh:mm tt");
                return Json(new { success = true, time });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
