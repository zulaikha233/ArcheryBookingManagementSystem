using ArcheryAlley.Models;
using System;
using System.Collections.Generic;

namespace ArcheryAlley
{
    public interface IArcheryAlleyRepository
    {
        public int BookSlots(int Slotid, string Empid, string CustomerName);


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

        // Class Session Registration
        public void RegisterClassSession(ClassRegistrations registration);
        public List<ClassRegistrations> GetClassRegistrationsByEmail(string email);
        public List<Reservations> GetReservationsByEmail(string email);

        List <Reservations> GetReservationsByDate(DateTime Date);
        void UpdateAttendance(int reservationId, bool attended);
    }
}
