using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ContosoDashboard.Services;

namespace ContosoDashboard.Controllers;

[ApiController]
[Authorize]
[Route("api/documents")]
public sealed class DocumentController : ControllerBase
{
    private readonly DocumentService _documents;
    private readonly DocumentAuditService _audit;

    public DocumentController(DocumentService documents, DocumentAuditService audit)
    {
        _documents = documents;
        _audit = audit;
    }

    [HttpPost]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] DocumentUploadRequest request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();
        if (request.Files is null || request.Files.Count == 0)
            return BadRequest(new { error = "At least one file is required." });

        var results = new List<DocumentUploadResult>();
        foreach (var file in request.Files)
        {
            results.Add(await _documents.UploadAsync(file, request.Title ?? Path.GetFileNameWithoutExtension(file.FileName), request.Description, request.Category ?? "Other", request.Tags, request.ProjectId, userId, cancellationToken));
        }
        return Accepted(results);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? query, [FromQuery] string? category, [FromQuery] int? projectId)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        return Ok(await _documents.GetAuthorizedAsync(userId, new DocumentSearchFilters(query, category, projectId)));
    }

    [HttpGet("{documentId:int}/content")]
    public async Task<IActionResult> Content(int documentId)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        var document = await _documents.GetByIdAsync(documentId);
        if (document is null || !await _documents.CanReadAsync(document, userId)) return NotFound();
        var stream = await _documents.OpenReadAsync(document, userId);
        if (stream is not null) await _audit.RecordAsync(userId, documentId, "Download", "Succeeded");
        return stream is null ? NotFound() : File(stream, document.FileType, document.FileName, enableRangeProcessing: true);
    }

    [HttpDelete("{documentId:int}")]
    public async Task<IActionResult> Delete(int documentId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        return await _documents.DeleteAsync(documentId, userId, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpPut("{documentId:int}/metadata")]
    public async Task<IActionResult> UpdateMetadata(int documentId, [FromBody] DocumentMetadataRequest request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        return await _documents.UpdateMetadataAsync(documentId, userId, request.Title, request.Description, request.Category, request.Tags, cancellationToken) ? NoContent() : NotFound();
    }

    [HttpPost("{documentId:int}/replacement")]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<IActionResult> Replace(int documentId, IFormFile file, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        return await _documents.ReplaceAsync(documentId, file, userId, cancellationToken) ? Accepted() : BadRequest(new { error = "Replacement was rejected or unauthorized." });
    }

    [HttpPost("{documentId:int}/shares")]
    public async Task<IActionResult> Share(int documentId, [FromBody] DocumentShareRequest request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        var result = await _documents.ShareAsync(documentId, request.RecipientUserId, userId, cancellationToken);
        return result.Succeeded ? Ok(result) : Forbid();
    }
}

public sealed class DocumentUploadRequest
{
    public List<IFormFile> Files { get; set; } = new();
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Tags { get; set; }
    public int? ProjectId { get; set; }
}

public sealed record DocumentMetadataRequest(string Title, string? Description, string Category, string? Tags);
public sealed record DocumentShareRequest(int RecipientUserId);