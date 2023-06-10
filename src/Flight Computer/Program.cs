using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SPIN.Core.Contracts.Requests.Abstractions;
using SPIN.Core.Contracts.Responses.Abstractions;
using SPIN.Core.Installers.Abstractions;


var configurationBuilder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddCommandLine(args);
var configuration = configurationBuilder.Build();

var serviceCollection = new ServiceCollection();
serviceCollection.AddMediatR((cfg) =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(SPIN.Core.Contracts.Requests.Abstractions.ISensorRequest).Assembly);
});

var serviceTypes = typeof(IInstaller).Assembly.ExportedTypes
    .Where(type => typeof(IInstaller).IsAssignableFrom(type) &&
                   !type.IsInterface &&
                   !type.IsAbstract)
    .ToList();

foreach (var serviceType in serviceTypes)
{
    var service = Activator.CreateInstance(serviceType) as IInstaller;
    var serviceIsNull = service is null;
    if (serviceIsNull)
    {
        continue;
    }

    var serviceCannotInstall = !service!.CanInstall;
    if (serviceCannotInstall)
    {
        continue;
    }

    service.InstallService(serviceCollection, configuration);
}

var serviceProvider = serviceCollection.BuildServiceProvider();

var mediator = serviceProvider.GetRequiredService<IMediator>();

var types = typeof(ISensorRequest).Assembly.ExportedTypes
    .Where(type => typeof(ISensorRequest).IsAssignableFrom(type) &&
                    !type.IsInterface &&
                    !type.IsAbstract)
    .ToList();

foreach(var type in types)
{
    var request = Activator.CreateInstance(type);
    var response = await mediator.Send(request);

    var responseStatus = (SensorResponseStatus)response.GetType().GetProperty("Status").GetValue(response, null);
    var responseValue = response.GetType().GetProperty("Value").GetValue(response, null);

    Console.WriteLine($"Response status is: {responseStatus}");
    Console.WriteLine($"Response value is: {responseValue ?? ""}");
}
