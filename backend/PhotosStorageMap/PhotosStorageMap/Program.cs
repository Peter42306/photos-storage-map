using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PhotosStorageMap.Api;
using PhotosStorageMap.Api.Extensions;
using PhotosStorageMap.Api.Services;
using PhotosStorageMap.Application.Interfaces;
using PhotosStorageMap.Infrastructure.Data;
using PhotosStorageMap.Infrastructure.Email;
using PhotosStorageMap.Infrastructure.Extensions;
using PhotosStorageMap.Infrastructure.Identity;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// DbContext (Postgres)
//var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//        options.UseNpgsql(connectionString));
builder.Services.AddDatabase(builder.Configuration);

// Identity token provider + security stamp validator
builder.Services.AddDataProtection();
builder.Services.AddSingleton(TimeProvider.System);

// Identity
//builder.Services
//    .AddIdentityCore<ApplicationUser>(options =>
//{
//    options.User.RequireUniqueEmail = true;
//    options.SignIn.RequireConfirmedEmail = true;

//    options.Lockout.MaxFailedAccessAttempts = 5;
//    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
//    options.Lockout.AllowedForNewUsers = true;

//    options.Password.RequiredLength = 8;
//    options.Password.RequireDigit = true;
//    options.Password.RequireNonAlphanumeric = false;
//    options.Password.RequireLowercase = false;
//    options.Password.RequireUppercase = false;
//})
//    .AddRoles<IdentityRole>()
//    .AddEntityFrameworkStores<ApplicationDbContext>()
//    .AddSignInManager()
//    .AddDefaultTokenProviders();
builder.Services.AddApplicationIdentity();


// CORS (dev)
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy(CorsPolicies.Dev, policy =>
//    {
//        policy
//            .WithOrigins("http://localhost:5173")
//            .AllowAnyHeader()
//            .AllowAnyMethod();
//    });
//});
builder.Services.AddApplicationCors();


builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

//builder.Services
//    .AddSwaggerGen(c =>
//    {
//        c.SwaggerDoc("v1", new()
//        { 
//            Title = "PhotosStorageMap",
//            Version = "v1"
//        });

//        c.AddSecurityDefinition("Bearer", new()
//        {
//            Name = "Authorization",
//            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
//            Scheme = "Bearer",
//            BearerFormat = "JWT",
//            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
//            Description = "Enter JWT token"
//        });

//        c.AddSecurityRequirement(new()
//        {
//            {
//                new()
//                {
//                    Reference = new()
//                    {
//                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
//                        Id = "Bearer"
//                    }
//                },
//                Array.Empty<string>()
//            }
//        });
//    });
builder.Services.AddApplicationSwagger();

builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<IEmailService, DevEmailService>();



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
