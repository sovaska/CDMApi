using CDMApi.Features.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CDMApi.Features.Systemusers
{
    public static class SystemuserExtensions
    {
        public static void AddSystemusers(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ADLSSettings>(configuration.GetSection("ADLSSettings"));

            services.AddSingleton<CsvContentParser>();
            services.AddSingleton<EntityGenerator>();
            services.AddSingleton<CDMMetadataRepository>();
            services.AddSingleton<CDMService<SystemuserModel>>();
        }
    }
}