using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace Klient
{
    public partial class Register : Form
    {
        public Register()
        {
            InitializeComponent();
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

        private void label8_Click(object sender, EventArgs e)
        {
            new Login().Show();
            this.Hide();
        }

        private void label8_MouseMove(object sender, MouseEventArgs e)
        {
            label8.ForeColor = Color.AliceBlue;
        }

        private void label8_MouseLeave(object sender, EventArgs e)
        {
            label8.ForeColor = Color.Black;
        }



        private void textBoxUsername_Enter(object sender, EventArgs e)
        {
            String username = textBoxUsername.Text;
            if (username.ToLower().Trim().Equals("username"))
            {
                textBoxUsername.Text = "";
                textBoxUsername.ForeColor = Color.Black;
            }
        }

        private void textBoxUsername_Leave(object sender, EventArgs e)
        {
            String username = textBoxUsername.Text;
            if (username.ToLower().Trim().Equals("username") || username.Trim().Equals(""))
            {
                textBoxUsername.Text = "username";
                textBoxUsername.ForeColor = Color.LightSlateGray;
            }
        }

        private void textBoxEMail_Enter(object sender, EventArgs e)
        {
            String email = textBoxEMail.Text;
            if (email.ToLower().Trim().Equals("email"))
            {
                textBoxEMail.Text = "";
                textBoxEMail.ForeColor = Color.Black;
            }
        }

        private void textBoxEMail_Leave(object sender, EventArgs e)
        {
            String email = textBoxEMail.Text;
            if (email.ToLower().Trim().Equals("email") || email.Trim().Equals(""))
            {
                textBoxEMail.Text = "email";
                textBoxEMail.ForeColor = Color.LightSlateGray;
            }
        }

        private void textBoxConfirm_Enter(object sender, EventArgs e)
        {
            String pass = textBoxConfirm.Text;
            if (pass.ToLower().Trim().Equals("password"))
            {
                textBoxConfirm.Text = "";
                textBoxConfirm.ForeColor = Color.Black;
            }
        }

        private void textBoxConfirm_Leave(object sender, EventArgs e)
        {
            String pass = textBoxConfirm.Text;
            if (pass.ToLower().Trim().Equals("password") || pass.Trim().Equals(""))
            {
                textBoxConfirm.Text = "password";
                textBoxConfirm.ForeColor = Color.LightSlateGray;
            }
        }

        private void textBoxPassword_Enter(object sender, EventArgs e)
        {
            String pass = textBoxPassword.Text;
            if (pass.ToLower().Trim().Equals("password"))
            {
                textBoxPassword.Text = "";
                textBoxPassword.ForeColor = Color.Black;
            }
        }

        private void textBoxPassword_Leave(object sender, EventArgs e)
        {
            String pass = textBoxPassword.Text;
            if (pass.ToLower().Trim().Equals("password") || pass.Trim().Equals(""))
            {
                textBoxPassword.Text = "password";
                textBoxPassword.ForeColor = Color.LightSlateGray;
            }
        }

        private void buttonRegister_Click(object sender, EventArgs e)
        {
            //new user
            DB db = new DB();
            MySqlCommand command = new MySqlCommand("INSERT INTO users (username, password, email) values ('" + textBoxUsername.Text + "', '" + textBoxPassword.Text + "', '" + textBoxEMail.Text + "' )", db.getConnection());

            command.Parameters.Add("@usn", MySqlDbType.VarChar).Value = textBoxUsername.Text;
            command.Parameters.Add("@pass", MySqlDbType.VarChar).Value = textBoxPassword.Text;
            command.Parameters.Add("@email", MySqlDbType.VarChar).Value = textBoxEMail.Text;

            db.otworzPolaczenie();

            //zapytanie do bazy danych

            // czy textboxy zawieraja domyslne dane
            if (!SprawdzTextBoxy())
            {
                //czy user sie powtarza w bazie
                if (SprawdzDuplikaty())
                {
                    MessageBox.Show("This username already exists, try a different one!", "Username", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                //czy email sie powtarza w bazie
                else if (SprawdzDuplikaty2())
                {
                    MessageBox.Show("This email is already registered, use a different one!", "Email", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                //czy hasla sie zgadzaja
                else if (!SprawdzHasla())
                {
                    MessageBox.Show("Passwords do not match!", "Passwords", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    if (command.ExecuteNonQuery() == 1)
                    {
                        MessageBox.Show("Succesfully created an account", "Account", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Failure", "Account", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                //niekomoletne dane
                MessageBox.Show("Fill the form", "Form incomplete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            db.zamknijPolaczenie();

        }

        public Boolean SprawdzDuplikaty()
        {
            DB db = new DB();

            String username = textBoxUsername.Text;

            DataTable table = new DataTable();

            MySqlDataAdapter adapter = new MySqlDataAdapter();
            MySqlCommand command = new MySqlCommand("SELECT * FROM `users` WHERE username ='" + username + "'", db.getConnection());

            command.Parameters.Add("@usn", MySqlDbType.VarChar).Value = username;

            adapter.SelectCommand = command;

            adapter.Fill(table);

            //czy taki username istnieje
            if (table.Rows.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean SprawdzDuplikaty2()
        {
            DB db = new DB();

            String email = textBoxEMail.Text;

            DataTable table = new DataTable();

            MySqlDataAdapter adapter = new MySqlDataAdapter();
            MySqlCommand command = new MySqlCommand("SELECT * FROM `users` WHERE email ='" + email + "'", db.getConnection());

            command.Parameters.Add("@email", MySqlDbType.VarChar).Value = email;

            adapter.SelectCommand = command;

            adapter.Fill(table);

            //czy taki email istnieje
            if (table.Rows.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean SprawdzTextBoxy()
        {
            String uname = textBoxUsername.Text;
            String pass = textBoxPassword.Text;
            String email = textBoxEMail.Text;
            String conf = textBoxConfirm.Text;

            if (uname.Equals("username") || pass.Equals("password") || email.Equals("email") || conf.Equals("password"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean SprawdzHasla()
        {
            String pass = textBoxPassword.Text;
            String conf = textBoxConfirm.Text;

            if (textBoxPassword.Text.Equals(textBoxConfirm.Text))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
