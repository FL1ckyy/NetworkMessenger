using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Common;

namespace ChatServer
{
    public class Server
    {
        private TcpListener _listener;
        private List<ClientHandler> _clients;
        private bool _isRunning;

        public event Action<string> LogMessage;
        public event Action<List<string>> UsersUpdated;

        public Server()
        {
            _clients = new List<ClientHandler>();
        }

        public void Start(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _isRunning = true;

            LogMessage?.Invoke($"Server started on port {port}");

            Task.Run(() => AcceptClientsAsync());
        }

        private async Task AcceptClientsAsync()
        {
            while (_isRunning)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    ClientHandler handler = new ClientHandler(client, this);
                    _clients.Add(handler);
                    handler.StartListening();

                    LogMessage?.Invoke($"New client connected: {client.Client.RemoteEndPoint}");
                    UpdateUsersList();
                }
                catch (Exception ex)
                {
                    LogMessage?.Invoke($"Error accepting client: {ex.Message}");
                }
            }
        }

        public void BroadcastMessage(Message message, string senderId)
        {
            string messageJson = message.ToJson();

            foreach (var client in _clients)
            {
                if (client.ClientId != senderId)
                {
                    client.SendMessage(messageJson);
                }
            }

            LogMessage?.Invoke($"{message}");
        }

        public void RemoveClient(ClientHandler client)
        {
            _clients.Remove(client);
            UpdateUsersList();
            LogMessage?.Invoke($"Client disconnected: {client.ClientId}");
        }

        private void UpdateUsersList()
        {
            List<string> users = new List<string>();
            foreach (var client in _clients)
            {
                if (!string.IsNullOrEmpty(client.ClientId))
                {
                    users.Add(client.ClientId);
                }
            }
            UsersUpdated?.Invoke(users);

            foreach (var client in _clients)
            {
                client.SendUserList(users);
            }
        }

        public List<ClientHandler> GetClients()
        {
            return _clients;
        }

        public void Stop()
        {
            _isRunning = false;
            foreach (var client in _clients)
            {
                client.Disconnect();
            }
            _clients.Clear();
            _listener?.Stop();
            LogMessage?.Invoke("Server stopped");
        }
    }
}