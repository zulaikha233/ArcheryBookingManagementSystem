using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace ArcheryAlley.Models
{
    public partial class ArcheryAlleyDBContext : DbContext
    {
        public ArcheryAlleyDBContext()
        {
        }

        public ArcheryAlleyDBContext(DbContextOptions<ArcheryAlleyDBContext> options)
            : base(options)
        {
        }

        public  DbSet<BookingSlots> BookingSlots { get; set; }
        public  DbSet<Reservations> Reservations { get; set; }
        public  DbSet<Roles> Roles { get; set; }
        public  DbSet<Customers> Customers { get; set; }
        public  DbSet<Rates> Rates { get; set; }
        public  DbSet<Payments> Payments { get; set; }
        public  DbSet<Lanes> Lanes { get; set; }
        public  DbSet<Targets> Targets { get; set; }
        public  DbSet<ClassRegistrations> ClassRegistrations { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            /*if (!optionsBuilder.IsConfigured)
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json");

                var connection = builder.Build();
                string connectionString = connection.GetConnectionString("ArcheryAlleyDBConnectionString");

                optionsBuilder.UseSqlServer(connectionString);
            }*/
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BookingSlots>(entity =>
            {
                entity.HasKey(e => e.SlotId);

                entity.HasMany(e => e.Reservations)
                      .WithOne(r => r.Slot)
                      .HasForeignKey(r => r.SlotId)
                      .OnDelete(DeleteBehavior.ClientSetNull)
                      .HasConstraintName("FK_Reservations_BookingSlots");

                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);
            });

            modelBuilder.Entity<Reservations>(entity =>
            {
                entity.HasKey(e => e.ReservationId);

                entity.Property(e => e.ReservedOn)
                    .HasColumnType("datetime");

                entity.Property(e => e.TotalPrice)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.Status)
                    .HasDefaultValueSql("((1))");

                entity.Property(e => e.RateCode)
                    .HasMaxLength(20);

            });

            modelBuilder.Entity<Roles>(entity =>
            {
                entity.HasKey(e => e.EmpId);

                entity.Property(e => e.EmpId)
                    .HasMaxLength(15);

                entity.Property(e => e.EmpName)
                    .IsRequired()
                    .HasMaxLength(50);


                entity.Property(e => e.Password)
                      .IsRequired()
                      .HasMaxLength(10)
                      .HasDefaultValue("1478523690");

            }
            );



            modelBuilder.Entity<Rates>(entity =>
            {
                entity.HasKey(e => e.RateId);

                entity.Property(e => e.RateCode)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.RateName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.BasePrice)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.DiscountPercentage)
                    .HasColumnType("decimal(5,2)");

                entity.Property(e => e.FinalPrice)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);

                entity.Property(e => e.UpdatedOn)
                    .HasColumnType("datetime");
            });

            modelBuilder.Entity<Payments>(entity =>
            {
                entity.HasKey(e => e.PaymentId);
                entity.Property(e => e.PaymentDate).HasColumnType("datetime");
            });

            modelBuilder.Entity<Lanes>(entity =>
            {
                entity.HasKey(e => e.LaneId);
            });

            modelBuilder.Entity<Targets>(entity =>
            {
                entity.HasKey(e => e.TargetId);
            });
        }

    }

    
}
