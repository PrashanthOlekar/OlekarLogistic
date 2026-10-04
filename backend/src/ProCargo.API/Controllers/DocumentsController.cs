using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProCargo.API.Configuration;
using ProCargo.Application.Abstractions.Storage;
using ProCargo.Application.Common.Paging;
using ProCargo.Application.Features.Common;
using ProCargo.Application.Features.Documents;
using ProCargo.Domain.Constants;

namespace ProCargo.API.Controllers;

/// <summary>KYC and vehicle documents: upload, list, open and (admins) review.</summary>
[Route(ApiRoutes.Base + "/documents")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public sealed class DocumentsController(IDocumentService documents) : ApiControllerBase
{
    private const long MaxUploadBytes = FileUploadRules.MaxFileBytes + (1024 * 1024);

    /// <summary>
    /// Upload a document for my own profile, or for one of my vehicles
    /// (multipart form: entityType, entityId, docType, documentNumber, expiryDate, file). PDF or photo, up to 10 MB.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Policies.AccountHolder)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadBytes)]
    [ProducesResponseType(typeof(CreatedResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreatedResource>> UploadDocument(
        [FromForm] UploadDocumentRequest request,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        await using Stream? content = file?.OpenReadStream();
        FileUpload? upload = file is null ? null : new FileUpload(content!, file.FileName, file.ContentType, file.Length);

        CreatedResource created = await documents.UploadAsync(request, upload, cancellationToken);
        return CreatedAtAction(nameof(DownloadFile), new { id = created.Id }, created);
    }

    /// <summary>Documents, newest first. Users see the ones on their own profile; admins see all except trip photos.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<DocumentListItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DocumentListItem>>> GetDocuments([FromQuery] DocumentQuery query, CancellationToken cancellationToken) =>
        Ok(await documents.GetPagedAsync(query, cancellationToken));

    /// <summary>Open the file. Only the person who uploaded it and admins may.</summary>
    [HttpGet("{id:long}/file")]
    [Produces("application/octet-stream")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadFile(long id, CancellationToken cancellationToken)
    {
        DocumentFile file = await documents.GetFileAsync(id, cancellationToken);
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>Admin: verify or reject a document (a reason is required to reject).</summary>
    [HttpPut("{id:long}/review")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReviewDocument(long id, VerificationRequest request, CancellationToken cancellationToken)
    {
        await documents.ReviewAsync(id, request, cancellationToken);
        return NoContent();
    }
}
