using CoreWCF;
using CoreWCF.Configuration;
using CoreWCF.Description;
using Microsoft.EntityFrameworkCore;
using Sensor.Service;
using Sensor.Service.Data;
using Shared.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5011");
builder.Services.AddServiceModelServices();

// DB
var dataDir = Path.Combine(AppContext.BaseDirectory, "data");
Directory.CreateDirectory(dataDir);
var cs = $"Data Source={Path.Combine(dataDir, "s1.db")}";
builder.Services.AddDbContext<SensorDbContext>(opt => opt.UseSqlite(cs));

// sampling infra
builder.Services.AddSingleton<ISamplingSwitch, SamplingSwitch>();
builder.Services.AddHostedService(sp => new SamplerWorker(
    sp.GetRequiredService<ILogger<SamplerWorker>>(),
    sp,
    sp.GetRequiredService<ISamplingSwitch>(),
    "S1"
));

builder.Services.AddScoped<SensorService>(sp =>
    new SensorService("S1",
        sp.GetRequiredService<SensorDbContext>(),
        sp.GetRequiredService<ISamplingSwitch>()));

var app = builder.Build();
app.UseServiceModel(serviceBuilder =>
{
    serviceBuilder.AddService<SensorService>();

    // exception debugging
    serviceBuilder.ConfigureServiceHostBase<SensorService>(host =>
    {
        var dbg = host.Description.Behaviors.Find<ServiceDebugBehavior>();
        if (dbg == null)
        {
            host.Description.Behaviors.Add(new ServiceDebugBehavior { IncludeExceptionDetailInFaults = true });
        }
        else
        {
            dbg.IncludeExceptionDetailInFaults = true;
        }
    });

    serviceBuilder.AddServiceEndpoint<SensorService, ISensorService>(new BasicHttpBinding(), "/sensor");
});

Console.WriteLine("S1 listening at http://localhost:5011/sensor (BasicHttpBinding)");
app.Run();
