using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer
{
    public class ClientHandler
    {
        private TcpClient _tcpClient;
        private NetworkStream _stream;
        private Server _server;
        private bool _isConnected;

        public string ClientId { get; private set; }

        public ClientHandler(TcpClient client, Server server)
        {
            _tcpClient = client;
            _stream = client.GetStream();
            _server = server;
            _isConnected = true;
        }

        public void StartListening()
        {
            Task.Run(() => ListenAsync());
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

                    // Ищем конец сообщения (новую строку)
                    while (true)
                    {
                        int newLineIndex = allData.IndexOf('\n');
                        if (newLineIndex >= 0)
                        {
                            string messageJson = allData.Substring(0, newLineIndex).Trim();
                            if (!string.IsNullOrEmpty(messageJson))
                            {
                                ProcessMessage(messageJson);
                            }

                            // Убираем обработанное сообщение
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
                _server.OnLogMessage($"Ошибка клиента: {ex.Message}");
            }
        }

        private void ProcessMessage(string messageJson)
        {
            try
            {
                // Проверяем, это обычное сообщение или системное (список пользователей)?
                if (messageJson.StartsWith("USERSLIST:"))
                {
                    // Это системное сообщение от сервера, игнорируем
                    return;
                }
                else if (messageJson.StartsWith("USERS:"))
                {
                    // Это системное сообщение от сервера, игнорируем
                    return;
                }

                Message message = Message.FromJson(messageJson);

                if (string.IsNullOrEmpty(ClientId))
                {
                    ClientId = message.Author;
                    _server.OnLogMessage($"Пользователь зарегистрирован: {ClientId}");

                    // После регистрации отправляем клиенту текущий список пользователей
                    var userList = _server.GetUserList();
                    SendUserList(userList);
                }
                else
                {
                    _server.BroadcastMessage(message, ClientId);
                }
            }
            catch (Exception ex)
            {
                _server.OnLogMessage($"Ошибка обработки сообщения: {ex.Message}");
            }
        }

        public void SendMessage(string messageJson)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(messageJson + "\n");
                _stream.Write(data, 0, data.Length);
            }
            catch
            {
                Disconnect();
            }
        }

        public void SendUserList(List<string> users)
        {
            try
            {
                string userListMessage = $"USERSLIST:{string.Join(",", users)}\n";
                byte[] data = Encoding.UTF8.GetBytes(userListMessage);
                _stream.Write(data, 0, data.Length);
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
            _server.RemoveClient(this);
        }
    }
}