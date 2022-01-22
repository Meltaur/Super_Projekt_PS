using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Net;
using System.Net.Sockets;
using System.Text.Json;


namespace Klient
{
    public partial class Login : Form
    {

        private TcpClient klient;
        private NetworkStream netStream;

        public Login()
        {
            InitializeComponent();
        }

        public class LoginData
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }
        private void label4_Click(object sender, EventArgs e)
        {
            this.Close();
            System.Windows.Forms.Application.Exit();
        }

        private void label4_MouseMove(object sender, MouseEventArgs e)
        {
            label4.ForeColor = Color.White;
        }

        private void label4_MouseLeave(object sender, EventArgs e)
        {
            label4.ForeColor = Color.Black;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                textBoxPassword.PasswordChar = '\0';

            }
            else
                textBoxPassword.PasswordChar = '•';
        }

        private void label5_Click(object sender, EventArgs e)
        {
            new Register().Show();
            this.Hide();
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textBoxUsername_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBoxPassword_TextChanged(object sender, EventArgs e)
        {

        }

        private async void buttonLogin_Click(object sender, EventArgs e)
        {
            try
            {
                klient = new TcpClient("127.0.0.1", Convert.ToInt32("11000"));
                IPEndPoint IP = (IPEndPoint)klient.Client.RemoteEndPoint;
                var userData = new LoginData
                {
                    Username = textBoxUsername.Text,
                    Password = textBoxPassword.Text
                };
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(JsonSerializer.Serialize(userData));
                netStream = klient.GetStream();
                netStream.Write(data, 0, data.Length);
                netStream.Flush();
                netStream = klient.GetStream();
                if (netStream.CanRead)
                {
                    byte[] bytes = new byte[klient.ReceiveBufferSize];
                    if (klient.Connected)
                        await netStream.ReadAsync(bytes, 0, (int)klient.ReceiveBufferSize);

                    string returnData = Encoding.UTF8.GetString(bytes.ToArray());
                    returnData = returnData.Replace("\0", string.Empty);
                    if (returnData.Equals("Correct"))
                    {
                        this.Hide();
                        System.Windows.Forms.Application.Exit();
                    }
                    else
                    {
                        MessageBox.Show("incorrect username or password", "login failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                klient.Close();
                netStream.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Błąd");
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void label5_MouseMove(object sender, MouseEventArgs e)
        {
            label5.ForeColor = Color.AliceBlue;
        }

        private void label5_MouseLeave(object sender, EventArgs e)
        {
            label5.ForeColor = Color.Black;
        }

    }
}
