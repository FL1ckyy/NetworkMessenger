using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

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

            OnLogMessage($"Сервер запущен на порту {port}");

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

                    OnLogMessage($"Новый клиент подключен: {client.Client.RemoteEndPoint}");

                    UpdateUsersList();
                }
                catch (Exception ex)
                {
                    OnLogMessage($"Ошибка при подключении клиента: {ex.Message}");
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

            OnLogMessage($"{message}");
        }

        public void RemoveClient(ClientHandler client)
        {
            _clients.Remove(client);
            UpdateUsersList();

            OnLogMessage($"Клиент отключен: {client.ClientId}");
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

            OnUsersUpdated(users);

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

            OnLogMessage("Сервер остановлен");
        }

        public void OnLogMessage(string message)
        {
            LogMessage?.Invoke(message);
        }

        public void OnUsersUpdated(List<string> users)
        {
            UsersUpdated?.Invoke(users);
        }
        public List<string> GetUserList()
        {
            List<string> users = new List<string>();
            foreach (var client in _clients)
            {
                if (!string.IsNullOrEmpty(client.ClientId))
                {
                    users.Add(client.ClientId);
                }
            }
            return users;
        }
    }

}