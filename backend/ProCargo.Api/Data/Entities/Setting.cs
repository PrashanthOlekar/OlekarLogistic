namespace ProCargo.Api.Data.Entities;

/// <summary>
/// A business setting such as the commission percentage. Table: Settings.
/// </summary>
public class Setting
{
    public string SettingKey { get; set; } = "";

    public string SettingValue { get; set; } = "";

    public string? Description { get; set; }

    public long? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
