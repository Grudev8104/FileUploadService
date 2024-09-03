using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using System.Xml;

namespace FileUploadServiceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileUploadController : ControllerBase
    {
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File not uploaded");
            }

            string tempFilePath = null;
            try
            {
                // Съхранение на файла временно
                tempFilePath = Path.GetTempFileName();
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Конвертиране на XML в JSON
                var jsonContent = ConvertXmlToJson(tempFilePath);

                // Изпращане на JSON към ProcessedFilesServiceAPI
                var isSuccess = await SendJsonToProcessedFilesService(file.FileName, jsonContent);

                if (!isSuccess)
                {
                    return StatusCode(500, "Error occurred while sending JSON to the storage service.");
                }

                return Ok(new { message = "File processed and sent successfully" });
            }
            catch (XmlException ex)
            {
                // Грешка при конвертиране на XML в JSON
                return StatusCode(500, $"Error processing XML file: {ex.Message}");
            }
            catch (HttpRequestException ex)
            {
                // Грешка при изпращане на JSON към ProcessedFilesServiceAPI
                return StatusCode(500, $"Error communicating with the storage service: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Общи грешки
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
            finally
            {
                // Уверяваме се, че временният файл е изтрит
                if (tempFilePath != null && System.IO.File.Exists(tempFilePath))
                {
                    System.IO.File.Delete(tempFilePath);
                }
            }
        }

        private string ConvertXmlToJson(string filePath)
        {
            var doc = new XmlDocument();
            doc.Load(filePath);
            return JsonConvert.SerializeXmlNode(doc);
        }

        private async Task<bool> SendJsonToProcessedFilesService(string fileName, string jsonData)
        {
            using (var httpClient = new HttpClient())
            {
                var content = new StringContent(JsonConvert.SerializeObject(new { FileName = fileName, FileContent = jsonData }), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync("https://localhost:{ProcessedFilesServicePort}/api/ProcessedFiles", content);
                return response.IsSuccessStatusCode;
            }
        }
    }
}
