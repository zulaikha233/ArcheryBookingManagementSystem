using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ArcheryAlley.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

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

            var parent = _repository.GetCustomerByEmail(email);
            if (parent == null)
                return RedirectToAction("CustomerLogin", "Account");

            string status = HttpContext.Session.GetString("CustomerStatus") ?? "Inactive";
            if (status == "Inactive" || parent.Status != "Active")
            {
                TempData["ErrorMessage"] = "You must activate your account by paying the annual fee before you can add archers.";
                return RedirectToAction("MyArchers");
            }

            if (string.IsNullOrEmpty(parent.PhoneNumber) || string.IsNullOrEmpty(parent.Address))
            {
                TempData["ErrorMessage"] = "Please complete your profile details before adding an archer. <a href='/Account/MemberProfile' class='rounded-pill' style='background: #ff4d4d; color: #000; border: none; font-size: 0.75rem; font-weight: 800; padding: 6px 14px; text-decoration: none; display: inline-flex; align-items: center; gap: 6px; margin-left: 10px; transition: 0.2s; box-shadow: 0 4px 10px rgba(255, 77, 77, 0.2);'>Go to Profile <i class='bi bi-arrow-right'></i></a>";
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
        public IActionResult AddArcherRegister(int? studentId = null)
        {
            string email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("CustomerLogin", "Account");

            var parent = _repository.GetCustomerByEmail(email);
            if (parent == null)
                return RedirectToAction("CustomerLogin", "Account");

            if (parent.Status != "Active")
            {
                TempData["ErrorMessage"] = "You must be an active member and pay the annual fee before you can register for a class.";
                return RedirectToAction("MyArchers", "Archer");
            }

            var allStudents = _repository.GetStudentsByParentId(parent.CustomerId);
            var unregisteredStudents = new List<Students>();
            foreach (var s in allStudents)
            {
                var reg = _repository.GetClassRegistrationByStudentId(s.StudentId);
                if (reg == null)
                {
                    unregisteredStudents.Add(s);
                }
            }

            if (!unregisteredStudents.Any())
            {
                TempData["ErrorMessage"] = "You have no eligible archers to register. Please add a new archer first.";
                return RedirectToAction("MyArchers", "Archer");
            }

            ViewBag.UnregisteredStudents = unregisteredStudents;
            ViewBag.PreSelectedStudentId = studentId;

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

                // User requested: Archer's pending payment should not affect the parent account's status.
                // It should remain Inactive until the parent pays the annual membership.

                return Json(new { success = true, archerName = student.FullName });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        public class BulkRegModel
        {
            public int StudentId { get; set; }
            public string PackageType { get; set; }
            public decimal PackagePrice { get; set; }
        }

        [HttpPost]
        public IActionResult ConfirmBulkClassRegistration([FromBody] List<BulkRegModel> registrations)
        {
            string email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email)) return Json(new { success = false, message = "Session expired." });

            var parent = _repository.GetCustomerByEmail(email);
            if (parent == null) return Json(new { success = false, message = "Customer not found." });

            if (registrations == null || !registrations.Any())
                return Json(new { success = false, message = "No archers selected." });

            decimal totalAmount = 0;
            string transactionId = "PEND-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

            try
            {
                foreach (var reg in registrations)
                {
                    var student = _repository.GetStudentById(reg.StudentId);
                    if (student == null || student.ParentCustomerId != parent.CustomerId) continue;

                    decimal annualFee = 80m;
                    decimal totalPrice = annualFee + reg.PackagePrice;
                    totalAmount += totalPrice;

                    var classReg = new ClassRegistrations
                    {
                        CustomerEmail = email,
                        CustomerName = student.FullName,
                        PackageType = reg.PackageType,
                        PackagePrice = reg.PackagePrice,
                        LearningMethod = "N/A",
                        LearningMethodPax = 0,
                        LearningMethodPrice = 0,
                        AnnualFee = annualFee,
                        TotalPrice = totalPrice,
                        PaymentMethod = "Pending",
                        PaymentStatus = "Pending",
                        TransactionId = transactionId, // Use SAME transaction ID for grouping!
                        StudentId = reg.StudentId
                    };

                    _repository.RegisterClassSession(classReg);
                    _repository.UpdateStudentStatus(reg.StudentId, "Registered");
                }

                return Json(new { success = true, totalCount = registrations.Count, totalPrice = totalAmount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            string csvHeader = "FullName,Age,ICNumber\n";
            byte[] fileBytes = System.Text.Encoding.UTF8.GetBytes(csvHeader);
            return File(fileBytes, "text/csv", "Archers_Template.csv");
        }

        [HttpPost]
        public async Task<IActionResult> BulkUpload(IFormFile file)
        {
            string email = HttpContext.Session.GetString("CustomerEmail");
            if (string.IsNullOrEmpty(email)) return Json(new { success = false, message = "Please login first." });

            var parent = _repository.GetCustomerByEmail(email);
            if (parent == null) return Json(new { success = false, message = "Customer not found." });

            if (file == null || file.Length == 0) return Json(new { success = false, message = "File is empty." });

            try
            {
                using (var reader = new System.IO.StreamReader(file.OpenReadStream()))
                {
                    var lines = (await reader.ReadToEndAsync()).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    int addedCount = 0;
                    
                    // Skip header (assuming first line is FullName,Age,ICNumber)
                    for (int i = 1; i < lines.Length; i++)
                    {
                        var cols = lines[i].Split(',');
                        if (cols.Length < 1 || string.IsNullOrWhiteSpace(cols[0])) continue;

                        string fullName = cols[0].Trim();
                        int? age = null;
                        if (cols.Length > 1 && int.TryParse(cols[1].Trim(), out int parsedAge))
                        {
                            age = parsedAge;
                        }
                        string icNumber = cols.Length > 2 ? cols[2].Trim() : "";

                        var student = new Students
                        {
                            FullName = fullName,
                            Age = age,
                            ICNumber = icNumber,
                            ParentCustomerId = parent.CustomerId,
                            Status = "Pending",
                            CreatedAt = DateTime.Now
                        };
                        _repository.AddStudent(student);
                        addedCount++;
                    }

                    return Json(new { success = true, message = $"Successfully added {addedCount} archers." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to parse file: " + ex.Message });
            }
        }
    }
}
