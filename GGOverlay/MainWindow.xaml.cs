using System.Windows;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DatabaseHelper.InitializeDatabase();
        UpdateCounterDisplay();
    }

    private void Increment_Click(object sender, RoutedEventArgs e)
    {
        var currentValue = DatabaseHelper.GetCounterValue();
        DatabaseHelper.UpdateCounter(currentValue + 1);
        UpdateCounterDisplay();
    }

    private void Decrement_Click(object sender, RoutedEventArgs e)
    {
        var currentValue = DatabaseHelper.GetCounterValue();
        DatabaseHelper.UpdateCounter(currentValue - 1);
        UpdateCounterDisplay();
    }

    private void UpdateCounterDisplay()
    {
        lblCounter.Content = "Counter: " + DatabaseHelper.GetCounterValue();
    }
}
