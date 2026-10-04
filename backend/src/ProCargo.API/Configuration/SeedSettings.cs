namespace ProCargo.API.Configuration;

/// <summary>The "Seed" section: the first admin account, created at start-up when there is none.</summary>
public sealed class SeedSettings
{
    public const string SectionName = "Seed";

    public string AdminMobile { get; set; } = "9999999999";

    public string AdminName { get; set; } = "ProCargo Admin";
}
