namespace ProCargo.Api.Data.Entities;

/// <summary>
/// A type of goods customers can send. Table: GoodsCategories.
/// </summary>
public class GoodsCategory
{
    public int GoodsCategoryId { get; set; }

    public string Name { get; set; } = "";

    public bool RequiresEwayBill { get; set; } = true;

    public bool IsActive { get; set; } = true;
}
