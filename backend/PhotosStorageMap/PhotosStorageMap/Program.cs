using Amazon.S3;
using Microsoft.Extensions.Options;
using PhotosStorageMap.Api;
using PhotosStorageMap.Api.Extensions;
using PhotosStorageMap.Api.Services;
using PhotosStorageMap.Application.Interfaces;
using PhotosStorageMap.Infrastructure.BackgroundProcessing;
using PhotosStorageMap.Infrastructure.Email;
using PhotosStorageMap.Infrastructure.Extensions;
using PhotosStorageMap.Infrastructure.Policies;
using PhotosStorageMap.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

// DbContext (Postgres)
builder.Services.AddDatabase(builder.Configuration);

// Identity token provider + security stamp validator
builder.Services.AddDataProtection();
builder.Services.AddSingleton(TimeProvider.System);

// Identity
builder.Services.AddApplicationIdentity();


// CORS (dev)
builder.Services.AddApplicationCors();


builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddApplicationSwagger();

builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<IEmailService, DevEmailService>();
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(CorsPolicies.Dev);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
