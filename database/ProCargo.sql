/* =============================================================================
   ProCargo — SQL Server database
   =============================================================================

   What this script does
     Creates the ProCargo database, its 30 tables, two reporting views and the
     starting data (vehicle rate card, goods types, cities, settings).

   How to run it
     1. Open SQL Server Management Studio (SSMS) and connect to your server.
     2. File > Open > this file.
     3. Press F5 (Execute). It takes a few seconds.
     Run it once on an empty server. To start again, delete the ProCargo
     database first (right-click > Delete).

   Conventions used in every table
     - Primary key:   <TableName>Id, a number SQL Server fills in.
     - Money:         DECIMAL(12, 2), in rupees.
     - Dates/times:   DATETIME2(0) in UTC. The app shows them in IST.
     - Status:        short text codes, limited by a CHECK constraint.
     - RowVersion:    stops two people overwriting the same row at once.
     - Nothing is deleted: rows are closed with a Status or IsActive = 0,
                      and important changes are written to AuditLogs.

   Privacy
     PAN and bank account numbers are stored encrypted (VARBINARY).
     Only the last 4 digits of Aadhaar are ever stored.

   Contents
      1. Accounts               Users, RefreshTokens, Addresses
      2. Customers              Customers
      3. Partners               Owners, OwnerBankAccounts, Drivers
      4. Master data            Cities, VehicleTypes, GoodsCategories, Settings
      5. Fleet and documents    Vehicles, Documents
      6. Bookings and trips     Bookings, Quotes, LoadOffers, Trips, TripEvents, TripLocations, OtpCodes
      7. Money                  Payments, Refunds, Invoices, Settlements
      8. Support and platform   Tickets, TicketMessages, Ratings, Notifications, FraudFlags, AuditLogs
      9. Starting data
     10. Reporting views
   ============================================================================= */


IF DB_ID('ProCargo') IS NULL
    CREATE DATABASE ProCargo;
GO

USE ProCargo;
GO


-- Number sequences for the reference numbers people see (PC-24001, TRP-24001...)
CREATE SEQUENCE dbo.BookingNumberSeq AS INT START WITH 24001 INCREMENT BY 1;
CREATE SEQUENCE dbo.TripNumberSeq    AS INT START WITH 24001 INCREMENT BY 1;
CREATE SEQUENCE dbo.InvoiceNumberSeq AS INT START WITH 1     INCREMENT BY 1;
CREATE SEQUENCE dbo.TicketNumberSeq  AS INT START WITH 1001  INCREMENT BY 1;
GO


/* =============================================================================
   1. ACCOUNTS
   People who sign in and their saved addresses.
   ============================================================================= */

-- Users: everyone who signs in: customers, lorry owners, drivers and admins.
CREATE TABLE dbo.Users
(
    UserId               BIGINT           IDENTITY(1, 1) NOT NULL,
    Role                 VARCHAR(20)      NOT NULL,    -- Customer | Owner | Driver | Admin
    FullName             NVARCHAR(150)    NOT NULL,
    Mobile               VARCHAR(15)      NOT NULL,    -- 10-digit mobile, used to sign in
    Email                NVARCHAR(200)    NULL,
    PasswordHash         VARCHAR(200)     NULL,        -- only admins use a password
    PreferredLanguage    CHAR(2)          NOT NULL CONSTRAINT DF_Users_PreferredLanguage DEFAULT ('en'),
    Status               VARCHAR(20)      NOT NULL CONSTRAINT DF_Users_Status DEFAULT ('Active'),
    LastLoginAt          DATETIME2(0)     NULL,
    CreatedAt            DATETIME2(0)     NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt            DATETIME2(0)     NULL,
    RowVersion           ROWVERSION,                   -- stops two people overwriting each other

    CONSTRAINT PK_Users PRIMARY KEY (UserId),
    CONSTRAINT CK_Users_Role CHECK (Role IN ('Customer', 'Owner', 'Driver', 'Admin')),
    CONSTRAINT CK_Users_Language CHECK (PreferredLanguage IN ('en', 'kn', 'hi', 'mr', 'ta', 'te')),
    CONSTRAINT CK_Users_Status CHECK (Status IN ('Active', 'PendingKyc', 'Blocked', 'Closed'))
);
CREATE UNIQUE INDEX UX_Users_Mobile ON dbo.Users (Mobile);
CREATE UNIQUE INDEX UX_Users_Email ON dbo.Users (Email) WHERE Email IS NOT NULL;
GO

-- RefreshTokens: keeps users signed in on their devices.
CREATE TABLE dbo.RefreshTokens
(
    RefreshTokenId    BIGINT           IDENTITY(1, 1) NOT NULL,
    UserId            BIGINT           NOT NULL,
    TokenHash         VARCHAR(128)     NOT NULL,
    DeviceInfo        NVARCHAR(200)    NULL,
    ExpiresAt         DATETIME2(0)     NOT NULL,
    RevokedAt         DATETIME2(0)     NULL,
    CreatedAt         DATETIME2(0)     NOT NULL CONSTRAINT DF_RefreshTokens_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_RefreshTokens PRIMARY KEY (RefreshTokenId),
    CONSTRAINT FK_RefreshTokens_User FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId)
);
CREATE UNIQUE INDEX UX_RefreshTokens_Hash ON dbo.RefreshTokens (TokenHash);
GO

-- Addresses: saved pickup and delivery points.
CREATE TABLE dbo.Addresses
(
    AddressId       BIGINT           IDENTITY(1, 1) NOT NULL,
    UserId          BIGINT           NOT NULL,
    Label           NVARCHAR(60)     NULL,    -- e.g. "Peenya warehouse"
    ContactName     NVARCHAR(150)    NULL,
    ContactPhone    VARCHAR(15)      NULL,
    Line1           NVARCHAR(250)    NOT NULL,
    Line2           NVARCHAR(250)    NULL,
    City            NVARCHAR(100)    NOT NULL,
    State           NVARCHAR(100)    NOT NULL,
    Pincode         CHAR(6)          NOT NULL,
    Latitude        DECIMAL(9, 6)    NULL,
    Longitude       DECIMAL(9, 6)    NULL,
    IsActive        BIT              NOT NULL CONSTRAINT DF_Addresses_IsActive DEFAULT (1),
    CreatedAt       DATETIME2(0)     NOT NULL CONSTRAINT DF_Addresses_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_Addresses PRIMARY KEY (AddressId),
    CONSTRAINT FK_Addresses_User FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId)
);
CREATE INDEX IX_Addresses_User ON dbo.Addresses (UserId);
GO


/* =============================================================================
   2. CUSTOMERS
   Businesses and people who book trucks.
   ============================================================================= */

-- Customers: customer profile. Company name and GSTIN are optional.
CREATE TABLE dbo.Customers
(
    CustomerId          BIGINT            IDENTITY(1, 1) NOT NULL,
    UserId              BIGINT            NOT NULL,
    CompanyName         NVARCHAR(200)     NULL,    -- empty for individuals
    GSTIN               CHAR(15)          NULL,
    BillingAddressId    BIGINT            NULL,
    CreditLimit         DECIMAL(12, 2)    NOT NULL CONSTRAINT DF_Customers_CreditLimit DEFAULT (0),
    CreatedAt           DATETIME2(0)      NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt           DATETIME2(0)      NULL,

    CONSTRAINT PK_Customers PRIMARY KEY (CustomerId),
    CONSTRAINT FK_Customers_User FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_Customers_BillingAddress FOREIGN KEY (BillingAddressId) REFERENCES dbo.Addresses (AddressId)
);
CREATE UNIQUE INDEX UX_Customers_User ON dbo.Customers (UserId);
GO


/* =============================================================================
   3. PARTNERS
   Lorry owners, their bank accounts, and drivers.
   ============================================================================= */

-- Owners: lorry owner profile and KYC. PAN is encrypted; only the last 4 Aadhaar digits are kept.
CREATE TABLE dbo.Owners
(
    OwnerId            BIGINT            IDENTITY(1, 1) NOT NULL,
    UserId             BIGINT            NOT NULL,
    BusinessName       NVARCHAR(200)     NULL,
    PanEncrypted       VARBINARY(256)    NULL,    -- encrypted by the API
    PanLast4           CHAR(4)           NULL,
    AadhaarLast4       CHAR(4)           NULL,    -- never store the full Aadhaar number
    AadhaarVaultRef    VARCHAR(100)      NULL,    -- reference from the KYC provider
    GSTIN              CHAR(15)          NULL,
    KycStatus          VARCHAR(20)       NOT NULL CONSTRAINT DF_Owners_KycStatus DEFAULT ('Pending'),
    RejectionReason    NVARCHAR(500)     NULL,
    VerifiedBy         BIGINT            NULL,
    VerifiedAt         DATETIME2(0)      NULL,
    Rating             DECIMAL(3, 2)     NULL,
    CreatedAt          DATETIME2(0)      NOT NULL CONSTRAINT DF_Owners_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt          DATETIME2(0)      NULL,
    RowVersion         ROWVERSION,

    CONSTRAINT PK_Owners PRIMARY KEY (OwnerId),
    CONSTRAINT FK_Owners_User FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_Owners_VerifiedBy FOREIGN KEY (VerifiedBy) REFERENCES dbo.Users (UserId),
    CONSTRAINT CK_Owners_KycStatus CHECK (KycStatus IN ('Pending', 'Approved', 'Rejected'))
);
CREATE UNIQUE INDEX UX_Owners_User ON dbo.Owners (UserId);
CREATE INDEX IX_Owners_KycStatus ON dbo.Owners (KycStatus);
GO

-- OwnerBankAccounts: where owner payouts are sent. Account number is encrypted.
CREATE TABLE dbo.OwnerBankAccounts
(
    OwnerBankAccountId        BIGINT            IDENTITY(1, 1) NOT NULL,
    OwnerId                   BIGINT            NOT NULL,
    AccountHolder             NVARCHAR(150)     NOT NULL,
    AccountNumberEncrypted    VARBINARY(256)    NOT NULL,
    AccountLast4              CHAR(4)           NOT NULL,
    IFSC                      CHAR(11)          NOT NULL,
    BankName                  NVARCHAR(100)     NULL,
    PennyDropStatus           VARCHAR(20)       NOT NULL CONSTRAINT DF_OwnerBankAccounts_PennyDropStatus DEFAULT ('Pending'),   -- ₹1 test transfer result
    IsPrimary                 BIT               NOT NULL CONSTRAINT DF_OwnerBankAccounts_IsPrimary DEFAULT (1),
    IsActive                  BIT               NOT NULL CONSTRAINT DF_OwnerBankAccounts_IsActive DEFAULT (1),
    CreatedAt                 DATETIME2(0)      NOT NULL CONSTRAINT DF_OwnerBankAccounts_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_OwnerBankAccounts PRIMARY KEY (OwnerBankAccountId),
    CONSTRAINT FK_OwnerBankAccounts_Owner FOREIGN KEY (OwnerId) REFERENCES dbo.Owners (OwnerId),
    CONSTRAINT CK_OwnerBankAccounts_PennyDrop CHECK (PennyDropStatus IN ('Pending', 'Verified', 'NameMismatch', 'Failed'))
);
CREATE UNIQUE INDEX UX_OwnerBankAccounts_OnePrimary ON dbo.OwnerBankAccounts (OwnerId) WHERE IsPrimary = 1 AND IsActive = 1;
GO

-- Drivers: driver profile. A driver is linked to the owner whose trucks they drive.
CREATE TABLE dbo.Drivers
(
    DriverId                 BIGINT           IDENTITY(1, 1) NOT NULL,
    UserId                   BIGINT           NOT NULL,
    OwnerId                  BIGINT           NULL,        -- empty = drives their own truck
    LicenceNumber            VARCHAR(20)      NOT NULL,
    LicenceClass             VARCHAR(20)      NOT NULL,    -- LMV | TRANSPORT | HGMV | HPMV
    LicenceExpiry            DATE             NOT NULL,
    AadhaarLast4             CHAR(4)          NULL,
    EmergencyContactName     NVARCHAR(150)    NULL,
    EmergencyContactPhone    VARCHAR(15)      NULL,
    KycStatus                VARCHAR(20)      NOT NULL CONSTRAINT DF_Drivers_KycStatus DEFAULT ('Pending'),
    DutyStatus               VARCHAR(20)      NOT NULL CONSTRAINT DF_Drivers_DutyStatus DEFAULT ('OffDuty'),   -- Available | OnTrip | OffDuty
    RejectionReason          NVARCHAR(500)    NULL,
    VerifiedBy               BIGINT           NULL,
    VerifiedAt               DATETIME2(0)     NULL,
    Rating                   DECIMAL(3, 2)    NULL,
    CreatedAt                DATETIME2(0)     NOT NULL CONSTRAINT DF_Drivers_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt                DATETIME2(0)     NULL,
    RowVersion               ROWVERSION,

    CONSTRAINT PK_Drivers PRIMARY KEY (DriverId),
    CONSTRAINT FK_Drivers_User FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_Drivers_Owner FOREIGN KEY (OwnerId) REFERENCES dbo.Owners (OwnerId),
    CONSTRAINT FK_Drivers_VerifiedBy FOREIGN KEY (VerifiedBy) REFERENCES dbo.Users (UserId),
    CONSTRAINT CK_Drivers_LicenceClass CHECK (LicenceClass IN ('LMV', 'TRANSPORT', 'HGMV', 'HPMV')),
    CONSTRAINT CK_Drivers_KycStatus CHECK (KycStatus IN ('Pending', 'Approved', 'Rejected')),
    CONSTRAINT CK_Drivers_DutyStatus CHECK (DutyStatus IN ('Available', 'OnTrip', 'OffDuty'))
);
CREATE UNIQUE INDEX UX_Drivers_User ON dbo.Drivers (UserId);
CREATE UNIQUE INDEX UX_Drivers_Licence ON dbo.Drivers (LicenceNumber);
CREATE INDEX IX_Drivers_Owner ON dbo.Drivers (OwnerId);
GO


/* =============================================================================
   4. MASTER DATA
   Lists the rest of the system relies on: cities, vehicle classes, goods, settings.
   ============================================================================= */

-- Cities: cities we serve, with map coordinates for distance.
CREATE TABLE dbo.Cities
(
    CityId           INT              IDENTITY(1, 1) NOT NULL,
    Name             NVARCHAR(100)    NOT NULL,
    NameKn           NVARCHAR(100)    NULL,    -- Kannada name
    State            NVARCHAR(100)    NOT NULL,
    Latitude         DECIMAL(9, 6)    NOT NULL,
    Longitude        DECIMAL(9, 6)    NOT NULL,
    IsServiceable    BIT              NOT NULL CONSTRAINT DF_Cities_IsServiceable DEFAULT (1),

    CONSTRAINT PK_Cities PRIMARY KEY (CityId)
);
CREATE UNIQUE INDEX UX_Cities_NameState ON dbo.Cities (Name, State);
GO

-- VehicleTypes: the vehicle classes customers choose from, with the rate card.
CREATE TABLE dbo.VehicleTypes
(
    VehicleTypeId        INT               IDENTITY(1, 1) NOT NULL,
    Code                 VARCHAR(20)       NOT NULL,    -- short code, e.g. 14FT
    Name                 NVARCHAR(60)      NOT NULL,
    BodyType             VARCHAR(20)       NOT NULL,    -- Closed | Open | Container | Flatbed
    MaxLoadKg            INT               NOT NULL,
    LengthFt             DECIMAL(5, 1)     NOT NULL,
    WidthFt              DECIMAL(5, 1)     NOT NULL,
    HeightFt             DECIMAL(5, 1)     NULL,
    RecommendedGoods     NVARCHAR(200)     NULL,
    RatePerKm            DECIMAL(8, 2)     NOT NULL,    -- ₹ per km
    MinFare              DECIMAL(10, 2)    NOT NULL,    -- ₹ minimum for short trips
    DriverBattaPerDay    DECIMAL(8, 2)     NOT NULL,    -- ₹ driver allowance per day
    IsActive             BIT               NOT NULL CONSTRAINT DF_VehicleTypes_IsActive DEFAULT (1),
    SortOrder            INT               NOT NULL CONSTRAINT DF_VehicleTypes_SortOrder DEFAULT (0),

    CONSTRAINT PK_VehicleTypes PRIMARY KEY (VehicleTypeId),
    CONSTRAINT CK_VehicleTypes_BodyType CHECK (BodyType IN ('Closed', 'Open', 'Container', 'Flatbed'))
);
CREATE UNIQUE INDEX UX_VehicleTypes_Code ON dbo.VehicleTypes (Code);
GO

-- GoodsCategories: types of goods customers can send.
CREATE TABLE dbo.GoodsCategories
(
    GoodsCategoryId     INT              IDENTITY(1, 1) NOT NULL,
    Name                NVARCHAR(100)    NOT NULL,
    RequiresEwayBill    BIT              NOT NULL CONSTRAINT DF_GoodsCategories_RequiresEwayBill DEFAULT (1),
    IsActive            BIT              NOT NULL CONSTRAINT DF_GoodsCategories_IsActive DEFAULT (1),

    CONSTRAINT PK_GoodsCategories PRIMARY KEY (GoodsCategoryId)
);
GO

-- Settings: business settings the API reads: commission, GST, OTP rules.
CREATE TABLE dbo.Settings
(
    SettingKey      VARCHAR(60)      NOT NULL,
    SettingValue    NVARCHAR(400)    NOT NULL,
    Description     NVARCHAR(400)    NULL,
    UpdatedBy       BIGINT           NULL,
    UpdatedAt       DATETIME2(0)     NULL,

    CONSTRAINT PK_Settings PRIMARY KEY (SettingKey),
    CONSTRAINT FK_Settings_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES dbo.Users (UserId)
);
GO


/* =============================================================================
   5. FLEET AND DOCUMENTS
   Lorries and every uploaded file (KYC, vehicle papers, delivery proof).
   ============================================================================= */

-- Vehicles: each registered lorry. Must be Approved before it can take loads.
CREATE TABLE dbo.Vehicles
(
    VehicleId             BIGINT           IDENTITY(1, 1) NOT NULL,
    OwnerId               BIGINT           NOT NULL,
    VehicleTypeId         INT              NOT NULL,
    RegistrationNumber    VARCHAR(15)      NOT NULL,    -- no spaces, e.g. KA01AB4521
    CapacityKg            INT              NOT NULL,
    MakeModel             NVARCHAR(100)    NULL,
    ManufactureYear       SMALLINT         NULL,
    AvailabilityStatus    VARCHAR(20)      NOT NULL CONSTRAINT DF_Vehicles_AvailabilityStatus DEFAULT ('Available'),   -- Available | Busy | Maintenance
    VerificationStatus    VARCHAR(20)      NOT NULL CONSTRAINT DF_Vehicles_VerificationStatus DEFAULT ('Pending'),
    CurrentDriverId       BIGINT           NULL,
    HomeCityId            INT              NULL,
    LastLatitude          DECIMAL(9, 6)    NULL,
    LastLongitude         DECIMAL(9, 6)    NULL,
    LastSeenAt            DATETIME2(0)     NULL,
    CreatedAt             DATETIME2(0)     NOT NULL CONSTRAINT DF_Vehicles_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt             DATETIME2(0)     NULL,
    RowVersion            ROWVERSION,

    CONSTRAINT PK_Vehicles PRIMARY KEY (VehicleId),
    CONSTRAINT FK_Vehicles_Owner FOREIGN KEY (OwnerId) REFERENCES dbo.Owners (OwnerId),
    CONSTRAINT FK_Vehicles_VehicleType FOREIGN KEY (VehicleTypeId) REFERENCES dbo.VehicleTypes (VehicleTypeId),
    CONSTRAINT FK_Vehicles_CurrentDriver FOREIGN KEY (CurrentDriverId) REFERENCES dbo.Drivers (DriverId),
    CONSTRAINT FK_Vehicles_HomeCity FOREIGN KEY (HomeCityId) REFERENCES dbo.Cities (CityId),
    CONSTRAINT CK_Vehicles_Availability CHECK (AvailabilityStatus IN ('Available', 'Busy', 'Maintenance')),
    CONSTRAINT CK_Vehicles_Verification CHECK (VerificationStatus IN ('Pending', 'Approved', 'Rejected', 'Suspended'))
);
CREATE UNIQUE INDEX UX_Vehicles_RegistrationNumber ON dbo.Vehicles (RegistrationNumber);
CREATE INDEX IX_Vehicles_Owner ON dbo.Vehicles (OwnerId);
CREATE INDEX IX_Vehicles_Matching ON dbo.Vehicles (VehicleTypeId, AvailabilityStatus, VerificationStatus)
    INCLUDE (LastLatitude, LastLongitude);
GO

-- Documents: every uploaded file. The file itself lives in storage; this row holds its details and review status.
CREATE TABLE dbo.Documents
(
    DocumentId         BIGINT           IDENTITY(1, 1) NOT NULL,
    EntityType         VARCHAR(20)      NOT NULL,    -- who it belongs to: Owner, Driver, Vehicle, Trip...
    EntityId           BIGINT           NOT NULL,    -- id of that owner / driver / vehicle / trip
    DocType            VARCHAR(30)      NOT NULL,    -- RC, Insurance, PAN, POD...
    BlobPath           NVARCHAR(400)    NOT NULL,    -- path in private storage
    FileName           NVARCHAR(200)    NOT NULL,
    ContentType        VARCHAR(100)     NOT NULL,
    SizeBytes          BIGINT           NOT NULL,
    Sha256             CHAR(64)         NULL,        -- spots re-used or altered files
    DocumentNumber     NVARCHAR(60)     NULL,        -- policy / permit number, never Aadhaar
    ExpiryDate         DATE             NULL,
    Status             VARCHAR(20)      NOT NULL CONSTRAINT DF_Documents_Status DEFAULT ('Pending'),
    RejectionReason    NVARCHAR(500)    NULL,
    ReviewedBy         BIGINT           NULL,
    ReviewedAt         DATETIME2(0)     NULL,
    UploadedBy         BIGINT           NOT NULL,
    UploadedAt         DATETIME2(0)     NOT NULL CONSTRAINT DF_Documents_UploadedAt DEFAULT (SYSUTCDATETIME()),
    Latitude           DECIMAL(9, 6)    NULL,        -- where a trip photo was taken
    Longitude          DECIMAL(9, 6)    NULL,

    CONSTRAINT PK_Documents PRIMARY KEY (DocumentId),
    CONSTRAINT FK_Documents_ReviewedBy FOREIGN KEY (ReviewedBy) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_Documents_UploadedBy FOREIGN KEY (UploadedBy) REFERENCES dbo.Users (UserId),
    CONSTRAINT CK_Documents_EntityType CHECK (EntityType IN ('Owner', 'Driver', 'Vehicle', 'Customer', 'Trip', 'Ticket', 'Invoice')),
    CONSTRAINT CK_Documents_DocType CHECK (DocType IN
    (
        'RC', 'Insurance', 'Fitness', 'Permit', 'PUC', 'Aadhaar',
        'PAN', 'Licence', 'DriverPhoto', 'CancelledCheque', 'GST', 'PickupPhoto',
        'POD', 'DeliveryPhoto', 'EwayBill', 'InvoicePdf', 'Other'
    )),
    CONSTRAINT CK_Documents_Status CHECK (Status IN ('Pending', 'Verified', 'Rejected', 'Expired'))
);
CREATE INDEX IX_Documents_Entity ON dbo.Documents (EntityType, EntityId, DocType);
CREATE INDEX IX_Documents_PendingReview ON dbo.Documents (Status) WHERE Status = 'Pending';
CREATE INDEX IX_Documents_Expiry ON dbo.Documents (ExpiryDate) WHERE ExpiryDate IS NOT NULL;
GO


/* =============================================================================
   6. BOOKINGS AND TRIPS
   From a customer's request, to the price, to the actual journey.
   ============================================================================= */

-- Bookings: a customer's request to move goods. Status: Quoted → Confirmed (paid) → Assigned → InTransit → Delivered → Completed.
CREATE TABLE dbo.Bookings
(
    BookingId              BIGINT            IDENTITY(1, 1) NOT NULL,
    BookingNumber          VARCHAR(20)       NOT NULL CONSTRAINT DF_Bookings_BookingNumber DEFAULT ('PC-' + CAST(NEXT VALUE FOR dbo.BookingNumberSeq AS VARCHAR(10))),   -- shown to customers, e.g. PC-24001
    CustomerId             BIGINT            NOT NULL,
    PickupAddressText      NVARCHAR(400)     NOT NULL,
    PickupCityId           INT               NULL,
    PickupLatitude         DECIMAL(9, 6)     NULL,
    PickupLongitude        DECIMAL(9, 6)     NULL,
    PickupContactName      NVARCHAR(150)     NULL,
    PickupContactPhone     VARCHAR(15)       NULL,
    DropAddressText        NVARCHAR(400)     NOT NULL,
    DropCityId             INT               NULL,
    DropLatitude           DECIMAL(9, 6)     NULL,
    DropLongitude          DECIMAL(9, 6)     NULL,
    DropContactName        NVARCHAR(150)     NULL,
    DropContactPhone       VARCHAR(15)       NULL,
    GoodsCategoryId        INT               NOT NULL,
    GoodsDescription       NVARCHAR(400)     NOT NULL,
    WeightKg               INT               NOT NULL,
    GoodsValue             DECIMAL(14, 2)    NULL,    -- needed for an e-way bill
    VehicleTypeId          INT               NOT NULL,
    PickupDate             DATE              NOT NULL,
    PickupSlot             VARCHAR(20)       NULL,    -- e.g. Morning (6–10 am)
    SpecialInstructions    NVARCHAR(1000)    NULL,
    Status                 VARCHAR(20)       NOT NULL CONSTRAINT DF_Bookings_Status DEFAULT ('QuotePending'),
    CancelledReason        NVARCHAR(400)     NULL,
    CancelledBy            BIGINT            NULL,
    CreatedAt              DATETIME2(0)      NOT NULL CONSTRAINT DF_Bookings_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt              DATETIME2(0)      NULL,
    RowVersion             ROWVERSION,                -- stops two owners taking the same load

    CONSTRAINT PK_Bookings PRIMARY KEY (BookingId),
    CONSTRAINT FK_Bookings_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (CustomerId),
    CONSTRAINT FK_Bookings_PickupCity FOREIGN KEY (PickupCityId) REFERENCES dbo.Cities (CityId),
    CONSTRAINT FK_Bookings_DropCity FOREIGN KEY (DropCityId) REFERENCES dbo.Cities (CityId),
    CONSTRAINT FK_Bookings_GoodsCategory FOREIGN KEY (GoodsCategoryId) REFERENCES dbo.GoodsCategories (GoodsCategoryId),
    CONSTRAINT FK_Bookings_VehicleType FOREIGN KEY (VehicleTypeId) REFERENCES dbo.VehicleTypes (VehicleTypeId),
    CONSTRAINT FK_Bookings_CancelledBy FOREIGN KEY (CancelledBy) REFERENCES dbo.Users (UserId),
    CONSTRAINT CK_Bookings_Weight CHECK (WeightKg > 0),
    CONSTRAINT CK_Bookings_Status CHECK (Status IN
    (
        'QuotePending', 'Quoted', 'Confirmed', 'Assigned',
        'InTransit', 'Delivered', 'Completed', 'Cancelled'
    ))
);
CREATE UNIQUE INDEX UX_Bookings_BookingNumber ON dbo.Bookings (BookingNumber);
CREATE INDEX IX_Bookings_Customer ON dbo.Bookings (CustomerId, CreatedAt DESC);
CREATE INDEX IX_Bookings_Status ON dbo.Bookings (Status, PickupDate);
GO

-- Quotes: the price offered for a booking. The customer pays TotalAmount; the owner receives OwnerPayout.
CREATE TABLE dbo.Quotes
(
    QuoteId           BIGINT            IDENTITY(1, 1) NOT NULL,
    BookingId         BIGINT            NOT NULL,
    DistanceKm        DECIMAL(8, 1)     NOT NULL,
    VehicleCost       DECIMAL(12, 2)    NOT NULL,
    DriverCost        DECIMAL(12, 2)    NOT NULL,
    LoadingCharges    DECIMAL(12, 2)    NOT NULL CONSTRAINT DF_Quotes_LoadingCharges DEFAULT (0),
    PlatformFee       DECIMAL(12, 2)    NOT NULL,    -- ProCargo commission, taken from the owner's side
    TaxAmount         DECIMAL(12, 2)    NOT NULL,    -- GST
    TotalAmount       DECIMAL(12, 2)    NOT NULL,    -- what the customer pays
    OwnerPayout       DECIMAL(12, 2)    NOT NULL,    -- freight minus commission
    ValidUntil        DATETIME2(0)      NOT NULL,
    Status            VARCHAR(20)       NOT NULL CONSTRAINT DF_Quotes_Status DEFAULT ('Sent'),
    CreatedBy         BIGINT            NULL,        -- empty = priced automatically
    CreatedAt         DATETIME2(0)      NOT NULL CONSTRAINT DF_Quotes_CreatedAt DEFAULT (SYSUTCDATETIME()),
    AcceptedAt        DATETIME2(0)      NULL,

    CONSTRAINT PK_Quotes PRIMARY KEY (QuoteId),
    CONSTRAINT FK_Quotes_Booking FOREIGN KEY (BookingId) REFERENCES dbo.Bookings (BookingId),
    CONSTRAINT FK_Quotes_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users (UserId),
    CONSTRAINT CK_Quotes_Status CHECK (Status IN ('Draft', 'Sent', 'Accepted', 'Expired', 'Superseded', 'Rejected'))
);
CREATE INDEX IX_Quotes_Booking ON dbo.Quotes (BookingId, Status);
GO

-- LoadOffers: owners' responses to paid loads (accepted or declined).
CREATE TABLE dbo.LoadOffers
(
    LoadOfferId      BIGINT            IDENTITY(1, 1) NOT NULL,
    BookingId        BIGINT            NOT NULL,
    OwnerId          BIGINT            NOT NULL,
    VehicleId        BIGINT            NULL,
    OfferedPayout    DECIMAL(12, 2)    NOT NULL,
    Status           VARCHAR(20)       NOT NULL CONSTRAINT DF_LoadOffers_Status DEFAULT ('Offered'),
    ExpiresAt        DATETIME2(0)      NOT NULL,
    RespondedAt      DATETIME2(0)      NULL,
    CreatedAt        DATETIME2(0)      NOT NULL CONSTRAINT DF_LoadOffers_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_LoadOffers PRIMARY KEY (LoadOfferId),
    CONSTRAINT FK_LoadOffers_Booking FOREIGN KEY (BookingId) REFERENCES dbo.Bookings (BookingId),
    CONSTRAINT FK_LoadOffers_Owner FOREIGN KEY (OwnerId) REFERENCES dbo.Owners (OwnerId),
    CONSTRAINT FK_LoadOffers_Vehicle FOREIGN KEY (VehicleId) REFERENCES dbo.Vehicles (VehicleId),
    CONSTRAINT CK_LoadOffers_Status CHECK (Status IN ('Offered', 'Accepted', 'Declined', 'Expired', 'Withdrawn'))
);
CREATE INDEX IX_LoadOffers_Owner ON dbo.LoadOffers (OwnerId, Status);
CREATE INDEX IX_LoadOffers_Booking ON dbo.LoadOffers (BookingId);
GO

-- Trips: the actual journey: one truck and one driver carrying one booking.
CREATE TABLE dbo.Trips
(
    TripId                  BIGINT            IDENTITY(1, 1) NOT NULL,
    TripNumber              VARCHAR(20)       NOT NULL CONSTRAINT DF_Trips_TripNumber DEFAULT ('TRP-' + CAST(NEXT VALUE FOR dbo.TripNumberSeq AS VARCHAR(10))),
    BookingId               BIGINT            NOT NULL,
    OwnerId                 BIGINT            NOT NULL,
    VehicleId               BIGINT            NOT NULL,
    DriverId                BIGINT            NOT NULL,
    Status                  VARCHAR(20)       NOT NULL CONSTRAINT DF_Trips_Status DEFAULT ('Assigned'),
    PickupOtpProtected      VARCHAR(400)      NULL,    -- encrypted code the sender gives the driver
    DeliveryOtpProtected    VARCHAR(400)      NULL,    -- encrypted code the receiver gives the driver
    EwayBillNumber          VARCHAR(20)       NULL,
    PlannedDistanceKm       DECIMAL(8, 1)     NULL,
    ActualDistanceKm        DECIMAL(8, 1)     NULL,
    ReachedPickupAt         DATETIME2(0)      NULL,
    LoadedAt                DATETIME2(0)      NULL,
    StartedAt               DATETIME2(0)      NULL,
    ReachedDropAt           DATETIME2(0)      NULL,
    DeliveredAt             DATETIME2(0)      NULL,
    PodApprovedBy           BIGINT            NULL,
    PodApprovedAt           DATETIME2(0)      NULL,
    OwnerPayout             DECIMAL(12, 2)    NOT NULL,
    DriverPay               DECIMAL(12, 2)    NULL,
    CreatedAt               DATETIME2(0)      NOT NULL CONSTRAINT DF_Trips_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt               DATETIME2(0)      NULL,
    RowVersion              ROWVERSION,

    CONSTRAINT PK_Trips PRIMARY KEY (TripId),
    CONSTRAINT FK_Trips_Booking FOREIGN KEY (BookingId) REFERENCES dbo.Bookings (BookingId),
    CONSTRAINT FK_Trips_Owner FOREIGN KEY (OwnerId) REFERENCES dbo.Owners (OwnerId),
    CONSTRAINT FK_Trips_Vehicle FOREIGN KEY (VehicleId) REFERENCES dbo.Vehicles (VehicleId),
    CONSTRAINT FK_Trips_Driver FOREIGN KEY (DriverId) REFERENCES dbo.Drivers (DriverId),
    CONSTRAINT FK_Trips_PodApprovedBy FOREIGN KEY (PodApprovedBy) REFERENCES dbo.Users (UserId),
    CONSTRAINT CK_Trips_Status CHECK (Status IN
    (
        'Assigned', 'EnRouteToPickup', 'AtPickup', 'Loaded', 'InTransit',
        'AtDestination', 'Delivered', 'Completed', 'Cancelled'
    ))
);
CREATE UNIQUE INDEX UX_Trips_TripNumber ON dbo.Trips (TripNumber);
CREATE UNIQUE INDEX UX_Trips_OneActivePerBooking ON dbo.Trips (BookingId) WHERE Status <> 'Cancelled';
CREATE INDEX IX_Trips_Driver ON dbo.Trips (DriverId, Status);
CREATE INDEX IX_Trips_Vehicle ON dbo.Trips (VehicleId, Status);
CREATE INDEX IX_Trips_Owner ON dbo.Trips (OwnerId, CreatedAt DESC);
CREATE INDEX IX_Trips_Status ON dbo.Trips (Status);
GO

-- TripEvents: the trip timeline the customer sees: each step the driver or admin takes.
CREATE TABLE dbo.TripEvents
(
    TripEventId    BIGINT           IDENTITY(1, 1) NOT NULL,
    TripId         BIGINT           NOT NULL,
    EventType      VARCHAR(30)      NOT NULL,
    Note           NVARCHAR(500)    NULL,
    Latitude       DECIMAL(9, 6)    NULL,
    Longitude      DECIMAL(9, 6)    NULL,
    DocumentId     BIGINT           NULL,    -- photo or POD for this step
    CreatedBy      BIGINT           NOT NULL,
    CreatedAt      DATETIME2(0)     NOT NULL CONSTRAINT DF_TripEvents_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_TripEvents PRIMARY KEY (TripEventId),
    CONSTRAINT FK_TripEvents_Trip FOREIGN KEY (TripId) REFERENCES dbo.Trips (TripId),
    CONSTRAINT FK_TripEvents_Document FOREIGN KEY (DocumentId) REFERENCES dbo.Documents (DocumentId),
    CONSTRAINT FK_TripEvents_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users (UserId),
    CONSTRAINT CK_TripEvents_EventType CHECK (EventType IN
    (
        'Assigned', 'EnRouteToPickup', 'ReachedPickup', 'PickupOtpVerified', 'GoodsPhotoUploaded',
        'TripStarted', 'LocationSharingOn', 'Delayed', 'ReachedDestination', 'PodUploaded',
        'DeliveryOtpVerified', 'Completed', 'PodApproved', 'Cancelled', 'Note'
    ))
);
CREATE INDEX IX_TripEvents_Trip ON dbo.TripEvents (TripId, CreatedAt);
GO

-- TripLocations: gPS points from the driver's phone during a trip.
CREATE TABLE dbo.TripLocations
(
    TripLocationId    BIGINT           IDENTITY(1, 1) NOT NULL,
    TripId            BIGINT           NOT NULL,
    Latitude          DECIMAL(9, 6)    NOT NULL,
    Longitude         DECIMAL(9, 6)    NOT NULL,
    SpeedKmph         DECIMAL(5, 1)    NULL,
    HeadingDeg        SMALLINT         NULL,
    AccuracyM         SMALLINT         NULL,
    RecordedAt        DATETIME2(0)     NOT NULL,    -- phone time, UTC

    CONSTRAINT PK_TripLocations PRIMARY KEY (TripLocationId),
    CONSTRAINT FK_TripLocations_Trip FOREIGN KEY (TripId) REFERENCES dbo.Trips (TripId)
);
CREATE INDEX IX_TripLocations_Trip ON dbo.TripLocations (TripId, RecordedAt DESC);
GO

-- OtpCodes: one-time codes for sign-in and sign-up. Only a hash of each code is stored.
CREATE TABLE dbo.OtpCodes
(
    OtpCodeId    BIGINT          IDENTITY(1, 1) NOT NULL,
    Mobile       VARCHAR(15)     NOT NULL,
    Purpose      VARCHAR(20)     NOT NULL,    -- Login | Signup | Pickup | Delivery | BankChange
    TripId       BIGINT          NULL,
    CodeHash     VARCHAR(128)    NOT NULL,    -- never store the code itself
    Attempts     TINYINT         NOT NULL CONSTRAINT DF_OtpCodes_Attempts DEFAULT (0),
    ExpiresAt    DATETIME2(0)    NOT NULL,
    UsedAt       DATETIME2(0)    NULL,
    CreatedAt    DATETIME2(0)    NOT NULL CONSTRAINT DF_OtpCodes_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_OtpCodes PRIMARY KEY (OtpCodeId),
    CONSTRAINT FK_OtpCodes_Trip FOREIGN KEY (TripId) REFERENCES dbo.Trips (TripId),
    CONSTRAINT CK_OtpCodes_Purpose CHECK (Purpose IN ('Login', 'Signup', 'Pickup', 'Delivery', 'BankChange'))
);
CREATE INDEX IX_OtpCodes_Lookup ON dbo.OtpCodes (Mobile, Purpose, ExpiresAt DESC);
GO


/* =============================================================================
   7. MONEY
   Customer payments, refunds, GST invoices and owner payouts.
   ============================================================================= */

-- Payments: money received from customers. Held until delivery is confirmed.
CREATE TABLE dbo.Payments
(
    PaymentId           BIGINT            IDENTITY(1, 1) NOT NULL,
    BookingId           BIGINT            NOT NULL,
    CustomerId          BIGINT            NOT NULL,
    QuoteId             BIGINT            NOT NULL,
    Amount              DECIMAL(12, 2)    NOT NULL,
    Method              VARCHAR(20)       NOT NULL,    -- UPI | CreditCard | DebitCard | NetBanking | Wallet | Credit
    Gateway             VARCHAR(20)       NOT NULL,    -- Razorpay, Cashfree... or Test
    GatewayOrderId      VARCHAR(100)      NULL,
    GatewayPaymentId    VARCHAR(100)      NULL,
    Status              VARCHAR(20)       NOT NULL CONSTRAINT DF_Payments_Status DEFAULT ('Created'),
    FailureReason       NVARCHAR(300)     NULL,
    PaidAt              DATETIME2(0)      NULL,
    CreatedAt           DATETIME2(0)      NOT NULL CONSTRAINT DF_Payments_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_Payments PRIMARY KEY (PaymentId),
    CONSTRAINT FK_Payments_Booking FOREIGN KEY (BookingId) REFERENCES dbo.Bookings (BookingId),
    CONSTRAINT FK_Payments_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (CustomerId),
    CONSTRAINT FK_Payments_Quote FOREIGN KEY (QuoteId) REFERENCES dbo.Quotes (QuoteId),
    CONSTRAINT CK_Payments_Method CHECK (Method IN ('UPI', 'CreditCard', 'DebitCard', 'NetBanking', 'Wallet', 'Credit')),
    CONSTRAINT CK_Payments_Status CHECK (Status IN ('Created', 'Authorized', 'Captured', 'Failed', 'Refunded', 'PartiallyRefunded'))
);
CREATE INDEX IX_Payments_Booking ON dbo.Payments (BookingId);
CREATE UNIQUE INDEX UX_Payments_GatewayPaymentId ON dbo.Payments (GatewayPaymentId) WHERE GatewayPaymentId IS NOT NULL;
GO

-- Refunds: money returned to customers.
CREATE TABLE dbo.Refunds
(
    RefundId           BIGINT            IDENTITY(1, 1) NOT NULL,
    PaymentId          BIGINT            NOT NULL,
    Amount             DECIMAL(12, 2)    NOT NULL,
    Reason             NVARCHAR(300)     NOT NULL,
    GatewayRefundId    VARCHAR(100)      NULL,
    Status             VARCHAR(20)       NOT NULL CONSTRAINT DF_Refunds_Status DEFAULT ('Requested'),
    RequestedBy        BIGINT            NOT NULL,
    ProcessedAt        DATETIME2(0)      NULL,
    CreatedAt          DATETIME2(0)      NOT NULL CONSTRAINT DF_Refunds_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_Refunds PRIMARY KEY (RefundId),
    CONSTRAINT FK_Refunds_Payment FOREIGN KEY (PaymentId) REFERENCES dbo.Payments (PaymentId),
    CONSTRAINT FK_Refunds_RequestedBy FOREIGN KEY (RequestedBy) REFERENCES dbo.Users (UserId),
    CONSTRAINT CK_Refunds_Status CHECK (Status IN ('Requested', 'Processed', 'Failed'))
);
GO

-- Invoices: gST tax invoices, issued when the delivery proof is approved.
CREATE TABLE dbo.Invoices
(
    InvoiceId        BIGINT            IDENTITY(1, 1) NOT NULL,
    InvoiceNumber    VARCHAR(30)       NOT NULL,    -- e.g. PC/26-27/000412
    BookingId        BIGINT            NOT NULL,
    CustomerId       BIGINT            NOT NULL,
    CustomerGSTIN    CHAR(15)          NULL,
    PlaceOfSupply    NVARCHAR(60)      NOT NULL,    -- state
    TaxableAmount    DECIMAL(12, 2)    NOT NULL,
    CGST             DECIMAL(12, 2)    NOT NULL CONSTRAINT DF_Invoices_CGST DEFAULT (0),   -- same-state trips
    SGST             DECIMAL(12, 2)    NOT NULL CONSTRAINT DF_Invoices_SGST DEFAULT (0),   -- same-state trips
    IGST             DECIMAL(12, 2)    NOT NULL CONSTRAINT DF_Invoices_IGST DEFAULT (0),   -- inter-state trips
    TotalAmount      DECIMAL(12, 2)    NOT NULL,
    PdfDocumentId    BIGINT            NULL,
    IssuedAt         DATETIME2(0)      NOT NULL CONSTRAINT DF_Invoices_IssuedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_Invoices PRIMARY KEY (InvoiceId),
    CONSTRAINT FK_Invoices_Booking FOREIGN KEY (BookingId) REFERENCES dbo.Bookings (BookingId),
    CONSTRAINT FK_Invoices_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (CustomerId),
    CONSTRAINT FK_Invoices_PdfDocument FOREIGN KEY (PdfDocumentId) REFERENCES dbo.Documents (DocumentId)
);
CREATE UNIQUE INDEX UX_Invoices_InvoiceNumber ON dbo.Invoices (InvoiceNumber);
GO

-- Settlements: owner payouts. Status: AwaitingPod → Approved (POD checked) → Released (money sent).
CREATE TABLE dbo.Settlements
(
    SettlementId          BIGINT            IDENTITY(1, 1) NOT NULL,
    TripId                BIGINT            NOT NULL,
    OwnerId               BIGINT            NOT NULL,
    OwnerBankAccountId    BIGINT            NULL,
    GrossAmount           DECIMAL(12, 2)    NOT NULL,    -- freight
    CommissionAmount      DECIMAL(12, 2)    NOT NULL,    -- ProCargo's share
    TdsAmount             DECIMAL(12, 2)    NOT NULL CONSTRAINT DF_Settlements_TdsAmount DEFAULT (0),
    NetAmount             DECIMAL(12, 2)    NOT NULL,    -- what the owner receives
    Status                VARCHAR(20)       NOT NULL CONSTRAINT DF_Settlements_Status DEFAULT ('AwaitingPod'),
    PayoutReference       VARCHAR(100)      NULL,        -- payment gateway payout id
    UTR                   VARCHAR(30)       NULL,        -- bank transfer reference
    ApprovedBy            BIGINT            NULL,
    ReleasedBy            BIGINT            NULL,
    ReleasedAt            DATETIME2(0)      NULL,
    CreatedAt             DATETIME2(0)      NOT NULL CONSTRAINT DF_Settlements_CreatedAt DEFAULT (SYSUTCDATETIME()),
    RowVersion            ROWVERSION,

    CONSTRAINT PK_Settlements PRIMARY KEY (SettlementId),
    CONSTRAINT FK_Settlements_Trip FOREIGN KEY (TripId) REFERENCES dbo.Trips (TripId),
    CONSTRAINT FK_Settlements_Owner FOREIGN KEY (OwnerId) REFERENCES dbo.Owners (OwnerId),
    CONSTRAINT FK_Settlements_OwnerBankAccount FOREIGN KEY (OwnerBankAccountId) REFERENCES dbo.OwnerBankAccounts (OwnerBankAccountId),
    CONSTRAINT FK_Settlements_ApprovedBy FOREIGN KEY (ApprovedBy) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_Settlements_ReleasedBy FOREIGN KEY (ReleasedBy) REFERENCES dbo.Users (UserId),
    CONSTRAINT CK_Settlements_Status CHECK (Status IN ('AwaitingPod', 'Approved', 'Processing', 'Released', 'Failed', 'OnHold'))
);
CREATE UNIQUE INDEX UX_Settlements_Trip ON dbo.Settlements (TripId);
CREATE INDEX IX_Settlements_Owner ON dbo.Settlements (OwnerId, Status);
GO


/* =============================================================================
   8. SUPPORT AND PLATFORM
   Complaints, ratings, messages to users, fraud checks and the audit trail.
   ============================================================================= */

-- Tickets: complaints and support requests.
CREATE TABLE dbo.Tickets
(
    TicketId        BIGINT            IDENTITY(1, 1) NOT NULL,
    TicketNumber    VARCHAR(20)       NOT NULL CONSTRAINT DF_Tickets_TicketNumber DEFAULT ('TCK-' + CAST(NEXT VALUE FOR dbo.TicketNumberSeq AS VARCHAR(10))),
    RaisedBy        BIGINT            NOT NULL,
    TripId          BIGINT            NULL,
    BookingId       BIGINT            NULL,
    Category        VARCHAR(30)       NOT NULL,
    Priority        VARCHAR(10)       NOT NULL CONSTRAINT DF_Tickets_Priority DEFAULT ('Medium'),
    Subject         NVARCHAR(200)     NOT NULL,
    Description     NVARCHAR(2000)    NOT NULL,
    Status          VARCHAR(20)       NOT NULL CONSTRAINT DF_Tickets_Status DEFAULT ('Open'),
    AssignedTo      BIGINT            NULL,
    Resolution      NVARCHAR(1000)    NULL,
    ResolvedAt      DATETIME2(0)      NULL,
    CreatedAt       DATETIME2(0)      NOT NULL CONSTRAINT DF_Tickets_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt       DATETIME2(0)      NULL,

    CONSTRAINT PK_Tickets PRIMARY KEY (TicketId),
    CONSTRAINT FK_Tickets_RaisedBy FOREIGN KEY (RaisedBy) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_Tickets_Trip FOREIGN KEY (TripId) REFERENCES dbo.Trips (TripId),
    CONSTRAINT FK_Tickets_Booking FOREIGN KEY (BookingId) REFERENCES dbo.Bookings (BookingId),
    CONSTRAINT FK_Tickets_AssignedTo FOREIGN KEY (AssignedTo) REFERENCES dbo.Users (UserId),
    CONSTRAINT CK_Tickets_Category CHECK (Category IN ('Delay', 'Damage', 'MissingGoods', 'Payment', 'Invoice', 'DriverBehaviour', 'App', 'Other')),
    CONSTRAINT CK_Tickets_Priority CHECK (Priority IN ('Low', 'Medium', 'High', 'Critical')),
    CONSTRAINT CK_Tickets_Status CHECK (Status IN ('Open', 'InProgress', 'WaitingOnUser', 'Resolved', 'Closed'))
);
CREATE UNIQUE INDEX UX_Tickets_TicketNumber ON dbo.Tickets (TicketNumber);
CREATE INDEX IX_Tickets_Queue ON dbo.Tickets (Status, Priority, CreatedAt);
GO

-- TicketMessages: replies on a support ticket.
CREATE TABLE dbo.TicketMessages
(
    TicketMessageId         BIGINT            IDENTITY(1, 1) NOT NULL,
    TicketId                BIGINT            NOT NULL,
    SenderUserId            BIGINT            NOT NULL,
    Message                 NVARCHAR(2000)    NOT NULL,
    AttachmentDocumentId    BIGINT            NULL,
    IsInternalNote          BIT               NOT NULL CONSTRAINT DF_TicketMessages_IsInternalNote DEFAULT (0),   -- visible to staff only
    CreatedAt               DATETIME2(0)      NOT NULL CONSTRAINT DF_TicketMessages_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_TicketMessages PRIMARY KEY (TicketMessageId),
    CONSTRAINT FK_TicketMessages_Ticket FOREIGN KEY (TicketId) REFERENCES dbo.Tickets (TicketId),
    CONSTRAINT FK_TicketMessages_SenderUser FOREIGN KEY (SenderUserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_TicketMessages_AttachmentDocument FOREIGN KEY (AttachmentDocumentId) REFERENCES dbo.Documents (DocumentId)
);
CREATE INDEX IX_TicketMessages_Ticket ON dbo.TicketMessages (TicketId, CreatedAt);
GO

-- Ratings: star ratings after each trip.
CREATE TABLE dbo.Ratings
(
    RatingId      BIGINT           IDENTITY(1, 1) NOT NULL,
    TripId        BIGINT           NOT NULL,
    FromUserId    BIGINT           NOT NULL,
    ToUserId      BIGINT           NOT NULL,
    Stars         TINYINT          NOT NULL,
    Comment       NVARCHAR(500)    NULL,
    CreatedAt     DATETIME2(0)     NOT NULL CONSTRAINT DF_Ratings_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_Ratings PRIMARY KEY (RatingId),
    CONSTRAINT FK_Ratings_Trip FOREIGN KEY (TripId) REFERENCES dbo.Trips (TripId),
    CONSTRAINT FK_Ratings_FromUser FOREIGN KEY (FromUserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_Ratings_ToUser FOREIGN KEY (ToUserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT CK_Ratings_Stars CHECK (Stars BETWEEN 1 AND 5)
);
CREATE UNIQUE INDEX UX_Ratings_OnePerTrip ON dbo.Ratings (TripId, FromUserId, ToUserId);
GO

-- Notifications: messages sent to users by SMS, WhatsApp, email or in the app.
CREATE TABLE dbo.Notifications
(
    NotificationId    BIGINT            IDENTITY(1, 1) NOT NULL,
    UserId            BIGINT            NOT NULL,
    Channel           VARCHAR(10)       NOT NULL,    -- InApp | SMS | WhatsApp | Email | Push
    TemplateCode      VARCHAR(50)       NULL,        -- SMS DLT / WhatsApp template id
    Title             NVARCHAR(200)     NOT NULL,
    Body              NVARCHAR(1000)    NOT NULL,
    RelatedEntity     VARCHAR(20)       NULL,
    RelatedId         BIGINT            NULL,
    Status            VARCHAR(20)       NOT NULL CONSTRAINT DF_Notifications_Status DEFAULT ('Queued'),
    SentAt            DATETIME2(0)      NULL,
    ReadAt            DATETIME2(0)      NULL,
    CreatedAt         DATETIME2(0)      NOT NULL CONSTRAINT DF_Notifications_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_Notifications PRIMARY KEY (NotificationId),
    CONSTRAINT FK_Notifications_User FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT CK_Notifications_Channel CHECK (Channel IN ('InApp', 'SMS', 'WhatsApp', 'Email', 'Push')),
    CONSTRAINT CK_Notifications_Status CHECK (Status IN ('Queued', 'Sent', 'Delivered', 'Failed', 'Read'))
);
CREATE INDEX IX_Notifications_User ON dbo.Notifications (UserId, CreatedAt DESC);
CREATE INDEX IX_Notifications_Queue ON dbo.Notifications (Status) WHERE Status = 'Queued';
GO

-- FraudFlags: suspicious activity for the team to review.
CREATE TABLE dbo.FraudFlags
(
    FraudFlagId    BIGINT           IDENTITY(1, 1) NOT NULL,
    UserId         BIGINT           NOT NULL,
    RuleCode       VARCHAR(50)      NOT NULL,    -- e.g. RepeatCancellation, DuplicateDocument
    Detail         NVARCHAR(500)    NOT NULL,
    Severity       VARCHAR(10)      NOT NULL,
    Status         VARCHAR(20)      NOT NULL CONSTRAINT DF_FraudFlags_Status DEFAULT ('Open'),
    ReviewedBy     BIGINT           NULL,
    ReviewedAt     DATETIME2(0)     NULL,
    CreatedAt      DATETIME2(0)     NOT NULL CONSTRAINT DF_FraudFlags_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_FraudFlags PRIMARY KEY (FraudFlagId),
    CONSTRAINT FK_FraudFlags_User FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_FraudFlags_ReviewedBy FOREIGN KEY (ReviewedBy) REFERENCES dbo.Users (UserId),
    CONSTRAINT CK_FraudFlags_Severity CHECK (Severity IN ('Low', 'Medium', 'High')),
    CONSTRAINT CK_FraudFlags_Status CHECK (Status IN ('Open', 'Dismissed', 'ActionTaken'))
);
CREATE INDEX IX_FraudFlags_Open ON dbo.FraudFlags (Status, Severity) WHERE Status = 'Open';
GO

-- AuditLogs: who changed what, and when. Written by the API for every important action.
CREATE TABLE dbo.AuditLogs
(
    AuditLogId     BIGINT           IDENTITY(1, 1) NOT NULL,
    ActorUserId    BIGINT           NULL,        -- empty = system job
    Action         VARCHAR(60)      NOT NULL,    -- e.g. owner.Approved, Settlement.Released
    EntityType     VARCHAR(30)      NOT NULL,
    EntityId       BIGINT           NOT NULL,
    OldValues      NVARCHAR(MAX)    NULL,        -- JSON
    NewValues      NVARCHAR(MAX)    NULL,        -- JSON
    IpAddress      VARCHAR(45)      NULL,
    CreatedAt      DATETIME2(0)     NOT NULL CONSTRAINT DF_AuditLogs_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_AuditLogs PRIMARY KEY (AuditLogId),
    CONSTRAINT FK_AuditLogs_ActorUser FOREIGN KEY (ActorUserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT CK_AuditLogs_OldValuesJson CHECK (OldValues IS NULL OR ISJSON(OldValues) = 1),
    CONSTRAINT CK_AuditLogs_NewValuesJson CHECK (NewValues IS NULL OR ISJSON(NewValues) = 1)
);
CREATE INDEX IX_AuditLogs_Entity ON dbo.AuditLogs (EntityType, EntityId, CreatedAt DESC);
GO


/* =============================================================================
   9. STARTING DATA
   Sample rates. Confirm them with your accountant before launch.
   ============================================================================= */


-- Vehicle classes and rate card
INSERT INTO dbo.VehicleTypes
    (Code,        Name,               BodyType,    MaxLoadKg, LengthFt, WidthFt, HeightFt, RecommendedGoods,                            RatePerKm, MinFare, DriverBattaPerDay, SortOrder)
VALUES
    ('ACE',       N'Tata Ace',        'Closed',          750,      7.0,     4.8,      6.0, N'Parcels, FMCG, single-room moves',                16,     450,               500,         1),
    ('PICKUP',    N'Pickup Truck',    'Open',           1500,      8.0,     5.5,     NULL, N'Appliances, hardware, farm produce',              20,     650,               500,         2),
    ('8FT',       N'8 FT Truck',      'Closed',         2000,      8.0,     5.5,      6.0, N'1 BHK moves, retail stock',                       22,     900,               600,         3),
    ('14FT',      N'14 FT Truck',     'Closed',         4000,     14.0,     6.0,      6.5, N'2-3 BHK moves, e-commerce, garments',             32,    1800,               700,         4),
    ('17FT',      N'17 FT Truck',     'Closed',         5000,     17.0,     6.5,      7.0, N'Office moves, packaged goods',                    38,    2400,               800,         5),
    ('20FT',      N'20 FT Truck',     'Closed',         7000,     20.0,     7.0,      7.0, N'Electronics, FMCG distribution',                  45,    3000,               900,         6),
    ('22FT',      N'22 FT Truck',     'Closed',        10000,     22.0,     7.5,      7.0, N'Cement, tiles, bulk cartons',                     52,    3800,               900,         7),
    ('32FT',      N'32 FT Truck',     'Closed',        16000,     32.0,     8.0,      8.0, N'Factory loads, inter-state freight',              72,    6500,              1000,         8),
    ('CONT24',    N'Container',       'Container',     12000,     24.0,     8.0,      8.0, N'High-value, weather-sensitive cargo',             60,    5500,              1000,         9),
    ('OPEN10W',   N'Open Body Truck', 'Open',          21000,     24.0,     8.0,     NULL, N'Steel, sand, agri produce, machinery',            65,    6000,              1000,        10),
    ('TRAILER40', N'Trailer',         'Flatbed',       28000,     40.0,     8.0,     NULL, N'Heavy machinery, coils, ODC',                     95,   12000,              1200,        11);

-- Types of goods
INSERT INTO dbo.GoodsCategories
    (Name,                       RequiresEwayBill)
VALUES
    (N'Household goods',         0),
    (N'Industrial goods',        1),
    (N'Agricultural produce',    0),
    (N'Construction materials',  1),
    (N'FMCG / retail',           1),
    (N'Electronics',             1),
    (N'Textiles & garments',     1),
    (N'Furniture',               1),
    (N'Machinery',               1),
    (N'Other',                   1);

-- Business settings read by the API
INSERT INTO dbo.Settings
    (SettingKey,                SettingValue, Description)
VALUES
    ('CommissionPercent',       N'7',         N'ProCargo commission on the freight, taken from the owner''s payout'),
    ('GstOnFreightPercent',     N'5',         N'GST on freight (confirm the GTA treatment with your CA)'),
    ('GstOnPlatformFeePercent', N'18',        N'GST on the platform fee'),
    ('OtpExpiryMinutes',        N'10',        N'How long a sign-in code is valid'),
    ('OtpMaxAttempts',          N'5',         N'Wrong tries before a code is locked'),
    ('QuoteValidityMinutes',    N'30',        N'How long a customer can pay a quote'),
    ('LoadOfferExpiryMinutes',  N'20',        N'How long an owner has to accept a load'),
    ('GpsPingSeconds',          N'30',        N'How often the driver app sends location'),
    ('SettlementDelayHours',    N'48',        N'Payout time after the POD is approved');

-- Cities served (Karnataka first)
INSERT INTO dbo.Cities
    (Name,           NameKn,          State,           Latitude,   Longitude)
VALUES
    (N'Bengaluru',   N'ಬೆಂಗಳೂರು',      N'Karnataka',    12.971600,  77.594600),
    (N'Mysuru',      N'ಮೈಸೂರು',        N'Karnataka',    12.295800,  76.639400),
    (N'Tumakuru',    N'ತುಮಕೂರು',       N'Karnataka',    13.340900,  77.101000),
    (N'Chitradurga', N'ಚಿತ್ರದುರ್ಗ',     N'Karnataka',    14.230600,  76.398000),
    (N'Davanagere',  N'ದಾವಣಗೆರೆ',      N'Karnataka',    14.464400,  75.921800),
    (N'Hubballi',    N'ಹುಬ್ಬಳ್ಳಿ',       N'Karnataka',    15.364700,  75.124000),
    (N'Belagavi',    N'ಬೆಳಗಾವಿ',       N'Karnataka',    15.849700,  74.497700),
    (N'Mangaluru',   N'ಮಂಗಳೂರು',       N'Karnataka',    12.914100,  74.856000),
    (N'Kalaburagi',  N'ಕಲಬುರಗಿ',       N'Karnataka',    17.329700,  76.834300),
    (N'Pune',        NULL,            N'Maharashtra',  18.520400,  73.856700),
    (N'Mumbai',      NULL,            N'Maharashtra',  19.076000,  72.877700),
    (N'Chennai',     NULL,            N'Tamil Nadu',   13.082700,  80.270700),
    (N'Hyderabad',   NULL,            N'Telangana',    17.385000,  78.486700),
    (N'Kochi',       NULL,            N'Kerala',        9.931200,  76.267300),
    (N'Panaji',      NULL,            N'Goa',          15.490900,  73.827800);
GO


/* =============================================================================
   10. REPORTING VIEWS
   Ready-made queries. Use them like tables: SELECT * FROM dbo.vw_ActiveTrips;
   ============================================================================= */


-- Trips currently on the road, with truck, driver and last known position
CREATE VIEW dbo.vw_ActiveTrips
AS
SELECT
    trip.TripId,
    trip.TripNumber,
    booking.BookingNumber,
    trip.Status,
    booking.PickupAddressText,
    booking.DropAddressText,
    vehicle.RegistrationNumber,
    vehicleType.Name        AS VehicleType,
    driverUser.FullName     AS DriverName,
    driverUser.Mobile       AS DriverMobile,
    vehicle.LastLatitude,
    vehicle.LastLongitude,
    vehicle.LastSeenAt,
    booking.CustomerId,
    trip.OwnerId
FROM dbo.Trips              AS trip
JOIN dbo.Bookings           AS booking      ON booking.BookingId         = trip.BookingId
JOIN dbo.Vehicles           AS vehicle      ON vehicle.VehicleId         = trip.VehicleId
JOIN dbo.VehicleTypes       AS vehicleType  ON vehicleType.VehicleTypeId = vehicle.VehicleTypeId
JOIN dbo.Drivers            AS driver       ON driver.DriverId           = trip.DriverId
JOIN dbo.Users              AS driverUser   ON driverUser.UserId         = driver.UserId
WHERE trip.Status NOT IN ('Completed', 'Cancelled');
GO

-- Verified documents expiring in the next 30 days (insurance, permit, PUC...)
CREATE VIEW dbo.vw_ExpiringDocuments
AS
SELECT
    DocumentId,
    EntityType,
    EntityId,
    DocType,
    ExpiryDate,
    DATEDIFF(DAY, CAST(SYSUTCDATETIME() AS DATE), ExpiryDate) AS DaysLeft
FROM dbo.Documents
WHERE ExpiryDate IS NOT NULL
  AND Status = 'Verified'
  AND ExpiryDate <= DATEADD(DAY, 30, CAST(SYSUTCDATETIME() AS DATE));
GO
