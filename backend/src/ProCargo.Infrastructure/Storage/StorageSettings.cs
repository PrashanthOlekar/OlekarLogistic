namespace ProCargo.Infrastructure.Storage;

/// <summary>The "Storage" section of appsettings.</summary>
public sealed class StorageSettings
{
    public const string SectionName = "Storage";

    /// <summary>Private folder for uploads, relative to the app folder unless absolute.</summary>
    public string LocalFolder { get; set; } = "App_Data/uploads";

    /// <summary>Where the Data Protection keys are kept, relative to the app folder unless absolute.</summary>
    public string KeysFolder { get; set; } = "App_Data/keys";
}
