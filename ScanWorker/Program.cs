using Microsoft.EntityFrameworkCore;
using Repository;
using ScanWorker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<ScanWorkerContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
