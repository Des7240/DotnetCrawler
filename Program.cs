using DotnetCrawler.Data;
using DotnetCrawler.Services;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using Microsoft.AspNetCore.OData;
using Microsoft.OData.ModelBuilder;
using DotnetCrawler.Entities;
using Microsoft.OData.Edm;

var builder = WebApplication.CreateBuilder(args);

// Load .env
Env.Load();

// Config Database
var connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING") 
                       ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

// Register Services
builder.Services.AddHttpClient<StorageService>();
builder.Services.AddSingleton<CrawlerService>();



// Define EDM Model for OData
IEdmModel GetEdmModel()
{
    var odataBuilder = new ODataConventionModelBuilder();
    odataBuilder.EntitySet<Subject>("Subjects");
    odataBuilder.EntitySet<CourseThread>("CourseThreads");
    odataBuilder.EntitySet<QuestionDto>("Questions");
    odataBuilder.EntitySet<Comment>("Comments");
    odataBuilder.EntitySet<ThreadFile>("ThreadFiles");
    return odataBuilder.GetEdmModel();
}

// Controllers & JSON config
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    })
    .AddOData(opt => opt.AddRouteComponents("odata", GetEdmModel())
                        .Select()
                        .Filter()
                        .OrderBy()
                        .SetMaxTop(100)
                        .Count()
                        .Expand());

// Enable CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builderPolicy => builderPolicy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

var app = builder.Build();

app.UseCors("AllowAll");

// Response Caching Middleware for Client Cache Headers
app.UseResponseCaching();

app.UseAuthorization();
app.MapControllers();

// Auto Migrate Database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try 
    {
        db.Database.Migrate();
        Console.WriteLine("PostgreSQL Database Migrated Successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"DB Migration Failed: {ex.Message}");
    }
}

app.Run();
