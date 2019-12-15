using CDMApi.Features.Roles;
using CDMApi.Features.Shared;
using CDMApi.Features.Systemusers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CDMApi
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            using (var host = CreateHostBuilder(args).Build())
            {
                try
                {
                    if (!await InitializeAsync(host.Services).ConfigureAwait(false))
                    {
                        return;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return;
                }

                await host.RunAsync().ConfigureAwait(false);
            }
        }

        private static async Task<bool> InitializeAsync(IServiceProvider services)
        {
#if DEBUG
            // Uncomment two lines below if you want to create C# POCOs from CDM metadata
            //var repository = services.GetService(typeof(CDMMetadataRepository)) as CDMMetadataRepository;
            //await repository.CreateModelsAsync();
#endif

            var roleModelService = services.GetService(typeof(CDMService<RoleModel>)) as CDMService<RoleModel>;
            if (roleModelService == null)
            {
                return false;
            }
            var systemuserModelService = services.GetService(typeof(CDMService<SystemuserModel>)) as CDMService<SystemuserModel>;
            if (systemuserModelService == null)
            {
                return false;
            }

            var tasks = new List<Task<bool>>();
            tasks.Add(roleModelService.InitializeAsync());
            tasks.Add(systemuserModelService.InitializeAsync());
            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            if (results.Any(r => !r))
            {
                return false;
            }

            return true;
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
