namespace MediaService.BLL.Options;

public sealed class MediaStorageOptions
{
    public string RootPath { get; set; } = "storage/media";
    public string PublicBaseUrl { get; set; } = "";
    public long MaxImageBytes { get; set; } = 10 * 1024 * 1024;
    public string[] AllowedImageContentTypes { get; set; } = ["image/jpeg", "image/png", "image/webp"];
}
