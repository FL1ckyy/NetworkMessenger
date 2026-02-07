using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ChatServer
{
    public partial class Form1 : Form
    {
        private Server _server;
        private bool _isRunning;

        private Label lblPort;
        private TextBox txtPort;
        private Button btnStart;
        private Button btnStop;
        private TextBox txtChat;
        private ListBox lstUsers;

        public Form1()
        {
            InitializeComponent();
            InitializeForm();
        }

        private void InitializeForm()
        {
            this.Text = "Chat Server";
            this.Size = new Size(600, 500);
            this.FormClosing += Form1_FormClosing;

            Panel pnlControls = new Panel();
            pnlControls.Dock = DockStyle.Top;
            pnlControls.Height = 50;
            pnlControls.BackColor = Color.LightGray;

            lblPort = new Label();
            lblPort.Text = "Port:";
            lblPort.Location = new Point(10, 15);
            lblPort.AutoSize = true;

            txtPort = new TextBox();
            txtPort.Text = "5000";
            txtPort.Location = new Point(50, 12);
            txtPort.Size = new Size(80, 20);

            btnStart = new Button();
            btnStart.Text = "Start Server";
            btnStart.Location = new Point(150, 10);
            btnStart.Size = new Size(100, 30);
            btnStart.Click += btnStart_Click;

            btnStop = new Button();
            btnStop.Text = "Stop Server";
            btnStop.Location = new Point(260, 10);
            btnStop.Size = new Size(100, 30);
            btnStop.Click += btnStop_Click;
            btnStop.Enabled = false;

            pnlControls.Controls.AddRange(new Control[] { lblPort, txtPort, btnStart, btnStop });

            SplitContainer splitContainer = new SplitContainer();
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.SplitterDistance = 400;

            txtChat = new TextBox();
            txtChat.Multiline = true;
            txtChat.ScrollBars = ScrollBars.Vertical;
            txtChat.ReadOnly = true;
            txtChat.Dock = DockStyle.Fill;

            Panel pnlUsers = new Panel();
            pnlUsers.Dock = DockStyle.Fill;

            Label lblUsers = new Label();
            lblUsers.Text = "Connected Users:";
            lblUsers.Dock = DockStyle.Top;
            lblUsers.Height = 20;

            lstUsers = new ListBox();
            lstUsers.Dock = DockStyle.Fill;

            pnlUsers.Controls.Add(lstUsers);
            pnlUsers.Controls.Add(lblUsers);

            splitContainer.Panel1.Controls.Add(txtChat);
            splitContainer.Panel2.Controls.Add(pnlUsers);

            this.Controls.Add(splitContainer);
            this.Controls.Add(pnlControls);
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (int.TryParse(txtPort.Text, out int port))
            {
                _server = new Server();
                _server.LogMessage += UpdateChat;
                _server.UsersUpdated += UpdateUserList;

                _server.Start(port);
                _isRunning = true;
                btnStart.Enabled = false;
                btnStop.Enabled = true;
                txtPort.Enabled = false;
            }
            else
            {
                MessageBox.Show("Please enter a valid port number");
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (_isRunning)
            {
                _server?.Stop();
                _isRunning = false;
                btnStart.Enabled = true;
                btnStop.Enabled = false;
                txtPort.Enabled = true;
            }
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

            lstUsers.Items.Clear();
            foreach (var user in users)
            {
                lstUsers.Items.Add(user);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isRunning)
            {
                _server?.Stop();
            }
        }
    }
}