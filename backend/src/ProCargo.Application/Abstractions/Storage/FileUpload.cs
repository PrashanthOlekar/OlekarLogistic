namespace ProCargo.Application.Abstractions.Storage;

/// <summary>A file sent by the browser, independent of ASP.NET's IFormFile.</summary>
public sealed record FileUpload(Stream Content, string FileName, string ContentType, long Length);
