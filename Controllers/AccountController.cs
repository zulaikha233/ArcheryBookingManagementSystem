using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ArcheryAlley.Models;
using System;
using System.Linq;
using System.Collections.Generic;

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
                return RedirectToAction("Login");

            ViewBag.StaffName = HttpContext.Session.GetString("UserName");
            ViewBag.EmpId = HttpContext.Session.GetString("EmpId");
            return View("~/Views/Staff/StaffProfile.cshtml");
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
                    return RedirectToAction("ManageSlots", "Slot");
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
            if (!string.IsNullOrEmpty(Email) && !string.IsNullOrEmpty(Password))
            {
                var customer = _repository.GetCustomerLogin(Email, Password);
                if (customer != null)
                {
                    HttpContext.Session.SetString("CustomerEmail", customer.Email);
                    HttpContext.Session.SetString("CustomerName", customer.FullName ?? customer.Username);
                    HttpContext.Session.SetString("CustomerPhone", customer.PhoneNumber ?? "");
                    HttpContext.Session.SetString("CustomerUsername", customer.Username);
                    HttpContext.Session.SetString("CustomerStatus", customer.Status ?? "Inactive");
                    HttpContext.Session.SetString("UserRole", "Member"); // Assign Member role
                    return RedirectToAction("MemberDashboard", "Account");
                }
            }
            ViewBag.ErrorMessage = "Please enter valid credentials.";
            return View("~/Views/Account/MemberLogin.cshtml");
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
                Password = Password
            };

            try
            {
                _repository.RegisterCustomer(newCustomer);
                
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
        public IActionResult ClassRegistration()
        {
            string email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("CustomerLogin");
                
            return View("~/Views/Account/ClassRegistration.cshtml");
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
            string FullName, string PhoneNumber, DateTime Birthday, string Address,
            string PackageType, decimal PackagePrice, string LearningMethod, int LearningMethodPax, decimal LearningMethodPrice,
            decimal AnnualFee, decimal TotalPrice, string PaymentMethod)
        {
            string email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email))
            {
                return Json(new { success = false, message = "Session expired. Please login again." });
            }

            if (string.IsNullOrEmpty(FullName) || string.IsNullOrEmpty(PhoneNumber) || Birthday == default || string.IsNullOrEmpty(Address))
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
            customer.Birthday = Birthday;
            customer.Address = Address;
            customer.Status = "Active";


            var registration = new ClassRegistrations
            {
                CustomerEmail = email,
                CustomerName = FullName,
                PackageType = PackageType,
                PackagePrice = PackagePrice,
                LearningMethod = LearningMethod,
                LearningMethodPax = LearningMethodPax,
                LearningMethodPrice = LearningMethodPrice,
                AnnualFee = AnnualFee,
                TotalPrice = TotalPrice,
                PaymentMethod = PaymentMethod == "fpx" ? "Online (FPX)" : "Credit/Debit Card",
                PaymentStatus = "Success",
                TransactionId = "TXN-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper()
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
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
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

            ViewBag.ClassRegistrations = _repository.GetClassRegistrationsByEmail(email);

            return View("~/Views/Dashboard/MemberDashboard.cshtml");
        }
    }
}
