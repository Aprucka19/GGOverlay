using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Networking
{
    public class NetworkClient
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private StreamReader _reader;
        private StreamWriter _writer;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isDisconnecting = false; // Flag to prevent multiple disconnections

        public event Action<string> OnMessageReceived;
        public event Action<string> OnLog;
        public event Action OnDisconnected; // Event to handle disconnection

        public bool IsConnected => _client?.Connected ?? false;

        // ConnectAsync method with a connection timeout
        public async Task ConnectAsync(string ipAddress, int port, int timeoutSeconds = 5)
        {
            try
            {
                _isDisconnecting = false;
                _client = new TcpClient();
                _cancellationTokenSource = new CancellationTokenSource();

                // Create a cancellation token with a timeout
                var connectionTask = _client.ConnectAsync(ipAddress, port);
                var delayTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), _cancellationTokenSource.Token);

                // Wait for either the connection task to complete or the timeout
                if (await Task.WhenAny(connectionTask, delayTask) == delayTask)
                {
                    // Cancel the connection attempt if it times out
                    _cancellationTokenSource.Cancel();
                    throw new TimeoutException($"Connection attempt to {ipAddress}:{port} timed out after {timeoutSeconds} seconds.");
                }

                // If the connection was successful, proceed
                _stream = _client.GetStream();
                _reader = new StreamReader(_stream, Encoding.UTF8);
                _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };

                OnLog?.Invoke($"Connected to server at {ipAddress}:{port}");

                // Start receiving messages asynchronously
                _ = ReceiveMessagesAsync(_cancellationTokenSource.Token);
            }
            catch (TimeoutException ex)
            {
                OnLog?.Invoke($"Timeout error: {ex.Message}");
                throw; // Re-throw the exception to propagate the timeout error
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Error connecting to server: {ex.Message}");
                throw; // Re-throw the exception to indicate connection failure
            }
        }

        public async Task SendMessageAsync(string message)
        {
            try
            {
                if (_writer != null && IsConnected)
                {
                    await _writer.WriteLineAsync(message).ConfigureAwait(false);
                    OnLog?.Invoke($"Sent message: {message}");
                }
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Error sending message: {ex.Message}");
            }
        }

        private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && IsConnected)
                {
                    try
                    {
                        string message = await _reader.ReadLineAsync().ConfigureAwait(false);
                        if (message != null)
                        {
                            OnMessageReceived?.Invoke(message);
                            OnLog?.Invoke($"Received message: {message}");
                        }
                        else
                        {
                            // Null message indicates server disconnection
                            OnLog?.Invoke("Server has disconnected.");
                            break;
                        }
                    }
                    catch (IOException ioEx) when (cancellationToken.IsCancellationRequested)
                    {
                        // Expected exception due to cancellation; handle gracefully
                        OnLog?.Invoke("Receiving loop canceled due to client disconnect.");
                        break;
                    }
                    catch (IOException ioEx)
                    {
                        // Handle disconnection scenario from the server
                        OnLog?.Invoke($"Disconnected from server: {ioEx.Message}");
                        break;
                    }
                    catch (ObjectDisposedException)
                    {
                        // Stream has been disposed; expected when shutting down
                        break;
                    }
                    catch (Exception ex)
                    {
                        OnLog?.Invoke($"Error receiving message: {ex.Message}");
                        break;
                    }
                }
            }
            catch (Exception ex) when (cancellationToken.IsCancellationRequested)
            {
                // Handle the task cancellation gracefully
                OnLog?.Invoke("Receiving loop canceled.");
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Error receiving message: {ex.Message}");
            }
            finally
            {
                // Trigger disconnect events if the loop exits
                if (!_isDisconnecting) // Avoid redundant disconnections
                {
                    Disconnect();
                    OnDisconnected?.Invoke(); // Notify that client has been disconnected
                }
            }
        }

        public void Disconnect()
        {
            if (_isDisconnecting) return; // Prevent multiple disconnection attempts
            _isDisconnecting = true;

            OnLog?.Invoke("Disconnecting from server...");

            try
            {
                _cancellationTokenSource?.Cancel(); // Cancel receiving loop
                _reader?.Dispose();
                _writer?.Dispose();
                _stream?.Dispose();
                _client?.Close();
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Error during disconnect: {ex.Message}");
            }
            finally
            {
                
                OnLog?.Invoke("Disconnected from server.");
            }
        }
    }
}
