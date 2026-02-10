namespace PhotosStorageMap.Api.Extensions
{
    public static class CorsExtensions
    {
        public static IServiceCollection AddApplicationCors(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(CorsPolicies.Dev, policy =>
                {
                    policy
                    .WithOrigins(
                        "http://localhost:5173",
                        "http://192.168.1.102:5173")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
                });
            });

            return services;
        }
    }
}
