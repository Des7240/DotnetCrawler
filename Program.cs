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
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseInMemoryDatabase("CrawlerDbTest");
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

// Auto Create In-Memory DB
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try 
    {
        db.Database.EnsureCreated();
        Console.WriteLine("In-Memory Database Ready.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"DB Creation Failed: {ex.Message}");
    }
}

app.Run();
