/* =============================================================================
   ProCargo stored procedures — 3. Owners, drivers and vehicles
   Profiles, KYC decisions, the fleet, and the admin approvals queue.
   ============================================================================= */

USE ProCargo;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO


/* ---------------------------------------------------------------- customers */

CREATE OR ALTER PROCEDURE dbo.usp_Customer_GetByUserId
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CustomerId, UserId, CompanyName, GSTIN AS Gstin, CreatedAt
    FROM dbo.Customers
    WHERE UserId = @UserId;
END
GO


/* ---------------------------------------------------------------- owners */

CREATE OR ALTER PROCEDURE dbo.usp_Owner_GetById
    @OwnerId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT OwnerId, UserId, BusinessName, PanLast4, AadhaarLast4, KycStatus, RejectionReason, CreatedAt
    FROM dbo.Owners
    WHERE OwnerId = @OwnerId;
END
GO


CREATE OR ALTER PROCEDURE dbo.usp_Owner_GetByUserId
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT OwnerId, UserId, BusinessName, PanLast4, AadhaarLast4, KycStatus, RejectionReason, CreatedAt
    FROM dbo.Owners
    WHERE UserId = @UserId;
END
GO


-- A driver links to their owner by the owner's mobile number.
CREATE OR ALTER PROCEDURE dbo.usp_Owner_GetByMobile
    @Mobile VARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT owner.OwnerId, owner.UserId, owner.BusinessName, owner.PanLast4, owner.AadhaarLast4,
           owner.KycStatus, owner.RejectionReason, owner.CreatedAt
    FROM dbo.Owners AS owner
    JOIN dbo.Users  AS ownerUser ON ownerUser.UserId = owner.UserId
    WHERE ownerUser.Mobile = @Mobile;
END
GO


-- Approve or reject an owner's KYC. Approval also activates the account.
CREATE OR ALTER PROCEDURE dbo.usp_Owner_SetVerification
    @OwnerId     BIGINT,
    @Approve     BIT,
    @Reason      NVARCHAR(500) = NULL,
    @AdminUserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @UserId BIGINT;

    UPDATE dbo.Owners
    SET KycStatus       = CASE WHEN @Approve = 1 THEN 'Approved' ELSE 'Rejected' END,
        RejectionReason = CASE WHEN @Approve = 1 THEN NULL ELSE @Reason END,
        VerifiedBy      = @AdminUserId,
        VerifiedAt      = SYSUTCDATETIME(),
        UpdatedAt       = SYSUTCDATETIME(),
        @UserId         = UserId
    WHERE OwnerId = @OwnerId;

    DECLARE @RowsAffected INT = @@ROWCOUNT;

    IF @Approve = 1 AND @UserId IS NOT NULL
    BEGIN
        UPDATE dbo.Users
        SET Status = 'Active', UpdatedAt = SYSUTCDATETIME()
        WHERE UserId = @UserId AND Status = 'PendingKyc';
    END

    COMMIT TRANSACTION;
    SELECT @RowsAffected AS RowsAffected;
END
GO


-- Totals for the owner's overview page, then the primary bank account.
CREATE OR ALTER PROCEDURE dbo.usp_Dashboard_GetOwner
    @OwnerId      BIGINT,
    @MonthStartAt DATETIME2(0)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        owner.OwnerId,
        owner.BusinessName,
        owner.KycStatus,
        owner.RejectionReason,
        (SELECT COUNT(*) FROM dbo.Vehicles WHERE OwnerId = @OwnerId) AS Vehicles,
        (SELECT COUNT(*) FROM dbo.Vehicles WHERE OwnerId = @OwnerId AND VerificationStatus = 'Approved') AS VehiclesApproved,
        (SELECT COUNT(*) FROM dbo.Drivers WHERE OwnerId = @OwnerId) AS Drivers,
        (SELECT COUNT(*) FROM dbo.Trips WHERE OwnerId = @OwnerId AND Status NOT IN ('Completed', 'Cancelled')) AS ActiveTrips,
        ISNULL((SELECT SUM(NetAmount) FROM dbo.Settlements
                WHERE OwnerId = @OwnerId AND Status = 'Released' AND ReleasedAt >= @MonthStartAt), 0) AS EarnedThisMonth,
        ISNULL((SELECT SUM(NetAmount) FROM dbo.Settlements
                WHERE OwnerId = @OwnerId AND Status <> 'Released'), 0) AS PendingPayout
    FROM dbo.Owners AS owner
    WHERE owner.OwnerId = @OwnerId;

    SELECT TOP (1) AccountHolder, AccountLast4, IFSC AS Ifsc, BankName, PennyDropStatus
    FROM dbo.OwnerBankAccounts
    WHERE OwnerId = @OwnerId AND IsPrimary = 1 AND IsActive = 1;
END
GO


/* ---------------------------------------------------------------- drivers */

CREATE OR ALTER PROCEDURE dbo.usp_Driver_GetById
    @DriverId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DriverId, UserId, OwnerId, LicenceNumber, LicenceClass, LicenceExpiry, KycStatus, DutyStatus, RejectionReason, Rating
    FROM dbo.Drivers
    WHERE DriverId = @DriverId;
END
GO


CREATE OR ALTER PROCEDURE dbo.usp_Driver_GetByUserId
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DriverId, UserId, OwnerId, LicenceNumber, LicenceClass, LicenceExpiry, KycStatus, DutyStatus, RejectionReason, Rating
    FROM dbo.Drivers
    WHERE UserId = @UserId;
END
GO


CREATE OR ALTER PROCEDURE dbo.usp_Driver_HasActiveTrip
    @DriverId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CAST(CASE WHEN EXISTS (
        SELECT 1 FROM dbo.Trips
        WHERE DriverId = @DriverId AND Status NOT IN ('Delivered', 'Completed', 'Cancelled')
    ) THEN 1 ELSE 0 END AS BIT) AS HasActiveTrip;
END
GO


-- Drivers, for an owner (@OwnerId) or the admin (all). @Sort: Name (default) | Newest
CREATE OR ALTER PROCEDURE dbo.usp_Driver_GetPaged
    @OwnerId    BIGINT        = NULL,
    @KycStatus  VARCHAR(20)   = NULL,
    @DutyStatus VARCHAR(20)   = NULL,
    @Search     NVARCHAR(100) = NULL,
    @Sort       VARCHAR(20)   = 'Name',
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
        driver.DriverId AS Id,
        driverUser.FullName AS Name,
        driverUser.Mobile,
        driver.LicenceNumber,
        driver.LicenceClass,
        driver.LicenceExpiry,
        driver.KycStatus,
        driver.DutyStatus,
        driver.Rating,
        ownerUser.FullName AS OwnerName,
        driver.CreatedAt
    FROM dbo.Drivers AS driver
    JOIN dbo.Users AS driverUser     ON driverUser.UserId = driver.UserId
    LEFT JOIN dbo.Owners AS owner    ON owner.OwnerId = driver.OwnerId
    LEFT JOIN dbo.Users AS ownerUser ON ownerUser.UserId = owner.UserId
    WHERE (@OwnerId IS NULL OR driver.OwnerId = @OwnerId)
      AND (@KycStatus IS NULL OR driver.KycStatus = @KycStatus)
      AND (@DutyStatus IS NULL OR driver.DutyStatus = @DutyStatus)
      AND (@Pattern IS NULL OR driverUser.FullName LIKE @Pattern OR driverUser.Mobile LIKE @Pattern OR driver.LicenceNumber LIKE @Pattern)
    ORDER BY
        CASE WHEN @Sort = 'Newest' THEN driver.CreatedAt END DESC,
        driverUser.FullName,
        driver.DriverId
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;

    SELECT COUNT_BIG(*) AS TotalRecords
    FROM dbo.Drivers AS driver
    JOIN dbo.Users AS driverUser ON driverUser.UserId = driver.UserId
    WHERE (@OwnerId IS NULL OR driver.OwnerId = @OwnerId)
      AND (@KycStatus IS NULL OR driver.KycStatus = @KycStatus)
      AND (@DutyStatus IS NULL OR driver.DutyStatus = @DutyStatus)
      AND (@Pattern IS NULL OR driverUser.FullName LIKE @Pattern OR driverUser.Mobile LIKE @Pattern OR driver.LicenceNumber LIKE @Pattern);
END
GO


-- Approve or reject a driver. Approval puts the driver on duty and activates the account.
CREATE OR ALTER PROCEDURE dbo.usp_Driver_SetVerification
    @DriverId    BIGINT,
    @Approve     BIT,
    @Reason      NVARCHAR(500) = NULL,
    @AdminUserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @UserId BIGINT;

    UPDATE dbo.Drivers
    SET KycStatus       = CASE WHEN @Approve = 1 THEN 'Approved' ELSE 'Rejected' END,
        RejectionReason = CASE WHEN @Approve = 1 THEN NULL ELSE @Reason END,
        DutyStatus      = CASE WHEN @Approve = 1 THEN 'Available' ELSE DutyStatus END,
        VerifiedBy      = @AdminUserId,
        VerifiedAt      = SYSUTCDATETIME(),
        UpdatedAt       = SYSUTCDATETIME(),
        @UserId         = UserId
    WHERE DriverId = @DriverId;

    DECLARE @RowsAffected INT = @@ROWCOUNT;

    IF @Approve = 1 AND @UserId IS NOT NULL
    BEGIN
        UPDATE dbo.Users
        SET Status = 'Active', UpdatedAt = SYSUTCDATETIME()
        WHERE UserId = @UserId AND Status = 'PendingKyc';
    END

    COMMIT TRANSACTION;
    SELECT @RowsAffected AS RowsAffected;
END
GO


/* ---------------------------------------------------------------- vehicles */

CREATE OR ALTER PROCEDURE dbo.usp_Vehicle_GetById
    @VehicleId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT VehicleId, OwnerId, VehicleTypeId, RegistrationNumber, CapacityKg, MakeModel, ManufactureYear,
           AvailabilityStatus, VerificationStatus, CurrentDriverId, HomeCityId, CreatedAt
    FROM dbo.Vehicles
    WHERE VehicleId = @VehicleId;
END
GO


CREATE OR ALTER PROCEDURE dbo.usp_Vehicle_HasActiveTrip
    @VehicleId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CAST(CASE WHEN EXISTS (
        SELECT 1 FROM dbo.Trips
        WHERE VehicleId = @VehicleId AND Status NOT IN ('Delivered', 'Completed', 'Cancelled')
    ) THEN 1 ELSE 0 END AS BIT) AS HasActiveTrip;
END
GO


-- Vehicles, for an owner (@OwnerId) or the admin (all), then the documents of the vehicles on this page.
-- @Sort: Registration (default) | Newest
CREATE OR ALTER PROCEDURE dbo.usp_Vehicle_GetPaged
    @OwnerId            BIGINT        = NULL,
    @VehicleTypeId      INT           = NULL,
    @AvailabilityStatus VARCHAR(20)   = NULL,
    @VerificationStatus VARCHAR(20)   = NULL,
    @Search             NVARCHAR(100) = NULL,
    @Sort               VARCHAR(20)   = 'Registration',
    @PageNumber         INT           = 1,
    @PageSize           INT           = 20
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Pattern NVARCHAR(110) =
        CASE WHEN @Search IS NULL OR LTRIM(@Search) = N'' THEN NULL
             ELSE N'%' + REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(@Search)), N'[', N'[[]'), N'%', N'[%]'), N'_', N'[_]') + N'%'
        END;

    DECLARE @Page TABLE (VehicleId BIGINT PRIMARY KEY, RowNumber INT);

    -- Pick the ids for this page first, so the documents below can be limited to the same vehicles.
    INSERT INTO @Page (VehicleId, RowNumber)
    SELECT ranked.VehicleId, ranked.RowNumber
    FROM
    (
        SELECT vehicle.VehicleId,
               ROW_NUMBER() OVER (ORDER BY
                   CASE WHEN @Sort = 'Newest' THEN vehicle.CreatedAt END DESC,
                   vehicle.RegistrationNumber,
                   vehicle.VehicleId) AS RowNumber
        FROM dbo.Vehicles AS vehicle
        JOIN dbo.Owners AS owner ON owner.OwnerId = vehicle.OwnerId
        JOIN dbo.Users AS ownerUser ON ownerUser.UserId = owner.UserId
        WHERE (@OwnerId IS NULL OR vehicle.OwnerId = @OwnerId)
          AND (@VehicleTypeId IS NULL OR vehicle.VehicleTypeId = @VehicleTypeId)
          AND (@AvailabilityStatus IS NULL OR vehicle.AvailabilityStatus = @AvailabilityStatus)
          AND (@VerificationStatus IS NULL OR vehicle.VerificationStatus = @VerificationStatus)
          AND (@Pattern IS NULL OR vehicle.RegistrationNumber LIKE @Pattern OR vehicle.MakeModel LIKE @Pattern OR ownerUser.FullName LIKE @Pattern)
    ) AS ranked
    ORDER BY ranked.RowNumber
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;

    SELECT
        vehicle.VehicleId AS Id,
        vehicle.RegistrationNumber,
        vehicleType.Name AS VehicleType,
        vehicle.VehicleTypeId,
        vehicle.CapacityKg,
        vehicle.MakeModel,
        vehicle.AvailabilityStatus,
        vehicle.VerificationStatus,
        vehicle.CurrentDriverId,
        driverUser.FullName AS DriverName,
        ownerUser.FullName AS OwnerName,
        vehicle.CreatedAt
    FROM @Page AS page
    JOIN dbo.Vehicles AS vehicle          ON vehicle.VehicleId = page.VehicleId
    JOIN dbo.VehicleTypes AS vehicleType  ON vehicleType.VehicleTypeId = vehicle.VehicleTypeId
    JOIN dbo.Owners AS owner              ON owner.OwnerId = vehicle.OwnerId
    JOIN dbo.Users AS ownerUser           ON ownerUser.UserId = owner.UserId
    LEFT JOIN dbo.Drivers AS driver       ON driver.DriverId = vehicle.CurrentDriverId
    LEFT JOIN dbo.Users AS driverUser     ON driverUser.UserId = driver.UserId
    ORDER BY page.RowNumber;

    SELECT COUNT_BIG(*) AS TotalRecords
    FROM dbo.Vehicles AS vehicle
    JOIN dbo.Owners AS owner ON owner.OwnerId = vehicle.OwnerId
    JOIN dbo.Users AS ownerUser ON ownerUser.UserId = owner.UserId
    WHERE (@OwnerId IS NULL OR vehicle.OwnerId = @OwnerId)
      AND (@VehicleTypeId IS NULL OR vehicle.VehicleTypeId = @VehicleTypeId)
      AND (@AvailabilityStatus IS NULL OR vehicle.AvailabilityStatus = @AvailabilityStatus)
      AND (@VerificationStatus IS NULL OR vehicle.VerificationStatus = @VerificationStatus)
      AND (@Pattern IS NULL OR vehicle.RegistrationNumber LIKE @Pattern OR vehicle.MakeModel LIKE @Pattern OR ownerUser.FullName LIKE @Pattern);

    SELECT document.EntityId AS VehicleId, document.DocumentId AS Id, document.DocType, document.Status, document.ExpiryDate
    FROM dbo.Documents AS document
    JOIN @Page AS page ON page.VehicleId = document.EntityId
    WHERE document.EntityType = 'Vehicle'
    ORDER BY document.UploadedAt;
END
GO


-- Adds a lorry. A duplicate registration number fails on UX_Vehicles_RegistrationNumber (HTTP 409).
CREATE OR ALTER PROCEDURE dbo.usp_Vehicle_Create
    @OwnerId            BIGINT,
    @VehicleTypeId      INT,
    @RegistrationNumber VARCHAR(15),
    @CapacityKg         INT,
    @MakeModel          NVARCHAR(100) = NULL,
    @ManufactureYear    SMALLINT      = NULL,
    @HomeCityId         INT           = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Vehicles
        (OwnerId, VehicleTypeId, RegistrationNumber, CapacityKg, MakeModel, ManufactureYear, HomeCityId,
         AvailabilityStatus, VerificationStatus)
    VALUES
        (@OwnerId, @VehicleTypeId, @RegistrationNumber, @CapacityKg, @MakeModel, @ManufactureYear, @HomeCityId,
         'Available', 'Pending');

    SELECT CAST(SCOPE_IDENTITY() AS BIGINT) AS VehicleId;
END
GO


-- Partial update by the owner: availability and/or the regular driver.
-- @UpdateDriver = 1 applies @CurrentDriverId (NULL removes the driver).
CREATE OR ALTER PROCEDURE dbo.usp_Vehicle_Patch
    @VehicleId          BIGINT,
    @AvailabilityStatus VARCHAR(20) = NULL,
    @UpdateDriver       BIT         = 0,
    @CurrentDriverId    BIGINT      = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Vehicles
    SET AvailabilityStatus = ISNULL(@AvailabilityStatus, AvailabilityStatus),
        CurrentDriverId    = CASE WHEN @UpdateDriver = 1 THEN @CurrentDriverId ELSE CurrentDriverId END,
        UpdatedAt          = SYSUTCDATETIME()
    WHERE VehicleId = @VehicleId;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO


CREATE OR ALTER PROCEDURE dbo.usp_Vehicle_SetVerification
    @VehicleId BIGINT,
    @Approve   BIT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Vehicles
    SET VerificationStatus = CASE WHEN @Approve = 1 THEN 'Approved' ELSE 'Rejected' END,
        UpdatedAt = SYSUTCDATETIME()
    WHERE VehicleId = @VehicleId;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO


/* ---------------------------------------------------------------- admin approvals queue */

-- Everything waiting for a KYC decision: owners, drivers, vehicles, then the documents of all of them.
CREATE OR ALTER PROCEDURE dbo.usp_Approval_GetPending
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        owner.OwnerId AS Id,
        ownerUser.FullName AS Name,
        ownerUser.Mobile,
        owner.BusinessName,
        owner.PanLast4,
        owner.AadhaarLast4,
        owner.CreatedAt,
        (SELECT TOP (1) account.IFSC + N' ••' + account.AccountLast4
         FROM dbo.OwnerBankAccounts AS account
         WHERE account.OwnerId = owner.OwnerId AND account.IsPrimary = 1) AS Bank
    FROM dbo.Owners AS owner
    JOIN dbo.Users AS ownerUser ON ownerUser.UserId = owner.UserId
    WHERE owner.KycStatus = 'Pending'
    ORDER BY owner.CreatedAt;

    SELECT
        driver.DriverId AS Id,
        driverUser.FullName AS Name,
        driverUser.Mobile,
        driver.LicenceNumber,
        driver.LicenceClass,
        driver.LicenceExpiry,
        driver.CreatedAt,
        ownerUser.FullName AS Owner
    FROM dbo.Drivers AS driver
    JOIN dbo.Users AS driverUser     ON driverUser.UserId = driver.UserId
    LEFT JOIN dbo.Owners AS owner    ON owner.OwnerId = driver.OwnerId
    LEFT JOIN dbo.Users AS ownerUser ON ownerUser.UserId = owner.UserId
    WHERE driver.KycStatus = 'Pending'
    ORDER BY driver.CreatedAt;

    SELECT
        vehicle.VehicleId AS Id,
        vehicle.RegistrationNumber,
        vehicleType.Name AS VehicleType,
        vehicle.CapacityKg,
        ownerUser.FullName AS Owner,
        vehicle.CreatedAt
    FROM dbo.Vehicles AS vehicle
    JOIN dbo.VehicleTypes AS vehicleType ON vehicleType.VehicleTypeId = vehicle.VehicleTypeId
    JOIN dbo.Owners AS owner             ON owner.OwnerId = vehicle.OwnerId
    JOIN dbo.Users AS ownerUser          ON ownerUser.UserId = owner.UserId
    WHERE vehicle.VerificationStatus = 'Pending'
    ORDER BY vehicle.CreatedAt;

    SELECT document.EntityType, document.EntityId, document.DocumentId AS Id, document.DocType, document.Status, document.ExpiryDate
    FROM dbo.Documents AS document
    WHERE (document.EntityType = 'Owner'   AND document.EntityId IN (SELECT OwnerId FROM dbo.Owners WHERE KycStatus = 'Pending'))
       OR (document.EntityType = 'Driver'  AND document.EntityId IN (SELECT DriverId FROM dbo.Drivers WHERE KycStatus = 'Pending'))
       OR (document.EntityType = 'Vehicle' AND document.EntityId IN (SELECT VehicleId FROM dbo.Vehicles WHERE VerificationStatus = 'Pending'))
    ORDER BY document.UploadedAt;
END
GO
