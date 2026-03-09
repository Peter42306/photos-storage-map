using System.ComponentModel.DataAnnotations;

namespace PhotosStorageMap.Application.DTOs
{
    public sealed record UpdatePhotoDescriptionRequest(        
        string? Description         
    );
}
