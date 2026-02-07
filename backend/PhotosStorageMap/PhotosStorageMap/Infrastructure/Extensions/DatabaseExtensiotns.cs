using Microsoft.EntityFrameworkCore;
using PhotosStorageMap.Infrastructure.Data;

namespace PhotosStorageMap.Infrastructure.Extensions
{
    public static class DatabaseExtensiotns
    {
        public static IServiceCollection AddDatabase(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");            

            services.AddDbContext<ApplicationDbContext>(options => 
                options.UseNpgsql(connectionString));

            return services;
        }
    }
}
