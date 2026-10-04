namespace ProCargo.Domain.Constants;

/// <summary>Column lengths from database/ProCargo.sql, used by the validators.</summary>
public static class FieldLimits
{
    public const int FullName = 150;
    public const int Email = 200;
    public const int CompanyName = 200;
    public const int BusinessName = 200;
    public const int Address = 400;
    public const int ContactName = 150;
    public const int GoodsDescription = 400;
    public const int PickupSlot = 20;
    public const int SpecialInstructions = 1000;
    public const int CancelReason = 400;
    public const int RejectionReason = 500;
    public const int MakeModel = 100;
    public const int BankName = 100;
    public const int DocumentNumber = 60;
    public const int Utr = 30;
    public const int MaxVehicleCapacityKg = 60000;
    public const int MaxPageSize = 100;
}
