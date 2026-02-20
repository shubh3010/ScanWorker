using Microsoft.EntityFrameworkCore;
using Repository;
using ScanWorker.Configuration;
using ScanWorker.Interface;
using ScanWorker.Repository;
using ScanWorker.Respository;
using ScanWorker.Services;
using ScanWorker.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<ScanWorkerContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var scanApiOptions = builder.Configuration.GetSection(ScanApiOptions.SectionName).Get<ScanApiOptions>();

builder.Services.AddHttpClient<IScanEventClient, ScanEventClient>(client =>
{
    client.BaseAddress = new (scanApiOptions.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(scanApiOptions.TimeoutSeconds);
});

// Repositories
builder.Services.AddScoped<IEventProcessingStateRepository, EventProcessingStateRepository>();
builder.Services.AddScoped<IScanEventRepository, ScanEventRepository>();
builder.Services.AddScoped<IParcelRepository, ParcelRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Services
builder.Services.AddScoped<IScanEventProcessor, ScanEventProcessorService>();

builder.Services.AddHostedService<ScanEventWorker>();

var host = builder.Build();
host.Run();
