using Microsoft.Extensions.DependencyInjection;

using MediatR;

using SPIN.Core.Contracts.Requests.Generic;
using SPIN.Core.Installers.Abstractions;

namespace SPIN.FlightComputer.RequestHandler.Generic;

public class RegisterServicesRequestHandler : IRequestHandler<RegisterServicesRequest, IServiceProvider>
{
    public async Task<IServiceProvider> Handle(RegisterServicesRequest request, CancellationToken cancellationToken)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddMediatR((cfg) =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(SPIN.Core.Contracts.Requests.Abstractions.IRequest).Assembly);
        });

        var serviceTypes = typeof(IInstaller).Assembly.ExportedTypes
            .Where(type => typeof(IInstaller).IsAssignableFrom(type) &&
                           !type.IsInterface &&
                           !type.IsAbstract)
            .Select(Activator.CreateInstance)
            .Cast<IInstaller>()
            .OrderBy(type => type.Priority)
            .ToList();

        foreach (var service in serviceTypes)
        {
            var serviceIsNull = service is null;
            if (serviceIsNull)
            {
                continue;
            }

            bool serviceCannotInstall = !service!.CanInstall(serviceCollection, request.Configuration);
            if (serviceCannotInstall)
            {
                continue;
            }

            service.InstallService(serviceCollection, request.Configuration);
        }

        var serviceProvider = serviceCollection.BuildServiceProvider();

        return serviceProvider;
    }
}
