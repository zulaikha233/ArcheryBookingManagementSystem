using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ArcheryAlley.Models;
using System;
using System.Linq;

namespace ArcheryAlley.Controllers
{
    public class ArcherController : Controller
    {
        private readonly IArcheryAlleyRepository _repository;

        public ArcherController(IArcheryAlleyRepository repository)
        {
            _repository = repository;
        }



        [HttpGet]
        public IActionResult MyArchers()
        {
            string email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("CustomerLogin", "Account");

            var parent = _repository.GetCustomerByEmail(email);
            if (parent != null)
            {
                var students = _repository.GetStudentsByParentId(parent.CustomerId);
                var classRegs = _repository.GetClassRegistrationsByEmail(email);
                ViewBag.Students = students.Select(s => new ArcherViewModel {
                    StudentId = s.StudentId,
                    FullName = s.FullName,
                    ICNumber = s.ICNumber,
                    Birthday = s.Birthday,
                    Age = s.Age,
                    ClassReg = classRegs.FirstOrDefault(cr => cr.StudentId == s.StudentId)
                }).ToList();
            }

            return View("~/Views/Archer/MyArchers.cshtml");
        }

        [HttpGet]
        public IActionResult AddArcher()
        {
            string email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("CustomerLogin", "Account");

            string status = HttpContext.Session.GetString("CustomerStatus") ?? "Inactive";
            if (status == "Inactive")
            {
                TempData["ErrorMessage"] = "You must activate your account by registering for a class before you can add archers.";
                return RedirectToAction("MyArchers");
            }

            return View("~/Views/Archer/AddArcher.cshtml");
        }

        [HttpPost]
        public IActionResult AddArcher(string FullName, string ICNumber, DateTime? Birthday, int Age)
        {
            string email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("CustomerLogin", "Account");

            // Server-side fallback/validation for IC Number to compute Birthday and Age
            if (!string.IsNullOrEmpty(ICNumber))
            {
                string cleanIc = new string(ICNumber.Where(char.IsDigit).ToArray());
                if (cleanIc.Length == 12)
                {
                    if (int.TryParse(cleanIc.Substring(0, 2), out int yearPart) &&
                        int.TryParse(cleanIc.Substring(2, 2), out int monthPart) &&
                        int.TryParse(cleanIc.Substring(4, 2), out int dayPart))
                    {
                        int currentYear2Digit = DateTime.Now.Year % 100;
                        int year = yearPart + (yearPart > currentYear2Digit ? 1900 : 2000);
                        try
                        {
                            var parsedDate = new DateTime(year, monthPart, dayPart);
                            Birthday = parsedDate;

                            int calculatedAge = DateTime.Now.Year - year;
                            if (DateTime.Now.Month < monthPart || (DateTime.Now.Month == monthPart && DateTime.Now.Day < dayPart))
                            {
                                calculatedAge--;
                            }
                            Age = calculatedAge >= 0 ? calculatedAge : 0;
                        }
                        catch
                        {
                            // Keep any existing values if parsing fails
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(FullName) || string.IsNullOrEmpty(ICNumber) || !Birthday.HasValue || Age <= 0)
            {
                ViewBag.ErrorMessage = "Please provide a valid Malaysian IC Number and Full Name.";
                return View("~/Views/Archer/AddArcher.cshtml");
            }

            var parent = _repository.GetCustomerByEmail(email);
            if (parent == null)
                return RedirectToAction("CustomerLogin", "Account");

            var student = new Students
            {
                ParentCustomerId = parent.CustomerId,
                FullName = FullName,
                ICNumber = ICNumber,
                Birthday = Birthday,
                Age = Age
            };

            _repository.AddStudent(student);

            return RedirectToAction("MyArchers", "Archer");
        }

        [HttpGet]
        public IActionResult AddArcherRegister(int studentId)
        {
            string email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("CustomerLogin", "Account");

            var parent = _repository.GetCustomerByEmail(email);
            if (parent == null)
                return RedirectToAction("CustomerLogin", "Account");

            var student = _repository.GetStudentById(studentId);
            if (student == null || student.ParentCustomerId != parent.CustomerId)
                return RedirectToAction("MyArchers", "Archer");

            // Check if already registered
            var existingReg = _repository.GetClassRegistrationByStudentId(studentId);
            if (existingReg != null)
                return RedirectToAction("MyArchers", "Archer");

            ViewBag.StudentId = student.StudentId;
            ViewBag.StudentName = student.FullName;
            ViewBag.StudentAge = student.Age;

            return View("~/Views/Archer/AddArcherRegister.cshtml");
        }

        [HttpPost]
        public IActionResult CompleteArcherRegistration(
            int StudentId, string PackageType, decimal PackagePrice,
            string LearningMethod, int LearningMethodPax, decimal LearningMethodPrice,
            decimal AnnualFee, decimal TotalPrice)
        {
            string email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email))
                return Json(new { success = false, message = "Session expired. Please login again." });

            var parent = _repository.GetCustomerByEmail(email);
            if (parent == null)
                return Json(new { success = false, message = "Customer not found." });

            var student = _repository.GetStudentById(StudentId);
            if (student == null || student.ParentCustomerId != parent.CustomerId)
                return Json(new { success = false, message = "Invalid archer." });

            var registration = new ClassRegistrations
            {
                CustomerEmail = email,
                CustomerName = student.FullName,
                PackageType = PackageType,
                PackagePrice = PackagePrice,
                LearningMethod = LearningMethod,
                LearningMethodPax = LearningMethodPax,
                LearningMethodPrice = LearningMethodPrice,
                AnnualFee = AnnualFee,
                TotalPrice = TotalPrice,
                PaymentMethod = "Pending",
                PaymentStatus = "Pending",
                TransactionId = "PEND-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                StudentId = StudentId
            };

            try
            {
                _repository.RegisterClassSession(registration);
                _repository.UpdateStudentStatus(StudentId, "Registered");

                // Update parent customer status to Pending if not already
                if (parent.Status != "Active")
                {
                    _repository.UpdateCustomerStatus(email, "Pending");
                }
                HttpContext.Session.SetString("CustomerStatus", "Pending");

                return Json(new { success = true, archerName = student.FullName });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }
    }
}
