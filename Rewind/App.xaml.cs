using System.Configuration;
using System.Data;
using System.Windows;
using Rewind.Helpers;

namespace Rewind;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        ShutdownMode = ShutdownMode.OnMainWindowClose;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Session.FlushToDatabase();
        base.OnExit(e);
    }
}

