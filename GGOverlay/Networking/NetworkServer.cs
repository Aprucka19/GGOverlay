using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Networking
{
    public class NetworkServer
    {
        private TcpListener _listener;
        private ConcurrentDictionary<TcpClient, StreamWriter> _clients = new ConcurrentDictionary<TcpClient, StreamWriter>();
        private CancellationTokenSource _cancellationTokenSource;

        public event Action<string, TcpClient> OnMessageReceived;
        public event Action<TcpClient> OnClientConnected;
        public event Action<string> OnLog;

        public async Task StartAsync(int port)
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _listener = new TcpListener(IPAddress.Any, port);
                _listener.Start();
                OnLog?.Invoke($"Server started on port: {port}");

                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        client.Close();
                        break;
                    }

                    OnClientConnected?.Invoke(client);
                    OnLog?.Invoke($"Client connected: {client.Client?.RemoteEndPoint}");
                    _ = HandleClientAsync(client, _cancellationTokenSource.Token);
                }
            }
            catch (Exception ex)
            {
                if (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    OnLog?.Invoke($"Error starting server: {ex.Message}");
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            var stream = client.GetStream();
            var reader = new StreamReader(stream, Encoding.UTF8);
            var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            _clients.TryAdd(client, writer);
            OnLog?.Invoke($"Handling client: {client.Client?.RemoteEndPoint}");

            try
            {
                while (!IsClientDisconnected(client) && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        string message = await reader.ReadLineAsync().ConfigureAwait(false);
                        if (cancellationToken.IsCancellationRequested) break;

                        if (message != null)
                        {
                            OnMessageReceived?.Invoke(message, client);
                            OnLog?.Invoke($"Received message from {client.Client?.RemoteEndPoint}: {message}");
                        }
                        else
                        {
                            break;
                        }
                    }
                    catch (IOException ioEx) when (cancellationToken.IsCancellationRequested)
                    {
                        // Expected exception on cancellation, handle gracefully
                        break;
                    }
                    catch (Exception ex)
                    {
                        OnLog?.Invoke($"Error handling client {client.Client?.RemoteEndPoint}: {ex.Message}");
                        break;
                    }
                }
            }
            finally
            {
                _clients.TryRemove(client, out _);
                if (client?.Client != null)
                {
                    OnLog?.Invoke($"Client disconnected: {client.Client.RemoteEndPoint}");
                }
                client?.Close();
            }
        }

        private bool IsClientDisconnected(TcpClient client)
        {
            try
            {
                return client.Client?.Poll(1, SelectMode.SelectRead) == true && client.Available == 0;
            }
            catch (SocketException)
            {
                return true;
            }
            catch (ObjectDisposedException)
            {
                return true;
            }
        }

        public async Task BroadcastMessageAsync(string message)
        {
            OnLog?.Invoke($"Broadcasting message: {message}");
            foreach (var writer in _clients.Values)
            {
                try
                {
                    await writer.WriteLineAsync(message).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    OnLog?.Invoke($"Error broadcasting to a client: {ex.Message}");
                }
            }
        }

        public async Task BroadcastMessageToAllExceptOneAsync(string message, TcpClient excludedClient)
        {
            OnLog?.Invoke($"Broadcasting message to all except {excludedClient.Client?.RemoteEndPoint}: {message}");
            foreach (var kvp in _clients)
            {
                if (kvp.Key != excludedClient)
                {
                    try
                    {
                        await kvp.Value.WriteLineAsync(message).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        OnLog?.Invoke($"Error broadcasting to a client: {ex.Message}");
                    }
                }
            }
        }

        public async Task SendMessageToClientAsync(string message, TcpClient client)
        {
            if (_clients.TryGetValue(client, out StreamWriter writer))
            {
                try
                {
                    await writer.WriteLineAsync(message).ConfigureAwait(false);
                    OnLog?.Invoke($"Sent message to {client.Client?.RemoteEndPoint}: {message}");
                }
                catch (Exception ex)
                {
                    OnLog?.Invoke($"Error sending message to {client.Client?.RemoteEndPoint}: {ex.Message}");
                }
            }
        }

        public void Stop()
        {
            OnLog?.Invoke("Stopping server...");
            _cancellationTokenSource?.Cancel();

            foreach (var client in _clients.Keys)
            {
                if (client != null)
                {
                    OnLog?.Invoke($"Closing connection to {client.Client?.RemoteEndPoint}");
                    client.Close();
                }
            }

            _listener.Stop();
            OnLog?.Invoke("Server stopped.");
        }
    }
}
