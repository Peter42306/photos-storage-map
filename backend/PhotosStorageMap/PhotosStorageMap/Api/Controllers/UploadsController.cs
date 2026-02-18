using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PhotosStorageMap.Application.Interfaces;

namespace PhotosStorageMap.Api.Controllers
{
    [Route("api/uploads")]
    [ApiController]
    [Authorize]
    public sealed class UploadsController : ControllerBase
    {
        private readonly IFileStorage _fileStorage;
        
    }
}
