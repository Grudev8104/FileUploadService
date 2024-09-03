using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Xml;

namespace FileUploadServiceAPI1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileUploadController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public FileUploadController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ProcessedFilesServiceClient");
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file, [FromForm] string fileName)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File not uploaded");
            }

            if (string.IsNullOrEmpty(fileName))
            {
                fileName = Path.GetFileNameWithoutExtension(file.FileName);  // Използвайте оригиналното име на файла, ако не е посочено
            }

            string tempFilePath = null;
            string jsonFilePath = null;
            try
            {
                tempFilePath = await SaveFileToTemporaryLocation(file);

                if (new FileInfo(tempFilePath).Length == 0)
                {
                    return BadRequest("Uploaded file is empty");
                }

                var jsonContent = ConvertXmlToJson(tempFilePath);

                jsonFilePath = await SaveJsonToFile(fileName, jsonContent);  // Използвайте подаденото име на файла

                var isSuccess = await SendProcessedFileToService(fileName, jsonContent);

                if (!isSuccess)
                {
                    return StatusCode(500, "Error occurred while sending JSON to the storage service.");
                }

                return Ok(new { message = "File processed and sent successfully" });
            }
            catch (XmlException ex)
            {
                return BadRequest($"Failed to process XML file: {ex.Message}");
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, $"Error communicating with the storage service: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
            finally
            {
                CleanUpTempFiles(tempFilePath, jsonFilePath);
            }
        }

        private async Task<string> SaveFileToTemporaryLocation(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("The uploaded file is empty.");
            }

            string tempFilePath = Path.GetTempFileName();

            try
            {
                using (var sourceStream = file.OpenReadStream())
                using (var destinationStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                {
                    await sourceStream.CopyToAsync(destinationStream);
                }

                if (new FileInfo(tempFilePath).Length == 0)
                {
                    throw new IOException("Failed to write file to temporary location.");
                }
            }
            catch (Exception ex)
            {
                if (System.IO.File.Exists(tempFilePath))
                {
                    System.IO.File.Delete(tempFilePath);
                }
                throw new Exception($"Error saving file to temporary location: {ex.Message}", ex);
            }

            return tempFilePath;
        }



        private string ConvertXmlToJson(string filePath)
        {
            try
            {
                var doc = new XmlDocument();
                doc.Load(filePath);
                return JsonConvert.SerializeXmlNode(doc);
            }
            catch (XmlException ex)
            {
                throw new Exception($"Failed to convert XML to JSON. The XML is not valid: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while converting XML to JSON: {ex.Message}", ex);
            }
        }

        private async Task<string> SaveJsonToFile(string originalFileName, string jsonContent)
        {
            string jsonFilePath = Path.Combine(Path.GetTempPath(), $"{Path.GetFileNameWithoutExtension(originalFileName)}.json");
            await System.IO.File.WriteAllTextAsync(jsonFilePath, jsonContent);
            return jsonFilePath;
        }

        private async Task<bool> SendProcessedFileToService(string fileName, string jsonData)
        {
            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(new { FileName = fileName, FileContent = jsonData }), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/ProcessedFiles/ReceiveProcessedFile", content);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Failed to send JSON to ProcessedFilesServiceAPI. Status code: {response.StatusCode}");
                }

                return true;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"An error occurred while sending JSON to the storage service: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"An unexpected error occurred while sending JSON to the storage service: {ex.Message}", ex);
            }
        }

        private void CleanUpTempFiles(string tempFilePath, string jsonFilePath)
        {
            if (tempFilePath != null && System.IO.File.Exists(tempFilePath))
            {
                System.IO.File.Delete(tempFilePath);
            }
            if (jsonFilePath != null && System.IO.File.Exists(jsonFilePath))
            {
                System.IO.File.Delete(jsonFilePath);
            }
        }


    }
}
