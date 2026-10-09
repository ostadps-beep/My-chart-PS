using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace ChartMy;

public partial class App : Application
{
    [STAThread]
    public static void Main()
    {
        try
        {
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
        catch (Exception ex)
        {
            ShowException("Startup (App.InitializeComponent / Run)", ex);
        }
    }

    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ShowException("DispatcherUnhandledException", e.Exception);
        e.Handled = true;
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            ShowException("AppDomain.UnhandledException", ex);
    }

    internal static void ShowException(string source, Exception ex)
    {
        var text = new StringBuilder();
        text.AppendLine(source);
        text.AppendLine();
        var current = ex;
        while (current is not null)
        {
            text.AppendLine(current.GetType().FullName);
            text.AppendLine(current.Message);
            text.AppendLine();
            text.AppendLine(current.StackTrace);
            current = current.InnerException;
            if (current is not null)
            {
                text.AppendLine();
                text.AppendLine("--- InnerException ---");
                text.AppendLine();
            }
        }

        MessageBox.Show(text.ToString(), "ChartMy exception");
    }
}
