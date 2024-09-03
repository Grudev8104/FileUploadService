using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Models;

namespace ProcessedFilesServiceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProcessedFilesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly string _apiKey;


        public ProcessedFilesController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _apiKey = configuration["ProcessedFilesService:ApiKey"];
        }


        [HttpPost("ReceiveProcessedFile")]
        public async Task<IActionResult> ReceiveProcessedFile([FromBody] ProcessedFile processedFile, [FromHeader(Name = "ApiKey")] string apiKey)
        {
            if (apiKey != _apiKey)
            {
                return Unauthorized("Invalid API Key");
            }
            if (processedFile == null)
            {
                return BadRequest("Invalid file data");
            }

            try
            {
                _context.ProcessedFiles.Add(processedFile);
                await _context.SaveChangesAsync();
                return Ok(new { message = "File stored successfully", fileId = processedFile.Id });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, $"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }


        // Get all processed files
        [HttpGet]
        public async Task<IActionResult> GetProcessedFiles()
        {
            try
            {
                var files = await _context.ProcessedFiles.ToListAsync();
                return Ok(files);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving files: {ex.Message}");
            }
        }

        // Get a specific processed file by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProcessedFile(int id)
        {
            try
            {
                var file = await _context.ProcessedFiles.FindAsync(id);
                if (file == null)
                {
                    return NotFound();
                }
                return Ok(file);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving the file: {ex.Message}");
            }
        }

        // Download a specific processed file by ID
        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadProcessedFile(int id)
        {
            try
            {
                var file = await _context.ProcessedFiles.FindAsync(id);
                if (file == null)
                {
                    return NotFound();
                }

                var byteArray = System.Text.Encoding.UTF8.GetBytes(file.FileContent);
                var stream = new MemoryStream(byteArray);
                return new FileStreamResult(stream, "application/json")
                {
                    FileDownloadName = $"{file.FileName}.json"
                };
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while downloading the file: {ex.Message}");
            }
        }

        // Delete a specific processed file by ID
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProcessedFile(int id)
        {
            try
            {
                var file = await _context.ProcessedFiles.FindAsync(id);
                if (file == null)
                {
                    return NotFound();
                }

                _context.ProcessedFiles.Remove(file);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                // Грешка при изтриване от базата данни
                return StatusCode(500, $"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Общи грешки
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }
    }
}
