using System.Configuration;
using System.Data;
using System.Windows;
using Rewind.Helpers;
using Rewind.MVVM.Services;

namespace Rewind;

/// <summary>
/// Interaction logic for App.xaml.
/// Точка входа также регистрирует MVVM-сервисы (навигация, диалоги) в ServiceLocator.
/// </summary>
public partial class App : Application
{
    public App()
    {
        ShutdownMode = ShutdownMode.OnMainWindowClose;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        // Регистрация прикладных сервисов. Делаем до первого окна,
        // чтобы любые VM могли их резолвить через ServiceLocator.
        ServiceLocator.Register<INavigationService>(new NavigationService());
        ServiceLocator.Register<IDialogService>(new DialogService());
        ServiceLocator.Register<IPlayerService>(new PlayerService());

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Session.FlushToDatabase();
        base.OnExit(e);
    }
}

