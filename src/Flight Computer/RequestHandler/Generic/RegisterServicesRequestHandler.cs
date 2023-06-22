using Microsoft.Extensions.DependencyInjection;

using MediatR;

using SPIN.Core.Contracts.Requests.Abstractions;
using SPIN.Core.Contracts.Requests.Generic;
using SPIN.Core.Installers.Abstractions;

namespace SPIN.FlightComputer.RequestHandler.Generic;

public class RegisterServicesRequestHandler : IRequestHandler<RegisterServicesRequest, IServiceProvider>
{
    private static Task RegisterMediatR(RegisterServicesRequest request,
        IServiceCollection serviceCollection,
        // ReSharper disable once UnusedParameter.Local
        CancellationToken cancellationToken)
    {
        serviceCollection.AddMediatR(cfg =>
        {
            // ReSharper disable once HeapView.ObjectAllocation
            cfg.RegisterServicesFromAssemblies(typeof(Program).Assembly, typeof(ISensorRequest).Assembly);
        });

        return Task.CompletedTask;
    }

    private static async Task RegisterInstallableServices(RegisterServicesRequest request,
        IServiceCollection serviceCollection,
        // ReSharper disable once UnusedParameter.Local
        CancellationToken cancellationToken)
    {
        var exportedTypes = typeof(IInstaller).Assembly.ExportedTypes;
        var installersType = exportedTypes.Where(type =>
        {
            bool hasInheritedIInstaller = typeof(IInstaller).IsAssignableFrom(type);
            bool isNotInterface = !type.IsInterface;
            bool isNotAbstract = !type.IsAbstract;

            return hasInheritedIInstaller && isNotInterface && isNotAbstract;
        });
        var installersInstance = installersType.Select(Activator.CreateInstance)
            .Cast<IInstaller>();
        installersInstance = installersInstance.OrderBy(type => type.Priority);
        var installersInstanceList = installersInstance.ToList();

        foreach (var installer in installersInstanceList)
        {
            bool installerIsNull = installer is null;
            if (installerIsNull)
            {
                continue;
            }

            bool cannotInstallService = !await installer!.CanInstallAsync(serviceCollection, request.Configuration);
            if (cannotInstallService)
            {
                continue;
            }

            await installer.InstallServiceAsync(serviceCollection, request.Configuration);
        }
    }

    public async Task<IServiceProvider> Handle(RegisterServicesRequest request, CancellationToken cancellationToken)
    {
        // ReSharper disable once HeapView.ObjectAllocation.Evident
        var serviceCollection = new ServiceCollection();

        await RegisterMediatR(request, serviceCollection, cancellationToken);

        await RegisterInstallableServices(request, serviceCollection, cancellationToken);

        var serviceProvider = serviceCollection.BuildServiceProvider();
        return serviceProvider;
    }
}
