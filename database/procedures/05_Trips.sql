/* =============================================================================
   ProCargo stored procedures — 5. Trips
   Assigning a truck, every step of the journey, delivery proof and its approval.
   ============================================================================= */

USE ProCargo;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO


-- Creates the trip for a paid booking, in one transaction:
-- trip + first timeline event, booking Assigned, truck Busy with this driver, driver OnTrip.
-- When an owner takes the load (@AcceptedByOwnerId), their acceptance is recorded too.
-- The service checks the friendly rules first; this procedure re-checks the ones another user could change meanwhile.
CREATE OR ALTER PROCEDURE dbo.usp_Trip_Assign
    @BookingId            BIGINT,
    @VehicleId            BIGINT,
    @DriverId             BIGINT,
    @AcceptedByOwnerId    BIGINT = NULL,
    @PickupOtpProtected   VARCHAR(400),
    @DeliveryOtpProtected VARCHAR(400),
    @ActorUserId          BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM dbo.Bookings WITH (UPDLOCK, HOLDLOCK) WHERE BookingId = @BookingId AND Status = 'Confirmed')
        THROW 50409, 'This load has already been taken or is not paid yet.', 1;

    DECLARE @OwnerId BIGINT, @RegistrationNumber VARCHAR(15);

    SELECT @OwnerId = OwnerId, @RegistrationNumber = RegistrationNumber
    FROM dbo.Vehicles WITH (UPDLOCK, HOLDLOCK)
    WHERE VehicleId = @VehicleId
      AND VerificationStatus = 'Approved'
      AND AvailabilityStatus = 'Available';

    IF @OwnerId IS NULL
        THROW 50409, 'This vehicle is not available.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Trips WITH (UPDLOCK, HOLDLOCK)
               WHERE DriverId = @DriverId AND Status NOT IN ('Delivered', 'Completed', 'Cancelled'))
        THROW 50409, 'This driver is already on a trip.', 1;

    DECLARE @DistanceKm DECIMAL(8, 1), @OwnerPayout DECIMAL(12, 2);

    SELECT TOP (1) @DistanceKm = DistanceKm, @OwnerPayout = OwnerPayout
    FROM dbo.Quotes
    WHERE BookingId = @BookingId AND Status = 'Accepted'
    ORDER BY AcceptedAt DESC;

    IF @OwnerPayout IS NULL
        THROW 50409, 'This booking has no accepted quote.', 1;

    INSERT INTO dbo.Trips
        (BookingId, OwnerId, VehicleId, DriverId, Status, PickupOtpProtected, DeliveryOtpProtected, PlannedDistanceKm, OwnerPayout)
    VALUES
        (@BookingId, @OwnerId, @VehicleId, @DriverId, 'Assigned', @PickupOtpProtected, @DeliveryOtpProtected, @DistanceKm, @OwnerPayout);

    DECLARE @TripId BIGINT = SCOPE_IDENTITY();

    INSERT INTO dbo.TripEvents (TripId, EventType, Note, CreatedBy)
    VALUES (@TripId, 'Assigned', N'Vehicle ' + @RegistrationNumber + N' assigned', @ActorUserId);

    UPDATE dbo.Bookings SET Status = 'Assigned', UpdatedAt = SYSUTCDATETIME() WHERE BookingId = @BookingId;

    UPDATE dbo.Vehicles
    SET AvailabilityStatus = 'Busy', CurrentDriverId = @DriverId, UpdatedAt = SYSUTCDATETIME()
    WHERE VehicleId = @VehicleId;

    UPDATE dbo.Drivers SET DutyStatus = 'OnTrip', UpdatedAt = SYSUTCDATETIME() WHERE DriverId = @DriverId;

    IF @AcceptedByOwnerId IS NOT NULL
    BEGIN
        INSERT INTO dbo.LoadOffers (BookingId, OwnerId, VehicleId, OfferedPayout, Status, ExpiresAt, RespondedAt)
        VALUES (@BookingId, @AcceptedByOwnerId, @VehicleId, @OwnerPayout, 'Accepted', SYSUTCDATETIME(), SYSUTCDATETIME());
    END

    COMMIT TRANSACTION;

    SELECT TripId AS Id, TripNumber
    FROM dbo.Trips
    WHERE TripId = @TripId;
END
GO


-- The columns business rules need.
CREATE OR ALTER PROCEDURE dbo.usp_Trip_GetById
    @TripId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TripId, TripNumber, BookingId, OwnerId, VehicleId, DriverId, Status,
           PickupOtpProtected, DeliveryOtpProtected, PlannedDistanceKm, OwnerPayout, DriverPay, CreatedAt, DeliveredAt
    FROM dbo.Trips
    WHERE TripId = @TripId;
END
GO


-- Trips list for an owner, a driver, or the admin (all).
-- @Sort: Newest (default) | ActiveFirst (open trips first, then newest)
CREATE OR ALTER PROCEDURE dbo.usp_Trip_GetPaged
    @OwnerId    BIGINT        = NULL,
    @DriverId   BIGINT        = NULL,
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
        trip.TripId AS Id,
        trip.TripNumber,
        trip.Status,
        booking.BookingNumber,
        pickupCity.Name AS [From],
        dropCity.Name AS [To],
        booking.PickupDate,
        vehicle.RegistrationNumber AS Vehicle,
        driverUser.FullName AS Driver,
        driverUser.Mobile AS DriverMobile,
        ownerUser.FullName AS Owner,
        trip.OwnerPayout,
        trip.CreatedAt,
        trip.DeliveredAt,
        (SELECT TOP (1) document.DocumentId
         FROM dbo.Documents AS document
         WHERE document.EntityType = 'Trip' AND document.EntityId = trip.TripId AND document.DocType = 'POD'
         ORDER BY document.UploadedAt DESC) AS Pod,
        (SELECT TOP (1) tripEvent.EventType
         FROM dbo.TripEvents AS tripEvent
         WHERE tripEvent.TripId = trip.TripId
         ORDER BY tripEvent.CreatedAt DESC, tripEvent.TripEventId DESC) AS LastEvent
    FROM dbo.Trips AS trip
    JOIN dbo.Bookings AS booking       ON booking.BookingId = trip.BookingId
    JOIN dbo.Vehicles AS vehicle       ON vehicle.VehicleId = trip.VehicleId
    JOIN dbo.Drivers AS driver         ON driver.DriverId = trip.DriverId
    JOIN dbo.Users AS driverUser       ON driverUser.UserId = driver.UserId
    JOIN dbo.Owners AS owner           ON owner.OwnerId = trip.OwnerId
    JOIN dbo.Users AS ownerUser        ON ownerUser.UserId = owner.UserId
    LEFT JOIN dbo.Cities AS pickupCity ON pickupCity.CityId = booking.PickupCityId
    LEFT JOIN dbo.Cities AS dropCity   ON dropCity.CityId = booking.DropCityId
    WHERE (@OwnerId IS NULL OR trip.OwnerId = @OwnerId)
      AND (@DriverId IS NULL OR trip.DriverId = @DriverId)
      AND (@Status IS NULL OR trip.Status = @Status)
      AND (@Pattern IS NULL
           OR trip.TripNumber LIKE @Pattern
           OR booking.BookingNumber LIKE @Pattern
           OR vehicle.RegistrationNumber LIKE @Pattern
           OR driverUser.FullName LIKE @Pattern
           OR pickupCity.Name LIKE @Pattern
           OR dropCity.Name LIKE @Pattern)
    ORDER BY
        CASE WHEN @Sort = 'ActiveFirst' AND trip.Status IN ('Completed', 'Cancelled') THEN 1 ELSE 0 END,
        trip.CreatedAt DESC,
        trip.TripId DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;

    SELECT COUNT_BIG(*) AS TotalRecords
    FROM dbo.Trips AS trip
    JOIN dbo.Bookings AS booking       ON booking.BookingId = trip.BookingId
    JOIN dbo.Vehicles AS vehicle       ON vehicle.VehicleId = trip.VehicleId
    JOIN dbo.Drivers AS driver         ON driver.DriverId = trip.DriverId
    JOIN dbo.Users AS driverUser       ON driverUser.UserId = driver.UserId
    LEFT JOIN dbo.Cities AS pickupCity ON pickupCity.CityId = booking.PickupCityId
    LEFT JOIN dbo.Cities AS dropCity   ON dropCity.CityId = booking.DropCityId
    WHERE (@OwnerId IS NULL OR trip.OwnerId = @OwnerId)
      AND (@DriverId IS NULL OR trip.DriverId = @DriverId)
      AND (@Status IS NULL OR trip.Status = @Status)
      AND (@Pattern IS NULL
           OR trip.TripNumber LIKE @Pattern
           OR booking.BookingNumber LIKE @Pattern
           OR vehicle.RegistrationNumber LIKE @Pattern
           OR driverUser.FullName LIKE @Pattern
           OR pickupCity.Name LIKE @Pattern
           OR dropCity.Name LIKE @Pattern);
END
GO


-- One trip for the driver app: 1 trip with booking and stops, 2 timeline.
CREATE OR ALTER PROCEDURE dbo.usp_Trip_GetDetail
    @TripId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        trip.TripId AS Id,
        trip.TripNumber,
        trip.Status,
        trip.OwnerId,
        trip.DriverId,
        vehicle.RegistrationNumber AS Vehicle,
        CAST(CASE WHEN EXISTS (
            SELECT 1 FROM dbo.Documents
            WHERE EntityType = 'Trip' AND EntityId = trip.TripId AND DocType = 'POD'
        ) THEN 1 ELSE 0 END AS BIT) AS HasPod,
        trip.PlannedDistanceKm,
        trip.DriverPay,
        booking.BookingNumber,
        booking.GoodsDescription,
        booking.WeightKg,
        booking.PickupDate,
        booking.PickupSlot,
        booking.SpecialInstructions,
        pickupCity.Name AS PickupCity,
        booking.PickupAddressText AS PickupAddress,
        booking.PickupContactName,
        booking.PickupContactPhone,
        dropCity.Name AS DropCity,
        booking.DropAddressText AS DropAddress,
        booking.DropContactName,
        booking.DropContactPhone
    FROM dbo.Trips AS trip
    JOIN dbo.Bookings AS booking       ON booking.BookingId = trip.BookingId
    JOIN dbo.Vehicles AS vehicle       ON vehicle.VehicleId = trip.VehicleId
    LEFT JOIN dbo.Cities AS pickupCity ON pickupCity.CityId = booking.PickupCityId
    LEFT JOIN dbo.Cities AS dropCity   ON dropCity.CityId = booking.DropCityId
    WHERE trip.TripId = @TripId;

    SELECT EventType, Note, CreatedAt
    FROM dbo.TripEvents
    WHERE TripId = @TripId
    ORDER BY CreatedAt, TripEventId;
END
GO


-- Moves a trip one step: checks the current status, sets the new one and its time stamp,
-- records the timeline event and the truck's last position.
-- @AllowedFrom: comma-separated statuses, e.g. 'Assigned,EnRouteToPickup'.
CREATE OR ALTER PROCEDURE dbo.usp_Trip_ChangeStatus
    @TripId      BIGINT,
    @AllowedFrom VARCHAR(200),
    @NewStatus   VARCHAR(20),
    @EventType   VARCHAR(30),
    @Note        NVARCHAR(500)  = NULL,
    @Latitude    DECIMAL(9, 6)  = NULL,
    @Longitude   DECIMAL(9, 6)  = NULL,
    @ActorUserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @Status VARCHAR(20), @BookingId BIGINT, @VehicleId BIGINT;

    SELECT @Status = Status, @BookingId = BookingId, @VehicleId = VehicleId
    FROM dbo.Trips WITH (UPDLOCK, HOLDLOCK)
    WHERE TripId = @TripId;

    IF @Status IS NULL OR @Status NOT IN (SELECT LTRIM(RTRIM(value)) FROM STRING_SPLIT(@AllowedFrom, ','))
    BEGIN
        DECLARE @Message NVARCHAR(200) = N'This step isn''t available while the trip is ' + ISNULL(@Status, N'unknown') + N'.';
        THROW 50409, @Message, 1;
    END

    DECLARE @Now DATETIME2(0) = SYSUTCDATETIME();

    UPDATE dbo.Trips
    SET Status          = @NewStatus,
        ReachedPickupAt = CASE WHEN @NewStatus = 'AtPickup'      THEN @Now ELSE ReachedPickupAt END,
        LoadedAt        = CASE WHEN @NewStatus = 'Loaded'        THEN @Now ELSE LoadedAt END,
        StartedAt       = CASE WHEN @NewStatus = 'InTransit'     THEN @Now ELSE StartedAt END,
        ReachedDropAt   = CASE WHEN @NewStatus = 'AtDestination' THEN @Now ELSE ReachedDropAt END,
        UpdatedAt       = @Now
    WHERE TripId = @TripId;

    IF @NewStatus = 'InTransit'
        UPDATE dbo.Bookings SET Status = 'InTransit', UpdatedAt = @Now WHERE BookingId = @BookingId;

    INSERT INTO dbo.TripEvents (TripId, EventType, Note, Latitude, Longitude, CreatedBy)
    VALUES (@TripId, @EventType, @Note, @Latitude, @Longitude, @ActorUserId);

    IF @Latitude IS NOT NULL AND @Longitude IS NOT NULL
        UPDATE dbo.Vehicles
        SET LastLatitude = @Latitude, LastLongitude = @Longitude, LastSeenAt = @Now
        WHERE VehicleId = @VehicleId;

    COMMIT TRANSACTION;
END
GO


-- The receiver's code was confirmed: trip and booking Delivered, truck and driver free again,
-- and the owner's payout created (it waits for the POD approval).
CREATE OR ALTER PROCEDURE dbo.usp_Trip_CompleteDelivery
    @TripId      BIGINT,
    @ActorUserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @Status VARCHAR(20), @BookingId BIGINT, @VehicleId BIGINT, @DriverId BIGINT, @OwnerId BIGINT;

    SELECT @Status = Status, @BookingId = BookingId, @VehicleId = VehicleId, @DriverId = DriverId, @OwnerId = OwnerId
    FROM dbo.Trips WITH (UPDLOCK, HOLDLOCK)
    WHERE TripId = @TripId;

    IF @Status IS NULL OR @Status <> 'AtDestination'
        THROW 50409, 'Mark "Reached destination" first.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.Documents WHERE EntityType = 'Trip' AND EntityId = @TripId AND DocType = 'POD')
        THROW 50400, 'Upload the signed delivery receipt (POD) first.', 1;

    DECLARE @Now DATETIME2(0) = SYSUTCDATETIME();

    UPDATE dbo.Trips SET Status = 'Delivered', DeliveredAt = @Now, UpdatedAt = @Now WHERE TripId = @TripId;

    INSERT INTO dbo.TripEvents (TripId, EventType, Note, CreatedBy)
    VALUES (@TripId, 'DeliveryOtpVerified', N'Goods delivered', @ActorUserId);

    UPDATE dbo.Bookings SET Status = 'Delivered', UpdatedAt = @Now WHERE BookingId = @BookingId;
    UPDATE dbo.Vehicles SET AvailabilityStatus = 'Available', UpdatedAt = @Now WHERE VehicleId = @VehicleId;
    UPDATE dbo.Drivers SET DutyStatus = 'Available', UpdatedAt = @Now WHERE DriverId = @DriverId;

    DECLARE @Freight DECIMAL(12, 2), @Commission DECIMAL(12, 2);

    SELECT TOP (1) @Freight = VehicleCost + DriverCost + LoadingCharges, @Commission = PlatformFee
    FROM dbo.Quotes
    WHERE BookingId = @BookingId AND Status = 'Accepted'
    ORDER BY AcceptedAt DESC;

    INSERT INTO dbo.Settlements (TripId, OwnerId, OwnerBankAccountId, GrossAmount, CommissionAmount, TdsAmount, NetAmount, Status)
    SELECT
        @TripId,
        @OwnerId,
        (SELECT TOP (1) OwnerBankAccountId FROM dbo.OwnerBankAccounts
         WHERE OwnerId = @OwnerId AND IsPrimary = 1 AND IsActive = 1),
        @Freight,
        @Commission,
        0,
        @Freight - @Commission,
        'AwaitingPod';

    COMMIT TRANSACTION;
END
GO


-- A goods photo or signed POD from the driver: the document row and its timeline event, together.
CREATE OR ALTER PROCEDURE dbo.usp_Trip_AddPhoto
    @TripId      BIGINT,
    @DocType     VARCHAR(30),
    @BlobPath    NVARCHAR(400),
    @FileName    NVARCHAR(200),
    @ContentType VARCHAR(100),
    @SizeBytes   BIGINT,
    @Sha256      CHAR(64)      = NULL,
    @Latitude    DECIMAL(9, 6) = NULL,
    @Longitude   DECIMAL(9, 6) = NULL,
    @UploadedBy  BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    INSERT INTO dbo.Documents
        (EntityType, EntityId, DocType, BlobPath, FileName, ContentType, SizeBytes, Sha256, UploadedBy, Latitude, Longitude)
    VALUES
        ('Trip', @TripId, @DocType, @BlobPath, @FileName, @ContentType, @SizeBytes, @Sha256, @UploadedBy, @Latitude, @Longitude);

    DECLARE @DocumentId BIGINT = SCOPE_IDENTITY();

    INSERT INTO dbo.TripEvents (TripId, EventType, DocumentId, Latitude, Longitude, CreatedBy)
    VALUES (@TripId, CASE WHEN @DocType = 'POD' THEN 'PodUploaded' ELSE 'GoodsPhotoUploaded' END,
            @DocumentId, @Latitude, @Longitude, @UploadedBy);

    COMMIT TRANSACTION;
    SELECT @DocumentId AS DocumentId;
END
GO


CREATE OR ALTER PROCEDURE dbo.usp_Trip_HasPod
    @TripId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CAST(CASE WHEN EXISTS (
        SELECT 1 FROM dbo.Documents
        WHERE EntityType = 'Trip' AND EntityId = @TripId AND DocType = 'POD'
    ) THEN 1 ELSE 0 END AS BIT) AS HasPod;
END
GO


/* ---------------------------------------------------------------- POD approval and invoice */

-- What the invoice needs: the trip, the customer's GSTIN, both states and the accepted quote.
CREATE OR ALTER PROCEDURE dbo.usp_Trip_GetForPodApproval
    @TripId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        trip.TripId,
        trip.Status,
        trip.BookingId,
        booking.CustomerId,
        customer.GSTIN AS CustomerGstin,
        pickupCity.State AS PickupState,
        dropCity.State AS DropState,
        quote.TaxAmount,
        quote.TotalAmount
    FROM dbo.Trips AS trip
    JOIN dbo.Bookings AS booking       ON booking.BookingId = trip.BookingId
    JOIN dbo.Customers AS customer     ON customer.CustomerId = booking.CustomerId
    LEFT JOIN dbo.Cities AS pickupCity ON pickupCity.CityId = booking.PickupCityId
    LEFT JOIN dbo.Cities AS dropCity   ON dropCity.CityId = booking.DropCityId
    OUTER APPLY
    (
        SELECT TOP (1) accepted.TaxAmount, accepted.TotalAmount
        FROM dbo.Quotes AS accepted
        WHERE accepted.BookingId = booking.BookingId AND accepted.Status = 'Accepted'
        ORDER BY accepted.AcceptedAt DESC
    ) AS quote
    WHERE trip.TripId = @TripId;
END
GO


-- The next running number for invoices (the API formats it as PC/26-27/000001).
CREATE OR ALTER PROCEDURE dbo.usp_Invoice_NextSequence
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CAST(NEXT VALUE FOR dbo.InvoiceNumberSeq AS BIGINT) AS Sequence;
END
GO


-- The admin checked the signed delivery receipt. In one transaction:
-- trip and booking Completed, timeline event, payout Approved, POD documents Verified, GST invoice issued.
CREATE OR ALTER PROCEDURE dbo.usp_Trip_ApprovePod
    @TripId        BIGINT,
    @AdminUserId   BIGINT,
    @InvoiceNumber VARCHAR(30),
    @PlaceOfSupply NVARCHAR(60),
    @CustomerGstin CHAR(15)       = NULL,
    @TaxableAmount DECIMAL(12, 2),
    @Cgst          DECIMAL(12, 2),
    @Sgst          DECIMAL(12, 2),
    @Igst          DECIMAL(12, 2),
    @TotalAmount   DECIMAL(12, 2)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @Status VARCHAR(20), @BookingId BIGINT, @CustomerId BIGINT;

    SELECT @Status = trip.Status, @BookingId = trip.BookingId, @CustomerId = booking.CustomerId
    FROM dbo.Trips AS trip WITH (UPDLOCK, HOLDLOCK)
    JOIN dbo.Bookings AS booking ON booking.BookingId = trip.BookingId
    WHERE trip.TripId = @TripId;

    IF @Status IS NULL OR @Status <> 'Delivered'
        THROW 50409, 'Only delivered trips can have their POD approved.', 1;

    DECLARE @Now DATETIME2(0) = SYSUTCDATETIME();

    UPDATE dbo.Trips
    SET Status = 'Completed', PodApprovedBy = @AdminUserId, PodApprovedAt = @Now, UpdatedAt = @Now
    WHERE TripId = @TripId;

    UPDATE dbo.Bookings SET Status = 'Completed', UpdatedAt = @Now WHERE BookingId = @BookingId;

    INSERT INTO dbo.TripEvents (TripId, EventType, CreatedBy)
    VALUES (@TripId, 'PodApproved', @AdminUserId);

    UPDATE dbo.Settlements
    SET Status = 'Approved', ApprovedBy = @AdminUserId
    WHERE TripId = @TripId;

    UPDATE dbo.Documents
    SET Status = 'Verified', ReviewedBy = @AdminUserId, ReviewedAt = @Now
    WHERE EntityType = 'Trip' AND EntityId = @TripId AND DocType = 'POD';

    INSERT INTO dbo.Invoices
        (InvoiceNumber, BookingId, CustomerId, CustomerGSTIN, PlaceOfSupply, TaxableAmount, CGST, SGST, IGST, TotalAmount)
    VALUES
        (@InvoiceNumber, @BookingId, @CustomerId, @CustomerGstin, @PlaceOfSupply, @TaxableAmount, @Cgst, @Sgst, @Igst, @TotalAmount);

    COMMIT TRANSACTION;
END
GO
