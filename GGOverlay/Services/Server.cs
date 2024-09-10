using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using GGOverlay.SignalR; // Adjust this namespace to match your actual namespace

namespace GGOverlay.Services
{
    public class Server
    {
        private IHost _host;

        // Start the SignalR server
        public async Task StartAsync()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel() // Use Kestrel as the server
                        .UseUrls("http://localhost:5000") // Set the server URL
                        .ConfigureServices(services =>
                        {
                            services.AddSignalR(); // Add SignalR services
                        })
                        .Configure(app =>
                        {
                            app.UseRouting(); // Enable routing for the application
                            app.UseEndpoints(endpoints =>
                            {
                                endpoints.MapHub<CounterHub>("/counterHub"); // Map the SignalR hub to the specified endpoint
                            });
                        });
                })
                .Build();

            await _host.StartAsync(); // Start the server
        }

        // Stop the SignalR server
        public async Task StopAsync()
        {
            if (_host != null)
            {
                await _host.StopAsync(); // Stop the server gracefully
                _host.Dispose(); // Clean up resources
            }
        }
    }
}
