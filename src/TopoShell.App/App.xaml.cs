using System.Windows;
using Application = System.Windows.Application;

namespace TopoShell.App;

public partial class App : Application
{
    public App()
    {
        var windir = Environment.GetEnvironmentVariable("windir");
        var systemRoot = Environment.GetEnvironmentVariable("SystemRoot");

        if (string.IsNullOrWhiteSpace(windir) && !string.IsNullOrWhiteSpace(systemRoot))
        {
            Environment.SetEnvironmentVariable("windir", systemRoot, EnvironmentVariableTarget.Process);
        }
    }
}
