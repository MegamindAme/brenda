using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Brenda.App.Services;
using Brenda.App.ViewModels;
using Brenda.App.Views;
using Brenda.Infrastructure;
using Brenda.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Brenda.App;

public class App : Application
{
    public static ServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        Services = ConfigureServices();

        // Apply pending EF Core migrations on startup.
        using (var scope = Services.CreateScope())
        {
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<BrendaDbContext>>();
            using var db = factory.CreateDbContext();
            db.Database.Migrate();
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };
            desktop.Exit += (_, _) => Log.CloseAndFlush();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddBrendaInfrastructure();

        services.AddLogging(builder =>
        {
            var paths = new Infrastructure.AppPaths();
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Debug()
                .WriteTo.File(
                    Path.Combine(paths.LogsDirectory, "brenda-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7)
                .CreateLogger();

            builder.AddSerilog(Log.Logger, dispose: false);
        });

        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IUpdateService, UpdateService>();

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<ProjectsViewModel>();
        services.AddSingleton<VersionsViewModel>();
        services.AddSingleton<CleanupViewModel>();
        services.AddSingleton<SettingsViewModel>();

        return services.BuildServiceProvider();
    }
}
