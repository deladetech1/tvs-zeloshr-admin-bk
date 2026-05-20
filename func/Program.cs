using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ZelosHR.Functions.Shared;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<IFunctionDatabase, FunctionDatabase>();
        services.AddSingleton<IEmailService, EmailService>();
    })
    .Build();

host.Run();
