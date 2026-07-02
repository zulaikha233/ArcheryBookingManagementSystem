using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ArcheryAlley.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;

namespace ArcheryAlley.Controllers
{
    public class AccountController : Controller
    {
        private readonly IArcheryAlleyRepository _repository;

        public AccountController(IArcheryAlleyRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public IActionResult Profile()
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

            return View("~/Views/Staff/StaffProfile.cshtml");
        }
        [HttpPost]
        public IActionResult SaveProfile(string empName, string gender, string email,
         string phone, string eContactName, string eContactPhone, string profilePicture)
        {
            var empId = HttpContext.Session.GetString("EmpId");

            var role = new Roles
            {
                EmpId = empId,
                EmpName = empName,
                Gender = gender,
                Email = email,
                PhoneNumber = phone,
                EContactName = eContactName,
                EContactNumber = eContactPhone,
                ProfilePicture = profilePicture
            };

            _repository.UpdateStaffProfile(role);

            // Update session name if changed
            HttpContext.Session.SetString("UserName", empName);

            return Json(new { success = true });
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View("~/Views/Account/StaffLogin.cshtml");
        }

        [HttpPost]
        public IActionResult Login(string EmpId, string Password)
        {
            var role = _repository.GetRoleByEmpId(EmpId);

            if (role != null && role.Password == Password)
            {
                HttpContext.Session.SetString("UserName", role.EmpName);
                HttpContext.Session.SetString("EmpId", EmpId);
                HttpContext.Session.SetString("UserRole", role.RoleType ? "Admin" : "Staff");

                if (role.RoleType)
                {
                    return RedirectToAction("AdminDashboard", "Slot");
                }
                else
                {
                    return RedirectToAction("StaffDashboard", "Booking");
                }
            }

            ViewBag.ErrorMessage = "Invalid Employee ID or Password. Try again!";
            return View("~/Views/Account/StaffLogin.cshtml");
        }

        public IActionResult Logout()
        {
            string userRole = HttpContext.Session.GetString("UserRole");
            
            HttpContext.Session.Clear();

            if (userRole == "Admin")
            {
                return RedirectToAction("Login");
            }
            
            return RedirectToAction("CustomerLogin");
        }

        [HttpGet]
        public IActionResult CustomerLogin()
        {
            return View("~/Views/Account/MemberLogin.cshtml");
        }

        [HttpPost]
        public IActionResult CustomerLogin(string Email, string Password)
        {
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                ViewBag.ErrorMessage = "Please enter your email and password.";
                return View("~/Views/Account/MemberLogin.cshtml");
            }

            // Check if email exists first
            var customerByEmail = _repository.GetCustomerByEmail(Email);
            if (customerByEmail == null)
            {
                ViewBag.ErrorMessage = "Incorrect email. No account found with this email address.";
                return View("~/Views/Account/MemberLogin.cshtml");
            }

            // Email exists, now check password
            var customer = _repository.GetCustomerLogin(Email, Password);
            if (customer == null)
            {
                ViewBag.ErrorMessage = "Incorrect password. Please try again.";
                return View("~/Views/Account/MemberLogin.cshtml");
            }

            HttpContext.Session.SetString("CustomerEmail", customer.Email);
            HttpContext.Session.SetString("CustomerName", customer.FullName ?? customer.Username);
            HttpContext.Session.SetString("CustomerPhone", customer.PhoneNumber ?? "");
            HttpContext.Session.SetString("CustomerUsername", customer.Username);
            HttpContext.Session.SetString("CustomerStatus", customer.Status ?? "Inactive");
            HttpContext.Session.SetString("UserRole", "Member");
            return RedirectToAction("MemberDashboard", "Account");
        }

        [HttpGet]
        public IActionResult CustomerRegister()
        {
            return View("~/Views/Account/MemberRegister.cshtml");
        }

        [HttpPost]
        public IActionResult CustomerRegister(string Username, string Email, string Password, string ConfirmPassword)
        {
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                ViewBag.ErrorMessage = "Please fill in all details.";
                return View("~/Views/Account/MemberRegister.cshtml");
            }
            if (Password != ConfirmPassword)
            {
                ViewBag.ErrorMessage = "Passwords do not match.";
                return View("~/Views/Account/MemberRegister.cshtml");
            }

            var existingCustomer = _repository.GetCustomerByEmail(Email);
            if (existingCustomer != null)
            {
                ViewBag.ErrorMessage = "This email is already registered.";
                return View("~/Views/Account/MemberRegister.cshtml");
            }

            var newCustomer = new Customers
            {
                Username = Username,
                Email = Email,
                Password = Password,
                Status = "Inactive"
            };

            try
            {
                _repository.RegisterCustomer(newCustomer);

                // Create pending Annual Membership payment immediately
                var annualMembership = new MembershipPayments
                {
                    CustomerEmail = newCustomer.Email,
                    Amount = 80.00m,
                    PaymentMethod = "None",
                    Status = "Pending",
                    TransactionId = "MEM-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper()
                };
                _repository.AddMembershipPayment(annualMembership);
                
                HttpContext.Session.SetString("CustomerEmail", newCustomer.Email);
                HttpContext.Session.SetString("CustomerName", newCustomer.Username);
                HttpContext.Session.SetString("CustomerPhone", "");
                HttpContext.Session.SetString("CustomerUsername", newCustomer.Username);
                HttpContext.Session.SetString("CustomerStatus", "Inactive");
                HttpContext.Session.SetString("UserRole", "Member");
                
                return RedirectToAction("MemberDashboard", "Account");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "An error occurred: " + ex.Message;
                return View("~/Views/Account/MemberRegister.cshtml");
            }
        }

        [HttpGet]
        public IActionResult ClassRegistration(int? studentId = null)
        {
            string email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("CustomerLogin");

            var parent = _repository.GetCustomerByEmail(email);
            
            if (parent != null && parent.Status != "Active")
            {
                TempData["ErrorMessage"] = "You must be an active member and pay the annual fee before you can register for a class.";
                return RedirectToAction("MemberDashboard");
            }

            ViewBag.HasPaidAnnualFee = parent != null && parent.Status == "Active";

            // If registering for a student, validate the student belongs to this parent
            if (studentId.HasValue)
            {
                var student = _repository.GetStudentById(studentId.Value);
                if (student == null || parent == null || student.ParentCustomerId != parent.CustomerId)
                    return RedirectToAction("MemberDashboard");

                ViewBag.StudentId = studentId.Value;
                ViewBag.StudentName = student.FullName;
                ViewBag.StudentIC = student.ICNumber;
            }

            return View("~/Views/Account/ClassRegistration.cshtml", parent);
        }


        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin") return RedirectToAction("Login");
            ViewBag.NextId = _repository.GetNextEmpId();
            return View();
        }

        [HttpPost]
        public IActionResult Register(string EmpName, string Password, bool RoleType)
        {
            try
            {
                if (HttpContext.Session.GetString("UserRole") != "Admin")
                {
                    RoleType = false;
                }

                var role = new Roles
                {
                    EmpName = EmpName,
                    Password = Password,
                    RoleType = RoleType
                };

                _repository.RegisterStaff(role);

                TempData["SuccessTitle"] = "STAFF REGISTERED";
                TempData["SuccessMessage"] = "A new staff account has been successfully created and onboarded.";
                return RedirectToAction("Success", "Home");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Registration failed. Please try again or contact support.";
                return View();
            }
        }

        [HttpPost]
        public IActionResult CompleteClassRegistration(
            string FullName, string PhoneNumber, string ICNumber, string Address,
            string PackageType, decimal PackagePrice,
            decimal TotalPrice, string PaymentMethod, int? StudentId = null)
        {
            string email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email))
            {
                return Json(new { success = false, message = "Session expired. Please login again." });
            }

            if (string.IsNullOrEmpty(FullName) || string.IsNullOrEmpty(PhoneNumber) || string.IsNullOrEmpty(ICNumber) || string.IsNullOrEmpty(Address))
            {
                return Json(new { success = false, message = "Please fill in all member details." });
            }

            var customer = _repository.GetCustomerByEmail(email);
            if (customer == null)
            {
                return Json(new { success = false, message = "Customer not found." });
            }

            customer.FullName = FullName;
            customer.PhoneNumber = PhoneNumber;
            customer.ICNumber = ICNumber;
            customer.Address = Address;
            customer.Status = "Active";

            if (!string.IsNullOrEmpty(ICNumber) && ICNumber.Length == 12)
            {
                if (int.TryParse(ICNumber.Substring(0, 2), out int year) &&
                    int.TryParse(ICNumber.Substring(2, 2), out int month) &&
                    int.TryParse(ICNumber.Substring(4, 2), out int day))
                {
                    int currentYear2Digit = DateTime.Now.Year % 100;
                    year += (year > currentYear2Digit) ? 1900 : 2000;
                    try
                    {
                        customer.Birthday = new DateTime(year, month, day);
                    }
                    catch
                    {
                        customer.Birthday = null;
                    }
                }
            }

            decimal finalTotalPrice = TotalPrice;

            // If registering parent who is ALREADY ACTIVE, override to 0 annual fee
            if (!StudentId.HasValue && customer.Status == "Active")
            {
                finalTotalPrice = PackagePrice;
            }

            if (!StudentId.HasValue)
            {
                var selfStudent = _repository.GetStudentsByParentId(customer.CustomerId)
                                    .FirstOrDefault(s => s.FullName.ToLower() == customer.FullName.ToLower());

                if (selfStudent == null)
                {
                    int? calculatedAge = customer.Birthday.HasValue ? (DateTime.Now.Year - customer.Birthday.Value.Year) : (int?)null;
                    selfStudent = new Students
                    {
                        ParentCustomerId = customer.CustomerId,
                        FullName = customer.FullName,
                        Age = calculatedAge,
                        ICNumber = customer.ICNumber,
                        LevelCategory = "GrassRoots"
                    };
                    _repository.AddStudent(selfStudent);
                }

                StudentId = selfStudent.StudentId;
            }

            var registration = new ClassRegistrations
            {
                CustomerEmail = email,
                CustomerName = FullName,
                PackageType = PackageType,
                PackagePrice = PackagePrice,
                TotalPrice = finalTotalPrice,
                PaymentMethod = PaymentMethod == "fpx" ? "Online (FPX)" : "Credit/Debit Card",
                PaymentStatus = "Success",
                TransactionId = "TXN-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                StudentId = StudentId
            };

            try
            {
                _repository.RegisterClassSession(registration);

                HttpContext.Session.SetString("CustomerName", customer.FullName);
                HttpContext.Session.SetString("CustomerPhone", customer.PhoneNumber);
                HttpContext.Session.SetString("CustomerStatus", "Active");

                return Json(new { success = true, transactionId = registration.TransactionId });
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "An error occurred: " + msg });
            }
        }

        [HttpGet]
        public IActionResult MemberDashboard()
        {
            string email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("CustomerLogin");

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
                Slot = r.Slot != null ? new {
                    SlotId = r.Slot.SlotId,
                    SlotStartTime = r.Slot.SlotStartTime.ToString(@"hh\:mm"),
                    SlotEndTime = r.Slot.SlotEndTime.ToString(@"hh\:mm"),
                    IsNight = r.Slot.SlotStartTime.Hours >= 18
                } : null
            }).ToList();

            var classRegs = _repository.GetClassRegistrationsByEmail(email);
            ViewBag.ClassRegistrations = classRegs;
            ViewBag.MembershipPayments = _repository.GetMembershipPaymentsByEmail(email);

            var parent = _repository.GetCustomerByEmail(email);
            int? selfStudentId = null;
            if (parent != null)
            {
                var selfStudent = _repository.GetStudentsByParentId(parent.CustomerId).FirstOrDefault(s => s.FullName.ToLower() == parent.FullName?.ToLower());
                selfStudentId = selfStudent?.StudentId;
            }
            ViewBag.SelfStudentId = selfStudentId;

            var latestMainReg = classRegs?.Where(cr => cr.StudentId == null || cr.StudentId == selfStudentId).OrderByDescending(cr => cr.RegistrationDate).FirstOrDefault();
            bool mainIsCompleted = false;
            bool mainIsExpired = false;
            
            if (latestMainReg != null && (latestMainReg.PaymentStatus == "Success" || latestMainReg.PaymentStatus == "Paid"))
            {
                if (DateTime.Now >= latestMainReg.RegistrationDate.AddMonths(1))
                {
                    mainIsExpired = true;
                }
                int totalSlots = 4;
                if (!string.IsNullOrEmpty(latestMainReg.PackageType))
                {
                    if (latestMainReg.PackageType.Contains("4 Slot", StringComparison.OrdinalIgnoreCase) || latestMainReg.PackageType.Contains("Starter", StringComparison.OrdinalIgnoreCase)) totalSlots = 4;
                    else if (latestMainReg.PackageType.Contains("6 Slot", StringComparison.OrdinalIgnoreCase) || latestMainReg.PackageType.Contains("Progressive", StringComparison.OrdinalIgnoreCase)) totalSlots = 6;
                }

                var classHistory = rawReservations
                    .Where(r => (r.RateCode == "CLASS" || r.RateCode == "Class Session") && (r.StudentId == null || r.StudentId == selfStudentId)).ToList();

                int previousCapacity = 0;
                var previousRegs = classRegs.Where(cr => (cr.StudentId == null || cr.StudentId == selfStudentId) && cr.RegistrationId != latestMainReg.RegistrationId).ToList();
                foreach(var pr in previousRegs)
                {
                    if (!string.IsNullOrEmpty(pr.PackageType))
                    {
                        if (pr.PackageType.Contains("4 Slot", StringComparison.OrdinalIgnoreCase) || pr.PackageType.Contains("Starter", StringComparison.OrdinalIgnoreCase)) previousCapacity += 4;
                        else if (pr.PackageType.Contains("6 Slot", StringComparison.OrdinalIgnoreCase) || pr.PackageType.Contains("Progressive", StringComparison.OrdinalIgnoreCase)) previousCapacity += 6;
                        else previousCapacity += 4;
                    }
                    else
                    {
                        previousCapacity += 4;
                    }
                }

                int totalAttendedEver = classHistory.Where(r => r.Attended).Sum(r => Math.Max(1, r.DurationHours / 2));
                int attendedSlots = Math.Max(0, totalAttendedEver - previousCapacity);

                if (attendedSlots >= totalSlots)
                {
                    mainIsCompleted = true;
                }
            }
            ViewBag.MainAccountIsCompleted = mainIsCompleted;
            ViewBag.MainAccountIsExpired = mainIsExpired;

            // Pass students (children) list
            if (parent != null)
            {
                var students = _repository.GetStudentsByParentId(parent.CustomerId);

                ViewBag.Students = students.Select(s => new {
                    s.StudentId,
                    s.FullName,
                    s.ICNumber,
                    s.Birthday,
                    ClassReg = classRegs.FirstOrDefault(cr => cr.StudentId == s.StudentId)
                }).ToList();
            }

            return View("~/Views/Dashboard/MemberDashboard.cshtml");
        }

        [HttpGet]
        public IActionResult MemberProfile()
        {
            string email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("CustomerLogin");

            var customer = _repository.GetCustomerByEmail(email);
            if (customer == null)
                return RedirectToAction("CustomerLogin");

            int? selfStudentId = null;
            var selfStudent = _repository.GetStudentsByParentId(customer.CustomerId).FirstOrDefault(s => s.FullName.ToLower() == customer.FullName?.ToLower());
            if (selfStudent != null) selfStudentId = selfStudent.StudentId;
            ViewBag.SelfStudentId = selfStudentId;

            var classRegs = _repository.GetClassRegistrationsByEmail(email)
                                      ?.Where(cr => cr.StudentId == null || cr.StudentId == selfStudentId)
                                      .OrderByDescending(cr => cr.RegistrationDate)
                                      .ToList();
                                      
            var latestReg = classRegs?.FirstOrDefault();
            bool isCompleted = false;
            
            if (latestReg != null && (latestReg.PaymentStatus == "Success" || latestReg.PaymentStatus == "Paid"))
            {
                int totalSlots = 4;
                if (!string.IsNullOrEmpty(latestReg.PackageType))
                {
                    if (latestReg.PackageType.Contains("4 Slot", StringComparison.OrdinalIgnoreCase) || latestReg.PackageType.Contains("Starter", StringComparison.OrdinalIgnoreCase)) totalSlots = 4;
                    else if (latestReg.PackageType.Contains("6 Slot", StringComparison.OrdinalIgnoreCase) || latestReg.PackageType.Contains("Progressive", StringComparison.OrdinalIgnoreCase)) totalSlots = 6;
                }

                var history = _repository.GetReservationsByEmail(email)
                    .Where(r => (r.RateCode == "CLASS" || r.RateCode == "Class Session") && (r.StudentId == null || r.StudentId == selfStudentId)).ToList();

                int attendedSlots = history.Where(r => r.Attended && r.ReservedOn >= latestReg.RegistrationDate).Sum(r => Math.Max(1, r.DurationHours / 2));
                if (attendedSlots >= totalSlots)
                {
                    isCompleted = true;
                }
            }

            ViewBag.MainAccountClassReg = latestReg;
            ViewBag.ClassHistory = classRegs;
            ViewBag.MainAccountIsCompleted = isCompleted;

            return View("~/Views/Profile/MemberProfile.cshtml", customer);
        }

        [HttpPost]
        public IActionResult MemberProfile(string FullName, string PhoneNumber, string ICNumber, string Address, string Username)
        {
            string email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("CustomerLogin");

            var customer = _repository.GetCustomerByEmail(email);
            if (customer == null)
                return RedirectToAction("CustomerLogin");

            if (string.IsNullOrEmpty(FullName) || string.IsNullOrEmpty(PhoneNumber) || string.IsNullOrEmpty(ICNumber) || string.IsNullOrEmpty(Address))
            {
                ViewBag.ErrorMessage = "Please fill in all profile details.";
                return View("~/Views/Profile/MemberProfile.cshtml", customer);
            }

            customer.FullName = FullName;
            customer.PhoneNumber = PhoneNumber;
            customer.ICNumber = ICNumber;
            customer.Address = Address;
            if (!string.IsNullOrEmpty(Username))
            {
                customer.Username = Username;
            }

            if (!string.IsNullOrEmpty(ICNumber) && ICNumber.Length == 12)
            {
                if (int.TryParse(ICNumber.Substring(0, 2), out int year) &&
                    int.TryParse(ICNumber.Substring(2, 2), out int month) &&
                    int.TryParse(ICNumber.Substring(4, 2), out int day))
                {
                    int currentYear2Digit = DateTime.Now.Year % 100;
                    year += (year > currentYear2Digit) ? 1900 : 2000;
                    try
                    {
                        customer.Birthday = new DateTime(year, month, day);
                    }
                    catch
                    {
                        customer.Birthday = null;
                    }
                }
            }

            try
            {
                _repository.UpdateCustomer(customer);

                HttpContext.Session.SetString("CustomerName", customer.FullName ?? customer.Username);
                HttpContext.Session.SetString("CustomerUsername", customer.Username);
                HttpContext.Session.SetString("CustomerPhone", customer.PhoneNumber ?? "");

                ViewBag.SuccessMessage = "Profile updated successfully!";
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "An error occurred: " + ex.Message;
            }

            return View("~/Views/Profile/MemberProfile.cshtml", customer);
        }

        [HttpGet]
        public IActionResult MemberSettings()
        {
            string email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("CustomerLogin");

            var customer = _repository.GetCustomerByEmail(email);
            if (customer == null)
                return RedirectToAction("CustomerLogin");

            return View("~/Views/Profile/MemberSettings.cshtml", customer);
        }

        [HttpPost]
        public IActionResult MemberSettings(string CurrentPassword, string NewPassword)
        {
            string email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("CustomerLogin");

            var customer = _repository.GetCustomerByEmail(email);
            if (customer == null)
                return RedirectToAction("CustomerLogin");

            if (string.IsNullOrEmpty(CurrentPassword) || string.IsNullOrEmpty(NewPassword))
            {
                ViewBag.ErrorMessage = "Please fill in all password fields.";
                return View("~/Views/Profile/MemberSettings.cshtml", customer);
            }

            if (customer.Password != CurrentPassword)
            {
                ViewBag.ErrorMessage = "The current password you entered is incorrect.";
                return View("~/Views/Profile/MemberSettings.cshtml", customer);
            }

            try
            {
                customer.Password = NewPassword;
                _repository.UpdateCustomer(customer);
                ViewBag.SuccessMessage = "Password updated successfully!";
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "An error occurred: " + ex.Message;
            }

            return View("~/Views/Profile/MemberSettings.cshtml", customer);
        }

        [HttpGet]
        public IActionResult MemberPayment()
        {
            string email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("CustomerLogin");

            var customer = _repository.GetCustomerByEmail(email);
            if (customer == null)
                return RedirectToAction("CustomerLogin");

            var classRegs = _repository.GetClassRegistrationsByEmail(email) ?? new List<ClassRegistrations>();
            var membershipPayments = _repository.GetMembershipPaymentsByEmail(email) ?? new List<MembershipPayments>();
            var reservations = _repository.GetReservationsByEmail(email) ?? new List<Reservations>();
            var payments = _repository.GetPaymentsByEmail(email) ?? new List<Payments>();

            ViewBag.ClassRegs = classRegs;
            ViewBag.MembershipPayments = membershipPayments;
            ViewBag.Reservations = reservations;
            ViewBag.Payments = payments;

            return View("~/Views/Payment/Fee/MemberPayment.cshtml", customer);
        }

        [HttpPost]
        public IActionResult ClearPendingPayments(string? type = null, int? id = null)
        {
            string email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email))
                return Json(new { success = false, message = "Session expired. Please login again." });

            try
            {
                _repository.ClearPendingPaymentsByEmail(email, type, id);

                var customer = _repository.GetCustomerByEmail(email);
                if (customer != null)
                {
                    HttpContext.Session.SetString("CustomerStatus", customer.Status ?? "Inactive");
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult UpdateDefaultPaymentMethod(string method)
        {
            if (string.IsNullOrEmpty(method))
                return Json(new { success = false, message = "Invalid payment method." });

            HttpContext.Session.SetString("DefaultPaymentMethod", method);
            return Json(new { success = true });
        }

        // ─── Helper: read saved cards list from session ───────────────────────
        private List<Dictionary<string, string>> GetSavedCards()
        {
            var raw = HttpContext.Session.GetString("SavedCards");
            if (string.IsNullOrEmpty(raw))
                return new List<Dictionary<string, string>>();
            try
            {
                return JsonSerializer.Deserialize<List<Dictionary<string, string>>>(raw)
                       ?? new List<Dictionary<string, string>>();
            }
            catch
            {
                return new List<Dictionary<string, string>>();
            }
        }

        private void SaveCards(List<Dictionary<string, string>> cards)
        {
            HttpContext.Session.SetString("SavedCards", JsonSerializer.Serialize(cards));
        }

        // ─── Add a new card to the wallet ────────────────────────────────────
        [HttpPost]
        public IActionResult AddSavedCard(string cardNumber, string cardExpiry, string cardCvv)
        {
            if (string.IsNullOrWhiteSpace(cardNumber) || string.IsNullOrWhiteSpace(cardExpiry) || string.IsNullOrWhiteSpace(cardCvv))
                return Json(new { success = false, message = "Please fill in all card fields." });

            var cards = GetSavedCards();
            var newId = Guid.NewGuid().ToString();
            cards.Add(new Dictionary<string, string>
            {
                { "id",     newId },
                { "number", cardNumber.Trim() },
                { "expiry", cardExpiry.Trim() },
                { "cvv",    cardCvv.Trim() }
            });
            SaveCards(cards);

            // Auto-select the newly added card
            HttpContext.Session.SetString("SelectedCardId", newId);

            return Json(new { success = true, id = newId });
        }

        // ─── Delete a card by ID ─────────────────────────────────────────────
        [HttpPost]
        public IActionResult DeleteSavedCard(string cardId)
        {
            var cards = GetSavedCards();
            cards.RemoveAll(c => c.ContainsKey("id") && c["id"] == cardId);
            SaveCards(cards);

            // If deleted card was selected, pick the first remaining or clear
            var selectedId = HttpContext.Session.GetString("SelectedCardId");
            if (selectedId == cardId)
            {
                var first = cards.FirstOrDefault();
                HttpContext.Session.SetString("SelectedCardId", first != null ? first["id"] : "");
            }

            return Json(new { success = true });
        }

        // ─── Set the active/selected card ────────────────────────────────────
        [HttpPost]
        public IActionResult SelectCard(string cardId)
        {
            HttpContext.Session.SetString("SelectedCardId", cardId ?? "");
            return Json(new { success = true });
        }
    }
}
