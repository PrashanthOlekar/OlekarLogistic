/* =============================================================================
   ProCargo stored procedures — 4. Bookings, quotes, payments and loads
   A customer's booking from the first price to payment or cancellation,
   and the loads lorry owners can take.
   ============================================================================= */

USE ProCargo;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO


/* ---------------------------------------------------------------- bookings */

-- New booking with its first quote, together. Returns the id and the number customers see (PC-24001).
CREATE OR ALTER PROCEDURE dbo.usp_Booking_Create
    @CustomerId          BIGINT,
    @PickupCityId        INT,
    @PickupAddressText   NVARCHAR(400),
    @PickupLatitude      DECIMAL(9, 6)  = NULL,
    @PickupLongitude     DECIMAL(9, 6)  = NULL,
    @PickupContactName   NVARCHAR(150)  = NULL,
    @PickupContactPhone  VARCHAR(15)    = NULL,
    @DropCityId          INT,
    @DropAddressText     NVARCHAR(400),
    @DropLatitude        DECIMAL(9, 6)  = NULL,
    @DropLongitude       DECIMAL(9, 6)  = NULL,
    @DropContactName     NVARCHAR(150)  = NULL,
    @DropContactPhone    VARCHAR(15)    = NULL,
    @GoodsCategoryId     INT,
    @GoodsDescription    NVARCHAR(400),
    @WeightKg            INT,
    @GoodsValue          DECIMAL(14, 2) = NULL,
    @VehicleTypeId       INT,
    @PickupDate          DATE,
    @PickupSlot          VARCHAR(20)    = NULL,
    @SpecialInstructions NVARCHAR(1000) = NULL,
    -- the first quote
    @DistanceKm          DECIMAL(8, 1),
    @VehicleCost         DECIMAL(12, 2),
    @DriverCost          DECIMAL(12, 2),
    @PlatformFee         DECIMAL(12, 2),
    @TaxAmount           DECIMAL(12, 2),
    @TotalAmount         DECIMAL(12, 2),
    @OwnerPayout         DECIMAL(12, 2),
    @ValidUntil          DATETIME2(0)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    INSERT INTO dbo.Bookings
    (
        CustomerId, PickupAddressText, PickupCityId, PickupLatitude, PickupLongitude, PickupContactName, PickupContactPhone,
        DropAddressText, DropCityId, DropLatitude, DropLongitude, DropContactName, DropContactPhone,
        GoodsCategoryId, GoodsDescription, WeightKg, GoodsValue, VehicleTypeId,
        PickupDate, PickupSlot, SpecialInstructions, Status
    )
    VALUES
    (
        @CustomerId, @PickupAddressText, @PickupCityId, @PickupLatitude, @PickupLongitude, @PickupContactName, @PickupContactPhone,
        @DropAddressText, @DropCityId, @DropLatitude, @DropLongitude, @DropContactName, @DropContactPhone,
        @GoodsCategoryId, @GoodsDescription, @WeightKg, @GoodsValue, @VehicleTypeId,
        @PickupDate, @PickupSlot, @SpecialInstructions, 'Quoted'
    );

    DECLARE @BookingId BIGINT = SCOPE_IDENTITY();

    INSERT INTO dbo.Quotes
        (BookingId, DistanceKm, VehicleCost, DriverCost, LoadingCharges, PlatformFee, TaxAmount, TotalAmount, OwnerPayout, ValidUntil, Status)
    VALUES
        (@BookingId, @DistanceKm, @VehicleCost, @DriverCost, 0, @PlatformFee, @TaxAmount, @TotalAmount, @OwnerPayout, @ValidUntil, 'Sent');

    COMMIT TRANSACTION;

    SELECT BookingId AS Id, BookingNumber
    FROM dbo.Bookings
    WHERE BookingId = @BookingId;
END
GO


-- The columns business rules need (status, owner, size, route).
CREATE OR ALTER PROCEDURE dbo.usp_Booking_GetById
    @BookingId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT BookingId, BookingNumber, CustomerId, PickupCityId, DropCityId, GoodsCategoryId, VehicleTypeId,
           WeightKg, PickupDate, Status, CreatedAt
    FROM dbo.Bookings
    WHERE BookingId = @BookingId;
END
GO


-- Bookings list. Customers pass their @CustomerId; the admin leaves it empty to see everyone's.
-- @Sort: Newest (default) | PickupDate
CREATE OR ALTER PROCEDURE dbo.usp_Booking_GetPaged
    @CustomerId BIGINT        = NULL,
    @Status     VARCHAR(20)   = NULL,
    @Search     NVARCHAR(100) = NULL,
    @Sort       VARCHAR(20)   = 'Newest',
    @PageNumber INT           = 1,
    @PageSize   INT           = 20
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Pattern NVARCHAR(110) =
        CASE WHEN @Search IS NULL OR LTRIM(@Search) = N'' THEN NULL
             ELSE N'%' + REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(@Search)), N'[', N'[[]'), N'%', N'[%]'), N'_', N'[_]') + N'%'
        END;

    SELECT
        booking.BookingId AS Id,
        booking.BookingNumber,
        booking.Status,
        ISNULL(customer.CompanyName, customerUser.FullName) AS Customer,
        customerUser.Mobile AS CustomerMobile,
        pickupCity.Name AS [From],
        dropCity.Name AS [To],
        booking.PickupDate,
        booking.WeightKg,
        vehicleType.Name AS VehicleType,
        booking.VehicleTypeId,
        booking.CreatedAt,
        (SELECT TOP (1) quote.TotalAmount
         FROM dbo.Quotes AS quote
         WHERE quote.BookingId = booking.BookingId
         ORDER BY quote.CreatedAt DESC, quote.QuoteId DESC) AS Total
    FROM dbo.Bookings AS booking
    JOIN dbo.Customers AS customer       ON customer.CustomerId = booking.CustomerId
    JOIN dbo.Users AS customerUser       ON customerUser.UserId = customer.UserId
    JOIN dbo.VehicleTypes AS vehicleType ON vehicleType.VehicleTypeId = booking.VehicleTypeId
    LEFT JOIN dbo.Cities AS pickupCity   ON pickupCity.CityId = booking.PickupCityId
    LEFT JOIN dbo.Cities AS dropCity     ON dropCity.CityId = booking.DropCityId
    WHERE (@CustomerId IS NULL OR booking.CustomerId = @CustomerId)
      AND (@Status IS NULL OR booking.Status = @Status)
      AND (@Pattern IS NULL
           OR booking.BookingNumber LIKE @Pattern
           OR pickupCity.Name LIKE @Pattern
           OR dropCity.Name LIKE @Pattern
           OR customer.CompanyName LIKE @Pattern
           OR customerUser.FullName LIKE @Pattern
           OR customerUser.Mobile LIKE @Pattern)
    ORDER BY
        CASE WHEN @Sort = 'PickupDate' THEN booking.PickupDate END ASC,
        booking.CreatedAt DESC,
        booking.BookingId DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;

    SELECT COUNT_BIG(*) AS TotalRecords
    FROM dbo.Bookings AS booking
    JOIN dbo.Customers AS customer     ON customer.CustomerId = booking.CustomerId
    JOIN dbo.Users AS customerUser     ON customerUser.UserId = customer.UserId
    LEFT JOIN dbo.Cities AS pickupCity ON pickupCity.CityId = booking.PickupCityId
    LEFT JOIN dbo.Cities AS dropCity   ON dropCity.CityId = booking.DropCityId
    WHERE (@CustomerId IS NULL OR booking.CustomerId = @CustomerId)
      AND (@Status IS NULL OR booking.Status = @Status)
      AND (@Pattern IS NULL
           OR booking.BookingNumber LIKE @Pattern
           OR pickupCity.Name LIKE @Pattern
           OR dropCity.Name LIKE @Pattern
           OR customer.CompanyName LIKE @Pattern
           OR customerUser.FullName LIKE @Pattern
           OR customerUser.Mobile LIKE @Pattern);
END
GO


-- Everything on the booking page, in six result sets:
--   1 booking  2 latest quote  3 latest payment  4 invoice  5 active trip  6 trip timeline
CREATE OR ALTER PROCEDURE dbo.usp_Booking_GetDetail
    @BookingId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        booking.BookingId AS Id,
        booking.BookingNumber,
        booking.CustomerId,
        booking.Status,
        booking.PickupDate,
        booking.PickupSlot,
        booking.WeightKg,
        booking.GoodsValue,
        booking.GoodsDescription,
        booking.SpecialInstructions,
        booking.CreatedAt,
        booking.CancelledReason,
        goods.Name AS Goods,
        vehicleType.Name AS VehicleType,
        pickupCity.Name AS PickupCity,
        booking.PickupAddressText AS PickupAddress,
        booking.PickupContactName,
        booking.PickupContactPhone,
        dropCity.Name AS DropCity,
        booking.DropAddressText AS DropAddress,
        booking.DropContactName,
        booking.DropContactPhone
    FROM dbo.Bookings AS booking
    JOIN dbo.GoodsCategories AS goods    ON goods.GoodsCategoryId = booking.GoodsCategoryId
    JOIN dbo.VehicleTypes AS vehicleType ON vehicleType.VehicleTypeId = booking.VehicleTypeId
    LEFT JOIN dbo.Cities AS pickupCity   ON pickupCity.CityId = booking.PickupCityId
    LEFT JOIN dbo.Cities AS dropCity     ON dropCity.CityId = booking.DropCityId
    WHERE booking.BookingId = @BookingId;

    SELECT TOP (1)
        QuoteId, DistanceKm, VehicleCost, DriverCost, LoadingCharges, PlatformFee, TaxAmount, TotalAmount,
        OwnerPayout, ValidUntil, Status, CreatedAt
    FROM dbo.Quotes
    WHERE BookingId = @BookingId
    ORDER BY CreatedAt DESC, QuoteId DESC;

    SELECT TOP (1) Method, Gateway, Status, Amount, PaidAt, GatewayPaymentId
    FROM dbo.Payments
    WHERE BookingId = @BookingId
    ORDER BY CreatedAt DESC, PaymentId DESC;

    SELECT TOP (1) InvoiceNumber, TaxableAmount, CGST AS Cgst, SGST AS Sgst, IGST AS Igst, TotalAmount, IssuedAt
    FROM dbo.Invoices
    WHERE BookingId = @BookingId;

    SELECT TOP (1)
        trip.TripId,
        trip.TripNumber,
        trip.Status,
        vehicle.RegistrationNumber AS Vehicle,
        driverUser.FullName AS DriverName,
        driverUser.Mobile AS DriverMobile,
        trip.PickupOtpProtected,
        trip.DeliveryOtpProtected
    FROM dbo.Trips AS trip
    JOIN dbo.Vehicles AS vehicle  ON vehicle.VehicleId = trip.VehicleId
    JOIN dbo.Drivers AS driver    ON driver.DriverId = trip.DriverId
    JOIN dbo.Users AS driverUser  ON driverUser.UserId = driver.UserId
    WHERE trip.BookingId = @BookingId
      AND trip.Status <> 'Cancelled';

    SELECT tripEvent.EventType, tripEvent.Note, tripEvent.CreatedAt
    FROM dbo.TripEvents AS tripEvent
    JOIN dbo.Trips AS trip ON trip.TripId = tripEvent.TripId
    WHERE trip.BookingId = @BookingId
      AND trip.Status <> 'Cancelled'
    ORDER BY tripEvent.CreatedAt, tripEvent.TripEventId;
END
GO


/* ---------------------------------------------------------------- quotes and payment */

-- The newest quote still open for payment (Status = Sent), expired or not.
CREATE OR ALTER PROCEDURE dbo.usp_Quote_GetLatestOpen
    @BookingId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        QuoteId, BookingId, DistanceKm, VehicleCost, DriverCost, LoadingCharges, PlatformFee, TaxAmount, TotalAmount,
        OwnerPayout, ValidUntil, Status, CreatedAt, AcceptedAt
    FROM dbo.Quotes
    WHERE BookingId = @BookingId
      AND Status = 'Sent'
    ORDER BY CreatedAt DESC, QuoteId DESC;
END
GO


-- A fresh price for an unpaid booking. Older open quotes become Expired or Superseded.
CREATE OR ALTER PROCEDURE dbo.usp_Booking_Requote
    @BookingId   BIGINT,
    @DistanceKm  DECIMAL(8, 1),
    @VehicleCost DECIMAL(12, 2),
    @DriverCost  DECIMAL(12, 2),
    @PlatformFee DECIMAL(12, 2),
    @TaxAmount   DECIMAL(12, 2),
    @TotalAmount DECIMAL(12, 2),
    @OwnerPayout DECIMAL(12, 2),
    @ValidUntil  DATETIME2(0)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @Status VARCHAR(20) =
        (SELECT Status FROM dbo.Bookings WITH (UPDLOCK, HOLDLOCK) WHERE BookingId = @BookingId);

    IF @Status IS NULL OR @Status <> 'Quoted'
        THROW 50409, 'Only unpaid bookings can be re-quoted.', 1;

    UPDATE dbo.Quotes
    SET Status = CASE WHEN ValidUntil < SYSUTCDATETIME() THEN 'Expired' ELSE 'Superseded' END
    WHERE BookingId = @BookingId
      AND Status = 'Sent';

    INSERT INTO dbo.Quotes
        (BookingId, DistanceKm, VehicleCost, DriverCost, LoadingCharges, PlatformFee, TaxAmount, TotalAmount, OwnerPayout, ValidUntil, Status)
    VALUES
        (@BookingId, @DistanceKm, @VehicleCost, @DriverCost, 0, @PlatformFee, @TaxAmount, @TotalAmount, @OwnerPayout, @ValidUntil, 'Sent');

    DECLARE @QuoteId BIGINT = SCOPE_IDENTITY();

    UPDATE dbo.Bookings SET UpdatedAt = SYSUTCDATETIME() WHERE BookingId = @BookingId;

    COMMIT TRANSACTION;
    SELECT @QuoteId AS QuoteId;
END
GO


-- Records a captured payment for the open quote: payment row, quote Accepted, booking Confirmed.
CREATE OR ALTER PROCEDURE dbo.usp_Booking_Pay
    @BookingId        BIGINT,
    @QuoteId          BIGINT,
    @Method           VARCHAR(20),
    @Gateway          VARCHAR(20),
    @GatewayOrderId   VARCHAR(100) = NULL,
    @GatewayPaymentId VARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @Status VARCHAR(20), @CustomerId BIGINT;

    SELECT @Status = Status, @CustomerId = CustomerId
    FROM dbo.Bookings WITH (UPDLOCK, HOLDLOCK)
    WHERE BookingId = @BookingId;

    IF @Status IS NULL OR @Status <> 'Quoted'
        THROW 50409, 'This booking is already paid or closed.', 1;

    DECLARE @Amount DECIMAL(12, 2), @QuoteStatus VARCHAR(20), @ValidUntil DATETIME2(0);

    SELECT @Amount = TotalAmount, @QuoteStatus = Status, @ValidUntil = ValidUntil
    FROM dbo.Quotes WITH (UPDLOCK, HOLDLOCK)
    WHERE QuoteId = @QuoteId AND BookingId = @BookingId;

    IF @QuoteStatus IS NULL OR @QuoteStatus <> 'Sent'
        THROW 50409, 'Get a fresh quote first.', 1;

    IF @ValidUntil < SYSUTCDATETIME()
        THROW 50400, 'This quote has expired. Get a fresh quote first.', 1;

    INSERT INTO dbo.Payments
        (BookingId, CustomerId, QuoteId, Amount, Method, Gateway, GatewayOrderId, GatewayPaymentId, Status, PaidAt)
    VALUES
        (@BookingId, @CustomerId, @QuoteId, @Amount, @Method, @Gateway, @GatewayOrderId, @GatewayPaymentId, 'Captured', SYSUTCDATETIME());

    DECLARE @PaymentId BIGINT = SCOPE_IDENTITY();

    UPDATE dbo.Quotes
    SET Status = 'Accepted', AcceptedAt = SYSUTCDATETIME()
    WHERE QuoteId = @QuoteId;

    UPDATE dbo.Bookings
    SET Status = 'Confirmed', UpdatedAt = SYSUTCDATETIME()
    WHERE BookingId = @BookingId;

    COMMIT TRANSACTION;
    SELECT @PaymentId AS PaymentId, @Amount AS Amount;
END
GO


-- Cancels a booking before the goods are loaded: frees the truck and driver, refunds captured payments.
CREATE OR ALTER PROCEDURE dbo.usp_Booking_Cancel
    @BookingId   BIGINT,
    @CancelledBy BIGINT,
    @Reason      NVARCHAR(400) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @TooLate NVARCHAR(200) = N'Goods are already loaded, so this booking can''t be cancelled online. Call support.';

    BEGIN TRANSACTION;

    DECLARE @Status VARCHAR(20) =
        (SELECT Status FROM dbo.Bookings WITH (UPDLOCK, HOLDLOCK) WHERE BookingId = @BookingId);

    IF @Status IS NULL OR @Status NOT IN ('QuotePending', 'Quoted', 'Confirmed', 'Assigned')
        THROW 50400, @TooLate, 1;

    DECLARE @TripId BIGINT, @TripStatus VARCHAR(20), @VehicleId BIGINT, @DriverId BIGINT;

    SELECT @TripId = TripId, @TripStatus = Status, @VehicleId = VehicleId, @DriverId = DriverId
    FROM dbo.Trips WITH (UPDLOCK, HOLDLOCK)
    WHERE BookingId = @BookingId AND Status <> 'Cancelled';

    IF @TripId IS NOT NULL
    BEGIN
        IF @TripStatus NOT IN ('Assigned', 'EnRouteToPickup', 'AtPickup')
            THROW 50400, @TooLate, 1;

        UPDATE dbo.Trips SET Status = 'Cancelled', UpdatedAt = SYSUTCDATETIME() WHERE TripId = @TripId;
        UPDATE dbo.Vehicles SET AvailabilityStatus = 'Available', UpdatedAt = SYSUTCDATETIME() WHERE VehicleId = @VehicleId;
        UPDATE dbo.Drivers SET DutyStatus = 'Available', UpdatedAt = SYSUTCDATETIME() WHERE DriverId = @DriverId;

        INSERT INTO dbo.TripEvents (TripId, EventType, Note, CreatedBy)
        VALUES (@TripId, 'Cancelled', N'Cancelled by customer', @CancelledBy);
    END

    -- Test mode: payments are marked refunded. With a real gateway, call its refund API and add a Refunds row.
    UPDATE dbo.Payments
    SET Status = 'Refunded'
    WHERE BookingId = @BookingId AND Status = 'Captured';

    UPDATE dbo.Bookings
    SET Status = 'Cancelled',
        CancelledBy = @CancelledBy,
        CancelledReason = @Reason,
        UpdatedAt = SYSUTCDATETIME()
    WHERE BookingId = @BookingId;

    COMMIT TRANSACTION;
END
GO


-- Verified, free trucks of the right type and size for a booking, then the owners' free verified drivers.
CREATE OR ALTER PROCEDURE dbo.usp_Booking_GetAssignableVehicles
    @BookingId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @VehicleTypeId INT, @WeightKg INT;

    SELECT @VehicleTypeId = VehicleTypeId, @WeightKg = WeightKg
    FROM dbo.Bookings
    WHERE BookingId = @BookingId;

    SELECT
        vehicle.VehicleId AS Id,
        vehicle.OwnerId,
        vehicle.RegistrationNumber,
        ownerUser.FullName AS Owner,
        vehicle.CurrentDriverId
    FROM dbo.Vehicles AS vehicle
    JOIN dbo.Owners AS owner    ON owner.OwnerId = vehicle.OwnerId
    JOIN dbo.Users AS ownerUser ON ownerUser.UserId = owner.UserId
    WHERE vehicle.VehicleTypeId = @VehicleTypeId
      AND vehicle.CapacityKg >= @WeightKg
      AND vehicle.VerificationStatus = 'Approved'
      AND vehicle.AvailabilityStatus = 'Available'
      AND owner.KycStatus = 'Approved'
    ORDER BY vehicle.RegistrationNumber;

    SELECT driver.DriverId AS Id, driver.OwnerId, driverUser.FullName AS Name
    FROM dbo.Drivers AS driver
    JOIN dbo.Users AS driverUser ON driverUser.UserId = driver.UserId
    WHERE driver.KycStatus = 'Approved'
      AND driver.DutyStatus = 'Available'
      AND driver.OwnerId IN
      (
          SELECT vehicle.OwnerId
          FROM dbo.Vehicles AS vehicle
          WHERE vehicle.VehicleTypeId = @VehicleTypeId
            AND vehicle.CapacityKg >= @WeightKg
            AND vehicle.VerificationStatus = 'Approved'
            AND vehicle.AvailabilityStatus = 'Available'
      )
    ORDER BY driverUser.FullName;
END
GO


/* ---------------------------------------------------------------- loads for owners */

-- Paid bookings one of the owner's free, verified trucks could carry (declined ones hidden),
-- then the owner's free trucks. The service pairs each load with the trucks that fit it.
CREATE OR ALTER PROCEDURE dbo.usp_Load_GetAvailable
    @OwnerId BIGINT,
    @MaxRows INT = 100
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@MaxRows)
        booking.BookingId AS Id,
        booking.BookingNumber,
        booking.PickupDate,
        booking.PickupSlot,
        booking.WeightKg,
        booking.VehicleTypeId,
        booking.GoodsDescription AS Goods,
        pickupCity.Name AS [From],
        booking.PickupAddressText AS FromAddress,
        dropCity.Name AS [To],
        vehicleType.Name AS VehicleType,
        quote.DistanceKm,
        quote.OwnerPayout AS Payout
    FROM dbo.Bookings AS booking
    JOIN dbo.VehicleTypes AS vehicleType ON vehicleType.VehicleTypeId = booking.VehicleTypeId
    LEFT JOIN dbo.Cities AS pickupCity   ON pickupCity.CityId = booking.PickupCityId
    LEFT JOIN dbo.Cities AS dropCity     ON dropCity.CityId = booking.DropCityId
    OUTER APPLY
    (
        SELECT TOP (1) accepted.DistanceKm, accepted.OwnerPayout
        FROM dbo.Quotes AS accepted
        WHERE accepted.BookingId = booking.BookingId AND accepted.Status = 'Accepted'
    ) AS quote
    WHERE booking.Status = 'Confirmed'
      AND EXISTS
      (
          SELECT 1 FROM dbo.Vehicles AS vehicle
          WHERE vehicle.OwnerId = @OwnerId
            AND vehicle.VehicleTypeId = booking.VehicleTypeId
            AND vehicle.CapacityKg >= booking.WeightKg
            AND vehicle.VerificationStatus = 'Approved'
            AND vehicle.AvailabilityStatus = 'Available'
      )
      AND NOT EXISTS
      (
          SELECT 1 FROM dbo.LoadOffers AS offer
          WHERE offer.BookingId = booking.BookingId
            AND offer.OwnerId = @OwnerId
            AND offer.Status = 'Declined'
      )
    ORDER BY booking.PickupDate, booking.BookingId;

    SELECT VehicleId AS Id, RegistrationNumber, VehicleTypeId, CapacityKg, CurrentDriverId
    FROM dbo.Vehicles
    WHERE OwnerId = @OwnerId
      AND VerificationStatus = 'Approved'
      AND AvailabilityStatus = 'Available'
    ORDER BY RegistrationNumber;
END
GO


-- The owner hides a load from their list.
CREATE OR ALTER PROCEDURE dbo.usp_LoadOffer_Decline
    @BookingId BIGINT,
    @OwnerId   BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.LoadOffers WHERE BookingId = @BookingId AND OwnerId = @OwnerId AND Status = 'Declined')
    BEGIN
        INSERT INTO dbo.LoadOffers (BookingId, OwnerId, OfferedPayout, Status, ExpiresAt, RespondedAt)
        VALUES (@BookingId, @OwnerId, 0, 'Declined', SYSUTCDATETIME(), SYSUTCDATETIME());
    END
END
GO


/* ---------------------------------------------------------------- payments list (admin) */

CREATE OR ALTER PROCEDURE dbo.usp_Payment_GetPaged
    @Status     VARCHAR(20)   = NULL,
    @Search     NVARCHAR(100) = NULL,
    @PageNumber INT           = 1,
    @PageSize   INT           = 20
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Pattern NVARCHAR(110) =
        CASE WHEN @Search IS NULL OR LTRIM(@Search) = N'' THEN NULL
             ELSE N'%' + REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(@Search)), N'[', N'[[]'), N'%', N'[%]'), N'_', N'[_]') + N'%'
        END;

    SELECT
        payment.PaymentId AS Id,
        booking.BookingNumber,
        ISNULL(customer.CompanyName, customerUser.FullName) AS Customer,
        payment.Amount,
        payment.Method,
        payment.Gateway,
        payment.Status,
        payment.PaidAt,
        payment.GatewayPaymentId,
        payment.CreatedAt
    FROM dbo.Payments AS payment
    JOIN dbo.Bookings AS booking   ON booking.BookingId = payment.BookingId
    JOIN dbo.Customers AS customer ON customer.CustomerId = payment.CustomerId
    JOIN dbo.Users AS customerUser ON customerUser.UserId = customer.UserId
    WHERE (@Status IS NULL OR payment.Status = @Status)
      AND (@Pattern IS NULL
           OR booking.BookingNumber LIKE @Pattern
           OR customer.CompanyName LIKE @Pattern
           OR customerUser.FullName LIKE @Pattern
           OR payment.GatewayPaymentId LIKE @Pattern)
    ORDER BY payment.CreatedAt DESC, payment.PaymentId DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;

    SELECT COUNT_BIG(*) AS TotalRecords
    FROM dbo.Payments AS payment
    JOIN dbo.Bookings AS booking   ON booking.BookingId = payment.BookingId
    JOIN dbo.Customers AS customer ON customer.CustomerId = payment.CustomerId
    JOIN dbo.Users AS customerUser ON customerUser.UserId = customer.UserId
    WHERE (@Status IS NULL OR payment.Status = @Status)
      AND (@Pattern IS NULL
           OR booking.BookingNumber LIKE @Pattern
           OR customer.CompanyName LIKE @Pattern
           OR customerUser.FullName LIKE @Pattern
           OR payment.GatewayPaymentId LIKE @Pattern);
END
GO
