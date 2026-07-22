using System;
using System.Collections.Generic;
using ArcheryAlley.Controllers;
using ArcheryAlley.Models;

namespace ArcheryAlley
{
    public interface IArcheryAlleyRepository
    {
        public int BookSlots(int Slotid, string Empid, string CustomerName, int? StudentId = null);


        public Roles GetRoleByEmpId(string empId);


        public void AddSlot(TimeSpan st, TimeSpan et);

        public void DeleteSlot(int SlotId);

        public void UpdateSlot(int SlotId, TimeSpan newStart, TimeSpan newEnd);

        public string GetAdminName(string role);

        public List<Reservations> GetReservations();

        public List<BookingSlots> GetBookingSlots();

        public List<Reservations> GetReservationDetails();

        public List<BookingSlots> AvailableSlots(DateTime dt);

        public List<int> GetAvailableTargets(int slotId, DateTime date, int duration = 1);

        public int CreateReservation(Reservations reservation);

        public string GetNextEmpId();

        public void RegisterCustomer(Customers customer);
        public Customers GetCustomerLogin(string email, string password);
        public Customers GetCustomerByEmail(string email);
        public void UpdateCustomerStatus(string email, string status);
        public void UpdateCustomer(Customers customer);

        public void RegisterStaff(Roles role);

        public List<BookingSlots> SeedFixedSlots();

        // Rate management
        public List<Rates> GetAllRates();
        public Rates GetRateById(int rateId);
        public void SaveRate(Rates rate, string updatedByEmpId);
        public void AddRate(Rates rate, string createdByEmpId);
        public void DeleteRate(int rateId);
        public void SeedDefaultRates();
        public void ToggleRateActive(int rateId, string updatedByEmpId);
        
        // Lane & Target Management
        public List<Lanes> GetAllLanes();
        public List<Targets> GetAllTargets();
        public void ToggleLaneStatus(int laneId);
        public void ToggleTargetStatus(int targetId);
        public void SeedDefaultLanesAndTargets();

        // Payment Management
        public void AddPayment(Payments payment);
        public List<Payments> GetPaymentsByEmail(string email);
        
        // Membership Management
        public IEnumerable<MembershipPayments> GetMembershipPaymentsByEmail(string email);
        public void AddMembershipPayment(MembershipPayments mp);

        // Class Session Registration
        public void RegisterClassSession(ClassRegistrations registration);
        public List<ClassRegistrations> GetClassRegistrationsByEmail(string email);
        public List<Reservations> GetReservationsByEmail(string email);
        public void RemoveClassRegistration(int classRegId);

        // Student (Child) Management
        public void AddStudent(Students student);
        public List<Students> GetStudentsByParentId(int customerId);
        public Students GetStudentById(int studentId);
        public void RemoveStudent(int studentId);
        public ClassRegistrations GetClassRegistrationByStudentId(int studentId);
        public void UpdateStudentStatus(int studentId, string status);
        public void UpdateStudentProfile(int studentId, string fullName);
        public List<ClassRegistrations> GetPendingPaymentsByEmail(string email);
        public void ClearPendingPaymentsByEmail(string email, string? type = null, int? id = null);

        // Attendance Management
        public List<Reservations> GetReservationsByDate(DateTime date);
        public void UpdateAttendance(int reservationId, bool attended);
        public void UpdateAbsentReason(int groupId, string reason);

        //Performance Reporting
        List<PerformanceReports> GetReportsByStudent(string studentName, string level);
        void AddPerformanceReport(PerformanceReports report);
        void UpdateStudentLevel(string studentName, string newLevel);

        List<Students> GetAllArchers();

        //staff
        Roles GetStaffProfile(string empId);
        void UpdateStaffProfile(Roles role);

        CoachAttendance GetTodayAttendance(string empId);
        void ClockIn(string empId);
        void ClockOut(string empId);
        List<CoachAttendance> GetTodayCoachAttendance();
        List<Roles> GetAllStaff();
        List<CoachAttendance> GetCoachAttendanceHistory(DateTime from, DateTime to);
        // Staff Account Management
        void UpdateStaffPassword(string empId, string newPassword);
        void DeactivateStaff(string empId);
        Roles GetStaffById(string empId);
    }
}
