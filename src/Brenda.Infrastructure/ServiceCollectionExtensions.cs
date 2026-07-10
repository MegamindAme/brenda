using Brenda.Core.Abstractions;
using Brenda.Core.Abstractions.Future;
using Brenda.Infrastructure.Blender;
using Brenda.Infrastructure.Cleanup;
using Brenda.Infrastructure.Data;
using Brenda.Infrastructure.Projects;
using Brenda.Infrastructure.Stubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Brenda.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers all Brenda infrastructure services (persistence, Blender, cleanup).</summary>
    public static IServiceCollection AddBrendaInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IAppPaths, AppPaths>();

        services.AddDbContextFactory<BrendaDbContext>((provider, options) =>
        {
            var paths = provider.GetRequiredService<IAppPaths>();
            options.UseSqlite($"Data Source={paths.DatabasePath}");
        });

        services.AddHttpClient(nameof(BlenderReleaseProvider));
        services.AddSingleton<IBlenderReleaseProvider>(provider => new BlenderReleaseProvider(
            provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(BlenderReleaseProvider)),
            provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<BlenderReleaseProvider>>()));
        services.AddSingleton<IBlenderInstaller>(provider => new BlenderInstaller(
            provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(BlenderReleaseProvider)),
            provider.GetRequiredService<IAppPaths>(),
            provider.GetRequiredService<IBlenderRegistry>(),
            provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<BlenderInstaller>>()));

        services.AddSingleton<IBlendFileInspector, BlendFileInspector>();
        services.AddSingleton<BlenderVersionService>();
        services.AddSingleton<IBlenderVersionService>(p => p.GetRequiredService<BlenderVersionService>());
        services.AddSingleton<IBlenderRegistry>(p => p.GetRequiredService<BlenderVersionService>());

        services.AddSingleton<IProjectTemplateService, ProjectTemplateService>();
        services.AddSingleton<IProjectService, ProjectService>();
        services.AddSingleton<ICleanupService, CleanupService>();

        // Roadmap v1.1/v2.0 placeholders.
        services.AddSingleton<IRenderService, RenderServiceStub>();
        services.AddSingleton<IGitService, GitServiceStub>();
        services.AddSingleton<IAssetLibraryService, AssetLibraryServiceStub>();

        return services;
    }
}
