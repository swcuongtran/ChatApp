using FileService.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FileService.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FilesController : ControllerBase
    {
        private readonly ISender _sender;

        public FilesController(ISender sender) => _sender = sender;

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file, [FromForm] string? conversationId)
        {
            if (file == null || file.Length == 0) return BadRequest();

            using var stream = file.OpenReadStream();
            var command = new UploadFileCommand(
                FileStream: stream,
                FileName: file.FileName,
                ContentType: file.ContentType,
                Size: file.Length,
                UserId: User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                ConversationId: conversationId
            );

            var result = await _sender.Send(command);
            return Ok(result);
        }
    }
}
