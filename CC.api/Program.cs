using Microsoft.EntityFrameworkCore;
using CC.infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ========================================
// Database & Multi-Tenant Configuration
// ========================================
builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<MasterCrmDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("MasterCrm")
    ));

builder.Services.AddScoped<CC.api.Services.ITenantService, CC.api.Services.TenantService>();

builder.Services.AddDbContext<CrmDbContext>((serviceProvider, options) =>
{
    var tenantService = serviceProvider.GetRequiredService<CC.api.Services.ITenantService>();
    var connectionString = tenantService.GetTenantConnectionString();
    options.UseSqlServer(connectionString);
});


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
