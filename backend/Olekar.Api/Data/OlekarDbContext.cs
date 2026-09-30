using Microsoft.EntityFrameworkCore;

namespace Olekar.Api.Data;

public class OlekarDbContext : DbContext
{
    public OlekarDbContext(DbContextOptions<OlekarDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<OwnerBankAccount> OwnerBankAccounts => Set<OwnerBankAccount>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<VehicleType> VehicleTypes => Set<VehicleType>();
    public DbSet<GoodsCategory> GoodsCategories => Set<GoodsCategory>();
    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<LoadOffer> LoadOffers => Set<LoadOffer>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TripEvent> TripEvents => Set<TripEvent>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Settlement> Settlements => Set<Settlement>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Table names match the SQL script (plural names in the dbo schema).
        b.Entity<User>().ToTable("Users");
        b.Entity<Customer>().ToTable("Customers");
        b.Entity<Owner>().ToTable("Owners");
        b.Entity<OwnerBankAccount>().ToTable("OwnerBankAccounts");
        b.Entity<Driver>().ToTable("Drivers");
        b.Entity<City>().ToTable("Cities");
        b.Entity<VehicleType>().ToTable("VehicleTypes");
        b.Entity<GoodsCategory>().ToTable("GoodsCategories");
        b.Entity<Setting>().ToTable("Settings").HasKey(s => s.SettingKey);
        b.Entity<Vehicle>().ToTable("Vehicles");
        b.Entity<Document>().ToTable("Documents");
        b.Entity<Booking>().ToTable("Bookings");
        b.Entity<Quote>().ToTable("Quotes");
        b.Entity<LoadOffer>().ToTable("LoadOffers");
        b.Entity<Trip>().ToTable("Trips");
        b.Entity<TripEvent>().ToTable("TripEvents");
        b.Entity<OtpCode>().ToTable("OtpCodes");
        b.Entity<Payment>().ToTable("Payments");
        b.Entity<Invoice>().ToTable("Invoices");
        b.Entity<Settlement>().ToTable("Settlements");
        b.Entity<AuditLog>().ToTable("AuditLogs");

        // Reference numbers come from SQL Server sequences (see the schema's DEFAULT constraints).
        b.Entity<Booking>().Property(x => x.BookingNumber)
            .HasDefaultValueSql("('OLK-' + CAST(NEXT VALUE FOR dbo.BookingNumberSeq AS VARCHAR(10)))")
            .ValueGeneratedOnAdd();
        b.Entity<Trip>().Property(x => x.TripNumber)
            .HasDefaultValueSql("('TRP-' + CAST(NEXT VALUE FOR dbo.TripNumberSeq AS VARCHAR(10)))")
            .ValueGeneratedOnAdd();

        // Relationships with two links to the same table are spelled out so EF never guesses.
        b.Entity<Booking>().HasOne(x => x.PickupCity).WithMany().HasForeignKey(x => x.PickupCityId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<Booking>().HasOne(x => x.DropCity).WithMany().HasForeignKey(x => x.DropCityId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<Booking>().HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId);
        b.Entity<Booking>().HasOne(x => x.VehicleType).WithMany().HasForeignKey(x => x.VehicleTypeId);
        b.Entity<Booking>().HasOne(x => x.GoodsCategory).WithMany().HasForeignKey(x => x.GoodsCategoryId);
        b.Entity<Quote>().HasOne(x => x.Booking).WithMany(x => x.Quotes).HasForeignKey(x => x.BookingId);

        b.Entity<Customer>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        b.Entity<Owner>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        b.Entity<Driver>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        b.Entity<Driver>().HasOne(x => x.Owner).WithMany(x => x.Drivers).HasForeignKey(x => x.OwnerId);

        b.Entity<Vehicle>().HasOne(x => x.Owner).WithMany(x => x.Vehicles).HasForeignKey(x => x.OwnerId);
        b.Entity<Vehicle>().HasOne(x => x.VehicleType).WithMany().HasForeignKey(x => x.VehicleTypeId);
        b.Entity<Vehicle>().HasOne(x => x.CurrentDriver).WithMany().HasForeignKey(x => x.CurrentDriverId);

        b.Entity<Trip>().HasOne(x => x.Booking).WithMany().HasForeignKey(x => x.BookingId);
        b.Entity<Trip>().HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId);
        b.Entity<Trip>().HasOne(x => x.Vehicle).WithMany().HasForeignKey(x => x.VehicleId);
        b.Entity<Trip>().HasOne(x => x.Driver).WithMany().HasForeignKey(x => x.DriverId);
        b.Entity<Trip>().HasMany(x => x.Events).WithOne().HasForeignKey(x => x.TripId);

        b.Entity<Settlement>().HasOne(x => x.Trip).WithMany().HasForeignKey(x => x.TripId);
        b.Entity<Settlement>().HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId);
    }
}
