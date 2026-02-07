namespace PhotosStorageMap.Application.Interfaces
{
    public record FileSaveRequest(
        Stream Content,
        string FileName,
        string ContentType,
        string Folder);
}
