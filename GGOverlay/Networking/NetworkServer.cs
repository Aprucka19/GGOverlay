// Networking/NetworkServer.cs
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GGOverlay.Networking
{
    public class NetworkServer
    {
        private TcpListener _server;
        public List<TcpClient> ConnectedClients { get; private set; } = new List<TcpClient>(); // Track connected clients
        private const int Port = 5000;

        public event Action<string,TcpClient> OnMessageReceived;
        public event Action<TcpClient> OnClientConnected; // Event triggered when a client connects

        public bool HasClients => ConnectedClients.Count > 0; // Check if there are any connected clients

        // Start the server
        public async Task StartAsync()
        {
            try
            {
                _server = new TcpListener(IPAddress.Any, Port);
                _server.Start();
                Console.WriteLine("Server started... Waiting for clients.");
                await Task.Run(() => AcceptClientsAsync());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting server: {ex.Message}");
            }
        }

        // Accept incoming clients
        private async Task AcceptClientsAsync()
        {
            while (true)
            {
                var client = await _server.AcceptTcpClientAsync();
                ConnectedClients.Add(client);
                OnClientConnected?.Invoke(client); // Notify GameData when a client connects
                Console.WriteLine("Client connected.");
                _ = Task.Run(() => ReceiveDataAsync(client));
            }
        }

        // Receive data from a connected client
        private async Task ReceiveDataAsync(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                OnMessageReceived?.Invoke(message,client); // Pass the message without specifying the client
            }

            Console.WriteLine("Client disconnected.");
            ConnectedClients.Remove(client);
            client.Close();
        }

        // Broadcast a message to all connected clients
        public async Task BroadcastMessageAsync(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);

            foreach (TcpClient client in ConnectedClients)
            {
                if (client.Connected)
                {
                    try
                    {
                        await client.GetStream().WriteAsync(data, 0, data.Length);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending to client: {ex.Message}");
                    }
                }
            }
        }
    }
}
