using System.Threading;
using System.Threading.Tasks;

namespace WebOptimus.Services
{

    public interface IFileUploadService
    {
        Task<string> UploadFileAsync(IFormFile file, string folderName, string[] allowedExtensions, CancellationToken ct);
    }
}
