using IhGitWpf.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using IhGitWpf.Properties;

namespace IhGitWpf
{
    public partial class App : Application
    {
        /// <summary>
        /// Gets the current <see cref="App"/> instance in use
        /// </summary>
        public new static App Current => (App)Application.Current;

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
        /// </summary>
        public IServiceProvider Services { get; }

        public App()
        {
            Services = ConfigureServices(ParseOfflineSession());
        }

        private OfflineSession? ParseOfflineSession()
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, OfflineSession.Argument);
            if (index < 0)
                return null;

            try
            {
                if (index + 1 >= args.Length)
                    throw new ArgumentException($"{OfflineSession.Argument} requires the path to a scenario manifest.json");

                return new OfflineSession(IhGit.Core.Scenarios.ScenarioGuard.Validate(args[index + 1]));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Cannot start offline scenario");
                Environment.Exit(1);
                return null;
            }
        }

        public bool IsOffline => Services.GetService<OfflineSession>() is not null;

        /// <summary>
        /// Configures the services for the application.
        /// </summary>
        private static IServiceProvider ConfigureServices(OfflineSession? offline)
        {
            var services = new ServiceCollection();

            if (offline is not null)
                services.AddSingleton(offline);

            services.AddSingleton<MainViewModel>();

            return services.BuildServiceProvider();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Offline scenarios must never touch the user's real settings
            if (!IsOffline)
                Settings.Default.Save();

            base.OnExit(e);
        }
    }
}
