using CleanserBlazorUI.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CleanserBlazorUI.Services
{
    public class TempFileCleaner : IHostedService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public TempFileCleaner(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var tempFileCleanerService = scope.ServiceProvider.GetRequiredService<ITempFileCleanerService>();
                Console.WriteLine("Runing ...");
                //await tempFileCleanerService.CleanTemporaryFiles();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}