using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using SharedLibrary.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using ProcessedFilesServiceAPI.Controllers;
using ProcessedFilesServiceAPI;

namespace ProcessedFilesServiceAPI.Tests.Controllers
{
    public class ProcessedFilesControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly ProcessedFilesController _controller;

        public ProcessedFilesControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            _context = new ApplicationDbContext(options);

            _mockConfiguration = new Mock<IConfiguration>();
            _mockConfiguration.SetupGet(x => x["ProcessedFilesService:ApiKey"]).Returns("secure-api-key");

            _controller = new ProcessedFilesController(_context, _mockConfiguration.Object);
        }

        [Fact]
        public async Task ReceiveProcessedFile_ShouldReturnUnauthorized_WhenApiKeyIsInvalid()
        {
             var processedFile = new ProcessedFile { FileName = "test.json", FileContent = "{\"test\": \"data\"}" };

             var result = await _controller.ReceiveProcessedFile(processedFile, "invalid-api-key");

             Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task ReceiveProcessedFile_ShouldReturnBadRequest_WhenFileIsNull()
        {
             var result = await _controller.ReceiveProcessedFile(null, "secure-api-key");

             Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ReceiveProcessedFile_ShouldReturnOk_WhenFileIsStoredSuccessfully()
        {
             var processedFile = new ProcessedFile { FileName = "test.json", FileContent = "{\"test\": \"data\"}" };

             var result = await _controller.ReceiveProcessedFile(processedFile, "secure-api-key");

             Assert.IsType<OkObjectResult>(result);
            Assert.NotEqual(0, processedFile.Id); 
        }

        [Fact]
        public async Task GetProcessedFiles_ShouldReturnOk_WithListOfFiles()
        {
             _context.ProcessedFiles.RemoveRange(_context.ProcessedFiles);
            await _context.SaveChangesAsync();

             var files = new List<ProcessedFile>
            {
                new ProcessedFile { FileName = "file1.json", FileContent = "{}" },
                new ProcessedFile { FileName = "file2.json", FileContent = "{}" }
            };

            _context.ProcessedFiles.AddRange(files);
            await _context.SaveChangesAsync();

             var result = await _controller.GetProcessedFiles();

             var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<List<ProcessedFile>>(okResult.Value);
            Assert.Equal(2, returnValue.Count);
        }

        [Fact]
        public async Task GetProcessedFile_ShouldReturnNotFound_WhenFileDoesNotExist()
        {
             var result = await _controller.GetProcessedFile(1);

             Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetProcessedFile_ShouldReturnOk_WhenFileExists()
        {
             var processedFile = new ProcessedFile { Id = 1, FileName = "file1.json", FileContent = "{}" };
            _context.ProcessedFiles.Add(processedFile);
            await _context.SaveChangesAsync();

             var result = await _controller.GetProcessedFile(1);

             var okResult = Assert.IsType<OkObjectResult>(result);
            var returnValue = Assert.IsType<ProcessedFile>(okResult.Value);
            Assert.Equal(1, returnValue.Id);
        }

        [Fact]
        public async Task DownloadProcessedFile_ShouldReturnNotFound_WhenFileDoesNotExist()
        {
             var result = await _controller.DownloadProcessedFile(1);

             Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DownloadProcessedFile_ShouldReturnFileStreamResult_WhenFileExists()
        {
             var processedFile = new ProcessedFile { Id = 1, FileName = "file1.json", FileContent = "{\"test\":\"data\"}" };
            _context.ProcessedFiles.Add(processedFile);
            await _context.SaveChangesAsync();

             var result = await _controller.DownloadProcessedFile(1);

             var fileStreamResult = Assert.IsType<FileStreamResult>(result);
            Assert.Equal("application/json", fileStreamResult.ContentType);
        }

        [Fact]
        public async Task DeleteProcessedFile_ShouldReturnNotFound_WhenFileDoesNotExist()
        {
             var result = await _controller.DeleteProcessedFile(1);

             Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteProcessedFile_ShouldReturnNoContent_WhenFileIsDeleted()
        {
             var processedFile = new ProcessedFile { Id = 1, FileName = "file1.json", FileContent = "{}" };
            _context.ProcessedFiles.Add(processedFile);
            await _context.SaveChangesAsync();

             var result = await _controller.DeleteProcessedFile(1);

             Assert.IsType<NoContentResult>(result);
            Assert.Null(await _context.ProcessedFiles.FindAsync(1));  
        }

        [Fact]
        public async Task ReceiveProcessedFile_ShouldHandleMultipleConcurrentRequests()
        {
             var tasks = new List<Task<IActionResult>>();

            for (int i = 0; i < 100; i++)
            {
                var processedFile = new ProcessedFile { FileName = $"test{i}.json", FileContent = "{\"test\": \"data\"}" };
                tasks.Add(_controller.ReceiveProcessedFile(processedFile, "secure-api-key"));
            }

             var results = await Task.WhenAll(tasks);

             foreach (var result in results)
            {
                Assert.IsType<OkObjectResult>(result);
            }

            var fileCount = await _context.ProcessedFiles.CountAsync();
            Assert.Equal(100, fileCount);
        }


        public void Dispose()
        {
             _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
