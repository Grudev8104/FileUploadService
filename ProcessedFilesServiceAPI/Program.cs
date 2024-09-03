using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ProcessedFilesServiceAPI;

var builder = WebApplication.CreateBuilder(args);

 builder.Services.AddControllers();

 builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

 builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

 builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

 if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
