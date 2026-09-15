using Microsoft.EntityFrameworkCore;
using CC.infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ========================================
// Database Configuration
// ========================================
builder.Services.AddDbContext<MasterCrmDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("MasterCrm")
    ));

builder.Services.AddDbContext<CrmDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("LocalCrm")
    ));


// ========================================
// Add services to the container
// ========================================
//builder.Services.AddControllers();

// OpenAPI
//builder.Services.AddOpenApi();

// Add services to the container
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ========================================
// Configure the HTTP request pipeline
// ========================================
//if (app.Environment.IsDevelopment())
//{
//    app.MapOpenApi();
//}
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthorization();

app.MapControllers();

app.Run();
