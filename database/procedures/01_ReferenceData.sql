/* =============================================================================
   ProCargo stored procedures — 1. Reference data and settings
   Cities, vehicle types, goods categories and the Settings table.
   Safe to run again: every procedure uses CREATE OR ALTER.
   ============================================================================= */

USE ProCargo;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO


-- Lists for the booking and registration forms: three result sets.
CREATE OR ALTER PROCEDURE dbo.usp_ReferenceData_Get
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CityId AS Id, Name, NameKn, State
    FROM dbo.Cities
    WHERE IsServiceable = 1
    ORDER BY Name;

    SELECT
        VehicleTypeId AS Id, Code, Name, BodyType, MaxLoadKg,
        LengthFt, WidthFt, HeightFt, RecommendedGoods
    FROM dbo.VehicleTypes
    WHERE IsActive = 1
    ORDER BY SortOrder;

    SELECT GoodsCategoryId AS Id, Name
    FROM dbo.GoodsCategories
    WHERE IsActive = 1
    ORDER BY GoodsCategoryId;
END
GO


CREATE OR ALTER PROCEDURE dbo.usp_City_GetById
    @CityId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CityId, Name, NameKn, State, Latitude, Longitude, IsServiceable
    FROM dbo.Cities
    WHERE CityId = @CityId;
END
GO


CREATE OR ALTER PROCEDURE dbo.usp_VehicleType_GetById
    @VehicleTypeId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        VehicleTypeId, Code, Name, BodyType, MaxLoadKg, LengthFt, WidthFt, HeightFt,
        RecommendedGoods, RatePerKm, MinFare, DriverBattaPerDay, IsActive, SortOrder
    FROM dbo.VehicleTypes
    WHERE VehicleTypeId = @VehicleTypeId;
END
GO


-- The smallest active vehicle type that can carry @WeightKg (suggested when a load is too heavy).
CREATE OR ALTER PROCEDURE dbo.usp_VehicleType_GetSmallestForWeight
    @WeightKg INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        VehicleTypeId, Code, Name, BodyType, MaxLoadKg, LengthFt, WidthFt, HeightFt,
        RecommendedGoods, RatePerKm, MinFare, DriverBattaPerDay, IsActive, SortOrder
    FROM dbo.VehicleTypes
    WHERE IsActive = 1
      AND MaxLoadKg >= @WeightKg
    ORDER BY MaxLoadKg;
END
GO


CREATE OR ALTER PROCEDURE dbo.usp_GoodsCategory_GetById
    @GoodsCategoryId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT GoodsCategoryId, Name, RequiresEwayBill, IsActive
    FROM dbo.GoodsCategories
    WHERE GoodsCategoryId = @GoodsCategoryId;
END
GO


-- Every business setting (commission %, GST %, OTP rules...).
CREATE OR ALTER PROCEDURE dbo.usp_Setting_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT SettingKey, SettingValue
    FROM dbo.Settings;
END
GO
