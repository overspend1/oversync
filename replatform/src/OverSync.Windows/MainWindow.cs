using OverSync.Windows.Views;

namespace OverSync.Windows;

public sealed class MainWindow : Window
{
    public MainWindow(MainPage page)
    {
        Title = "OverSync Windows";
        Content = page;
    }
}
