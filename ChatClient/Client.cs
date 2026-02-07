using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatClient
{
    public class Client
    {
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private bool _isConnected;
        private string _username;

        public event Action<string> MessageReceived;
        public event Action<List<string>> UsersUpdated;
        public event Action<string> ConnectionStatusChanged;

        public bool IsConnected => _isConnected;
        public string Username => _username;

        public async Task<bool> Connect(string ip, int port, string username)
        {
            try
            {
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(ip, port);
                _stream = _tcpClient.GetStream();
                _isConnected = true;
                _username = username;

                Message registerMessage = new Message(username, "присоединился к чату");
                string json = registerMessage.ToJson();
                byte[] data = Encoding.UTF8.GetBytes(json + "\n");
                await _stream.WriteAsync(data, 0, data.Length);

                Task.Run(() => ListenAsync());

                OnConnectionStatusChanged($"Подключен к {ip}:{port}");

                return true;
            }
            catch (Exception ex)
            {
                OnConnectionStatusChanged($"Ошибка подключения: {ex.Message}");

                return false;
            }
        }

        private async Task ListenAsync()
        {
            byte[] buffer = new byte[4096];
            StringBuilder messageBuilder = new StringBuilder();

            try
            {
                while (_isConnected)
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        Disconnect();
                        break;
                    }

                    string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    messageBuilder.Append(receivedData);

                    string allData = messageBuilder.ToString();

                    while (true)
                    {
                        int newLineIndex = allData.IndexOf('\n');
                        if (newLineIndex >= 0)
                        {
                            string messageJson = allData.Substring(0, newLineIndex).Trim();
                            if (!string.IsNullOrEmpty(messageJson))
                            {
                                ProcessReceivedMessage(messageJson);
                            }

                         
                            allData = allData.Substring(newLineIndex + 1);
                            messageBuilder = new StringBuilder(allData);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Disconnect();
                OnConnectionStatusChanged($"Соединение потеряно: {ex.Message}");
            }
        }

        private void ProcessReceivedMessage(string messageJson)
        {
            try
            {
               
                if (messageJson.StartsWith("USERSLIST:"))
                {
                    string usersStr = messageJson.Substring(10);
                    var users = new List<string>(usersStr.Split(','));

                    users.RemoveAll(string.IsNullOrEmpty);

                    OnUsersUpdated(users);
                    return;
                }

              
                Message message = Message.FromJson(messageJson);
                OnMessageReceived(message.ToString());
            }
            catch (Exception ex)
            {
                OnConnectionStatusChanged($"Ошибка обработки сообщения: {ex.Message}");
            }
        }

        public async Task SendMessageAsync(Message message)
        {
            if (!_isConnected) return;

            try
            {
                string json = message.ToJson();
                byte[] data = Encoding.UTF8.GetBytes(json + "\n");
                await _stream.WriteAsync(data, 0, data.Length);
            }
            catch
            {
                Disconnect();
            }
        }

        public void Disconnect()
        {
            if (!_isConnected) return;

            _isConnected = false;
            _stream?.Close();
            _tcpClient?.Close();

            OnConnectionStatusChanged("Отключен");
        }

        public void OnMessageReceived(string message)
        {
            MessageReceived?.Invoke(message);
        }

        public void OnUsersUpdated(List<string> users)
        {
            UsersUpdated?.Invoke(users);
        }

        public void OnConnectionStatusChanged(string status)
        {
            ConnectionStatusChanged?.Invoke(status);
        }
    }
}