/* =============================================================================
   ProCargo stored procedures — 2. Accounts
   Users, sign-in codes (OTP), refresh tokens, registration and user admin.
   Error numbers: 50400 = request breaks a rule (HTTP 400), 50409 = conflict (HTTP 409).
   ============================================================================= */

USE ProCargo;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO


/* ---------------------------------------------------------------- users */

CREATE OR ALTER PROCEDURE dbo.usp_User_GetById
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT UserId, Role, FullName, Mobile, Email, Status, LastLoginAt, CreatedAt
    FROM dbo.Users
    WHERE UserId = @UserId;
END
GO


CREATE OR ALTER PROCEDURE dbo.usp_User_GetByMobile
    @Mobile VARCHAR(15)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT UserId, Role, FullName, Mobile, Email, Status, LastLoginAt, CreatedAt
    FROM dbo.Users
    WHERE Mobile = @Mobile;
END
GO


CREATE OR ALTER PROCEDURE dbo.usp_User_RecordLogin
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Users
    SET LastLoginAt = SYSUTCDATETIME()
    WHERE UserId = @UserId;
END
GO


-- The signed-in user's profile: the user, then the role's detail (only one of sets 2-4 has a row).
CREATE OR ALTER PROCEDURE dbo.usp_User_GetProfile
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT UserId AS Id, Role, FullName, Mobile, Email, Status
    FROM dbo.Users
    WHERE UserId = @UserId;

    SELECT CustomerId, CompanyName, GSTIN AS Gstin
    FROM dbo.Customers
    WHERE UserId = @UserId;

    SELECT OwnerId, BusinessName, KycStatus, RejectionReason, PanLast4, AadhaarLast4
    FROM dbo.Owners
    WHERE UserId = @UserId;

    SELECT
        driver.DriverId,
        driver.KycStatus,
        driver.RejectionReason,
        driver.LicenceNumber,
        driver.LicenceClass,
        driver.LicenceExpiry,
        ownerUser.FullName AS OwnerName
    FROM dbo.Drivers AS driver
    LEFT JOIN dbo.Owners AS owner     ON owner.OwnerId = driver.OwnerId
    LEFT JOIN dbo.Users  AS ownerUser ON ownerUser.UserId = owner.UserId
    WHERE driver.UserId = @UserId;
END
GO


-- Creates the first admin account if there is none yet. Returns 1 when one was created.
CREATE OR ALTER PROCEDURE dbo.usp_User_EnsureAdmin
    @Mobile   VARCHAR(15),
    @FullName NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM dbo.Users WITH (UPDLOCK, HOLDLOCK) WHERE Role = 'Admin')
    BEGIN
        COMMIT TRANSACTION;
        SELECT CAST(0 AS BIT) AS Created;
        RETURN;
    END

    INSERT INTO dbo.Users (Role, FullName, Mobile, Status)
    VALUES ('Admin', @FullName, @Mobile, 'Active');

    COMMIT TRANSACTION;
    SELECT CAST(1 AS BIT) AS Created;
END
GO


-- Admin user search. @Sort: Newest (default) | Name | LastLogin
CREATE OR ALTER PROCEDURE dbo.usp_User_GetPaged
    @Role       VARCHAR(20)   = NULL,
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

    SELECT UserId AS Id, Role, FullName, Mobile, Email, Status, CreatedAt, LastLoginAt
    FROM dbo.Users
    WHERE (@Role IS NULL OR Role = @Role)
      AND (@Status IS NULL OR Status = @Status)
      AND (@Pattern IS NULL OR FullName LIKE @Pattern OR Mobile LIKE @Pattern OR Email LIKE @Pattern)
    ORDER BY
        CASE WHEN @Sort = 'Name' THEN FullName END ASC,
        CASE WHEN @Sort = 'LastLogin' THEN LastLoginAt END DESC,
        CreatedAt DESC,
        UserId DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;

    SELECT COUNT_BIG(*) AS TotalRecords
    FROM dbo.Users
    WHERE (@Role IS NULL OR Role = @Role)
      AND (@Status IS NULL OR Status = @Status)
      AND (@Pattern IS NULL OR FullName LIKE @Pattern OR Mobile LIKE @Pattern OR Email LIKE @Pattern);
END
GO


-- Blocks, unblocks or closes an account. Blocking also signs the user out of every device.
CREATE OR ALTER PROCEDURE dbo.usp_User_SetStatus
    @UserId BIGINT,
    @Status VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    UPDATE dbo.Users
    SET Status = @Status,
        UpdatedAt = SYSUTCDATETIME()
    WHERE UserId = @UserId;

    DECLARE @RowsAffected INT = @@ROWCOUNT;

    IF @Status IN ('Blocked', 'Closed')
    BEGIN
        UPDATE dbo.RefreshTokens
        SET RevokedAt = SYSUTCDATETIME()
        WHERE UserId = @UserId
          AND RevokedAt IS NULL;
    END

    COMMIT TRANSACTION;
    SELECT @RowsAffected AS RowsAffected;
END
GO


/* ---------------------------------------------------------------- one-time sign-in codes */

CREATE OR ALTER PROCEDURE dbo.usp_OtpCode_CountRecent
    @Mobile VARCHAR(15),
    @Since  DATETIME2(0)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) AS RecentCodes
    FROM dbo.OtpCodes
    WHERE Mobile = @Mobile
      AND CreatedAt > @Since;
END
GO


CREATE OR ALTER PROCEDURE dbo.usp_OtpCode_Create
    @Mobile    VARCHAR(15),
    @Purpose   VARCHAR(20),
    @CodeHash  VARCHAR(128),
    @ExpiresAt DATETIME2(0)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.OtpCodes (Mobile, Purpose, CodeHash, ExpiresAt)
    VALUES (@Mobile, @Purpose, @CodeHash, @ExpiresAt);

    SELECT CAST(SCOPE_IDENTITY() AS BIGINT) AS OtpCodeId;
END
GO


-- The newest unused, unexpired code for this number and purpose.
CREATE OR ALTER PROCEDURE dbo.usp_OtpCode_GetLatestActive
    @Mobile  VARCHAR(15),
    @Purpose VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1) OtpCodeId, Mobile, Purpose, CodeHash, Attempts, ExpiresAt, UsedAt, CreatedAt
    FROM dbo.OtpCodes
    WHERE Mobile = @Mobile
      AND Purpose = @Purpose
      AND UsedAt IS NULL
      AND ExpiresAt > SYSUTCDATETIME()
    ORDER BY CreatedAt DESC, OtpCodeId DESC;
END
GO


CREATE OR ALTER PROCEDURE dbo.usp_OtpCode_RecordFailedAttempt
    @OtpCodeId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.OtpCodes
    SET Attempts = CASE WHEN Attempts < 255 THEN Attempts + 1 ELSE Attempts END
    WHERE OtpCodeId = @OtpCodeId;
END
GO


-- Marks a code used. Returns 0 if someone else used it a moment earlier.
CREATE OR ALTER PROCEDURE dbo.usp_OtpCode_MarkUsed
    @OtpCodeId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.OtpCodes
    SET UsedAt = SYSUTCDATETIME()
    WHERE OtpCodeId = @OtpCodeId
      AND UsedAt IS NULL;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO


/* ---------------------------------------------------------------- refresh tokens */

CREATE OR ALTER PROCEDURE dbo.usp_RefreshToken_Create
    @UserId     BIGINT,
    @TokenHash  VARCHAR(128),
    @DeviceInfo NVARCHAR(200) = NULL,
    @ExpiresAt  DATETIME2(0)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.RefreshTokens (UserId, TokenHash, DeviceInfo, ExpiresAt)
    VALUES (@UserId, @TokenHash, @DeviceInfo, @ExpiresAt);
END
GO


CREATE OR ALTER PROCEDURE dbo.usp_RefreshToken_GetByHash
    @TokenHash VARCHAR(128)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT RefreshTokenId, UserId, TokenHash, ExpiresAt, RevokedAt, CreatedAt
    FROM dbo.RefreshTokens
    WHERE TokenHash = @TokenHash;
END
GO


-- Swaps a refresh token for a new one in one step. Returns 0 if the old token was already used or expired.
CREATE OR ALTER PROCEDURE dbo.usp_RefreshToken_Rotate
    @OldTokenHash VARCHAR(128),
    @NewTokenHash VARCHAR(128),
    @DeviceInfo   NVARCHAR(200) = NULL,
    @ExpiresAt    DATETIME2(0)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @UserId BIGINT;

    BEGIN TRANSACTION;

    UPDATE dbo.RefreshTokens
    SET RevokedAt = SYSUTCDATETIME(),
        @UserId = UserId
    WHERE TokenHash = @OldTokenHash
      AND RevokedAt IS NULL
      AND ExpiresAt > SYSUTCDATETIME();

    IF @@ROWCOUNT = 0
    BEGIN
        ROLLBACK TRANSACTION;
        SELECT 0 AS RowsAffected;
        RETURN;
    END

    INSERT INTO dbo.RefreshTokens (UserId, TokenHash, DeviceInfo, ExpiresAt)
    VALUES (@UserId, @NewTokenHash, @DeviceInfo, @ExpiresAt);

    COMMIT TRANSACTION;
    SELECT 1 AS RowsAffected;
END
GO


CREATE OR ALTER PROCEDURE dbo.usp_RefreshToken_Revoke
    @TokenHash VARCHAR(128)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.RefreshTokens
    SET RevokedAt = SYSUTCDATETIME()
    WHERE TokenHash = @TokenHash
      AND RevokedAt IS NULL;
END
GO


CREATE OR ALTER PROCEDURE dbo.usp_RefreshToken_RevokeAllForUser
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.RefreshTokens
    SET RevokedAt = SYSUTCDATETIME()
    WHERE UserId = @UserId
      AND RevokedAt IS NULL;
END
GO


/* ---------------------------------------------------------------- registration */

-- New customer account and profile, together. Returns the new UserId.
CREATE OR ALTER PROCEDURE dbo.usp_Registration_CreateCustomer
    @FullName    NVARCHAR(150),
    @Mobile      VARCHAR(15),
    @Email       NVARCHAR(200) = NULL,
    @CompanyName NVARCHAR(200) = NULL,
    @Gstin       CHAR(15)      = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM dbo.Users WITH (UPDLOCK, HOLDLOCK) WHERE Mobile = @Mobile)
        THROW 50409, 'This number is already registered. Sign in instead.', 1;

    INSERT INTO dbo.Users (Role, FullName, Mobile, Email, Status)
    VALUES ('Customer', @FullName, @Mobile, @Email, 'Active');

    DECLARE @UserId BIGINT = SCOPE_IDENTITY();

    INSERT INTO dbo.Customers (UserId, CompanyName, GSTIN)
    VALUES (@UserId, @CompanyName, @Gstin);

    COMMIT TRANSACTION;
    SELECT @UserId AS UserId;
END
GO


-- New lorry owner: account, KYC profile and bank account, together. PAN and account number arrive encrypted.
CREATE OR ALTER PROCEDURE dbo.usp_Registration_CreateOwner
    @FullName               NVARCHAR(150),
    @Mobile                 VARCHAR(15),
    @Email                  NVARCHAR(200)  = NULL,
    @BusinessName           NVARCHAR(200)  = NULL,
    @PanEncrypted           VARBINARY(256),
    @PanLast4               CHAR(4),
    @AadhaarLast4           CHAR(4),
    @AccountHolder          NVARCHAR(150),
    @AccountNumberEncrypted VARBINARY(256),
    @AccountLast4           CHAR(4),
    @Ifsc                   CHAR(11),
    @BankName               NVARCHAR(100)  = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM dbo.Users WITH (UPDLOCK, HOLDLOCK) WHERE Mobile = @Mobile)
        THROW 50409, 'This number is already registered. Sign in instead.', 1;

    INSERT INTO dbo.Users (Role, FullName, Mobile, Email, Status)
    VALUES ('Owner', @FullName, @Mobile, @Email, 'PendingKyc');

    DECLARE @UserId BIGINT = SCOPE_IDENTITY();

    INSERT INTO dbo.Owners (UserId, BusinessName, PanEncrypted, PanLast4, AadhaarLast4)
    VALUES (@UserId, @BusinessName, @PanEncrypted, @PanLast4, @AadhaarLast4);

    DECLARE @OwnerId BIGINT = SCOPE_IDENTITY();

    INSERT INTO dbo.OwnerBankAccounts (OwnerId, AccountHolder, AccountNumberEncrypted, AccountLast4, IFSC, BankName)
    VALUES (@OwnerId, @AccountHolder, @AccountNumberEncrypted, @AccountLast4, @Ifsc, @BankName);

    COMMIT TRANSACTION;
    SELECT @UserId AS UserId;
END
GO


-- New driver account and profile. @OwnerId links the driver to the owner whose trucks they drive.
CREATE OR ALTER PROCEDURE dbo.usp_Registration_CreateDriver
    @FullName              NVARCHAR(150),
    @Mobile                VARCHAR(15),
    @Email                 NVARCHAR(200) = NULL,
    @OwnerId               BIGINT        = NULL,
    @LicenceNumber         VARCHAR(20),
    @LicenceClass          VARCHAR(20),
    @LicenceExpiry         DATE,
    @AadhaarLast4          CHAR(4)       = NULL,
    @EmergencyContactName  NVARCHAR(150) = NULL,
    @EmergencyContactPhone VARCHAR(15)   = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM dbo.Users WITH (UPDLOCK, HOLDLOCK) WHERE Mobile = @Mobile)
        THROW 50409, 'This number is already registered. Sign in instead.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Drivers WITH (UPDLOCK, HOLDLOCK) WHERE LicenceNumber = @LicenceNumber)
        THROW 50409, 'This licence is already registered.', 1;

    INSERT INTO dbo.Users (Role, FullName, Mobile, Email, Status)
    VALUES ('Driver', @FullName, @Mobile, @Email, 'PendingKyc');

    DECLARE @UserId BIGINT = SCOPE_IDENTITY();

    INSERT INTO dbo.Drivers
        (UserId, OwnerId, LicenceNumber, LicenceClass, LicenceExpiry, AadhaarLast4, EmergencyContactName, EmergencyContactPhone)
    VALUES
        (@UserId, @OwnerId, @LicenceNumber, @LicenceClass, @LicenceExpiry, @AadhaarLast4, @EmergencyContactName, @EmergencyContactPhone);

    COMMIT TRANSACTION;
    SELECT @UserId AS UserId;
END
GO


CREATE OR ALTER PROCEDURE dbo.usp_Driver_LicenceExists
    @LicenceNumber VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.Drivers WHERE LicenceNumber = @LicenceNumber) THEN 1 ELSE 0 END AS BIT) AS LicenceExists;
END
GO


/* ---------------------------------------------------------------- audit trail */

CREATE OR ALTER PROCEDURE dbo.usp_AuditLog_Create
    @ActorUserId BIGINT        = NULL,
    @Action      VARCHAR(60),
    @EntityType  VARCHAR(30),
    @EntityId    BIGINT,
    @NewValues   NVARCHAR(MAX) = NULL,
    @IpAddress   VARCHAR(45)   = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.AuditLogs (ActorUserId, Action, EntityType, EntityId, NewValues, IpAddress)
    VALUES (@ActorUserId, @Action, @EntityType, @EntityId, @NewValues, @IpAddress);
END
GO


CREATE OR ALTER PROCEDURE dbo.usp_AuditLog_GetPaged
    @EntityType VARCHAR(30)   = NULL,
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
        audit.AuditLogId AS Id,
        audit.Action,
        audit.EntityType,
        audit.EntityId,
        audit.ActorUserId,
        actor.FullName AS ActorName,
        audit.NewValues,
        audit.CreatedAt
    FROM dbo.AuditLogs AS audit
    LEFT JOIN dbo.Users AS actor ON actor.UserId = audit.ActorUserId
    WHERE (@EntityType IS NULL OR audit.EntityType = @EntityType)
      AND (@Pattern IS NULL OR audit.Action LIKE @Pattern OR actor.FullName LIKE @Pattern)
    ORDER BY audit.CreatedAt DESC, audit.AuditLogId DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;

    SELECT COUNT_BIG(*) AS TotalRecords
    FROM dbo.AuditLogs AS audit
    LEFT JOIN dbo.Users AS actor ON actor.UserId = audit.ActorUserId
    WHERE (@EntityType IS NULL OR audit.EntityType = @EntityType)
      AND (@Pattern IS NULL OR audit.Action LIKE @Pattern OR actor.FullName LIKE @Pattern);
END
GO
