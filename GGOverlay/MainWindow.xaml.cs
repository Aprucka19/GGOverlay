using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GGOverlay.Game;
using Microsoft.Win32;

using System;
using System.Windows;
using System.Windows.Input;


using System.Windows;
using System.Windows.Input;

namespace GGOverlay
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ShowLaunchView();
        }

        public void ShowLaunchView()
        {
            this.Title = "GGOverlay - Connection Window";
            ContentArea.Content = new LaunchView(this);
        }

        public void ShowLobbyView(IGameInterface game)
        {
            this.Title = "GGOverlay - Lobby";
            ContentArea.Content = new LobbyView(this, game);
        }

        public void ShowEditRulesView(IGameInterface game)
        {
            this.Title = "GGOverlay - Edit Rules";
            ContentArea.Content = new EditRulesView(this, game);
        }


        public void ShowEditPlayerView(IGameInterface game)
        {
            this.Title = "GGOverlay - Edit Player";
            ContentArea.Content = new EditPlayerView(this, game);
        }

        private void TopBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Allows dragging the window around
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}


