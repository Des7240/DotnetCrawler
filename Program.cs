using DotnetCrawler.Data;
using DotnetCrawler.Services;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

// Load .env
Env.Load();

// Config Database
var connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING") 
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("POSTGRES_CONNECTION_STRING is not configured.");
    }
    
    options.UseNpgsql(connectionString);
});

// Register Services
builder.Services.AddHttpClient<StorageService>();
builder.Services.AddSingleton<CrawlerService>();

// Controllers & JSON config
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// Enable CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

var app = builder.Build();

app.UseCors("AllowAll");

// Serve static files from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

// Response Caching Middleware for Client Cache Headers
app.UseResponseCaching();

app.UseAuthorization();
app.MapControllers();

// Auto Migrate (only run this carefully in prod)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try 
    {
        db.Database.Migrate();
        Console.WriteLine("Database Migrated Successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Migration Failed: {ex.Message}");
    }
}

app.Run();
