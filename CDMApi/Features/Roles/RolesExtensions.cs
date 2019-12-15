using CDMApi.Features.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CDMApi.Features.Roles
{
    public static class RolesExtensions
    {
        public static void AddRoles(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ADLSSettings>(configuration.GetSection("ADLSSettings"));

            services.AddSingleton<CsvContentParser>();
            services.AddSingleton<EntityGenerator>();
            services.AddSingleton<CDMMetadataRepository>();
            services.AddSingleton<CDMService<RoleModel>>();
        }
    }
}