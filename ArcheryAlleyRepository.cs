#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using ArcheryAlley.Models;
using Azure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ArcheryAlley
{
    public class ArcheryAlleyRepository : IArcheryAlleyRepository
    {
        private readonly ArcheryAlleyDBContext _context;
        private readonly ILogger<ArcheryAlleyRepository> _logger;

        public ArcheryAlleyRepository(ArcheryAlleyDBContext context, ILogger<ArcheryAlleyRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public int CreateReservation(Reservations reservation)
        {
            try
            {
                _context.Reservations.Add(reservation);
                _context.SaveChanges();
                return reservation.ReservationId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating reservation");
                throw;
            }
        }

        public List<int> GetAvailableTargets(int slotId, DateTime date, int duration = 1)
        {
            var allSlots = _context.BookingSlots.Where(s => s.IsActive).OrderBy(s => s.SlotStartTime).ToList();
            var startSlot = allSlots.FirstOrDefault(s => s.SlotId == slotId);
            if (startSlot == null) return new List<int>();

            var startIndex = allSlots.IndexOf(startSlot);
            var requestedSlotIds = allSlots.Skip(startIndex).Take(duration).Select(s => s.SlotId).ToList();

            // Find all reservations for this date
            var reservations = _context.Reservations
                .Where(r => r.ReservedOn.Date == date.Date && r.Status != 2)
                .ToList();

            var occupiedTargets = new HashSet<int>();

            foreach (var res in reservations)
            {
                // Find which slots this reservation occupies
                var resStartSlot = allSlots.FirstOrDefault(s => s.SlotId == res.SlotId);
                if (resStartSlot == null) continue;

                var resStartIndex = allSlots.IndexOf(resStartSlot);
                var resOccupiedSlotIds = allSlots.Skip(resStartIndex).Take(res.DurationHours).Select(s => s.SlotId).ToList();

                // If there's an intersection between requested slots and this reservation's slots
                if (requestedSlotIds.Intersect(resOccupiedSlotIds).Any())
                {
                    occupiedTargets.Add(res.TargetNo);
                }
            }

            // Seed if empty (ensures we have targets in DB)
            SeedDefaultLanesAndTargets();

            // Only get targets where both lane and target are active/available
            var activeTargets = _context.Targets
                .Include(t => t.Lane)
                .Where(t => t.Status == "Available" && t.Lane.Status == "Active")
                .Select(t => t.TargetNumber)
                .ToList();

            return activeTargets.Where(t => !occupiedTargets.Contains(t)).ToList();
        }

        public int BookSlots(int SlotId, string EmpId, string CustomerName, int? StudentId = null)
        {
            var reservation = new Reservations
            {
                SlotId = SlotId,
                ReservedBy = EmpId,
                ReservedOn = DateTime.Now,
                Status = 1,
                CustomerName = CustomerName,
                TargetNo = 1, // Default for old system compatibility
                RangeNo = 1,
                DurationHours = 1,
                TotalPrice = 10, // Default
                StudentId = StudentId
            };

            int resId = CreateReservation(reservation);
            
            var payment = new Payments
            {
                ReservationId = resId,
                Amount = reservation.TotalPrice,
                PaymentMethod = "Cash/Counter",
                PaymentDate = DateTime.Now,
                Status = "Success",
                TransactionId = "POS-" + Guid.NewGuid().ToString().Substring(0, 5).ToUpper()
            };
            AddPayment(payment);

            return resId;
        }





        public string GetAdminName(string role)
        {
            var admin = _context.Roles.FirstOrDefault(r => r.RoleType && r.EmpId == role);
            return admin != null ? admin.EmpName : "Admin not found";
        }

        public List<Reservations> GetReservations()
        {
            return _context.Reservations.ToList();
        }

        public List<BookingSlots> GetBookingSlots()
        {
            return _context.BookingSlots.Where(s => s.IsActive).ToList();
        }

        public List<Reservations> GetReservationDetails()
        {
            return _context.Reservations
                .Select(r => new Reservations
                {
                    ReservationId = r.ReservationId,
                    ReservedBy = r.ReservedBy,
                    ReservedOn = r.ReservedOn,
                    Status = r.Status,
                    CustomerName = r.CustomerName,
                    Slot = r.Slot
                })
                .ToList();
        }

        public List<BookingSlots> AvailableSlots(DateTime date)
        {
            // Get already booked slot IDs for today/the specific date 
            var bookedSlotIds = _context.Reservations
                .Where(r => r.Status == 1 && r.ReservedOn.Date == date.Date)
                .Select(r => r.SlotId)
                .ToList();

            // Return only those slots that are NOT in the booked list and are ACTIVE 
            return _context.BookingSlots
                .Where(s => s.IsActive && !bookedSlotIds.Contains(s.SlotId))
                .ToList();
        }







        public Roles GetRoleByEmpId(string empId)
        {
            // If no roles exist, seed a default admin! 🛡️👑
            if (!_context.Roles.Any())
            {
                var defaultAdmin = new Roles
                {
                    EmpId = "ADMIN01",
                    EmpName = "System Admin",
                    Password = "1234",
                    RoleType = true // Admin
                };
                _context.Roles.Add(defaultAdmin);
                _context.SaveChanges();
            }
            return _context.Roles.FirstOrDefault(r => r.EmpId == empId);
        }






        public void AddSlot(TimeSpan st, TimeSpan et)
        {
            BookingSlots _bs = new BookingSlots()
            {
                SlotStartTime = st,
                SlotEndTime = et,
                IsActive = true
            };

            _context.BookingSlots.Add(_bs);
            _context.SaveChanges();

        }

        public void DeleteSlot(int SlotId)
        {
            var slot = _context.BookingSlots.FirstOrDefault(s => s.SlotId == SlotId);
            if (slot != null)
            {
                slot.IsActive = false;
                _context.SaveChanges();
            }
        }

        public void UpdateSlot(int SlotId, TimeSpan newStart, TimeSpan newEnd)
        {
            var slot = _context.BookingSlots.FirstOrDefault(s => s.SlotId == SlotId);
            if (slot != null)
            {
                slot.SlotStartTime = newStart;
                slot.SlotEndTime   = newEnd;
                _context.SaveChanges();
            }
        }



        public string GetNextEmpId()
        {
            string year = DateTime.Now.ToString("yy");
            string prefix = $"ARC{year}";

            var lastId = _context.Roles
                .Where(r => r.EmpId.StartsWith(prefix))
                .OrderByDescending(r => r.EmpId)
                .Select(r => r.EmpId)
                .FirstOrDefault();

            if (lastId == null)
            {
                return $"{prefix}001";
            }

            try
            {
                // Extract the sequence number (last 3 digits)
                int sequence = int.Parse(lastId.Substring(lastId.Length - 3));
                return $"{prefix}{(sequence + 1).ToString("D3")}";
            }
            catch
            {
                return $"{prefix}001";
            }
        }

        public void RegisterStaff(Roles role)
        {
            if (string.IsNullOrEmpty(role.EmpId))
            {
                role.EmpId = GetNextEmpId();
            }
            _context.Roles.Add(role);
            _context.SaveChanges();
        }

        public void RegisterCustomer(Customers customer)
        {
            _context.Customers.Add(customer);
            _context.SaveChanges();
        }

        public Customers GetCustomerLogin(string email, string password)
        {
            return _context.Customers.FirstOrDefault(c => c.Email == email && c.Password == password);
        }

        public Customers GetCustomerByEmail(string email)
        {
            return _context.Customers.FirstOrDefault(c => c.Email.ToLower() == email.ToLower());
        }

        public void UpdateCustomerStatus(string email, string status)
        {
            var customer = _context.Customers.FirstOrDefault(c => c.Email.ToLower() == email.ToLower());
            if (customer != null)
            {
                customer.Status = status;
                _context.SaveChanges();
            }
        }

        public void UpdateCustomer(Customers customer)
        {
            var existing = _context.Customers.FirstOrDefault(c => c.CustomerId == customer.CustomerId);
            if (existing != null)
            {
                existing.FullName = customer.FullName;
                existing.PhoneNumber = customer.PhoneNumber;
                existing.ICNumber = customer.ICNumber;
                existing.Address = customer.Address;
                existing.Birthday = customer.Birthday;
                existing.Status = customer.Status;
                _context.SaveChanges();
            }
        }

        public List<BookingSlots> SeedFixedSlots()
        {
            var requiredSlots = new List<(TimeSpan Start, TimeSpan End)>
            {
                // Pagi (1 Hour Slots for Game Session)
                (new TimeSpan(8, 15, 0), new TimeSpan(9, 15, 0)),
                (new TimeSpan(9, 15, 0), new TimeSpan(10, 15, 0)),
                (new TimeSpan(10, 15, 0), new TimeSpan(11, 15, 0)),
                (new TimeSpan(11, 15, 0), new TimeSpan(12, 15, 0)),
                (new TimeSpan(13, 15, 0), new TimeSpan(14, 15, 0)),
                (new TimeSpan(14, 15, 0), new TimeSpan(15, 15, 0)),
                (new TimeSpan(15, 15, 0), new TimeSpan(16, 15, 0)),

                // Class 2-Hour Slots (8:15 - 10:15, 10:30 - 12:30, 14:30 - 16:30)
                (new TimeSpan(8, 15, 0), new TimeSpan(10, 15, 0)),
                (new TimeSpan(10, 30, 0), new TimeSpan(12, 30, 0)),
                (new TimeSpan(14, 30, 0), new TimeSpan(16, 30, 0)),

                // Self Training 2-Hour Slots (8 PM - 12 AM)
                (new TimeSpan(20, 0, 0), new TimeSpan(22, 0, 0)),
                (new TimeSpan(22, 0, 0), new TimeSpan(0, 0, 0)),

                // Malam (1 Hour Slots)
                (new TimeSpan(19, 45, 0), new TimeSpan(20, 45, 0)),
                (new TimeSpan(20, 45, 0), new TimeSpan(21, 45, 0)),
                (new TimeSpan(21, 45, 0), new TimeSpan(22, 45, 0)),
                (new TimeSpan(22, 45, 0), new TimeSpan(23, 45, 0)),
                (new TimeSpan(23, 45, 0), new TimeSpan(0, 45, 0))
            };

            foreach (var slot in requiredSlots)
            {
                var exists = _context.BookingSlots.Any(s => s.SlotStartTime == slot.Start && s.SlotEndTime == slot.End);
                if (!exists)
                {
                    _context.BookingSlots.Add(new BookingSlots
                    {
                        SlotStartTime = slot.Start,
                        SlotEndTime = slot.End,
                        IsActive = true
                    });
                }
            }
            _context.SaveChanges();

            return _context.BookingSlots.ToList();
        }

        // ──────────────────────────────────────────────
        // RATE MANAGEMENT
        // ──────────────────────────────────────────────

        public List<Rates> GetAllRates()
        {
            return _context.Rates.OrderBy(r => r.RateId).ToList();
        }

        public Rates GetRateById(int rateId)
        {
            return _context.Rates.FirstOrDefault(r => r.RateId == rateId);
        }

        public void SaveRate(Rates rate, string updatedByEmpId)
        {
            // Auto-compute FinalPrice
            if (rate.RateCategory == 4) // FOC
            {
                rate.FinalPrice = 0;
                rate.DiscountPercentage = 100;
            }
            else if (rate.DiscountPercentage.HasValue && rate.DiscountPercentage.Value > 0)
            {
                // Apply discount for any non-FOC category
                rate.FinalPrice = rate.BasePrice * (1 - rate.DiscountPercentage.Value / 100);
            }
            else
            {
                rate.FinalPrice = rate.BasePrice;
                rate.DiscountPercentage = null;
            }

            rate.UpdatedBy = updatedByEmpId;
            rate.UpdatedOn = DateTime.Now;

            var existing = _context.Rates.FirstOrDefault(r => r.RateId == rate.RateId);
            if (existing == null)
            {
                _context.Rates.Add(rate);
            }
            else
            {
                existing.RateCode = rate.RateCode;
                existing.RateName = rate.RateName;
                existing.BasePrice = rate.BasePrice;
                existing.DiscountPercentage = rate.DiscountPercentage;
                existing.FinalPrice = rate.FinalPrice;
                existing.IsActive = rate.IsActive;
                existing.UpdatedBy = rate.UpdatedBy;
                existing.UpdatedOn = rate.UpdatedOn;
            }
            _context.SaveChanges();
        }

        public void AddRate(Rates rate, string createdByEmpId)
        {
            // Auto-compute FinalPrice
            if (rate.RateCategory == 4) // FOC
            {
                rate.FinalPrice = 0;
                rate.DiscountPercentage = 100;
            }
            else if (rate.DiscountPercentage.HasValue && rate.DiscountPercentage.Value > 0)
            {
                // Apply discount for any non-FOC category
                rate.FinalPrice = rate.BasePrice * (1 - rate.DiscountPercentage.Value / 100);
            }
            else
            {
                rate.FinalPrice = rate.BasePrice;
                rate.DiscountPercentage = null;
            }

            rate.UpdatedBy = createdByEmpId;
            rate.UpdatedOn = DateTime.Now;
            rate.IsActive  = true;

            _context.Rates.Add(rate);
            _context.SaveChanges();
        }

        public void DeleteRate(int rateId)
        {
            var rate = _context.Rates.FirstOrDefault(r => r.RateId == rateId);
            if (rate != null)
            {
                _context.Rates.Remove(rate);
                _context.SaveChanges();
            }
        }

        public void ToggleRateActive(int rateId, string updatedByEmpId)
        {
            var rate = _context.Rates.FirstOrDefault(r => r.RateId == rateId);
            if (rate != null)
            {
                rate.IsActive = !rate.IsActive;
                rate.UpdatedBy = updatedByEmpId;
                rate.UpdatedOn = DateTime.Now;
                _context.SaveChanges();
            }
        }

        public void SeedDefaultRates()
        {
            if (_context.Rates.Any()) return;

            var defaults = new List<Rates>
            {
                new Rates
                {
                    RateCode  = "RATE-SN",
                    RateName  = "Siang (Normal)",
                    RateCategory = 1,
                    SessionType  = 1,
                    BasePrice = 10.00m,
                    FinalPrice = 10.00m,
                    IsActive = true,
                    UpdatedOn = DateTime.Now
                },
                new Rates
                {
                    RateCode  = "RATE-MN",
                    RateName  = "Malam (Normal)",
                    RateCategory = 1,
                    SessionType  = 2,
                    BasePrice = 12.00m,
                    FinalPrice = 12.00m,
                    IsActive = true,
                    UpdatedOn = DateTime.Now
                },
                new Rates
                {
                    RateCode  = "RATE-SD",
                    RateName  = "Siang (Discount)",
                    RateCategory = 2,
                    SessionType  = 1,
                    BasePrice = 10.00m,
                    DiscountPercentage = 0,
                    FinalPrice = 10.00m,
                    IsActive = true,
                    UpdatedOn = DateTime.Now
                },
                new Rates
                {
                    RateCode  = "RATE-MD",
                    RateName  = "Malam (Discount)",
                    RateCategory = 2,
                    SessionType  = 2,
                    BasePrice = 12.00m,
                    DiscountPercentage = 0,
                    FinalPrice = 12.00m,
                    IsActive = true,
                    UpdatedOn = DateTime.Now
                },
                new Rates
                {
                    RateCode  = "RATE-SS",
                    RateName  = "Siang (Special)",
                    RateCategory = 3,
                    SessionType  = 1,
                    BasePrice = 10.00m,
                    FinalPrice = 10.00m,
                    IsActive = true,
                    UpdatedOn = DateTime.Now
                },
                new Rates
                {
                    RateCode  = "RATE-MS",
                    RateName  = "Malam (Special)",
                    RateCategory = 3,
                    SessionType  = 2,
                    BasePrice = 12.00m,
                    FinalPrice = 12.00m,
                    IsActive = true,
                    UpdatedOn = DateTime.Now
                },
                new Rates
                {
                    RateCode  = "RATE-FOC",
                    RateName  = "FOC (Free of Charge)",
                    RateCategory = 4,
                    SessionType  = 0,
                    BasePrice = 0.00m,
                    DiscountPercentage = 100,
                    FinalPrice = 0.00m,
                    IsActive = true,
                    UpdatedOn = DateTime.Now
                }
            };

            _context.Rates.AddRange(defaults);
            _context.SaveChanges();
        }

        // ──────────────────────────────────────────────
        // LANE & TARGET MANAGEMENT
        // ──────────────────────────────────────────────

        public List<Lanes> GetAllLanes()
        {
            return _context.Lanes.OrderBy(l => l.LaneNumber).ToList();
        }

        public List<Targets> GetAllTargets()
        {
            return _context.Targets.Include(t => t.Lane).OrderBy(t => t.TargetNumber).ToList();
        }

        public void ToggleLaneStatus(int laneId)
        {
            var lane = _context.Lanes.FirstOrDefault(l => l.LaneId == laneId);
            if (lane != null)
            {
                lane.Status = (lane.Status == "Active") ? "Maintenance" : "Active";
                _context.SaveChanges();
            }
        }

        public void ToggleTargetStatus(int targetId)
        {
            var target = _context.Targets.FirstOrDefault(t => t.TargetId == targetId);
            if (target != null)
            {
                target.Status = (target.Status == "Available") ? "Maintenance" : "Available";
                _context.SaveChanges();
            }
        }

        public void SeedDefaultLanesAndTargets()
        {
            if (_context.Lanes.Any()) return;

            for (int i = 1; i <= 20; i++)
            {
                var lane = new Lanes
                {
                    LaneNumber = i,
                    Status = "Active"
                };
                _context.Lanes.Add(lane);
                _context.SaveChanges(); // Save to get LaneId

                var target = new Targets
                {
                    TargetNumber = i,
                    LaneId = lane.LaneId,
                    MaxCapacity = 4,
                    Status = "Available"
                };
                _context.Targets.Add(target);
            }
            _context.SaveChanges();
        }

        public void AddPayment(Payments payment)
        {
            _context.Payments.Add(payment);
            _context.SaveChanges();
        }

        public List<Payments> GetPaymentsByEmail(string email)
        {
            return _context.Payments
                .Include(p => p.Reservation)
                .Where(p => p.Reservation.CustomerEmail.ToLower() == email.ToLower() || p.Reservation.ReservedBy.ToLower() == email.ToLower())
                .ToList();
        }

        public void RegisterClassSession(ClassRegistrations cr)
        {
            _context.ClassRegistrations.Add(cr);
            _context.SaveChanges();
        }

        public IEnumerable<MembershipPayments> GetMembershipPaymentsByEmail(string email)
        {
            return _context.MembershipPayments
                .Where(m => m.CustomerEmail.ToLower() == email.ToLower())
                .ToList();
        }

        public void AddMembershipPayment(MembershipPayments mp)
        {
            _context.MembershipPayments.Add(mp);
            _context.SaveChanges();
        }

        public List<ClassRegistrations> GetClassRegistrationsByEmail(string email)
        {
            return _context.ClassRegistrations.Where(cr => cr.CustomerEmail.ToLower() == email.ToLower()).ToList();
        }

        public void RemoveClassRegistration(int classRegId)
        {
            var cr = _context.ClassRegistrations.Find(classRegId);
            if (cr != null)
            {
                _context.ClassRegistrations.Remove(cr);
                _context.SaveChanges();
            }
        }

        public List<Reservations> GetReservationsByEmail(string email)
        {
            return _context.Reservations
                .Include(r => r.Slot)
                .Include(r => r.Student)
                .Where(r => r.CustomerEmail.ToLower() == email.ToLower() || r.ReservedBy.ToLower() == email.ToLower())
                .ToList();
        }

        // ──────────────────────────────────────────────
        // STUDENT (CHILD) MANAGEMENT
        // ──────────────────────────────────────────────

        public void AddStudent(Students student)
        {
            _context.Students.Add(student);
            _context.SaveChanges();
        }

        public List<Students> GetStudentsByParentId(int customerId)
        {
            return _context.Students
                .Where(s => s.ParentCustomerId == customerId)
                .OrderBy(s => s.CreatedAt)
                .ToList();
        }

        public Students GetStudentById(int studentId)
        {
            return _context.Students.FirstOrDefault(s => s.StudentId == studentId);
        }

        public void RemoveStudent(int studentId)
        {
            var s = _context.Students.Find(studentId);
            if (s != null)
            {
                // Remove related class registration if any
                var cr = _context.ClassRegistrations.FirstOrDefault(c => c.StudentId == studentId);
                if (cr != null)
                {
                    _context.ClassRegistrations.Remove(cr);
                }
                
                _context.Students.Remove(s);
                _context.SaveChanges();
            }
        }

        public ClassRegistrations GetClassRegistrationByStudentId(int studentId)
        {
            return _context.ClassRegistrations
                           .Where(cr => cr.StudentId == studentId)
                           .OrderByDescending(cr => cr.RegistrationDate)
                           .FirstOrDefault();
        }

        public void UpdateStudentStatus(int studentId, string status)
        {
            var student = _context.Students.FirstOrDefault(s => s.StudentId == studentId);
            if (student != null)
            {
                student.Status = status;
                _context.SaveChanges();
            }
        }

        public void UpdateStudentProfile(int studentId, string fullName)
        {
            var student = _context.Students.FirstOrDefault(s => s.StudentId == studentId);
            if (student != null)
            {
                student.FullName = fullName;
                _context.SaveChanges();
            }
        }

        public List<ClassRegistrations> GetPendingPaymentsByEmail(string email)
        {
            return _context.ClassRegistrations
                .Where(cr => cr.CustomerEmail.ToLower() == email.ToLower() && cr.PaymentStatus == "Pending")
                .ToList();
        }

        public void ClearPendingPaymentsByEmail(string email, string? type = null, int? id = null)
        {
            // Class registrations
            var pendingClasses = _context.ClassRegistrations
                .Where(cr => cr.CustomerEmail.ToLower() == email.ToLower() && cr.PaymentStatus == "Pending" && 
                             (string.IsNullOrEmpty(type) || type == "all" || (type == "class" && cr.RegistrationId == id)))
                .ToList();
            foreach (var pc in pendingClasses)
            {
                pc.PaymentStatus = "Success";
                pc.PaymentMethod = "Online (FPX)";
                pc.TransactionId = "TXN-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
            }

            // Membership Payments
            var pendingMemberships = _context.MembershipPayments
                .Where(mp => mp.CustomerEmail.ToLower() == email.ToLower() && mp.Status == "Pending" &&
                             (string.IsNullOrEmpty(type) || type == "all" || type == "membership" || type == "Annual Membership"))
                .ToList();
            
            foreach (var mp in pendingMemberships)
            {
                mp.Status = "Success";
                mp.PaymentMethod = "Online (FPX)";
                mp.TransactionId = "TXN-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

                var customer = _context.Customers.FirstOrDefault(c => c.Email.ToLower() == email.ToLower());
                if (customer != null)
                {
                    customer.Status = "Active";
                }
            }

            // Reservations
            List<Reservations> pendingReservations;
            if (string.IsNullOrEmpty(type) || type == "all")
            {
                pendingReservations = _context.Reservations
                    .Where(r => (r.CustomerEmail.ToLower() == email.ToLower() || r.ReservedBy.ToLower() == email.ToLower()) && r.Status == 0)
                    .ToList();
            }
            else if (type == "reservation" && id.HasValue)
            {
                var targetRes = _context.Reservations.FirstOrDefault(r => r.ReservationId == id.Value);
                if (targetRes != null)
                {
                    var targetPayment = _context.Payments.FirstOrDefault(p => p.ReservationId == targetRes.ReservationId);
                    if (targetPayment != null && !string.IsNullOrEmpty(targetPayment.TransactionId))
                    {
                        var siblingReservationIds = _context.Payments
                            .Where(p => p.TransactionId == targetPayment.TransactionId)
                            .Select(p => p.ReservationId)
                            .ToList();

                        pendingReservations = _context.Reservations
                            .Where(r => siblingReservationIds.Contains(r.ReservationId) && r.Status == 0)
                            .ToList();
                    }
                    else
                    {
                        pendingReservations = _context.Reservations
                            .Where(r => (r.CustomerEmail.ToLower() == email.ToLower() || r.ReservedBy.ToLower() == email.ToLower()) && 
                                        r.Status == 0 && 
                                        r.SlotId == targetRes.SlotId && 
                                        r.ReservedOn.Date == targetRes.ReservedOn.Date)
                            .ToList();
                    }
                }
                else
                {
                    pendingReservations = new List<Reservations>();
                }
            }
            else
            {
                pendingReservations = new List<Reservations>();
            }

            var reservationPayments = pendingReservations.Select(pr => {
                var payment = _context.Payments.FirstOrDefault(p => p.ReservationId == pr.ReservationId);
                string groupKey = !string.IsNullOrEmpty(payment?.TransactionId) 
                    ? payment.TransactionId 
                    : $"PENDING-FALLBACK-{pr.SlotId}-{pr.ReservedOn.Date:yyyyMMdd}";
                return new { Reservation = pr, Payment = payment, GroupKey = groupKey };
            })
            .GroupBy(x => x.GroupKey);

            foreach (var group in reservationPayments)
            {
                string newTxnId = "TXN-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
                foreach (var item in group)
                {
                    item.Reservation.Status = 1; // Paid
                    if (item.Payment == null)
                    {
                        var payment = new Payments
                        {
                            ReservationId = item.Reservation.ReservationId,
                            Amount = item.Reservation.TotalPrice,
                            PaymentMethod = "Online (FPX)",
                            PaymentDate = DateTime.Now,
                            Status = "Success",
                            TransactionId = newTxnId
                        };
                        _context.Payments.Add(payment);
                    }
                    else
                    {
                        item.Payment.Status = "Success";
                        item.Payment.PaymentDate = DateTime.Now;
                        item.Payment.PaymentMethod = "Online (FPX)";
                        item.Payment.TransactionId = newTxnId;
                    }
                }
            }

            _context.SaveChanges();
        }

        public List<Reservations> GetReservationsByDate(DateTime date)
        {
            return _context.Reservations
                .Include(r => r.Slot)
                .Include(r => r.Student)
                .Where(r => r.ReservedOn.Date == date.Date && r.Status != 2)
                .ToList();
        }

        public void UpdateAttendance(int reservationId, bool attended)
        {
            var reservation = _context.Reservations.FirstOrDefault(r => r.ReservationId == reservationId);
            if (reservation != null)
            {
                reservation.Attended = attended;
                _context.SaveChanges();
            }
        }
        public List<PerformanceReports> GetReportsByStudent(string studentName, string level)
        {
            return _context.PerformanceReports
                .Where(r => r.StudentName.ToLower() == studentName.ToLower()
                         && r.LevelCategory == level)
                .OrderByDescending(r => r.ReportDate)
                .ToList();
        }

        public void AddPerformanceReport(PerformanceReports report)
        {
            report.ReportDate = DateTime.Now;
            _context.PerformanceReports.Add(report);
            _context.SaveChanges();
        }

        public List<Students> GetAllArchers()
        {
            return _context.Students.OrderBy(s => s.FullName).ToList();
        }

        public Roles GetStaffProfile(string empId)
        {
            return _context.Roles.FirstOrDefault(r => r.EmpId == empId);
        }

        public void UpdateStaffProfile(Roles role)
        {
            var existing = _context.Roles.FirstOrDefault(r => r.EmpId == role.EmpId);
            if (existing != null)
            {
                existing.EmpName = role.EmpName;
                existing.Gender = role.Gender;
                existing.Email = role.Email;
                existing.PhoneNumber = role.PhoneNumber;
                existing.EContactName = role.EContactName;
                existing.EContactNumber = role.EContactNumber;
                existing.ProfilePicture = role.ProfilePicture;
                _context.SaveChanges();
            }
        }
        public void UpdateAbsentReason(int groupId, string reason)
        {
            var baseReservation = _context.Reservations.FirstOrDefault(r => r.ReservationId == groupId);
            if (baseReservation != null)
            {
                var groupReservations = _context.Reservations.Where(r => 
                    r.CustomerEmail == baseReservation.CustomerEmail &&
                    r.SlotId == baseReservation.SlotId &&
                    r.ReservedOn.Date == baseReservation.ReservedOn.Date &&
                    r.RateCode == baseReservation.RateCode).ToList();
                
                foreach (var res in groupReservations)
                {
                    res.AbsentReason = reason;
                }
                _context.SaveChanges();
            }
        }

        public void UpdateStudentLevel(string studentName, string newLevel)
        {
            var student = _context.Students
                .FirstOrDefault(s => s.FullName.ToLower() == studentName.ToLower());
            if (student != null)
            {
                student.LevelCategory = newLevel;
                _context.SaveChanges();
            }
        }
    }
}
