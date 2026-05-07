using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace ArcheryAlley.Services
{
    public class SlotCleanerService : IHostedService
    {
        private readonly string _connectionString;

        public SlotCleanerService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("ArcheryAlleyDBConnectionString");
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await DeleteExpiredReservationsAsync();
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async Task DeleteExpiredReservationsAsync()
        {
            string query = @"
                BEGIN TRANSACTION;


                -- Now delete from Reservations
                DELETE FROM Reservations
                WHERE ReservationId IN (
                    SELECT r.ReservationId
                    FROM Reservations r
                    INNER JOIN BookingSlots bs ON r.SlotId = bs.SlotId
                    WHERE GETDATE() > bs.SlotEndTime
                );

                COMMIT TRANSACTION;
            ";

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while deleting expired reservations: {ex.Message}");
            }
        }
    }
}
