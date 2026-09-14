using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public sealed class DocumentValidationService
{
    public string? ValidateMetadata(string title, string category)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 255) return "A title between 1 and 255 characters is required.";
        if (!DocumentCategories.All.Contains(category, StringComparer.Ordinal)) return "Select an approved category.";
        return null;
    }
    private readonly IConfiguration _configuration;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".jpg", ".jpeg", ".png"
    };

    public DocumentValidationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string? Validate(IFormFile file, string category)
    {
        var maxSize = _configuration.GetValue<long>("Documents:MaxFileSizeBytes", 25 * 1024 * 1024);
        if (file.Length <= 0 || file.Length > maxSize) return "The file exceeds the 25 MB limit or is empty.";
        if (!AllowedExtensions.Contains(Path.GetExtension(file.FileName))) return "This file type is not supported.";
        if (!DocumentCategories.All.Contains(category, StringComparer.Ordinal)) return "This document category is not supported.";
        return null;
    }
}
