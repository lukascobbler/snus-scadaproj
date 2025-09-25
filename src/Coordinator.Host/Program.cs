using CoreWCF;
using CoreWCF.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Contracts;
using System.ServiceModel;
using HostBinding = CoreWCF.BasicHttpBinding;


var builder = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(l => l.AddConsole())
    .ConfigureServices((ctx, services) =>
    {
        services.AddServiceModelServices();
        services.AddSingleton<CoordinatorService>();
        services.AddHostedService<ReconcilerWorker>();       // minute scheduler

        // channel factories used by Coordinator to call sensors
        services.AddSingleton<ChannelFactory<ISensorService>>(
            _ => ChannelFactoryHelpers.CreateSensor("http://localhost:5011/sensor"));
        services.AddSingleton<ChannelFactory<ISensorService>>(
            _ => ChannelFactoryHelpers.CreateSensor("http://localhost:5012/sensor"));
        services.AddSingleton<ChannelFactory<ISensorService>>(
            _ => ChannelFactoryHelpers.CreateSensor("http://localhost:5013/sensor"));



    })
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.UseUrls("http://localhost:5020");
        webBuilder.Configure(app =>
        {
            app.UseServiceModel(sb =>
            {
                sb.AddService<CoordinatorService>();
                sb.AddServiceEndpoint<CoordinatorService, ICoordinatorService>(
                    new HostBinding(), "/coord");
            });
        });
    });

await builder.Build().RunAsync();
