using GGOverlay.Services;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GGOverlay.Utilities;

namespace GGOverlay.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private HubConnection _hubConnection;
        private readonly DatabaseService _databaseService;
        private int _counterValue;
        public int CounterValue
        {
            get => _counterValue;
            set
            {
                _counterValue = value;
                OnPropertyChanged(nameof(CounterValue));
            }
        }

        public ICommand IncreaseCommand { get; }
        public ICommand HostCommand { get; }
        public ICommand JoinCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainViewModel()
        {
            _databaseService = new DatabaseService();
            CounterValue = _databaseService.GetCounterValue();

            IncreaseCommand = new RelayCommand(async _ => await IncreaseCounter());
            HostCommand = new RelayCommand(async _ => await HostServer());
            JoinCommand = new RelayCommand(async _ => await JoinServer());
        }

        private async Task IncreaseCounter()
        {
            CounterValue++;
            _databaseService.UpdateCounterValue(CounterValue);
            if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
            {
                await _hubConnection.SendAsync("UpdateCounter", CounterValue);
            }
        }

        private async Task HostServer()
        {
            var server = new Server();
            await server.StartAsync();
            await SetupHubConnection("http://localhost:5000/counterHub");
        }

        private async Task JoinServer()
        {
            await SetupHubConnection("http://localhost:5000/counterHub");
        }

        private async Task SetupHubConnection(string url)
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(url)
                .Build();

            _hubConnection.On<int>("ReceiveCounterUpdate", newValue =>
            {
                CounterValue = newValue;
                _databaseService.UpdateCounterValue(newValue);
            });

            await _hubConnection.StartAsync();
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
