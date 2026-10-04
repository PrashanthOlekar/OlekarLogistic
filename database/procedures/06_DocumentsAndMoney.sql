/* =============================================================================
   ProCargo stored procedures — 6. Documents, owner payouts and the admin dashboard
   ============================================================================= */

USE ProCargo;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO


/* ---------------------------------------------------------------- documents */

CREATE OR ALTER PROCEDURE dbo.usp_Document_Create
    @EntityType     VARCHAR(20),
    @EntityId       BIGINT,
    @DocType        VARCHAR(30),
    @BlobPath       NVARCHAR(400),
    @FileName       NVARCHAR(200),
    @ContentType    VARCHAR(100),
    @SizeBytes      BIGINT,
    @Sha256         CHAR(64)     = NULL,
    @DocumentNumber NVARCHAR(60) = NULL,
    @ExpiryDate     DATE         = NULL,
    @UploadedBy     BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Documents
        (EntityType, EntityId, DocType, BlobPath, FileName, ContentType, SizeBytes, Sha256, DocumentNumber, ExpiryDate, Status, UploadedBy)
    VALUES
        (@EntityType, @EntityId, @DocType, @BlobPath, @FileName, @ContentType, @SizeBytes, @Sha256, @DocumentNumber, @ExpiryDate, 'Pending', @UploadedBy);

    SELECT CAST(SCOPE_IDENTITY() AS BIGINT) AS DocumentId;
END
GO


CREATE OR ALTER PROCEDURE dbo.usp_Document_GetById
    @DocumentId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DocumentId, EntityType, EntityId, DocType, BlobPath, FileName, ContentType, SizeBytes,
           DocumentNumber, ExpiryDate, Status, RejectionReason, UploadedBy, UploadedAt
    FROM dbo.Documents
    WHERE DocumentId = @DocumentId;
END
GO


-- Documents list. A user passes their own @EntityType/@EntityId; the admin filters by status
-- and leaves trip photos out with @ExcludeTrips = 1.
CREATE OR ALTER PROCEDURE dbo.usp_Document_GetPaged
    @EntityType   VARCHAR(20)   = NULL,
    @EntityId     BIGINT        = NULL,
    @Status       VARCHAR(20)   = NULL,
    @ExcludeTrips BIT           = 0,
    @Search       NVARCHAR(100) = NULL,
    @PageNumber   INT           = 1,
    @PageSize     INT           = 20
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Pattern NVARCHAR(110) =
        CASE WHEN @Search IS NULL OR LTRIM(@Search) = N'' THEN NULL
             ELSE N'%' + REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(@Search)), N'[', N'[[]'), N'%', N'[%]'), N'_', N'[_]') + N'%'
        END;

    SELECT
        DocumentId AS Id, EntityType, EntityId, DocType, FileName, DocumentNumber, ExpiryDate,
        Status, RejectionReason, UploadedAt
    FROM dbo.Documents
    WHERE (@EntityType IS NULL OR EntityType = @EntityType)
      AND (@EntityId IS NULL OR EntityId = @EntityId)
      AND (@Status IS NULL OR Status = @Status)
      AND (@ExcludeTrips = 0 OR EntityType <> 'Trip')
      AND (@Pattern IS NULL OR FileName LIKE @Pattern OR DocType LIKE @Pattern OR DocumentNumber LIKE @Pattern)
    ORDER BY UploadedAt DESC, DocumentId DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;

    SELECT COUNT_BIG(*) AS TotalRecords
    FROM dbo.Documents
    WHERE (@EntityType IS NULL OR EntityType = @EntityType)
      AND (@EntityId IS NULL OR EntityId = @EntityId)
      AND (@Status IS NULL OR Status = @Status)
      AND (@ExcludeTrips = 0 OR EntityType <> 'Trip')
      AND (@Pattern IS NULL OR FileName LIKE @Pattern OR DocType LIKE @Pattern OR DocumentNumber LIKE @Pattern);
END
GO


CREATE OR ALTER PROCEDURE dbo.usp_Document_Review
    @DocumentId BIGINT,
    @Status     VARCHAR(20),
    @Reason     NVARCHAR(500) = NULL,
    @ReviewedBy BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Documents
    SET Status = @Status,
        RejectionReason = CASE WHEN @Status = 'Rejected' THEN @Reason ELSE NULL END,
        ReviewedBy = @ReviewedBy,
        ReviewedAt = SYSUTCDATETIME()
    WHERE DocumentId = @DocumentId;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO


/* ---------------------------------------------------------------- owner payouts */

CREATE OR ALTER PROCEDURE dbo.usp_Settlement_GetById
    @SettlementId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT SettlementId, TripId, OwnerId, GrossAmount, CommissionAmount, TdsAmount, NetAmount, Status, UTR AS Utr, ReleasedAt, CreatedAt
    FROM dbo.Settlements
    WHERE SettlementId = @SettlementId;
END
GO


-- Payouts for an owner (@OwnerId) or the admin (all).
CREATE OR ALTER PROCEDURE dbo.usp_Settlement_GetPaged
    @OwnerId    BIGINT        = NULL,
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
        settlement.SettlementId AS Id,
        trip.TripNumber AS Trip,
        ISNULL(owner.BusinessName, ownerUser.FullName) AS Owner,
        account.IFSC + N' ••' + account.AccountLast4 AS Bank,
        settlement.GrossAmount,
        settlement.CommissionAmount,
        settlement.TdsAmount,
        settlement.NetAmount,
        settlement.Status,
        settlement.UTR AS Utr,
        settlement.ReleasedAt,
        settlement.CreatedAt
    FROM dbo.Settlements AS settlement
    JOIN dbo.Trips AS trip                    ON trip.TripId = settlement.TripId
    JOIN dbo.Owners AS owner                  ON owner.OwnerId = settlement.OwnerId
    JOIN dbo.Users AS ownerUser               ON ownerUser.UserId = owner.UserId
    LEFT JOIN dbo.OwnerBankAccounts AS account ON account.OwnerBankAccountId = settlement.OwnerBankAccountId
    WHERE (@OwnerId IS NULL OR settlement.OwnerId = @OwnerId)
      AND (@Status IS NULL OR settlement.Status = @Status)
      AND (@Pattern IS NULL
           OR trip.TripNumber LIKE @Pattern
           OR owner.BusinessName LIKE @Pattern
           OR ownerUser.FullName LIKE @Pattern
           OR settlement.UTR LIKE @Pattern)
    ORDER BY settlement.CreatedAt DESC, settlement.SettlementId DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;

    SELECT COUNT_BIG(*) AS TotalRecords
    FROM dbo.Settlements AS settlement
    JOIN dbo.Trips AS trip      ON trip.TripId = settlement.TripId
    JOIN dbo.Owners AS owner    ON owner.OwnerId = settlement.OwnerId
    JOIN dbo.Users AS ownerUser ON ownerUser.UserId = owner.UserId
    WHERE (@OwnerId IS NULL OR settlement.OwnerId = @OwnerId)
      AND (@Status IS NULL OR settlement.Status = @Status)
      AND (@Pattern IS NULL
           OR trip.TripNumber LIKE @Pattern
           OR owner.BusinessName LIKE @Pattern
           OR ownerUser.FullName LIKE @Pattern
           OR settlement.UTR LIKE @Pattern);
END
GO


-- Records a payout already sent from the bank. @Utr is the bank's transfer reference.
CREATE OR ALTER PROCEDURE dbo.usp_Settlement_RecordPayout
    @SettlementId BIGINT,
    @Utr          VARCHAR(30),
    @ReleasedBy   BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Settlements
    SET Status = 'Released',
        UTR = @Utr,
        ReleasedBy = @ReleasedBy,
        ReleasedAt = SYSUTCDATETIME()
    WHERE SettlementId = @SettlementId
      AND Status = 'Approved';

    IF @@ROWCOUNT = 0
        THROW 50409, 'Approve the trip''s POD before releasing the payout.', 1;
END
GO


/* ---------------------------------------------------------------- admin dashboard */

-- Counters, work queues and chart data for the admin overview, in five result sets:
--   1 counters  2 bookings per Indian day (last 7)  3 top routes  4 top pickup cities  5 fleet availability
-- Times are UTC; the API passes the UTC start of "today" and "this month" in Indian time.
CREATE OR ALTER PROCEDURE dbo.usp_Dashboard_GetAdmin
    @TodayStartAt  DATETIME2(0),
    @MonthStartAt  DATETIME2(0),
    @WeekStartAt   DATETIME2(0),
    @RoutesSinceAt DATETIME2(0)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        (SELECT COUNT(*) FROM dbo.Bookings WHERE CreatedAt >= @TodayStartAt) AS BookingsToday,
        (SELECT COUNT(*) FROM dbo.Trips WHERE Status NOT IN ('Delivered', 'Completed', 'Cancelled')) AS ActiveTrips,
        (SELECT COUNT(*) FROM dbo.Trips WHERE Status = 'Completed' AND PodApprovedAt >= @MonthStartAt) AS CompletedThisMonth,
        (SELECT COUNT(*) FROM dbo.Bookings WHERE Status = 'Quoted') AS AwaitingPayment,
        (SELECT COUNT(*) FROM dbo.Bookings WHERE Status = 'Confirmed') AS AwaitingTruck,
        ISNULL((SELECT SUM(Amount) FROM dbo.Payments WHERE Status = 'Captured' AND PaidAt >= @MonthStartAt), 0) AS RevenueThisMonth,
        ISNULL((SELECT SUM(CommissionAmount) FROM dbo.Settlements WHERE CreatedAt >= @MonthStartAt AND Status <> 'OnHold'), 0) AS CommissionThisMonth,
        (SELECT COUNT(*) FROM dbo.Owners WHERE KycStatus = 'Pending')
          + (SELECT COUNT(*) FROM dbo.Drivers WHERE KycStatus = 'Pending')
          + (SELECT COUNT(*) FROM dbo.Vehicles WHERE VerificationStatus = 'Pending') AS PendingApprovals,
        (SELECT COUNT(*) FROM dbo.Documents WHERE Status = 'Pending' AND EntityType <> 'Trip') AS PendingDocuments,
        (SELECT COUNT(*) FROM dbo.Trips WHERE Status = 'Delivered') AS PodToApprove,
        (SELECT COUNT(*) FROM dbo.Settlements WHERE Status = 'Approved') AS SettlementsToRelease;

    -- India is UTC+5:30, so add 330 minutes to get the Indian calendar date.
    SELECT CAST(DATEADD(MINUTE, 330, CreatedAt) AS DATE) AS [Date], COUNT(*) AS [Count]
    FROM dbo.Bookings
    WHERE CreatedAt >= @WeekStartAt
    GROUP BY CAST(DATEADD(MINUTE, 330, CreatedAt) AS DATE);

    SELECT TOP (6) pickupCity.Name + N' → ' + dropCity.Name AS [Route], COUNT(*) AS [Count]
    FROM dbo.Bookings AS booking
    JOIN dbo.Cities AS pickupCity ON pickupCity.CityId = booking.PickupCityId
    JOIN dbo.Cities AS dropCity   ON dropCity.CityId = booking.DropCityId
    WHERE booking.Status <> 'Cancelled' AND booking.CreatedAt >= @RoutesSinceAt
    GROUP BY pickupCity.Name, dropCity.Name
    ORDER BY COUNT(*) DESC;

    SELECT TOP (7) pickupCity.Name AS City, COUNT(*) AS [Count]
    FROM dbo.Bookings AS booking
    JOIN dbo.Cities AS pickupCity ON pickupCity.CityId = booking.PickupCityId
    WHERE booking.Status <> 'Cancelled'
    GROUP BY pickupCity.Name
    ORDER BY COUNT(*) DESC;

    SELECT AvailabilityStatus AS Status, COUNT(*) AS [Count]
    FROM dbo.Vehicles
    WHERE VerificationStatus = 'Approved'
    GROUP BY AvailabilityStatus;
END
GO
