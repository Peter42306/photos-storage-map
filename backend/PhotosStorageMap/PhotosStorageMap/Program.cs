using Amazon.S3;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhotosStorageMap.Api;
using PhotosStorageMap.Api.Extensions;
using PhotosStorageMap.Api.Services;
using PhotosStorageMap.Application.Interfaces;
using PhotosStorageMap.Infrastructure.BackgroundProcessing;
using PhotosStorageMap.Infrastructure.Data;
using PhotosStorageMap.Infrastructure.Email;
using PhotosStorageMap.Infrastructure.Extensions;
using PhotosStorageMap.Infrastructure.Identity;
using PhotosStorageMap.Infrastructure.Images;
using PhotosStorageMap.Infrastructure.Policies;
using PhotosStorageMap.Infrastructure.Services;
using PhotosStorageMap.Infrastructure.Storage;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

//Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
//Console.WriteLine($"Development config exists: {File.Exists(Path.Combine(builder.Environment.ContentRootPath, "appsettings.Development.json"))}");
//Console.WriteLine($"Content root: {builder.Environment.ContentRootPath}");
//Console.WriteLine($"Cors value: {builder.Configuration["Cors:AllowedOrigins:0"] ?? "<missing>"}");

// DbContext (Postgres)
builder.Services.AddDatabase(builder.Configuration);

// Identity token provider + security stamp validator
//builder.Services.AddDataProtection();
var dataProtectionKeyPath = builder.Environment.IsDevelopment()
    ? Path.Combine(builder.Environment.ContentRootPath, "keys")
    : "/var/lib/photo-map/keys";

builder.Services
    .AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeyPath))
    .SetApplicationName("PhotosStorageMap");
builder.Services.AddSingleton(TimeProvider.System);

// Identity
builder.Services.AddApplicationIdentity();


// CORS
builder.Services.AddApplicationCors(builder.Configuration);


builder.Services.AddJwtAuthentication(builder.Configuration);

//builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddApplicationSwagger();

builder.Services.AddScoped<JwtTokenService>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IEmailService, DevEmailService>();
}
else
{
    builder.Services
        .AddOptions<SendGridOptions>()
        .Bind(builder.Configuration.GetSection("SendGrid"))
        .Validate(options => !string.IsNullOrWhiteSpace(options.ApiKey), "SendGrid:ApiKey is required.")
        .Validate(options => !string.IsNullOrWhiteSpace(options.FromEmail), "SendGrid:FromEmail is required.")
        .ValidateOnStart();

    builder.Services.AddScoped<IEmailService, SendGridEmailService>();
}

builder.Services.AddScoped<IRetentionPolicy, DefaultRetentionPolicy>();

builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var options = sp.GetRequiredService<IOptions<StorageOptions>>().Value.S3;

    var config = new AmazonS3Config
    {
        ServiceURL = options.ServiceUrl,
        ForcePathStyle = options.ForcePathStyle,
    };

    return new AmazonS3Client(options.AccessKey, options.SecretKey, config);
});
builder.Services.AddScoped<IFileStorage, S3FileStorage>();

builder.Services.AddSingleton<IPhotoProcessingQueue, InMemoryPhotoProcessingQueue>();
builder.Services.AddScoped<IImageProcessor, ImageSharpImageProcessor>();
builder.Services.AddHostedService<PhotoProcessingWorker>();
builder.Services.AddScoped<IArchiveCollectionService, ArchiveCollectionService>();
builder.Services.AddScoped<ICollectionStatsService, CollectionStatsService>();
builder.Services.AddHostedService<PhotoCleanupWorker>();
builder.Services.AddHostedService<CollectionCleanupWorker>();
builder.Services.AddHostedService<OriginalPhotosCleanupWorker>();
builder.Services.AddSingleton<IZipJobStore, ZipJobStore>();
builder.Services.AddScoped<IStorageLimitService, StorageLimitService>();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        app.Logger.LogInformation("Applying database migrations...");
        await dbContext.Database.MigrateAsync();
        app.Logger.LogInformation("Database migrations completed.");
    }
    catch (Exception ex)
    {
        app.Logger.LogCritical(ex, "Database migration failed.");
        throw;
    }

    try
    {
        app.Logger.LogInformation("Seeding identity...");
        await IdentitySeeder.SeedAsync(scope.ServiceProvider);
        app.Logger.LogInformation("Identity seeding completed.");
    }
    catch (Exception ex)
    {
        app.Logger.LogCritical(ex, "Identity seeding failed.");
        throw;
    }

    
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors(CorsPolicies.Default);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
