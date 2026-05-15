using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ArcheryAlley.Models;
using System;

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
                    return RedirectToAction("StaffDashBoard", "Booking");
                }
            }

            ViewBag.ErrorMessage = "Invalid Employee ID or Password. Try again!";
            return View("~/Views/Account/StaffLogin.cshtml");
        }

        public IActionResult Logout()
        {
            // Determine where to redirect based on current session
            bool isCustomer = !string.IsNullOrEmpty(HttpContext.Session.GetString("CustomerEmail"));
            
            HttpContext.Session.Clear();

            if (isCustomer)
            {
                return RedirectToAction("CustomerLogin");
            }
            else
            {
                return RedirectToAction("Login");
            }
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
                    HttpContext.Session.SetString("CustomerName", customer.FullName);
                    HttpContext.Session.SetString("CustomerPhone", customer.PhoneNumber ?? "");
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
        public IActionResult CustomerRegister(string FullName, string Username, string Email, string PhoneNumber, string Password, string ConfirmPassword)
        {
            if (!string.IsNullOrEmpty(FullName) && !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Email) && !string.IsNullOrEmpty(PhoneNumber) && !string.IsNullOrEmpty(Password) && !string.IsNullOrEmpty(ConfirmPassword))
            {
                if (Password != ConfirmPassword)
                {
                    ViewBag.ErrorMessage = "Passwords do not match.";
                    return View("~/Views/Account/MemberRegister.cshtml");
                }

                var existingCustomer = _repository.GetCustomerByEmail(Email);
                if (existingCustomer != null)
                {
                    ViewBag.ErrorMessage = "This email address is already registered. Please login instead.";
                    return View("~/Views/Account/MemberRegister.cshtml");
                }

                var newCustomer = new Customers
                {
                    FullName = FullName,
                    Username = Username,
                    Email = Email,
                    PhoneNumber = PhoneNumber,
                    Password = Password
                };

                try
                {
                    _repository.RegisterCustomer(newCustomer);
                    ViewBag.SuccessMessage = "Account created successfully! Please login.";
                    return View("~/Views/Account/MemberLogin.cshtml");
                }
                catch (Exception)
                {
                    ViewBag.ErrorMessage = "An error occurred during registration. Please try again.";
                    return View("~/Views/Account/MemberRegister.cshtml");
                }
            }
            ViewBag.ErrorMessage = "Please fill in all fields to create an account.";
            return View("~/Views/Account/MemberRegister.cshtml");
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

        [HttpGet]
        public IActionResult MemberDashboard()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("CustomerEmail")))
                return RedirectToAction("CustomerLogin");

            ViewBag.CustomerName = HttpContext.Session.GetString("CustomerName");
            return View("~/Views/Dashboard/MemberDashboard.cshtml");
        }
    }
}
