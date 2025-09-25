using CoreWCF;
using CoreWCF.Configuration;
using Microsoft.EntityFrameworkCore;
using Sensor.Service;
using Sensor.Service.Data;
using Shared.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5013");
builder.Services.AddServiceModelServices();

var dataDir = Path.Combine(AppContext.BaseDirectory, "data");
Directory.CreateDirectory(dataDir);

// SQLite per-sensor DB
var cs = $"Data Source={Path.Combine(dataDir, "s3.db")}";
builder.Services.AddDbContext<SensorDbContext>(opt => opt.UseSqlite(cs));

// Sampling switch
builder.Services.AddSingleton<ISamplingSwitch, SamplingSwitch>();
builder.Services.AddHostedService(sp => new SamplerWorker(
    sp.GetRequiredService<ILogger<SamplerWorker>>(),
    sp,
    sp.GetRequiredService<ISamplingSwitch>(),
    "S3"
));

builder.Services.AddScoped<SensorService>(sp =>
    new SensorService("S3",
        sp.GetRequiredService<SensorDbContext>(),
        sp.GetRequiredService<ISamplingSwitch>()));

var app = builder.Build();
app.UseServiceModel(serviceBuilder =>
{
    serviceBuilder.AddService<SensorService>();
    serviceBuilder.AddServiceEndpoint<SensorService, ISensorService>(new BasicHttpBinding(), "/sensor");
});

Console.WriteLine("S3 listening at http://localhost:5013/sensor (BasicHttpBinding)");
app.Run();
