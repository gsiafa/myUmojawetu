using System.Security.Cryptography;

namespace WebOptimus.Custom_Validation
{
    public static class FileHelper
    {
        public static async Task<string> ComputeSha256HashAsync(IFormFile file, CancellationToken ct)
        {
            using var sha256 = SHA256.Create();
            using var stream = file.OpenReadStream();
            var hashBytes = await sha256.ComputeHashAsync(stream, ct);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
    }

}
