/* =====================================================================
   Olekar Logistics — SQL Server schema
   Target: SQL Server 2019+ / Azure SQL Database
   Creates 30 tables in 8 modules, keys, indexes, and starting master data.
   Conventions:
     - BIGINT IDENTITY primary keys named <Table>Id
     - Money DECIMAL(12,2) in INR; times DATETIME2(0) in UTC
     - Status codes are VARCHAR with CHECK constraints
     - No hard deletes: use IsActive / Status; changes go to AuditLogs
   Sensitive columns (PAN, bank account) are VARBINARY: encrypt them in the
   API (or use SQL Server Always Encrypted). Never store full Aadhaar numbers.
   ===================================================================== */

IF DB_ID('OlekarLogistics') IS NULL
    CREATE DATABASE OlekarLogistics;
GO
USE OlekarLogistics;
GO

/* ---------- Sequences for public reference numbers ---------- */
CREATE SEQUENCE dbo.BookingNumberSeq  AS INT START WITH 24001 INCREMENT BY 1;
CREATE SEQUENCE dbo.TripNumberSeq     AS INT START WITH 24001 INCREMENT BY 1;
CREATE SEQUENCE dbo.InvoiceNumberSeq  AS INT START WITH 1     INCREMENT BY 1;
CREATE SEQUENCE dbo.TicketNumberSeq   AS INT START WITH 1001  INCREMENT BY 1;
GO

/* =====================================================================
   MODULE 1 — ACCOUNTS
   ===================================================================== */
CREATE TABLE dbo.Users (
    UserId              BIGINT IDENTITY(1,1) CONSTRAINT PK_Users PRIMARY KEY,
    Role                VARCHAR(20)   NOT NULL CONSTRAINT CK_Users_Role CHECK (Role IN ('Customer','Owner','Driver','Admin')),
    FullName            NVARCHAR(150) NOT NULL,
    Mobile              VARCHAR(15)   NOT NULL,
    Email               NVARCHAR(200) NULL,
    PasswordHash        VARCHAR(200)  NULL,          -- NULL for OTP-only users (drivers, owners)
    PreferredLanguage   CHAR(2)       NOT NULL CONSTRAINT DF_Users_Lang DEFAULT ('en') CONSTRAINT CK_Users_Lang CHECK (PreferredLanguage IN ('en','kn','hi','mr','ta','te')),
    Status              VARCHAR(20)   NOT NULL CONSTRAINT DF_Users_Status DEFAULT ('Active') CONSTRAINT CK_Users_Status CHECK (Status IN ('Active','PendingKyc','Blocked','Closed')),
    LastLoginAt         DATETIME2(0)  NULL,
    CreatedAt           DATETIME2(0)  NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt           DATETIME2(0)  NULL,
    RowVersion          ROWVERSION
);
CREATE UNIQUE INDEX UX_Users_Mobile ON dbo.Users(Mobile);
CREATE UNIQUE INDEX UX_Users_Email  ON dbo.Users(Email) WHERE Email IS NOT NULL;
GO

CREATE TABLE dbo.RefreshTokens (
    RefreshTokenId      BIGINT IDENTITY(1,1) CONSTRAINT PK_RefreshTokens PRIMARY KEY,
    UserId              BIGINT        NOT NULL CONSTRAINT FK_RefreshTokens_Users REFERENCES dbo.Users(UserId),
    TokenHash           VARCHAR(128)  NOT NULL,
    DeviceInfo          NVARCHAR(200) NULL,
    ExpiresAt           DATETIME2(0)  NOT NULL,
    RevokedAt           DATETIME2(0)  NULL,
    CreatedAt           DATETIME2(0)  NOT NULL CONSTRAINT DF_RefreshTokens_CreatedAt DEFAULT (SYSUTCDATETIME())
);
CREATE UNIQUE INDEX UX_RefreshTokens_Hash ON dbo.RefreshTokens(TokenHash);
GO

CREATE TABLE dbo.Addresses (
    AddressId           BIGINT IDENTITY(1,1) CONSTRAINT PK_Addresses PRIMARY KEY,
    UserId              BIGINT        NOT NULL CONSTRAINT FK_Addresses_Users REFERENCES dbo.Users(UserId),
    Label               NVARCHAR(60)  NULL,          -- "Peenya warehouse"
    ContactName         NVARCHAR(150) NULL,
    ContactPhone        VARCHAR(15)   NULL,
    Line1               NVARCHAR(250) NOT NULL,
    Line2               NVARCHAR(250) NULL,
    City                NVARCHAR(100) NOT NULL,
    State               NVARCHAR(100) NOT NULL,
    Pincode             CHAR(6)       NOT NULL,
    Latitude            DECIMAL(9,6)  NULL,
    Longitude           DECIMAL(9,6)  NULL,
    IsActive            BIT           NOT NULL CONSTRAINT DF_Addresses_IsActive DEFAULT (1),
    CreatedAt           DATETIME2(0)  NOT NULL CONSTRAINT DF_Addresses_CreatedAt DEFAULT (SYSUTCDATETIME())
);
CREATE INDEX IX_Addresses_User ON dbo.Addresses(UserId);
GO

/* =====================================================================
   MODULE 2 — CUSTOMERS
   ===================================================================== */
CREATE TABLE dbo.Customers (
    CustomerId          BIGINT IDENTITY(1,1) CONSTRAINT PK_Customers PRIMARY KEY,
    UserId              BIGINT        NOT NULL CONSTRAINT FK_Customers_Users REFERENCES dbo.Users(UserId),
    CompanyName         NVARCHAR(200) NULL,          -- NULL for individuals
    GSTIN               CHAR(15)      NULL,
    BillingAddressId    BIGINT        NULL CONSTRAINT FK_Customers_Addresses REFERENCES dbo.Addresses(AddressId),
    CreditLimit         DECIMAL(12,2) NOT NULL CONSTRAINT DF_Customers_Credit DEFAULT (0),
    CreatedAt           DATETIME2(0)  NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt           DATETIME2(0)  NULL
);
CREATE UNIQUE INDEX UX_Customers_User ON dbo.Customers(UserId);
GO

/* =====================================================================
   MODULE 3 — PARTNERS (owners, bank accounts, drivers)
   ===================================================================== */
CREATE TABLE dbo.Owners (
    OwnerId             BIGINT IDENTITY(1,1) CONSTRAINT PK_Owners PRIMARY KEY,
    UserId              BIGINT         NOT NULL CONSTRAINT FK_Owners_Users REFERENCES dbo.Users(UserId),
    BusinessName        NVARCHAR(200)  NULL,
    PanEncrypted        VARBINARY(256) NULL,
    PanLast4            CHAR(4)        NULL,
    AadhaarLast4        CHAR(4)        NULL,          -- never store the full Aadhaar number
    AadhaarVaultRef     VARCHAR(100)   NULL,          -- reference token from the KYC provider
    GSTIN               CHAR(15)       NULL,
    KycStatus           VARCHAR(20)    NOT NULL CONSTRAINT DF_Owners_Kyc DEFAULT ('Pending') CONSTRAINT CK_Owners_Kyc CHECK (KycStatus IN ('Pending','Approved','Rejected')),
    RejectionReason     NVARCHAR(500)  NULL,
    VerifiedBy          BIGINT         NULL CONSTRAINT FK_Owners_VerifiedBy REFERENCES dbo.Users(UserId),
    VerifiedAt          DATETIME2(0)   NULL,
    Rating              DECIMAL(3,2)   NULL,
    CreatedAt           DATETIME2(0)   NOT NULL CONSTRAINT DF_Owners_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt           DATETIME2(0)   NULL,
    RowVersion          ROWVERSION
);
CREATE UNIQUE INDEX UX_Owners_User ON dbo.Owners(UserId);
CREATE INDEX IX_Owners_Kyc ON dbo.Owners(KycStatus);
GO

CREATE TABLE dbo.OwnerBankAccounts (
    OwnerBankAccountId      BIGINT IDENTITY(1,1) CONSTRAINT PK_OwnerBankAccounts PRIMARY KEY,
    OwnerId                 BIGINT         NOT NULL CONSTRAINT FK_OwnerBankAccounts_Owners REFERENCES dbo.Owners(OwnerId),
    AccountHolder           NVARCHAR(150)  NOT NULL,
    AccountNumberEncrypted  VARBINARY(256) NOT NULL,
    AccountLast4            CHAR(4)        NOT NULL,
    IFSC                    CHAR(11)       NOT NULL,
    BankName                NVARCHAR(100)  NULL,
    PennyDropStatus         VARCHAR(20)    NOT NULL CONSTRAINT DF_OBA_PennyDrop DEFAULT ('Pending') CONSTRAINT CK_OBA_PennyDrop CHECK (PennyDropStatus IN ('Pending','Verified','NameMismatch','Failed')),
    IsPrimary               BIT            NOT NULL CONSTRAINT DF_OBA_Primary DEFAULT (1),
    IsActive                BIT            NOT NULL CONSTRAINT DF_OBA_Active DEFAULT (1),
    CreatedAt               DATETIME2(0)   NOT NULL CONSTRAINT DF_OBA_CreatedAt DEFAULT (SYSUTCDATETIME())
);
CREATE UNIQUE INDEX UX_OBA_OnePrimary ON dbo.OwnerBankAccounts(OwnerId) WHERE IsPrimary = 1 AND IsActive = 1;
GO

CREATE TABLE dbo.Drivers (
    DriverId                BIGINT IDENTITY(1,1) CONSTRAINT PK_Drivers PRIMARY KEY,
    UserId                  BIGINT        NOT NULL CONSTRAINT FK_Drivers_Users REFERENCES dbo.Users(UserId),
    OwnerId                 BIGINT        NULL CONSTRAINT FK_Drivers_Owners REFERENCES dbo.Owners(OwnerId),  -- NULL = owner-driver or freelance
    LicenceNumber           VARCHAR(20)   NOT NULL,
    LicenceClass            VARCHAR(20)   NOT NULL CONSTRAINT CK_Drivers_Class CHECK (LicenceClass IN ('LMV','TRANSPORT','HGMV','HPMV')),
    LicenceExpiry           DATE          NOT NULL,
    AadhaarLast4            CHAR(4)       NULL,
    EmergencyContactName    NVARCHAR(150) NULL,
    EmergencyContactPhone   VARCHAR(15)   NULL,
    KycStatus               VARCHAR(20)   NOT NULL CONSTRAINT DF_Drivers_Kyc DEFAULT ('Pending') CONSTRAINT CK_Drivers_Kyc CHECK (KycStatus IN ('Pending','Approved','Rejected')),
    DutyStatus              VARCHAR(20)   NOT NULL CONSTRAINT DF_Drivers_Duty DEFAULT ('OffDuty') CONSTRAINT CK_Drivers_Duty CHECK (DutyStatus IN ('Available','OnTrip','OffDuty')),
    RejectionReason         NVARCHAR(500) NULL,
    VerifiedBy              BIGINT        NULL CONSTRAINT FK_Drivers_VerifiedBy REFERENCES dbo.Users(UserId),
    VerifiedAt              DATETIME2(0)  NULL,
    Rating                  DECIMAL(3,2)  NULL,
    CreatedAt               DATETIME2(0)  NOT NULL CONSTRAINT DF_Drivers_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt               DATETIME2(0)  NULL,
    RowVersion              ROWVERSION
);
CREATE UNIQUE INDEX UX_Drivers_User    ON dbo.Drivers(UserId);
CREATE UNIQUE INDEX UX_Drivers_Licence ON dbo.Drivers(LicenceNumber);
CREATE INDEX IX_Drivers_Owner ON dbo.Drivers(OwnerId);
GO

/* =====================================================================
   MODULE 4 — MASTER DATA
   ===================================================================== */
CREATE TABLE dbo.Cities (
    CityId          INT IDENTITY(1,1) CONSTRAINT PK_Cities PRIMARY KEY,
    Name            NVARCHAR(100) NOT NULL,
    NameKn          NVARCHAR(100) NULL,
    State           NVARCHAR(100) NOT NULL,
    Latitude        DECIMAL(9,6)  NOT NULL,
    Longitude       DECIMAL(9,6)  NOT NULL,
    IsServiceable   BIT           NOT NULL CONSTRAINT DF_Cities_Svc DEFAULT (1)
);
CREATE UNIQUE INDEX UX_Cities_Name_State ON dbo.Cities(Name, State);
GO

CREATE TABLE dbo.VehicleTypes (
    VehicleTypeId       INT IDENTITY(1,1) CONSTRAINT PK_VehicleTypes PRIMARY KEY,
    Code                VARCHAR(20)   NOT NULL,
    Name                NVARCHAR(60)  NOT NULL,
    BodyType            VARCHAR(20)   NOT NULL CONSTRAINT CK_VT_Body CHECK (BodyType IN ('Closed','Open','Container','Flatbed')),
    MaxLoadKg           INT           NOT NULL,
    LengthFt            DECIMAL(5,1)  NOT NULL,
    WidthFt             DECIMAL(5,1)  NOT NULL,
    HeightFt            DECIMAL(5,1)  NULL,
    RecommendedGoods    NVARCHAR(200) NULL,
    RatePerKm           DECIMAL(8,2)  NOT NULL,
    MinFare             DECIMAL(10,2) NOT NULL,
    DriverBattaPerDay   DECIMAL(8,2)  NOT NULL,
    IsActive            BIT           NOT NULL CONSTRAINT DF_VT_Active DEFAULT (1),
    SortOrder           INT           NOT NULL CONSTRAINT DF_VT_Sort DEFAULT (0)
);
CREATE UNIQUE INDEX UX_VehicleTypes_Code ON dbo.VehicleTypes(Code);
GO

CREATE TABLE dbo.GoodsCategories (
    GoodsCategoryId     INT IDENTITY(1,1) CONSTRAINT PK_GoodsCategories PRIMARY KEY,
    Name                NVARCHAR(100) NOT NULL,
    RequiresEwayBill    BIT           NOT NULL CONSTRAINT DF_GC_Eway DEFAULT (1),
    IsActive            BIT           NOT NULL CONSTRAINT DF_GC_Active DEFAULT (1)
);
GO

CREATE TABLE dbo.Settings (
    SettingKey      VARCHAR(60)   NOT NULL CONSTRAINT PK_Settings PRIMARY KEY,
    SettingValue    NVARCHAR(400) NOT NULL,
    Description     NVARCHAR(400) NULL,
    UpdatedBy       BIGINT        NULL CONSTRAINT FK_Settings_Users REFERENCES dbo.Users(UserId),
    UpdatedAt       DATETIME2(0)  NULL
);
GO

/* =====================================================================
   MODULE 5 — FLEET & DOCUMENTS
   ===================================================================== */
CREATE TABLE dbo.Vehicles (
    VehicleId               BIGINT IDENTITY(1,1) CONSTRAINT PK_Vehicles PRIMARY KEY,
    OwnerId                 BIGINT        NOT NULL CONSTRAINT FK_Vehicles_Owners REFERENCES dbo.Owners(OwnerId),
    VehicleTypeId           INT           NOT NULL CONSTRAINT FK_Vehicles_Types  REFERENCES dbo.VehicleTypes(VehicleTypeId),
    RegistrationNumber      VARCHAR(15)   NOT NULL,     -- stored without spaces: KA01AB4521
    CapacityKg              INT           NOT NULL,
    MakeModel               NVARCHAR(100) NULL,
    ManufactureYear         SMALLINT      NULL,
    AvailabilityStatus      VARCHAR(20)   NOT NULL CONSTRAINT DF_Vehicles_Avail DEFAULT ('Available') CONSTRAINT CK_Vehicles_Avail CHECK (AvailabilityStatus IN ('Available','Busy','Maintenance')),
    VerificationStatus      VARCHAR(20)   NOT NULL CONSTRAINT DF_Vehicles_Verif DEFAULT ('Pending') CONSTRAINT CK_Vehicles_Verif CHECK (VerificationStatus IN ('Pending','Approved','Rejected','Suspended')),
    CurrentDriverId         BIGINT        NULL CONSTRAINT FK_Vehicles_Drivers REFERENCES dbo.Drivers(DriverId),
    HomeCityId              INT           NULL CONSTRAINT FK_Vehicles_Cities REFERENCES dbo.Cities(CityId),
    LastLatitude            DECIMAL(9,6)  NULL,
    LastLongitude           DECIMAL(9,6)  NULL,
    LastSeenAt              DATETIME2(0)  NULL,
    CreatedAt               DATETIME2(0)  NOT NULL CONSTRAINT DF_Vehicles_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt               DATETIME2(0)  NULL,
    RowVersion              ROWVERSION
);
CREATE UNIQUE INDEX UX_Vehicles_RegNo ON dbo.Vehicles(RegistrationNumber);
CREATE INDEX IX_Vehicles_Owner ON dbo.Vehicles(OwnerId);
CREATE INDEX IX_Vehicles_Match ON dbo.Vehicles(VehicleTypeId, AvailabilityStatus, VerificationStatus) INCLUDE (LastLatitude, LastLongitude);
GO

CREATE TABLE dbo.Documents (
    DocumentId          BIGINT IDENTITY(1,1) CONSTRAINT PK_Documents PRIMARY KEY,
    EntityType          VARCHAR(20)   NOT NULL CONSTRAINT CK_Docs_Entity CHECK (EntityType IN ('Owner','Driver','Vehicle','Customer','Trip','Ticket','Invoice')),
    EntityId            BIGINT        NOT NULL,
    DocType             VARCHAR(30)   NOT NULL CONSTRAINT CK_Docs_Type CHECK (DocType IN (
                            'RC','Insurance','Fitness','Permit','PUC',
                            'Aadhaar','PAN','Licence','DriverPhoto','CancelledCheque','GST',
                            'PickupPhoto','POD','DeliveryPhoto','EwayBill','InvoicePdf','Other')),
    BlobPath            NVARCHAR(400) NOT NULL,      -- container/folder/file in Azure Blob Storage (private)
    FileName            NVARCHAR(200) NOT NULL,
    ContentType         VARCHAR(100)  NOT NULL,
    SizeBytes           BIGINT        NOT NULL,
    Sha256              CHAR(64)      NULL,          -- detects re-used / tampered files
    DocumentNumber      NVARCHAR(60)  NULL,          -- policy no., permit no. (not Aadhaar)
    ExpiryDate          DATE          NULL,
    Status              VARCHAR(20)   NOT NULL CONSTRAINT DF_Docs_Status DEFAULT ('Pending') CONSTRAINT CK_Docs_Status CHECK (Status IN ('Pending','Verified','Rejected','Expired')),
    RejectionReason     NVARCHAR(500) NULL,
    ReviewedBy          BIGINT        NULL CONSTRAINT FK_Docs_ReviewedBy REFERENCES dbo.Users(UserId),
    ReviewedAt          DATETIME2(0)  NULL,
    UploadedBy          BIGINT        NOT NULL CONSTRAINT FK_Docs_UploadedBy REFERENCES dbo.Users(UserId),
    UploadedAt          DATETIME2(0)  NOT NULL CONSTRAINT DF_Docs_UploadedAt DEFAULT (SYSUTCDATETIME()),
    Latitude            DECIMAL(9,6)  NULL,          -- where a trip photo / POD was taken
    Longitude           DECIMAL(9,6)  NULL
);
CREATE INDEX IX_Docs_Entity ON dbo.Documents(EntityType, EntityId, DocType);
CREATE INDEX IX_Docs_Review ON dbo.Documents(Status) WHERE Status = 'Pending';
CREATE INDEX IX_Docs_Expiry ON dbo.Documents(ExpiryDate) WHERE ExpiryDate IS NOT NULL;
GO

/* =====================================================================
   MODULE 6 — BOOKINGS & TRIPS
   ===================================================================== */
CREATE TABLE dbo.Bookings (
    BookingId               BIGINT IDENTITY(1,1) CONSTRAINT PK_Bookings PRIMARY KEY,
    BookingNumber           VARCHAR(20)   NOT NULL CONSTRAINT DF_Bookings_No DEFAULT ('OLK-' + CAST(NEXT VALUE FOR dbo.BookingNumberSeq AS VARCHAR(10))),
    CustomerId              BIGINT        NOT NULL CONSTRAINT FK_Bookings_Customers REFERENCES dbo.Customers(CustomerId),
    PickupAddressText       NVARCHAR(400) NOT NULL,
    PickupCityId            INT           NULL CONSTRAINT FK_Bookings_PickupCity REFERENCES dbo.Cities(CityId),
    PickupLatitude          DECIMAL(9,6)  NULL,
    PickupLongitude         DECIMAL(9,6)  NULL,
    PickupContactName       NVARCHAR(150) NULL,
    PickupContactPhone      VARCHAR(15)   NULL,
    DropAddressText         NVARCHAR(400) NOT NULL,
    DropCityId              INT           NULL CONSTRAINT FK_Bookings_DropCity REFERENCES dbo.Cities(CityId),
    DropLatitude            DECIMAL(9,6)  NULL,
    DropLongitude           DECIMAL(9,6)  NULL,
    DropContactName         NVARCHAR(150) NULL,
    DropContactPhone        VARCHAR(15)   NULL,
    GoodsCategoryId         INT           NOT NULL CONSTRAINT FK_Bookings_Goods REFERENCES dbo.GoodsCategories(GoodsCategoryId),
    GoodsDescription        NVARCHAR(400) NOT NULL,
    WeightKg                INT           NOT NULL CONSTRAINT CK_Bookings_Weight CHECK (WeightKg > 0),
    GoodsValue              DECIMAL(14,2) NULL,        -- needed for e-way bill above threshold
    VehicleTypeId           INT           NOT NULL CONSTRAINT FK_Bookings_VehicleType REFERENCES dbo.VehicleTypes(VehicleTypeId),
    PickupDate              DATE          NOT NULL,
    PickupSlot              VARCHAR(20)   NULL,        -- 'Morning', '06:00-09:00'
    SpecialInstructions     NVARCHAR(1000) NULL,
    Status                  VARCHAR(20)   NOT NULL CONSTRAINT DF_Bookings_Status DEFAULT ('QuotePending') CONSTRAINT CK_Bookings_Status CHECK (Status IN (
                                'QuotePending','Quoted','Confirmed','Assigned','InTransit','Delivered','Completed','Cancelled')),
    CancelledReason         NVARCHAR(400) NULL,
    CancelledBy             BIGINT        NULL CONSTRAINT FK_Bookings_CancelledBy REFERENCES dbo.Users(UserId),
    CreatedAt               DATETIME2(0)  NOT NULL CONSTRAINT DF_Bookings_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt               DATETIME2(0)  NULL,
    RowVersion              ROWVERSION
);
CREATE UNIQUE INDEX UX_Bookings_No ON dbo.Bookings(BookingNumber);
CREATE INDEX IX_Bookings_Customer ON dbo.Bookings(CustomerId, CreatedAt DESC);
CREATE INDEX IX_Bookings_Status   ON dbo.Bookings(Status, PickupDate);
GO

CREATE TABLE dbo.Quotes (
    QuoteId         BIGINT IDENTITY(1,1) CONSTRAINT PK_Quotes PRIMARY KEY,
    BookingId       BIGINT        NOT NULL CONSTRAINT FK_Quotes_Bookings REFERENCES dbo.Bookings(BookingId),
    DistanceKm      DECIMAL(8,1)  NOT NULL,
    VehicleCost     DECIMAL(12,2) NOT NULL,
    DriverCost      DECIMAL(12,2) NOT NULL,
    LoadingCharges  DECIMAL(12,2) NOT NULL CONSTRAINT DF_Quotes_Loading DEFAULT (0),
    PlatformFee     DECIMAL(12,2) NOT NULL,
    TaxAmount       DECIMAL(12,2) NOT NULL,
    TotalAmount     DECIMAL(12,2) NOT NULL,
    OwnerPayout     DECIMAL(12,2) NOT NULL,          -- what the owner will receive before TDS
    ValidUntil      DATETIME2(0)  NOT NULL,
    Status          VARCHAR(20)   NOT NULL CONSTRAINT DF_Quotes_Status DEFAULT ('Sent') CONSTRAINT CK_Quotes_Status CHECK (Status IN ('Draft','Sent','Accepted','Expired','Superseded','Rejected')),
    CreatedBy       BIGINT        NULL CONSTRAINT FK_Quotes_CreatedBy REFERENCES dbo.Users(UserId),   -- NULL = auto-quote
    CreatedAt       DATETIME2(0)  NOT NULL CONSTRAINT DF_Quotes_CreatedAt DEFAULT (SYSUTCDATETIME()),
    AcceptedAt      DATETIME2(0)  NULL
);
CREATE INDEX IX_Quotes_Booking ON dbo.Quotes(BookingId, Status);
GO

CREATE TABLE dbo.LoadOffers (
    LoadOfferId     BIGINT IDENTITY(1,1) CONSTRAINT PK_LoadOffers PRIMARY KEY,
    BookingId       BIGINT        NOT NULL CONSTRAINT FK_LoadOffers_Bookings REFERENCES dbo.Bookings(BookingId),
    OwnerId         BIGINT        NOT NULL CONSTRAINT FK_LoadOffers_Owners   REFERENCES dbo.Owners(OwnerId),
    VehicleId       BIGINT        NULL CONSTRAINT FK_LoadOffers_Vehicles     REFERENCES dbo.Vehicles(VehicleId),
    OfferedPayout   DECIMAL(12,2) NOT NULL,
    Status          VARCHAR(20)   NOT NULL CONSTRAINT DF_LoadOffers_Status DEFAULT ('Offered') CONSTRAINT CK_LoadOffers_Status CHECK (Status IN ('Offered','Accepted','Declined','Expired','Withdrawn')),
    ExpiresAt       DATETIME2(0)  NOT NULL,
    RespondedAt     DATETIME2(0)  NULL,
    CreatedAt       DATETIME2(0)  NOT NULL CONSTRAINT DF_LoadOffers_CreatedAt DEFAULT (SYSUTCDATETIME())
);
CREATE INDEX IX_LoadOffers_Owner   ON dbo.LoadOffers(OwnerId, Status);
CREATE INDEX IX_LoadOffers_Booking ON dbo.LoadOffers(BookingId);
GO

CREATE TABLE dbo.Trips (
    TripId              BIGINT IDENTITY(1,1) CONSTRAINT PK_Trips PRIMARY KEY,
    TripNumber          VARCHAR(20)   NOT NULL CONSTRAINT DF_Trips_No DEFAULT ('TRP-' + CAST(NEXT VALUE FOR dbo.TripNumberSeq AS VARCHAR(10))),
    BookingId           BIGINT        NOT NULL CONSTRAINT FK_Trips_Bookings REFERENCES dbo.Bookings(BookingId),
    OwnerId             BIGINT        NOT NULL CONSTRAINT FK_Trips_Owners   REFERENCES dbo.Owners(OwnerId),
    VehicleId           BIGINT        NOT NULL CONSTRAINT FK_Trips_Vehicles REFERENCES dbo.Vehicles(VehicleId),
    DriverId            BIGINT        NOT NULL CONSTRAINT FK_Trips_Drivers  REFERENCES dbo.Drivers(DriverId),
    Status              VARCHAR(20)   NOT NULL CONSTRAINT DF_Trips_Status DEFAULT ('Assigned') CONSTRAINT CK_Trips_Status CHECK (Status IN (
                            'Assigned','EnRouteToPickup','AtPickup','Loaded','InTransit','AtDestination','Delivered','Completed','Cancelled')),
    PickupOtpHash       VARCHAR(128)  NULL,          -- sender shares code with driver at loading
    DeliveryOtpHash     VARCHAR(128)  NULL,          -- receiver shares code with driver at unloading
    EwayBillNumber      VARCHAR(20)   NULL,
    PlannedDistanceKm   DECIMAL(8,1)  NULL,
    ActualDistanceKm    DECIMAL(8,1)  NULL,
    ReachedPickupAt     DATETIME2(0)  NULL,
    LoadedAt            DATETIME2(0)  NULL,
    StartedAt           DATETIME2(0)  NULL,
    ReachedDropAt       DATETIME2(0)  NULL,
    DeliveredAt         DATETIME2(0)  NULL,
    PodApprovedBy       BIGINT        NULL CONSTRAINT FK_Trips_PodApprovedBy REFERENCES dbo.Users(UserId),
    PodApprovedAt       DATETIME2(0)  NULL,
    OwnerPayout         DECIMAL(12,2) NOT NULL,
    DriverPay           DECIMAL(12,2) NULL,
    CreatedAt           DATETIME2(0)  NOT NULL CONSTRAINT DF_Trips_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt           DATETIME2(0)  NULL,
    RowVersion          ROWVERSION
);
CREATE UNIQUE INDEX UX_Trips_No ON dbo.Trips(TripNumber);
CREATE UNIQUE INDEX UX_Trips_ActiveBooking ON dbo.Trips(BookingId) WHERE Status <> 'Cancelled';
CREATE INDEX IX_Trips_Driver  ON dbo.Trips(DriverId, Status);
CREATE INDEX IX_Trips_Vehicle ON dbo.Trips(VehicleId, Status);
CREATE INDEX IX_Trips_Owner   ON dbo.Trips(OwnerId, CreatedAt DESC);
CREATE INDEX IX_Trips_Status  ON dbo.Trips(Status);
GO

CREATE TABLE dbo.TripEvents (
    TripEventId     BIGINT IDENTITY(1,1) CONSTRAINT PK_TripEvents PRIMARY KEY,
    TripId          BIGINT        NOT NULL CONSTRAINT FK_TripEvents_Trips REFERENCES dbo.Trips(TripId),
    EventType       VARCHAR(30)   NOT NULL CONSTRAINT CK_TripEvents_Type CHECK (EventType IN (
                        'Assigned','EnRouteToPickup','ReachedPickup','PickupOtpVerified','GoodsPhotoUploaded','TripStarted',
                        'LocationSharingOn','Delayed','ReachedDestination','PodUploaded','DeliveryOtpVerified','Completed',
                        'PodApproved','Cancelled','Note')),
    Note            NVARCHAR(500) NULL,
    Latitude        DECIMAL(9,6)  NULL,
    Longitude       DECIMAL(9,6)  NULL,
    DocumentId      BIGINT        NULL CONSTRAINT FK_TripEvents_Docs REFERENCES dbo.Documents(DocumentId),
    CreatedBy       BIGINT        NOT NULL CONSTRAINT FK_TripEvents_Users REFERENCES dbo.Users(UserId),
    CreatedAt       DATETIME2(0)  NOT NULL CONSTRAINT DF_TripEvents_CreatedAt DEFAULT (SYSUTCDATETIME())
);
CREATE INDEX IX_TripEvents_Trip ON dbo.TripEvents(TripId, CreatedAt);
GO

CREATE TABLE dbo.TripLocations (
    TripLocationId  BIGINT IDENTITY(1,1) CONSTRAINT PK_TripLocations PRIMARY KEY,
    TripId          BIGINT        NOT NULL CONSTRAINT FK_TripLocations_Trips REFERENCES dbo.Trips(TripId),
    Latitude        DECIMAL(9,6)  NOT NULL,
    Longitude       DECIMAL(9,6)  NOT NULL,
    SpeedKmph       DECIMAL(5,1)  NULL,
    HeadingDeg      SMALLINT      NULL,
    AccuracyM       SMALLINT      NULL,
    RecordedAt      DATETIME2(0)  NOT NULL          -- device time, UTC
);
CREATE INDEX IX_TripLocations_Trip ON dbo.TripLocations(TripId, RecordedAt DESC);
GO

CREATE TABLE dbo.OtpCodes (
    OtpCodeId       BIGINT IDENTITY(1,1) CONSTRAINT PK_OtpCodes PRIMARY KEY,
    Mobile          VARCHAR(15)   NOT NULL,
    Purpose         VARCHAR(20)   NOT NULL CONSTRAINT CK_Otp_Purpose CHECK (Purpose IN ('Login','Signup','Pickup','Delivery','BankChange')),
    TripId          BIGINT        NULL CONSTRAINT FK_Otp_Trips REFERENCES dbo.Trips(TripId),
    CodeHash        VARCHAR(128)  NOT NULL,          -- store a hash, never the code
    Attempts        TINYINT       NOT NULL CONSTRAINT DF_Otp_Attempts DEFAULT (0),
    ExpiresAt       DATETIME2(0)  NOT NULL,
    UsedAt          DATETIME2(0)  NULL,
    CreatedAt       DATETIME2(0)  NOT NULL CONSTRAINT DF_Otp_CreatedAt DEFAULT (SYSUTCDATETIME())
);
CREATE INDEX IX_Otp_Lookup ON dbo.OtpCodes(Mobile, Purpose, ExpiresAt DESC);
GO

/* =====================================================================
   MODULE 7 — MONEY
   ===================================================================== */
CREATE TABLE dbo.Payments (
    PaymentId           BIGINT IDENTITY(1,1) CONSTRAINT PK_Payments PRIMARY KEY,
    BookingId           BIGINT        NOT NULL CONSTRAINT FK_Payments_Bookings  REFERENCES dbo.Bookings(BookingId),
    CustomerId          BIGINT        NOT NULL CONSTRAINT FK_Payments_Customers REFERENCES dbo.Customers(CustomerId),
    QuoteId             BIGINT        NOT NULL CONSTRAINT FK_Payments_Quotes    REFERENCES dbo.Quotes(QuoteId),
    Amount              DECIMAL(12,2) NOT NULL,
    Method              VARCHAR(20)   NOT NULL CONSTRAINT CK_Payments_Method CHECK (Method IN ('UPI','CreditCard','DebitCard','NetBanking','Wallet','Credit')),
    Gateway             VARCHAR(20)   NOT NULL,       -- 'Razorpay', 'Cashfree', 'PayU'
    GatewayOrderId      VARCHAR(100)  NULL,
    GatewayPaymentId    VARCHAR(100)  NULL,
    Status              VARCHAR(20)   NOT NULL CONSTRAINT DF_Payments_Status DEFAULT ('Created') CONSTRAINT CK_Payments_Status CHECK (Status IN ('Created','Authorized','Captured','Failed','Refunded','PartiallyRefunded')),
    FailureReason       NVARCHAR(300) NULL,
    PaidAt              DATETIME2(0)  NULL,
    CreatedAt           DATETIME2(0)  NOT NULL CONSTRAINT DF_Payments_CreatedAt DEFAULT (SYSUTCDATETIME())
);
CREATE INDEX IX_Payments_Booking ON dbo.Payments(BookingId);
CREATE UNIQUE INDEX UX_Payments_GatewayPayment ON dbo.Payments(GatewayPaymentId) WHERE GatewayPaymentId IS NOT NULL;
GO

CREATE TABLE dbo.Refunds (
    RefundId            BIGINT IDENTITY(1,1) CONSTRAINT PK_Refunds PRIMARY KEY,
    PaymentId           BIGINT        NOT NULL CONSTRAINT FK_Refunds_Payments REFERENCES dbo.Payments(PaymentId),
    Amount              DECIMAL(12,2) NOT NULL,
    Reason              NVARCHAR(300) NOT NULL,
    GatewayRefundId     VARCHAR(100)  NULL,
    Status              VARCHAR(20)   NOT NULL CONSTRAINT DF_Refunds_Status DEFAULT ('Requested') CONSTRAINT CK_Refunds_Status CHECK (Status IN ('Requested','Processed','Failed')),
    RequestedBy         BIGINT        NOT NULL CONSTRAINT FK_Refunds_Users REFERENCES dbo.Users(UserId),
    ProcessedAt         DATETIME2(0)  NULL,
    CreatedAt           DATETIME2(0)  NOT NULL CONSTRAINT DF_Refunds_CreatedAt DEFAULT (SYSUTCDATETIME())
);
GO

CREATE TABLE dbo.Invoices (
    InvoiceId           BIGINT IDENTITY(1,1) CONSTRAINT PK_Invoices PRIMARY KEY,
    InvoiceNumber       VARCHAR(30)   NOT NULL,       -- e.g. OLK/26-27/000412, set by the API per financial year
    BookingId           BIGINT        NOT NULL CONSTRAINT FK_Invoices_Bookings  REFERENCES dbo.Bookings(BookingId),
    CustomerId          BIGINT        NOT NULL CONSTRAINT FK_Invoices_Customers REFERENCES dbo.Customers(CustomerId),
    CustomerGSTIN       CHAR(15)      NULL,
    PlaceOfSupply       NVARCHAR(60)  NOT NULL,
    TaxableAmount       DECIMAL(12,2) NOT NULL,
    CGST                DECIMAL(12,2) NOT NULL CONSTRAINT DF_Inv_CGST DEFAULT (0),
    SGST                DECIMAL(12,2) NOT NULL CONSTRAINT DF_Inv_SGST DEFAULT (0),
    IGST                DECIMAL(12,2) NOT NULL CONSTRAINT DF_Inv_IGST DEFAULT (0),
    TotalAmount         DECIMAL(12,2) NOT NULL,
    PdfDocumentId       BIGINT        NULL CONSTRAINT FK_Invoices_Docs REFERENCES dbo.Documents(DocumentId),
    IssuedAt            DATETIME2(0)  NOT NULL CONSTRAINT DF_Invoices_IssuedAt DEFAULT (SYSUTCDATETIME())
);
CREATE UNIQUE INDEX UX_Invoices_No ON dbo.Invoices(InvoiceNumber);
GO

CREATE TABLE dbo.Settlements (
    SettlementId        BIGINT IDENTITY(1,1) CONSTRAINT PK_Settlements PRIMARY KEY,
    TripId              BIGINT        NOT NULL CONSTRAINT FK_Settlements_Trips  REFERENCES dbo.Trips(TripId),
    OwnerId             BIGINT        NOT NULL CONSTRAINT FK_Settlements_Owners REFERENCES dbo.Owners(OwnerId),
    OwnerBankAccountId  BIGINT        NULL CONSTRAINT FK_Settlements_Bank REFERENCES dbo.OwnerBankAccounts(OwnerBankAccountId),
    GrossAmount         DECIMAL(12,2) NOT NULL,
    CommissionAmount    DECIMAL(12,2) NOT NULL,
    TdsAmount           DECIMAL(12,2) NOT NULL CONSTRAINT DF_Settle_Tds DEFAULT (0),
    NetAmount           DECIMAL(12,2) NOT NULL,
    Status              VARCHAR(20)   NOT NULL CONSTRAINT DF_Settle_Status DEFAULT ('AwaitingPod') CONSTRAINT CK_Settle_Status CHECK (Status IN ('AwaitingPod','Approved','Processing','Released','Failed','OnHold')),
    PayoutReference     VARCHAR(100)  NULL,          -- gateway payout id
    UTR                 VARCHAR(30)   NULL,          -- bank transfer reference
    ApprovedBy          BIGINT        NULL CONSTRAINT FK_Settle_ApprovedBy REFERENCES dbo.Users(UserId),
    ReleasedBy          BIGINT        NULL CONSTRAINT FK_Settle_ReleasedBy REFERENCES dbo.Users(UserId),
    ReleasedAt          DATETIME2(0)  NULL,
    CreatedAt           DATETIME2(0)  NOT NULL CONSTRAINT DF_Settle_CreatedAt DEFAULT (SYSUTCDATETIME()),
    RowVersion          ROWVERSION
);
CREATE UNIQUE INDEX UX_Settlements_Trip ON dbo.Settlements(TripId);
CREATE INDEX IX_Settlements_Owner ON dbo.Settlements(OwnerId, Status);
GO

/* =====================================================================
   MODULE 8 — SUPPORT & PLATFORM
   ===================================================================== */
CREATE TABLE dbo.Tickets (
    TicketId        BIGINT IDENTITY(1,1) CONSTRAINT PK_Tickets PRIMARY KEY,
    TicketNumber    VARCHAR(20)   NOT NULL CONSTRAINT DF_Tickets_No DEFAULT ('TCK-' + CAST(NEXT VALUE FOR dbo.TicketNumberSeq AS VARCHAR(10))),
    RaisedBy        BIGINT        NOT NULL CONSTRAINT FK_Tickets_RaisedBy REFERENCES dbo.Users(UserId),
    TripId          BIGINT        NULL CONSTRAINT FK_Tickets_Trips REFERENCES dbo.Trips(TripId),
    BookingId       BIGINT        NULL CONSTRAINT FK_Tickets_Bookings REFERENCES dbo.Bookings(BookingId),
    Category        VARCHAR(30)   NOT NULL CONSTRAINT CK_Tickets_Cat CHECK (Category IN ('Delay','Damage','MissingGoods','Payment','Invoice','DriverBehaviour','App','Other')),
    Priority        VARCHAR(10)   NOT NULL CONSTRAINT DF_Tickets_Prio DEFAULT ('Medium') CONSTRAINT CK_Tickets_Prio CHECK (Priority IN ('Low','Medium','High','Critical')),
    Subject         NVARCHAR(200) NOT NULL,
    Description     NVARCHAR(2000) NOT NULL,
    Status          VARCHAR(20)   NOT NULL CONSTRAINT DF_Tickets_Status DEFAULT ('Open') CONSTRAINT CK_Tickets_Status CHECK (Status IN ('Open','InProgress','WaitingOnUser','Resolved','Closed')),
    AssignedTo      BIGINT        NULL CONSTRAINT FK_Tickets_AssignedTo REFERENCES dbo.Users(UserId),
    Resolution      NVARCHAR(1000) NULL,
    ResolvedAt      DATETIME2(0)  NULL,
    CreatedAt       DATETIME2(0)  NOT NULL CONSTRAINT DF_Tickets_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt       DATETIME2(0)  NULL
);
CREATE UNIQUE INDEX UX_Tickets_No ON dbo.Tickets(TicketNumber);
CREATE INDEX IX_Tickets_Queue ON dbo.Tickets(Status, Priority, CreatedAt);
GO

CREATE TABLE dbo.TicketMessages (
    TicketMessageId         BIGINT IDENTITY(1,1) CONSTRAINT PK_TicketMessages PRIMARY KEY,
    TicketId                BIGINT         NOT NULL CONSTRAINT FK_TicketMessages_Tickets REFERENCES dbo.Tickets(TicketId),
    SenderUserId            BIGINT         NOT NULL CONSTRAINT FK_TicketMessages_Users   REFERENCES dbo.Users(UserId),
    Message                 NVARCHAR(2000) NOT NULL,
    AttachmentDocumentId    BIGINT         NULL CONSTRAINT FK_TicketMessages_Docs REFERENCES dbo.Documents(DocumentId),
    IsInternalNote          BIT            NOT NULL CONSTRAINT DF_TM_Internal DEFAULT (0),
    CreatedAt               DATETIME2(0)   NOT NULL CONSTRAINT DF_TM_CreatedAt DEFAULT (SYSUTCDATETIME())
);
CREATE INDEX IX_TicketMessages_Ticket ON dbo.TicketMessages(TicketId, CreatedAt);
GO

CREATE TABLE dbo.Ratings (
    RatingId        BIGINT IDENTITY(1,1) CONSTRAINT PK_Ratings PRIMARY KEY,
    TripId          BIGINT        NOT NULL CONSTRAINT FK_Ratings_Trips REFERENCES dbo.Trips(TripId),
    FromUserId      BIGINT        NOT NULL CONSTRAINT FK_Ratings_From  REFERENCES dbo.Users(UserId),
    ToUserId        BIGINT        NOT NULL CONSTRAINT FK_Ratings_To    REFERENCES dbo.Users(UserId),
    Stars           TINYINT       NOT NULL CONSTRAINT CK_Ratings_Stars CHECK (Stars BETWEEN 1 AND 5),
    Comment         NVARCHAR(500) NULL,
    CreatedAt       DATETIME2(0)  NOT NULL CONSTRAINT DF_Ratings_CreatedAt DEFAULT (SYSUTCDATETIME())
);
CREATE UNIQUE INDEX UX_Ratings_OnePerTrip ON dbo.Ratings(TripId, FromUserId, ToUserId);
GO

CREATE TABLE dbo.Notifications (
    NotificationId  BIGINT IDENTITY(1,1) CONSTRAINT PK_Notifications PRIMARY KEY,
    UserId          BIGINT        NOT NULL CONSTRAINT FK_Notifications_Users REFERENCES dbo.Users(UserId),
    Channel         VARCHAR(10)   NOT NULL CONSTRAINT CK_Notif_Channel CHECK (Channel IN ('InApp','SMS','WhatsApp','Email','Push')),
    TemplateCode    VARCHAR(50)   NULL,              -- DLT / WhatsApp template id
    Title           NVARCHAR(200) NOT NULL,
    Body            NVARCHAR(1000) NOT NULL,
    RelatedEntity   VARCHAR(20)   NULL,
    RelatedId       BIGINT        NULL,
    Status          VARCHAR(20)   NOT NULL CONSTRAINT DF_Notif_Status DEFAULT ('Queued') CONSTRAINT CK_Notif_Status CHECK (Status IN ('Queued','Sent','Delivered','Failed','Read')),
    SentAt          DATETIME2(0)  NULL,
    ReadAt          DATETIME2(0)  NULL,
    CreatedAt       DATETIME2(0)  NOT NULL CONSTRAINT DF_Notif_CreatedAt DEFAULT (SYSUTCDATETIME())
);
CREATE INDEX IX_Notifications_User ON dbo.Notifications(UserId, CreatedAt DESC);
CREATE INDEX IX_Notifications_Queue ON dbo.Notifications(Status) WHERE Status = 'Queued';
GO

CREATE TABLE dbo.FraudFlags (
    FraudFlagId     BIGINT IDENTITY(1,1) CONSTRAINT PK_FraudFlags PRIMARY KEY,
    UserId          BIGINT        NOT NULL CONSTRAINT FK_FraudFlags_Users REFERENCES dbo.Users(UserId),
    RuleCode        VARCHAR(50)   NOT NULL,          -- 'RepeatCancellation', 'DuplicateDocument', 'GpsMismatch'
    Detail          NVARCHAR(500) NOT NULL,
    Severity        VARCHAR(10)   NOT NULL CONSTRAINT CK_Fraud_Sev CHECK (Severity IN ('Low','Medium','High')),
    Status          VARCHAR(20)   NOT NULL CONSTRAINT DF_Fraud_Status DEFAULT ('Open') CONSTRAINT CK_Fraud_Status CHECK (Status IN ('Open','Dismissed','ActionTaken')),
    ReviewedBy      BIGINT        NULL CONSTRAINT FK_Fraud_ReviewedBy REFERENCES dbo.Users(UserId),
    ReviewedAt      DATETIME2(0)  NULL,
    CreatedAt       DATETIME2(0)  NOT NULL CONSTRAINT DF_Fraud_CreatedAt DEFAULT (SYSUTCDATETIME())
);
CREATE INDEX IX_FraudFlags_Open ON dbo.FraudFlags(Status, Severity) WHERE Status = 'Open';
GO

CREATE TABLE dbo.AuditLogs (
    AuditLogId      BIGINT IDENTITY(1,1) CONSTRAINT PK_AuditLogs PRIMARY KEY,
    ActorUserId     BIGINT        NULL CONSTRAINT FK_AuditLogs_Users REFERENCES dbo.Users(UserId),  -- NULL = system job
    Action          VARCHAR(60)   NOT NULL,          -- 'Owner.Approve', 'Settlement.Release'
    EntityType      VARCHAR(30)   NOT NULL,
    EntityId        BIGINT        NOT NULL,
    OldValues       NVARCHAR(MAX) NULL CONSTRAINT CK_Audit_OldJson CHECK (OldValues IS NULL OR ISJSON(OldValues) = 1),
    NewValues       NVARCHAR(MAX) NULL CONSTRAINT CK_Audit_NewJson CHECK (NewValues IS NULL OR ISJSON(NewValues) = 1),
    IpAddress       VARCHAR(45)   NULL,
    CreatedAt       DATETIME2(0)  NOT NULL CONSTRAINT DF_Audit_CreatedAt DEFAULT (SYSUTCDATETIME())
);
CREATE INDEX IX_AuditLogs_Entity ON dbo.AuditLogs(EntityType, EntityId, CreatedAt DESC);
GO

/* =====================================================================
   STARTING MASTER DATA (sample rates — confirm before launch)
   ===================================================================== */
INSERT INTO dbo.VehicleTypes (Code, Name, BodyType, MaxLoadKg, LengthFt, WidthFt, HeightFt, RecommendedGoods, RatePerKm, MinFare, DriverBattaPerDay, SortOrder) VALUES
 ('ACE',       N'Tata Ace',        'Closed',    750,   7.0, 4.8, 6.0, N'Parcels, FMCG, single-room moves',        16, 450,   500,  1),
 ('PICKUP',    N'Pickup Truck',    'Open',      1500,  8.0, 5.5, NULL,N'Appliances, hardware, farm produce',       20, 650,   500,  2),
 ('8FT',       N'8 FT Truck',      'Closed',    2000,  8.0, 5.5, 6.0, N'1 BHK moves, retail stock',                22, 900,   600,  3),
 ('14FT',      N'14 FT Truck',     'Closed',    4000, 14.0, 6.0, 6.5, N'2-3 BHK moves, e-commerce, garments',      32, 1800,  700,  4),
 ('17FT',      N'17 FT Truck',     'Closed',    5000, 17.0, 6.5, 7.0, N'Office moves, packaged goods',             38, 2400,  800,  5),
 ('20FT',      N'20 FT Truck',     'Closed',    7000, 20.0, 7.0, 7.0, N'Electronics, FMCG distribution',           45, 3000,  900,  6),
 ('22FT',      N'22 FT Truck',     'Closed',   10000, 22.0, 7.5, 7.0, N'Cement, tiles, bulk cartons',              52, 3800,  900,  7),
 ('32FT',      N'32 FT Truck',     'Closed',   16000, 32.0, 8.0, 8.0, N'Factory loads, inter-state freight',       72, 6500, 1000,  8),
 ('CONT24',    N'Container',       'Container',12000, 24.0, 8.0, 8.0, N'High-value, weather-sensitive cargo',      60, 5500, 1000,  9),
 ('OPEN10W',   N'Open Body Truck', 'Open',     21000, 24.0, 8.0, NULL,N'Steel, sand, agri produce, machinery',     65, 6000, 1000, 10),
 ('TRAILER40', N'Trailer',         'Flatbed',  28000, 40.0, 8.0, NULL,N'Heavy machinery, coils, ODC',              95, 12000,1200, 11);

INSERT INTO dbo.GoodsCategories (Name, RequiresEwayBill) VALUES
 (N'Household goods',0),(N'Industrial goods',1),(N'Agricultural produce',0),(N'Construction materials',1),
 (N'FMCG / retail',1),(N'Electronics',1),(N'Textiles & garments',1),(N'Furniture',1),(N'Machinery',1),(N'Other',1);

INSERT INTO dbo.Settings (SettingKey, SettingValue, Description) VALUES
 ('CommissionPercent',      N'7',   N'Platform commission on trip value'),
 ('GstOnFreightPercent',    N'5',   N'GST on freight (confirm GTA treatment with your CA)'),
 ('GstOnPlatformFeePercent',N'18',  N'GST on platform fee'),
 ('OtpExpiryMinutes',       N'10',  N'Login OTP validity'),
 ('OtpMaxAttempts',         N'5',   N'Wrong tries before OTP is locked'),
 ('QuoteValidityMinutes',   N'30',  N'How long a quote can be accepted'),
 ('LoadOfferExpiryMinutes', N'20',  N'How long an owner has to accept a load'),
 ('GpsPingSeconds',         N'30',  N'Driver app location interval'),
 ('SettlementDelayHours',   N'48',  N'Payout after POD approval');

INSERT INTO dbo.Cities (Name, NameKn, State, Latitude, Longitude) VALUES
 (N'Bengaluru',  N'ಬೆಂಗಳೂರು',  N'Karnataka',   12.971600, 77.594600),
 (N'Mysuru',     N'ಮೈಸೂರು',    N'Karnataka',   12.295800, 76.639400),
 (N'Tumakuru',   N'ತುಮಕೂರು',   N'Karnataka',   13.340900, 77.101000),
 (N'Chitradurga',N'ಚಿತ್ರದುರ್ಗ', N'Karnataka',   14.230600, 76.398000),
 (N'Davanagere', N'ದಾವಣಗೆರೆ',  N'Karnataka',   14.464400, 75.921800),
 (N'Hubballi',   N'ಹುಬ್ಬಳ್ಳಿ',   N'Karnataka',   15.364700, 75.124000),
 (N'Belagavi',   N'ಬೆಳಗಾವಿ',   N'Karnataka',   15.849700, 74.497700),
 (N'Mangaluru',  N'ಮಂಗಳೂರು',   N'Karnataka',   12.914100, 74.856000),
 (N'Kalaburagi', N'ಕಲಬುರಗಿ',   N'Karnataka',   17.329700, 76.834300),
 (N'Pune',       NULL,         N'Maharashtra', 18.520400, 73.856700),
 (N'Mumbai',     NULL,         N'Maharashtra', 19.076000, 72.877700),
 (N'Chennai',    NULL,         N'Tamil Nadu',  13.082700, 80.270700),
 (N'Hyderabad',  NULL,         N'Telangana',   17.385000, 78.486700),
 (N'Kochi',      NULL,         N'Kerala',       9.931200, 76.267300),
 (N'Panaji',     NULL,         N'Goa',         15.490900, 73.827800);
GO

/* =====================================================================
   HELPFUL VIEWS
   ===================================================================== */
CREATE VIEW dbo.vw_ActiveTrips AS
SELECT t.TripId, t.TripNumber, b.BookingNumber, t.Status,
       b.PickupAddressText, b.DropAddressText,
       v.RegistrationNumber, vt.Name AS VehicleType,
       du.FullName AS DriverName, du.Mobile AS DriverMobile,
       v.LastLatitude, v.LastLongitude, v.LastSeenAt, b.CustomerId, t.OwnerId
FROM dbo.Trips t
JOIN dbo.Bookings b      ON b.BookingId = t.BookingId
JOIN dbo.Vehicles v      ON v.VehicleId = t.VehicleId
JOIN dbo.VehicleTypes vt ON vt.VehicleTypeId = v.VehicleTypeId
JOIN dbo.Drivers d       ON d.DriverId = t.DriverId
JOIN dbo.Users du        ON du.UserId = d.UserId
WHERE t.Status NOT IN ('Completed','Cancelled');
GO

CREATE VIEW dbo.vw_ExpiringDocuments AS
SELECT DocumentId, EntityType, EntityId, DocType, ExpiryDate,
       DATEDIFF(DAY, CAST(SYSUTCDATETIME() AS DATE), ExpiryDate) AS DaysLeft
FROM dbo.Documents
WHERE ExpiryDate IS NOT NULL
  AND Status = 'Verified'
  AND ExpiryDate <= DATEADD(DAY, 30, CAST(SYSUTCDATETIME() AS DATE));
GO
