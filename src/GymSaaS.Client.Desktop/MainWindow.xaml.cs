using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.WebView.Wpf;
using GymSaaS.Client.Desktop.Services;
using GymSaaS.Client.Desktop.Persistence;
using GymSaaS.SharedUI.Services;

namespace GymSaaS.Client.Desktop;

public partial class MainWindow : Window
{
    public IServiceProvider Services { get; }

    public MainWindow()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddWpfBlazorWebView();

        // Registro de Base de Datos Local
        serviceCollection.AddDbContext<LocalDbContext>();

        // Registro de Servicios de API (Implementando la interfaz de SharedUI)
        serviceCollection.AddHttpClient("HubAPI", client => {
            client.BaseAddress = new Uri("http://localhost:5000");
        });
        
        serviceCollection.AddSingleton<IApiService, ApiService>();
        serviceCollection.AddSingleton<NavigationService>();

        Services = serviceCollection.BuildServiceProvider();

        // Asegurar que la DB local esté creada
        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
            db.Database.EnsureCreated();
        }

        InitializeComponent();

        var blazorWebView = new BlazorWebView
        {
            HostPage = "wwwroot/index.html",
            Services = Services
        };

        blazorWebView.RootComponents.Add(new RootComponent
        {
            Selector = "#app",
            ComponentType = typeof(MainApp)
        });

        MainGrid.Children.Add(blazorWebView);
    }
}