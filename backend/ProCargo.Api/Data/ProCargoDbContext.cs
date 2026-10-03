using Microsoft.EntityFrameworkCore;
using ProCargo.Api.Data.Entities;

namespace ProCargo.Api.Data;

/// <summary>
/// Entity Framework's view of the ProCargo SQL Server database.
/// The tables themselves are created by database/ProCargo.sql, not by EF migrations.
/// </summary>
public class ProCargoDbContext : DbContext
{
    public ProCargoDbContext(DbContextOptions<ProCargoDbContext> options)
        : base(options)
    {
    }

    // Accounts
    public DbSet<User> Users => Set<User>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();

    // Customers and partners
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<OwnerBankAccount> OwnerBankAccounts => Set<OwnerBankAccount>();
    public DbSet<Driver> Drivers => Set<Driver>();

    // Master data
    public DbSet<City> Cities => Set<City>();
    public DbSet<VehicleType> VehicleTypes => Set<VehicleType>();
    public DbSet<GoodsCategory> GoodsCategories => Set<GoodsCategory>();
    public DbSet<Setting> Settings => Set<Setting>();

    // Fleet and documents
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Document> Documents => Set<Document>();

    // Bookings and trips
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<LoadOffer> LoadOffers => Set<LoadOffer>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TripEvent> TripEvents => Set<TripEvent>();

    // Money
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Settlement> Settlements => Set<Settlement>();

    // Platform
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        MapTableNames(modelBuilder);
        MapGeneratedNumbers(modelBuilder);
        MapRelationships(modelBuilder);
    }

    /// <summary>Each class maps to the table of the same name (plural) in the dbo schema.</summary>
    private static void MapTableNames(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().ToTable("Users");
        modelBuilder.Entity<OtpCode>().ToTable("OtpCodes");
        modelBuilder.Entity<Customer>().ToTable("Customers");
        modelBuilder.Entity<Owner>().ToTable("Owners");
        modelBuilder.Entity<OwnerBankAccount>().ToTable("OwnerBankAccounts");
        modelBuilder.Entity<Driver>().ToTable("Drivers");
        modelBuilder.Entity<City>().ToTable("Cities");
        modelBuilder.Entity<VehicleType>().ToTable("VehicleTypes");
        modelBuilder.Entity<GoodsCategory>().ToTable("GoodsCategories");
        modelBuilder.Entity<Setting>().ToTable("Settings").HasKey(setting => setting.SettingKey);
        modelBuilder.Entity<Vehicle>().ToTable("Vehicles");
        modelBuilder.Entity<Document>().ToTable("Documents");
        modelBuilder.Entity<Booking>().ToTable("Bookings");
        modelBuilder.Entity<Quote>().ToTable("Quotes");
        modelBuilder.Entity<LoadOffer>().ToTable("LoadOffers");
        modelBuilder.Entity<Trip>().ToTable("Trips");
        modelBuilder.Entity<TripEvent>().ToTable("TripEvents");
        modelBuilder.Entity<Payment>().ToTable("Payments");
        modelBuilder.Entity<Invoice>().ToTable("Invoices");
        modelBuilder.Entity<Settlement>().ToTable("Settlements");
        modelBuilder.Entity<AuditLog>().ToTable("AuditLogs");
    }

    /// <summary>Booking and trip numbers (PC-24001, TRP-24001) are filled in by SQL Server sequences.</summary>
    private static void MapGeneratedNumbers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>()
            .Property(booking => booking.BookingNumber)
            .HasDefaultValueSql("('PC-' + CAST(NEXT VALUE FOR dbo.BookingNumberSeq AS VARCHAR(10)))")
            .ValueGeneratedOnAdd();

        modelBuilder.Entity<Trip>()
            .Property(trip => trip.TripNumber)
            .HasDefaultValueSql("('TRP-' + CAST(NEXT VALUE FOR dbo.TripNumberSeq AS VARCHAR(10)))")
            .ValueGeneratedOnAdd();
    }

    /// <summary>Links between tables. Spelled out so EF never has to guess.</summary>
    private static void MapRelationships(ModelBuilder modelBuilder)
    {
        // A user can be a customer, an owner or a driver.
        modelBuilder.Entity<Customer>()
            .HasOne(customer => customer.User)
            .WithMany()
            .HasForeignKey(customer => customer.UserId);

        modelBuilder.Entity<Owner>()
            .HasOne(owner => owner.User)
            .WithMany()
            .HasForeignKey(owner => owner.UserId);

        modelBuilder.Entity<Driver>()
            .HasOne(driver => driver.User)
            .WithMany()
            .HasForeignKey(driver => driver.UserId);

        // Owners have drivers and vehicles.
        modelBuilder.Entity<Driver>()
            .HasOne(driver => driver.Owner)
            .WithMany(owner => owner.Drivers)
            .HasForeignKey(driver => driver.OwnerId);

        modelBuilder.Entity<Vehicle>()
            .HasOne(vehicle => vehicle.Owner)
            .WithMany(owner => owner.Vehicles)
            .HasForeignKey(vehicle => vehicle.OwnerId);

        modelBuilder.Entity<Vehicle>()
            .HasOne(vehicle => vehicle.VehicleType)
            .WithMany()
            .HasForeignKey(vehicle => vehicle.VehicleTypeId);

        modelBuilder.Entity<Vehicle>()
            .HasOne(vehicle => vehicle.CurrentDriver)
            .WithMany()
            .HasForeignKey(vehicle => vehicle.CurrentDriverId);

        // A booking has two cities, so both links are named.
        modelBuilder.Entity<Booking>()
            .HasOne(booking => booking.Customer)
            .WithMany()
            .HasForeignKey(booking => booking.CustomerId);

        modelBuilder.Entity<Booking>()
            .HasOne(booking => booking.PickupCity)
            .WithMany()
            .HasForeignKey(booking => booking.PickupCityId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Booking>()
            .HasOne(booking => booking.DropCity)
            .WithMany()
            .HasForeignKey(booking => booking.DropCityId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<Booking>()
            .HasOne(booking => booking.VehicleType)
            .WithMany()
            .HasForeignKey(booking => booking.VehicleTypeId);

        modelBuilder.Entity<Booking>()
            .HasOne(booking => booking.GoodsCategory)
            .WithMany()
            .HasForeignKey(booking => booking.GoodsCategoryId);

        modelBuilder.Entity<Quote>()
            .HasOne(quote => quote.Booking)
            .WithMany(booking => booking.Quotes)
            .HasForeignKey(quote => quote.BookingId);

        // A trip joins a booking to an owner, a vehicle and a driver.
        modelBuilder.Entity<Trip>()
            .HasOne(trip => trip.Booking)
            .WithMany()
            .HasForeignKey(trip => trip.BookingId);

        modelBuilder.Entity<Trip>()
            .HasOne(trip => trip.Owner)
            .WithMany()
            .HasForeignKey(trip => trip.OwnerId);

        modelBuilder.Entity<Trip>()
            .HasOne(trip => trip.Vehicle)
            .WithMany()
            .HasForeignKey(trip => trip.VehicleId);

        modelBuilder.Entity<Trip>()
            .HasOne(trip => trip.Driver)
            .WithMany()
            .HasForeignKey(trip => trip.DriverId);

        modelBuilder.Entity<Trip>()
            .HasMany(trip => trip.Events)
            .WithOne()
            .HasForeignKey(tripEvent => tripEvent.TripId);

        // Owner payouts.
        modelBuilder.Entity<Settlement>()
            .HasOne(settlement => settlement.Trip)
            .WithMany()
            .HasForeignKey(settlement => settlement.TripId);

        modelBuilder.Entity<Settlement>()
            .HasOne(settlement => settlement.Owner)
            .WithMany()
            .HasForeignKey(settlement => settlement.OwnerId);
    }
}
