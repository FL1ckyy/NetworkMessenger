using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ChatClient
{
    public partial class Form1 : Form
    {
        private Client _client;
        private bool _isConnected;
        private string _username;

        private Label lblIP;
        private TextBox txtIP;
        private Label lblPort;
        private TextBox txtPort;
        private Label lblUsername;
        private TextBox txtUsername;
        private Button btnConnect;
        private Button btnDisconnect;
        private TextBox txtMessage;
        private Button btnSend;
        private TextBox txtChat;
        private ListBox lstUsers;

        public Form1()
        {
            InitializeComponent();
            InitializeForm();
        }

        private void InitializeForm()
        {
            this.Text = "Клиент чата";
            this.Size = new Size(700, 500);
            this.FormClosing += Form1_FormClosing;

            Panel pnlConnection = new Panel();
            pnlConnection.Dock = DockStyle.Top;
            pnlConnection.Height = 50;
            pnlConnection.BackColor = Color.LightBlue;

            int x = 10;
            lblIP = new Label();
            lblIP.Text = "IP сервера:";
            lblIP.Location = new Point(x, 15);
            lblIP.AutoSize = true;
            x += lblIP.Width + 5;

            txtIP = new TextBox();
            txtIP.Text = "127.0.0.1";
            txtIP.Location = new Point(x, 12);
            txtIP.Size = new Size(100, 20);
            x += txtIP.Width + 10;

            lblPort = new Label();
            lblPort.Text = "Порт:";
            lblPort.Location = new Point(x, 15);
            lblPort.AutoSize = true;
            x += lblPort.Width + 5;

            txtPort = new TextBox();
            txtPort.Text = "5000";
            txtPort.Location = new Point(x, 12);
            txtPort.Size = new Size(50, 20);
            x += txtPort.Width + 10;

            lblUsername = new Label();
            lblUsername.Text = "Имя пользователя:";
            lblUsername.Location = new Point(x, 15);
            lblUsername.AutoSize = true;
            x += lblUsername.Width + 5;

            txtUsername = new TextBox();
            txtUsername.Text = $"Пользователь{new Random().Next(1000)}";
            txtUsername.Location = new Point(x, 12);
            txtUsername.Size = new Size(100, 20);
            x += txtUsername.Width + 10;

            btnConnect = new Button();
            btnConnect.Text = "Подключиться";
            btnConnect.Location = new Point(x, 10);
            btnConnect.Size = new Size(100, 30);
            btnConnect.Click += btnConnect_Click;
            x += btnConnect.Width + 10;

            btnDisconnect = new Button();
            btnDisconnect.Text = "Отключиться";
            btnDisconnect.Location = new Point(x, 10);
            btnDisconnect.Size = new Size(100, 30);
            btnDisconnect.Click += btnDisconnect_Click;
            btnDisconnect.Enabled = false;

            pnlConnection.Controls.AddRange(new Control[] { lblIP, txtIP, lblPort, txtPort, lblUsername, txtUsername, btnConnect, btnDisconnect });

            Panel pnlMessage = new Panel();
            pnlMessage.Dock = DockStyle.Bottom;
            pnlMessage.Height = 40;
            pnlMessage.BackColor = Color.LightGray;

            txtMessage = new TextBox();
            txtMessage.Dock = DockStyle.Fill;
            txtMessage.Multiline = true;
            txtMessage.Height = 30;
            txtMessage.Margin = new Padding(5);
            txtMessage.Enabled = false;
            txtMessage.KeyPress += txtMessage_KeyPress;

            btnSend = new Button();
            btnSend.Text = "Отправить";
            btnSend.Dock = DockStyle.Right;
            btnSend.Width = 100;
            btnSend.Enabled = false;
            btnSend.Click += btnSend_Click;

            pnlMessage.Controls.Add(txtMessage);
            pnlMessage.Controls.Add(btnSend);

            SplitContainer splitContainer = new SplitContainer();
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.SplitterDistance = 500;

            txtChat = new TextBox();
            txtChat.Multiline = true;
            txtChat.ScrollBars = ScrollBars.Vertical;
            txtChat.ReadOnly = true;
            txtChat.Dock = DockStyle.Fill;

            Panel pnlUsers = new Panel();
            pnlUsers.Dock = DockStyle.Fill;

            Label lblUsers = new Label();
            lblUsers.Text = "Онлайн пользователи:";
            lblUsers.Dock = DockStyle.Top;
            lblUsers.Height = 20;

            lstUsers = new ListBox();
            lstUsers.Dock = DockStyle.Fill;

            pnlUsers.Controls.Add(lstUsers);
            pnlUsers.Controls.Add(lblUsers);

            splitContainer.Panel1.Controls.Add(txtChat);
            splitContainer.Panel2.Controls.Add(pnlUsers);

            this.Controls.Add(splitContainer);
            this.Controls.Add(pnlMessage);
            this.Controls.Add(pnlConnection);
        }

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtUsername.Text))
            {
                MessageBox.Show("Введите имя пользователя");
                return;
            }

            _username = txtUsername.Text;
            _client = new Client();

            // Подписываемся на события
            _client.MessageReceived += UpdateChat;
            _client.UsersUpdated += UpdateUserList;
            _client.ConnectionStatusChanged += UpdateConnectionStatus;

            bool connected = await _client.Connect(
                txtIP.Text,
                int.Parse(txtPort.Text),
                _username);

            if (connected)
            {
                _isConnected = true;
                txtIP.Enabled = false;
                txtPort.Enabled = false;
                txtUsername.Enabled = false;
                btnConnect.Enabled = false;
                btnDisconnect.Enabled = true;
                txtMessage.Enabled = true;
                btnSend.Enabled = true;
                txtMessage.Focus();
            }
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            if (_isConnected)
            {
                _client?.Disconnect();
                _isConnected = false;

                txtIP.Enabled = true;
                txtPort.Enabled = true;
                txtUsername.Enabled = true;
                btnConnect.Enabled = true;
                btnDisconnect.Enabled = false;
                txtMessage.Enabled = false;
                btnSend.Enabled = false;

                lstUsers.Items.Clear();
            }
        }

        private async void btnSend_Click(object sender, EventArgs e)
        {
            if (!_isConnected || string.IsNullOrWhiteSpace(txtMessage.Text))
                return;

            Message message = new Message(_username, txtMessage.Text);
            await _client.SendMessageAsync(message);

            UpdateChat(message.ToString());
            txtMessage.Clear();
            txtMessage.Focus();
        }

        private void UpdateChat(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(UpdateChat), message);
                return;
            }

            txtChat.AppendText(message + Environment.NewLine);
            txtChat.ScrollToCaret();
        }

        private void UpdateUserList(List<string> users)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<List<string>>(UpdateUserList), users);
                return;
            }

            // Отладка
            string debugInfo = $"Получен список пользователей ({users.Count}): {string.Join(", ", users)}";
            txtChat.AppendText($"[DEBUG] {debugInfo}{Environment.NewLine}");

            lstUsers.Items.Clear();
            foreach (var user in users)
            {
                if (user != _username && !string.IsNullOrEmpty(user))
                {
                    lstUsers.Items.Add(user);
                }
            }

            // Отладка: показываем сколько пользователей добавлено
            txtChat.AppendText($"[DEBUG] В список добавлено {lstUsers.Items.Count} пользователей{Environment.NewLine}");
        }

        private void UpdateConnectionStatus(string status)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(UpdateConnectionStatus), status);
                return;
            }

            txtChat.AppendText($"=== {status} ==={Environment.NewLine}");
        }

        private void txtMessage_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter && _isConnected)
            {
                btnSend_Click(sender, e);
                e.Handled = true;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isConnected)
            {
                _client?.Disconnect();
            }
        }
    }
}