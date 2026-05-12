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
            return View();
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
                    return RedirectToAction("StaffBooking", "Booking");
                }
            }

            ViewBag.ErrorMessage = "Invalid Employee ID or Password. Try again!";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult CustomerLogin()
        {
            return View();
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
                    return RedirectToAction("GetFreeSlots", "Booking");
                }
            }
            ViewBag.ErrorMessage = "Please enter valid credentials.";
            return View();
        }

        [HttpGet]
        public IActionResult CustomerRegister()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CustomerRegister(string FullName, string Username, string Email, string PhoneNumber, string Password, string ConfirmPassword)
        {
            if (!string.IsNullOrEmpty(FullName) && !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Email) && !string.IsNullOrEmpty(PhoneNumber) && !string.IsNullOrEmpty(Password) && !string.IsNullOrEmpty(ConfirmPassword))
            {
                if (Password != ConfirmPassword)
                {
                    ViewBag.ErrorMessage = "Passwords do not match.";
                    return View();
                }

                var existingCustomer = _repository.GetCustomerByEmail(Email);
                if (existingCustomer != null)
                {
                    ViewBag.ErrorMessage = "This email address is already registered. Please login instead.";
                    return View();
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
                    return View("CustomerLogin");
                }
                catch (Exception)
                {
                    ViewBag.ErrorMessage = "An error occurred during registration. Please try again.";
                    return View();
                }
            }
            ViewBag.ErrorMessage = "Please fill in all fields to create an account.";
            return View();
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
    }
}
