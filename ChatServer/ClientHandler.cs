using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Common;

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
                    int lastNewLine = allData.LastIndexOf('\n');

                    if (lastNewLine >= 0)
                    {
                        string completeMessages = allData.Substring(0, lastNewLine);
                        string[] messages = completeMessages.Split('\n');

                        foreach (string messageJson in messages)
                        {
                            if (!string.IsNullOrEmpty(messageJson))
                            {
                                ProcessMessage(messageJson.Trim());
                            }
                        }

                        messageBuilder.Clear();
                        if (lastNewLine + 1 < allData.Length)
                        {
                            messageBuilder.Append(allData.Substring(lastNewLine + 1));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Disconnect();
                _server.LogMessage?.Invoke($"Client error: {ex.Message}");
            }
        }

        private void ProcessMessage(string messageJson)
        {
            try
            {
                Message message = Message.FromJson(messageJson);

                if (string.IsNullOrEmpty(ClientId))
                {
                    ClientId = message.Author;
                    _server.LogMessage?.Invoke($"User registered: {ClientId}");
                }
                else
                {
                    _server.BroadcastMessage(message, ClientId);
                }
            }
            catch (Exception ex)
            {
                _server.LogMessage?.Invoke($"Error processing message: {ex.Message}");
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
                string userListMessage = $"USERS:{string.Join(",", users)}";
                byte[] data = Encoding.UTF8.GetBytes(userListMessage + "\n");
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