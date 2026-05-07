# Archery Alley Online Reservation System

The Archery Alley Online Reservation System allows members to reserve slots for the current day, while the admin can approve or reject these reservations. This system is built using ASP.NET Core MVC and Entity Framework Core, with Microsoft SQL Server for data storage.

## Features

### Member Features
- **Check Free Slots**: Members can view all available slots for the current day.
- **Book Slots**: Members can reserve slots for the current day.
- **Confirm Booking**: Members must provide their Employee ID to confirm the reservation.

### Admin Features
- **Approve/Reject Reservations**: Admins can approve or reject slot reservations made for the current day.
- **View Reservation Details**: Admins can view all slot reservations for the current day.
- **View Rejected Slots**: Admins can view all rejected slot details.

## Technologies Used
- **Backend**: ASP.NET Core MVC
- **Data Access**: Entity Framework Core
- **Database**: Microsoft SQL Server
- **IDE**: Microsoft Visual Studio 2017 Community Edition

## Prerequisites
- **Microsoft Visual Studio 2017 Community Edition**
- **Microsoft SQL Server 2012 Express Edition or above**
- **SQL Server Management Studio (SSMS)** for executing SQL scripts

## Project Setup

1. **Download the Project Files**  
   Download the supplied files for the Archery Alley project.

2. **Execute SQL Scripts**  
   Use SQL Server Management Studio to execute the provided SQL scripts. This will set up the required tables, stored procedures, and functions in the database.

3. **Open the Solution**  
   Open the supplied `ArcheryAlley` solution using Visual Studio 2017 Community Edition.

4. **Add a New .NET Core MVC Application**  
   Add a new .NET Core MVC application to the solution.

5. **Install the Required Packages**  
   Ensure the following packages are installed in both the class library and console application:
   - `Microsoft.EntityFrameworkCore.SqlServer`
   - `Microsoft.EntityFrameworkCore.Design`

   Additionally, in the class library, install:
   - `Microsoft.EntityFrameworkCore.Tools`
   - `Microsoft.Extensions.Configuration`
   - `Microsoft.Extensions.Configuration.Json`

## Database Design

The project utilizes the following tables:

1. **BookingSlots**  
   Stores details of the booking slots.

2. **Roles**  
   Contains member and admin details.

3. **Reservation**  
   Holds reservation details for the Archery Alley.

## Data Access Layer Implementation

The `ArcheryAlleyRepository` class provides the following methods:

- **ApproveOrReject**  
  Updates the reservation status in the `Reservation` table.
  
- **GetFreeSlots**  
  Retrieves all available slots for the current day.
  
- **GetReservedSlots**  
  Gets reserved slot details for the current day.

## Presentation Layer Implementation

The project contains the following controllers and views:

### `AdminController` Actions

1. **GetReservationDetails**  
   Displays all reserved slots for the current day.

2. **Approve**  
   Approves a reservation by setting the status to 1.

3. **Reject**  
   Rejects a reservation by setting the status to -1.

4. **GetRejectedSlots**  
   Displays all rejected slots.

### `MemberController` Actions

1. **GetFreeSlots**  
   Shows all available slots for the current day.

2. **BookSlots**  
   Allows members to reserve a slot.

3. **ConfirmBooking**  
   Confirms a reservation by validating the Employee ID.

### Views

- **GetReservationDetails**  
  Displays reserved slots with options for the admin to approve or reject each slot.
  
- **GetRejectedSlots**  
  Lists all rejected slot details.

- **GetFreeSlots**  
  Shows available slots for booking.

- **BookSlots**  
  Displays the selected slot details and captures the Employee ID for confirmation.

- **Success**  
  Displays a success message for successful operations.

- **Exception**  
  Shows an error message in case of failures.

## State Management

- Use session management to display the user type (Admin or Member) on all views.
- Implement a consistent layout for all Admin and Member views.

## Running the Application

1. **Start the SQL Server Database**  
   Make sure the SQL Server instance is running.

2. **Run the Application**  
   Launch the application in Visual Studio and navigate to the desired actions.

## Troubleshooting

- If you encounter issues with database connectivity, ensure the connection string in the `appsettings.json` file is correctly configured.
- Make sure all necessary NuGet packages are installed and up to date.

## License

This project is for educational purposes and is not intended for commercial use.

