namespace PhotosStorageMap.Api.Extensions
{
    public static class CorsExtensions
    {
        public static IServiceCollection AddApplicationCors(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var allowedOrigins = configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? [];

            if (allowedOrigins.Length == 0)
            {
                throw new InvalidOperationException("No CORS origins configured.");
            }

            services.AddCors(options =>
            {
                options.AddPolicy(CorsPolicies.Default, policy =>
                {
                    policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .WithExposedHeaders("Content-Disposition");
                });
            });

            return services;
        }
    }
}
