namespace ProCargo.Domain.Entities;

/// <summary>A type of goods customers can send (dbo.GoodsCategories).</summary>
public sealed class GoodsCategory
{
    public int GoodsCategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool RequiresEwayBill { get; set; }

    public bool IsActive { get; set; }
}
