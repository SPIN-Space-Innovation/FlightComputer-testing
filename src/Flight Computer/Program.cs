using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using MediatR;

using SPIN.Core.Contracts.Requests.Abstractions;
using SPIN.Core.Contracts.Requests.Generic;
using SPIN.Core.Contracts.Responses.Abstractions;
using IRequest = SPIN.Core.Contracts.Requests.Abstractions.IRequest;


var configurationBuilder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddCommandLine(args);
var configuration = configurationBuilder.Build();

IServiceProvider serviceProvider = new ServiceCollection()
    .AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
        cfg.RegisterServicesFromAssembly(typeof(IRequest).Assembly);
    })
    .BuildServiceProvider();

var mediator = serviceProvider.GetRequiredService<IMediator>();

serviceProvider = await mediator.Send(new RegisterServicesRequest { Configuration = configuration });
mediator = serviceProvider.GetRequiredService<IMediator>();

var types = typeof(ISensorRequest).Assembly.ExportedTypes
    .Where(type => typeof(ISensorRequest).IsAssignableFrom(type) &&
                    !type.IsInterface &&
                    !type.IsAbstract)
    .ToList();

while (true)
{
    foreach(var type in types)
    {
        var request = Activator.CreateInstance(type);
        var response = await mediator.Send(request);

        var responseStatus = (SensorResponseStatus)response.GetType().GetProperty("Status").GetValue(response, null);
        var responseValueType = response.GetType().GetProperty("Value").GetValue(response, null);
        var responseValue = responseValueType?.GetType().GetProperty("_value").GetValue(responseValueType, null) as double?;
        Console.WriteLine($"Response status is: {responseStatus}");
        Console.WriteLine($"Response value is: {responseValue}");
    }

    Thread.Sleep(2000);
}
