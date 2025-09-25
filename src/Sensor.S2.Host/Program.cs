using CoreWCF;
using CoreWCF.Configuration;
using Microsoft.EntityFrameworkCore;
using Sensor.Service;
using Sensor.Service.Data;
using Shared.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5012");
builder.Services.AddServiceModelServices();

var dataDir = Path.Combine(AppContext.BaseDirectory, "data");
Directory.CreateDirectory(dataDir);

// SQLite per-sensor DB
var cs = $"Data Source={Path.Combine(dataDir, "s2.db")}";
builder.Services.AddDbContext<SensorDbContext>(opt => opt.UseSqlite(cs));

// Sampling switch
builder.Services.AddSingleton<ISamplingSwitch, SamplingSwitch>();
builder.Services.AddHostedService(sp => new SamplerWorker(
    sp.GetRequiredService<ILogger<SamplerWorker>>(),
    sp,
    sp.GetRequiredService<ISamplingSwitch>(),
    "S2"
));

builder.Services.AddScoped<SensorService>(sp =>
    new SensorService("S2",
        sp.GetRequiredService<SensorDbContext>(),
        sp.GetRequiredService<ISamplingSwitch>()));

var app = builder.Build();
app.UseServiceModel(serviceBuilder =>
{
    serviceBuilder.AddService<SensorService>();
    serviceBuilder.AddServiceEndpoint<SensorService, ISensorService>(new BasicHttpBinding(), "/sensor");
});

Console.WriteLine("S2 listening at http://localhost:5012/sensor (BasicHttpBinding)");
app.Run();
