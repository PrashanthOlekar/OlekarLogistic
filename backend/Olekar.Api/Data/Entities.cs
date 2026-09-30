using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Olekar.Api.Data;

// Entity classes map 1:1 to the tables created by database/OlekarLogistics_schema.sql.
// Status values are the exact strings allowed by the CHECK constraints in that script.

public static class Roles
{
    public const string Customer = "Customer";
    public const string Owner = "Owner";
    public const string Driver = "Driver";
    public const string Admin = "Admin";
}

public class User
{
    public long UserId { get; set; }
    public string Role { get; set; } = Roles.Customer;
    public string FullName { get; set; } = "";
    public string Mobile { get; set; } = "";
    public string? Email { get; set; }
    public string? PasswordHash { get; set; }
    public string PreferredLanguage { get; set; } = "en";
    public string Status { get; set; } = "Active";           // Active | PendingKyc | Blocked | Closed
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public class Customer
{
    public long CustomerId { get; set; }
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    public string? CompanyName { get; set; }
    public string? GSTIN { get; set; }
    public long? BillingAddressId { get; set; }
    [Precision(12, 2)] public decimal CreditLimit { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class Owner
{
    public long OwnerId { get; set; }
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    public string? BusinessName { get; set; }
    public byte[]? PanEncrypted { get; set; }
    public string? PanLast4 { get; set; }
    public string? AadhaarLast4 { get; set; }
    public string? AadhaarVaultRef { get; set; }
    public string? GSTIN { get; set; }
    public string KycStatus { get; set; } = "Pending";        // Pending | Approved | Rejected
    public string? RejectionReason { get; set; }
    public long? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
    [Precision(3, 2)] public decimal? Rating { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public List<Vehicle> Vehicles { get; set; } = new();
    public List<Driver> Drivers { get; set; } = new();
}

public class OwnerBankAccount
{
    public long OwnerBankAccountId { get; set; }
    public long OwnerId { get; set; }
    public string AccountHolder { get; set; } = "";
    public byte[] AccountNumberEncrypted { get; set; } = Array.Empty<byte>();
    public string AccountLast4 { get; set; } = "";
    public string IFSC { get; set; } = "";
    public string? BankName { get; set; }
    public string PennyDropStatus { get; set; } = "Pending";
    public bool IsPrimary { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Driver
{
    public long DriverId { get; set; }
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    public long? OwnerId { get; set; }
    public Owner? Owner { get; set; }
    public string LicenceNumber { get; set; } = "";
    public string LicenceClass { get; set; } = "LMV";        // LMV | TRANSPORT | HGMV | HPMV
    public DateOnly LicenceExpiry { get; set; }
    public string? AadhaarLast4 { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string KycStatus { get; set; } = "Pending";
    public string DutyStatus { get; set; } = "OffDuty";      // Available | OnTrip | OffDuty
    public string? RejectionReason { get; set; }
    public long? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }
    [Precision(3, 2)] public decimal? Rating { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public class City
{
    public int CityId { get; set; }
    public string Name { get; set; } = "";
    public string? NameKn { get; set; }
    public string State { get; set; } = "";
    [Precision(9, 6)] public decimal Latitude { get; set; }
    [Precision(9, 6)] public decimal Longitude { get; set; }
    public bool IsServiceable { get; set; } = true;
}

public class VehicleType
{
    public int VehicleTypeId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string BodyType { get; set; } = "Closed";
    public int MaxLoadKg { get; set; }
    [Precision(5, 1)] public decimal LengthFt { get; set; }
    [Precision(5, 1)] public decimal WidthFt { get; set; }
    [Precision(5, 1)] public decimal? HeightFt { get; set; }
    public string? RecommendedGoods { get; set; }
    [Precision(8, 2)] public decimal RatePerKm { get; set; }
    [Precision(10, 2)] public decimal MinFare { get; set; }
    [Precision(8, 2)] public decimal DriverBattaPerDay { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

public class GoodsCategory
{
    public int GoodsCategoryId { get; set; }
    public string Name { get; set; } = "";
    public bool RequiresEwayBill { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public class Setting
{
    public string SettingKey { get; set; } = "";
    public string SettingValue { get; set; } = "";
    public string? Description { get; set; }
    public long? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class Vehicle
{
    public long VehicleId { get; set; }
    public long OwnerId { get; set; }
    public Owner Owner { get; set; } = null!;
    public int VehicleTypeId { get; set; }
    public VehicleType VehicleType { get; set; } = null!;
    public string RegistrationNumber { get; set; } = "";
    public int CapacityKg { get; set; }
    public string? MakeModel { get; set; }
    public short? ManufactureYear { get; set; }
    public string AvailabilityStatus { get; set; } = "Available";   // Available | Busy | Maintenance
    public string VerificationStatus { get; set; } = "Pending";     // Pending | Approved | Rejected | Suspended
    public long? CurrentDriverId { get; set; }
    public Driver? CurrentDriver { get; set; }
    public int? HomeCityId { get; set; }
    [Precision(9, 6)] public decimal? LastLatitude { get; set; }
    [Precision(9, 6)] public decimal? LastLongitude { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public class Document
{
    public long DocumentId { get; set; }
    public string EntityType { get; set; } = "";     // Owner | Driver | Vehicle | Customer | Trip | Ticket | Invoice
    public long EntityId { get; set; }
    public string DocType { get; set; } = "";        // RC | Insurance | ... | POD | PickupPhoto
    public string BlobPath { get; set; } = "";
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
    public string? Sha256 { get; set; }
    public string? DocumentNumber { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string Status { get; set; } = "Pending";  // Pending | Verified | Rejected | Expired
    public string? RejectionReason { get; set; }
    public long? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public long UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    [Precision(9, 6)] public decimal? Latitude { get; set; }
    [Precision(9, 6)] public decimal? Longitude { get; set; }
}

public class Booking
{
    public long BookingId { get; set; }
    public string BookingNumber { get; set; } = null!;   // generated by the database sequence
    public long CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public string PickupAddressText { get; set; } = "";
    public int? PickupCityId { get; set; }
    public City? PickupCity { get; set; }
    [Precision(9, 6)] public decimal? PickupLatitude { get; set; }
    [Precision(9, 6)] public decimal? PickupLongitude { get; set; }
    public string? PickupContactName { get; set; }
    public string? PickupContactPhone { get; set; }
    public string DropAddressText { get; set; } = "";
    public int? DropCityId { get; set; }
    public City? DropCity { get; set; }
    [Precision(9, 6)] public decimal? DropLatitude { get; set; }
    [Precision(9, 6)] public decimal? DropLongitude { get; set; }
    public string? DropContactName { get; set; }
    public string? DropContactPhone { get; set; }
    public int GoodsCategoryId { get; set; }
    public GoodsCategory GoodsCategory { get; set; } = null!;
    public string GoodsDescription { get; set; } = "";
    public int WeightKg { get; set; }
    [Precision(14, 2)] public decimal? GoodsValue { get; set; }
    public int VehicleTypeId { get; set; }
    public VehicleType VehicleType { get; set; } = null!;
    public DateOnly PickupDate { get; set; }
    public string? PickupSlot { get; set; }
    public string? SpecialInstructions { get; set; }
    // QuotePending | Quoted | Confirmed | Assigned | InTransit | Delivered | Completed | Cancelled
    public string Status { get; set; } = "QuotePending";
    public string? CancelledReason { get; set; }
    public long? CancelledBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public List<Quote> Quotes { get; set; } = new();
}

public class Quote
{
    public long QuoteId { get; set; }
    public long BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    [Precision(8, 1)] public decimal DistanceKm { get; set; }
    [Precision(12, 2)] public decimal VehicleCost { get; set; }
    [Precision(12, 2)] public decimal DriverCost { get; set; }
    [Precision(12, 2)] public decimal LoadingCharges { get; set; }
    [Precision(12, 2)] public decimal PlatformFee { get; set; }     // Olekar commission, deducted from the owner's payout
    [Precision(12, 2)] public decimal TaxAmount { get; set; }
    [Precision(12, 2)] public decimal TotalAmount { get; set; }     // what the customer pays
    [Precision(12, 2)] public decimal OwnerPayout { get; set; }     // freight minus commission
    public DateTime ValidUntil { get; set; }
    public string Status { get; set; } = "Sent";   // Draft | Sent | Accepted | Expired | Superseded | Rejected
    public long? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAt { get; set; }
}

public class LoadOffer
{
    public long LoadOfferId { get; set; }
    public long BookingId { get; set; }
    public long OwnerId { get; set; }
    public long? VehicleId { get; set; }
    [Precision(12, 2)] public decimal OfferedPayout { get; set; }
    public string Status { get; set; } = "Offered";  // Offered | Accepted | Declined | Expired | Withdrawn
    public DateTime ExpiresAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Trip
{
    public long TripId { get; set; }
    public string TripNumber { get; set; } = null!;   // generated by the database sequence
    public long BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public long OwnerId { get; set; }
    public Owner Owner { get; set; } = null!;
    public long VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
    public long DriverId { get; set; }
    public Driver Driver { get; set; } = null!;
    // Assigned | EnRouteToPickup | AtPickup | Loaded | InTransit | AtDestination | Delivered | Completed | Cancelled
    public string Status { get; set; } = "Assigned";
    public string? PickupOtpProtected { get; set; }     // encrypted; shown only to the booking's customer
    public string? DeliveryOtpProtected { get; set; }
    public string? EwayBillNumber { get; set; }
    [Precision(8, 1)] public decimal? PlannedDistanceKm { get; set; }
    [Precision(8, 1)] public decimal? ActualDistanceKm { get; set; }
    public DateTime? ReachedPickupAt { get; set; }
    public DateTime? LoadedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? ReachedDropAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public long? PodApprovedBy { get; set; }
    public DateTime? PodApprovedAt { get; set; }
    [Precision(12, 2)] public decimal OwnerPayout { get; set; }
    [Precision(12, 2)] public decimal? DriverPay { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public List<TripEvent> Events { get; set; } = new();
}

public class TripEvent
{
    public long TripEventId { get; set; }
    public long TripId { get; set; }
    public string EventType { get; set; } = "";
    public string? Note { get; set; }
    [Precision(9, 6)] public decimal? Latitude { get; set; }
    [Precision(9, 6)] public decimal? Longitude { get; set; }
    public long? DocumentId { get; set; }
    public long CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class OtpCode
{
    public long OtpCodeId { get; set; }
    public string Mobile { get; set; } = "";
    public string Purpose { get; set; } = "Login";   // Login | Signup | Pickup | Delivery | BankChange
    public long? TripId { get; set; }
    public string CodeHash { get; set; } = "";
    public byte Attempts { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Payment
{
    public long PaymentId { get; set; }
    public long BookingId { get; set; }
    public long CustomerId { get; set; }
    public long QuoteId { get; set; }
    [Precision(12, 2)] public decimal Amount { get; set; }
    public string Method { get; set; } = "UPI";      // UPI | CreditCard | DebitCard | NetBanking | Wallet | Credit
    public string Gateway { get; set; } = "Test";
    public string? GatewayOrderId { get; set; }
    public string? GatewayPaymentId { get; set; }
    public string Status { get; set; } = "Created";  // Created | Authorized | Captured | Failed | Refunded | PartiallyRefunded
    public string? FailureReason { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Invoice
{
    public long InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = "";
    public long BookingId { get; set; }
    public long CustomerId { get; set; }
    public string? CustomerGSTIN { get; set; }
    public string PlaceOfSupply { get; set; } = "";
    [Precision(12, 2)] public decimal TaxableAmount { get; set; }
    [Precision(12, 2)] public decimal CGST { get; set; }
    [Precision(12, 2)] public decimal SGST { get; set; }
    [Precision(12, 2)] public decimal IGST { get; set; }
    [Precision(12, 2)] public decimal TotalAmount { get; set; }
    public long? PdfDocumentId { get; set; }
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
}

public class Settlement
{
    public long SettlementId { get; set; }
    public long TripId { get; set; }
    public Trip Trip { get; set; } = null!;
    public long OwnerId { get; set; }
    public Owner Owner { get; set; } = null!;
    public long? OwnerBankAccountId { get; set; }
    [Precision(12, 2)] public decimal GrossAmount { get; set; }
    [Precision(12, 2)] public decimal CommissionAmount { get; set; }
    [Precision(12, 2)] public decimal TdsAmount { get; set; }
    [Precision(12, 2)] public decimal NetAmount { get; set; }
    public string Status { get; set; } = "AwaitingPod"; // AwaitingPod | Approved | Processing | Released | Failed | OnHold
    public string? PayoutReference { get; set; }
    public string? UTR { get; set; }
    public long? ApprovedBy { get; set; }
    public long? ReleasedBy { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public class AuditLog
{
    public long AuditLogId { get; set; }
    public long? ActorUserId { get; set; }
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public long EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
