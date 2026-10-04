namespace ProCargo.Application.Features.Documents;

/// <summary>An open file ready to send to the browser. The caller disposes the stream.</summary>
public sealed record DocumentFile(Stream Content, string ContentType, string FileName);
