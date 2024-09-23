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
        public event Action<TcpClient> OnClientDisconnected; // New event for client disconnections
        public event Action OnDisconnected;
        public event Action<string> OnLog;

        // Define a message terminator
        private const string MessageTerminator = "<END>";

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

            OnClientConnected?.Invoke(client);

            StringBuilder messageBuffer = new StringBuilder();

            try
            {
                while (!IsClientDisconnected(client) && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // Read incoming data character by character
                        char[] buffer = new char[1];
                        int read;
                        while ((read = await reader.ReadAsync(buffer, 0, 1).ConfigureAwait(false)) > 0)
                        {
                            // Append each character to the message buffer
                            messageBuffer.Append(buffer[0]);

                            // Check if the terminator is in the buffer
                            if (messageBuffer.ToString().EndsWith(MessageTerminator))
                            {
                                // Remove the terminator and get the full message
                                string completeMessage = messageBuffer.ToString().Replace(MessageTerminator, string.Empty);

                                // Trigger the OnMessageReceived event
                                OnMessageReceived?.Invoke(completeMessage, client);

                                // Clear the buffer for the next message
                                messageBuffer.Clear();
                            }
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
                    OnClientDisconnected?.Invoke(client); // Trigger the OnClientDisconnected event
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
            foreach (var writer in _clients.Values)
            {
                try
                {
                    // Append the message terminator before sending
                    await writer.WriteAsync(message + MessageTerminator).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    OnLog?.Invoke($"Error broadcasting to a client: {ex.Message}");
                }
            }
        }

        public async Task BroadcastMessageToAllExceptOneAsync(string message, TcpClient excludedClient)
        {
            foreach (var kvp in _clients)
            {
                if (kvp.Key != excludedClient)
                {
                    try
                    {
                        // Append the message terminator before sending
                        await kvp.Value.WriteAsync(message + MessageTerminator).ConfigureAwait(false);
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
                    // Append the message terminator before sending
                    await writer.WriteAsync(message + MessageTerminator).ConfigureAwait(false);
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
            OnDisconnected?.Invoke();

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
