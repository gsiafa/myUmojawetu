using WebOptimus.Data;
using WebOptimus.Models;
using WebOptimus.Services;

public class FileUploadService : IFileUploadService
{
    private readonly IWebHostEnvironment _env;
    private readonly IAuditService _auditService;

    public FileUploadService(IWebHostEnvironment env, IAuditService auditService)
    {
        _env = env;
        _auditService = auditService;
    }

    public async Task<string> UploadFileAsync(IFormFile file, string folderName, string[] allowedExtensions, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("No file uploaded");

        var ext = Path.GetExtension(file.FileName);
        if (!allowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("File type not allowed");

        var folderPath = Path.Combine(_env.WebRootPath, folderName);
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        var uniqueFileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(folderPath, uniqueFileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream, ct);

        await _auditService.RecordAuditAsync("Upload", "File", $"Uploaded {uniqueFileName} to /{folderName}", ct);

        return $"/{folderName}/{uniqueFileName}";
    }
}

