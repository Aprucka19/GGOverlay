using System.Windows;
using GGOverlay.ViewModels;

namespace GGOverlay.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}
