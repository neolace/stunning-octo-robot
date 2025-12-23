using Microsoft.AspNetCore.Mvc;

namespace PersonalAIAgent.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AgentController : ControllerBase
    {
        private readonly GraphService _graph;

        public AgentController(GraphService graph) => _graph = graph;

        [HttpPost("send-email")]
        public async Task<IActionResult> SendEmail(string to, string subject, string body)
        {
            await _graph.SendEmailAsync(to, subject, body);
            return Ok("Email sent.");
        }

        [HttpPost("upload-file")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            await _graph.UploadFileAsync($"/Uploads/{file.FileName}", stream);
            return Ok("File uploaded.");
        }
    }
}
