namespace Audivo.Application.Configuration;

/// <summary>
/// Strongly-typed settings for Cloudinary cloud storage, bound from the "Cloudinary" configuration section.
/// </summary>
public sealed class CloudinarySettings
{
    /// <summary>Configuration section name used in appsettings.</summary>
    public const string SectionName = "Cloudinary";

    /// <summary>Cloudinary cloud name.</summary>
    public string CloudName { get; init; } = string.Empty;

    /// <summary>Cloudinary API key.</summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>Cloudinary API secret. Must never be exposed to the client.</summary>
    public string ApiSecret { get; init; } = string.Empty;
}
