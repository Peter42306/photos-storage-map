using PhotosStorageMap.Api;
using PhotosStorageMap.Api.Extensions;
using PhotosStorageMap.Api.Services;
using PhotosStorageMap.Application.Interfaces;
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
builder.Services.AddScoped<IFileStorage, DiskFileStorage>();



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
