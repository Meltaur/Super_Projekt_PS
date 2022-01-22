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
using Newtonsoft.Json.Linq;
using MySql.Data.MySqlClient;
using System.IO;
using Microsoft.VisualBasic.FileIO;


namespace Serwer
{
    public partial class Form1 : Form
    {
        private TcpListener server;
        private TcpClient klient;
        private NetworkStream netStream;
        private delegate void Update(string s);
        MySqlDataAdapter adapter;
        MySqlConnection conn;
        bool flaga = false;
        //MySqlConnection connection = new MySqlConnection("server=db4free.net;port=3306;username=siatek;password=projektps;database=projektps");
        public Form1()
        {
            InitializeComponent();
        }

        async private void Start_Click(object sender, EventArgs e)
        {
            //tworzenie serwera
            try
            {
                server = new TcpListener(IPAddress.Parse("127.0.0.1"), Convert.ToInt32("11000"));
                server.Start();
                setText("Serwer rozpoczął pracę");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Błąd");
            }
            // połączenie do sql
            db_connect();

            //pobranie danych csv i wgranie danych do db
            string term_file = @"C:\Users\mbuko\source\repos\PROJEKT PS SERWER\bin\Debug\epl-2021-GMTStandardTime.csv";
            setText("pobieram najnowaszą baze danych...");
            string term_path = pobierz_terminarz();
            setText("uaktualniam baze danych...");
            update_terminarz_db(term_path);

            //tworzenie klienta
            klient = await server.AcceptTcpClientAsync();
            setText("Klient połączył się");

            //nasłuchiwanie wiadomości
            if (backgroundWorker1.IsBusy != true)
            {
                // Start the asynchronous operation.
                flaga = true;
                backgroundWorker1.RunWorkerAsync();
            }
        }

        private void setText(string text)
        {
            if (Komunikaty.InvokeRequired)
            {
                Invoke(new Update(setText), new object[] { text });
            }
            else
            {
                Komunikaty.Items.Add(text);
            }
        }

        private void Komunikaty_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private async void Form1_Load(object sender, EventArgs e)
        {

        }

        private async void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                IPEndPoint IP = (IPEndPoint)klient.Client.RemoteEndPoint;
                setText("Klient połączył się");
                netStream = klient.GetStream();
                if (netStream.CanRead)
                {
                    byte[] bytes = new byte[klient.ReceiveBufferSize];
                    if (klient.Connected)
                        await netStream.ReadAsync(bytes, 0, (int)klient.ReceiveBufferSize);

                    string returnData = Encoding.UTF8.GetString(bytes.ToArray());
                    dynamic json = JObject.Parse(returnData);
                    DB db = new DB();

                    DataTable table = new DataTable();

                    MySqlCommand command = new MySqlCommand("select * from `users` where username ='" + json.Username.ToString() + "' and password = '" + json.Password.ToString() + "'", db.getConnection());

                    command.Parameters.Add("@usn", MySqlDbType.VarChar).Value = json.Username.ToString();
                    command.Parameters.Add("@pass", MySqlDbType.VarChar).Value = json.Password.ToString();
                    adapter = new MySqlDataAdapter();
                    adapter.SelectCommand = command;

                    adapter.Fill(table);
                    netStream.Flush();
                    if (table.Rows.Count > 0)
                    {
                        string message = "Correct";
                        Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
                        netStream = klient.GetStream();
                        netStream.Write(data, 0, data.Length);
                    }
                    else
                    {
                        string message = "Error";
                        Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
                        netStream = klient.GetStream();
                        netStream.Write(data, 0, data.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Błąd");
            }
        }
        public string pobierz_terminarz()
        {
            string remoteUri = "https://fixturedownload.com/download/";
            string fileName = "epl-2021-GMTStandardTime.csv", myStringWebResource = null;
            string filePath;
            // Create a new WebClient instance.
            using (WebClient myWebClient = new WebClient())
            {
                myStringWebResource = remoteUri + fileName;
                Komunikaty.Items.Add("Downloading File" + fileName + "  from " + myStringWebResource + " .......\n\n");
                // Download the Web resource and save it into the current filesystem folder.
                myWebClient.DownloadFile(myStringWebResource, fileName);
                Komunikaty.Items.Add("Successfully Downloaded File " + fileName + " from: " + myStringWebResource);
                Komunikaty.Items.Add("\nDownloaded file saved in the following file system folder:\n\t" + Application.StartupPath);
                var path = Application.StartupPath + @"\"+  fileName; // Habeeb, "Dubai Media City, Dubai"
                Komunikaty.Items.Add("Path: " + path);
                filePath = path;
            }
            return filePath;
        }
        private void zapisz_terminarz_db(string path)
        {
            string Date;
            try
            {
               string sql_add_term = "CREATE TABLE terminarz (" +
                    "MatchNumber varchar(255), " +
                    "RoundNumber varchar(255)," +
                    "Date varchar(255), " +
                    "HomeTeam varchar(255)," +
                    "AwayTeam varchar(255)," +
                    "Result varchar(255))";

                MySqlCommand cmd_add_term = new MySqlCommand(sql_add_term, conn);
                cmd_add_term.ExecuteNonQuery();
                using (TextFieldParser csvParser = new TextFieldParser(path))
                {
                    csvParser.CommentTokens = new string[] { "#" };
                    csvParser.SetDelimiters(new string[] { "," });
                    csvParser.HasFieldsEnclosedInQuotes = true;
                    
                    // Skip the row with the column names
                    csvParser.ReadLine();
                    while (!csvParser.EndOfData)
                     {
                        // Read current line fields, pointer moves to the next line.
                        string fields = csvParser.ReadLine();
                        var values = fields.Split(',');
                        string sql = "INSERT INTO terminarz VALUES('" + values[0].ToString() + "','" + values[1].ToString() + "','" + values[2].ToString() + "','" + values[4].ToString() +  "','" + values[5].ToString() + "','" + values[6].ToString() +"')";

                        MySqlCommand cmd = new MySqlCommand(sql, conn);
                        cmd.ExecuteNonQuery();
                    }

                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString() + "\n");
            }
            Komunikaty.Items.Add("dodano terminarz do DB");
        }
        private void update_terminarz_db(string path)
        {
            string Date;
            try
            {
                using (TextFieldParser csvParser = new TextFieldParser(path))
                {
                    csvParser.CommentTokens = new string[] { "#" };
                    csvParser.SetDelimiters(new string[] { "," });
                    csvParser.HasFieldsEnclosedInQuotes = true;

                    // Skip the row with the column names
                    csvParser.ReadLine();
                    while (!csvParser.EndOfData)
                    {
                        // Read current line fields, pointer moves to the next line.
                        string fields = csvParser.ReadLine();
                        var values = fields.Split(',');
                        string sql = "UPDATE terminarz " +
                            "SET " +
                    "RoundNumber  = '" + values[1].ToString() + "'" +
                    ", Date  = '" + values[2].ToString() + "'" +
                    ", HomeTeam = '" + values[4].ToString() + "'" +
                    ", AwayTeam = '" + values[5].ToString() + "'" +
                    ", Result = '" + values[6].ToString() + "'" +
                            " WHERE MatchNumber = '" + values[0].ToString() + "'";


                        MySqlCommand cmd = new MySqlCommand(sql, conn);
                        cmd.ExecuteNonQuery();
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString() + "\n");
            }
            Komunikaty.Items.Add("baza uaktualniona");
        }
        public void wyswietl_csv(string path)
        {
            using (TextFieldParser csvParser = new TextFieldParser(path))
            {
                csvParser.CommentTokens = new string[] { "#" };
                csvParser.SetDelimiters(new string[] { "," });
                csvParser.HasFieldsEnclosedInQuotes = true;

                // Skip the row with the column names
                csvParser.ReadLine();

                while (!csvParser.EndOfData)
                {
                    // Read current line fields, pointer moves to the next line.
                    string[] fields = csvParser.ReadFields();
                    string Date = fields[1];
                    Komunikaty.Items.Add(Date);
                }
            }
        }
        public void db_connect()
        {

            //MySqlConnection connection = new MySqlConnection("server=db4free.net;port=3306;username=siatek;password=projektps;database=projektps");
            try
            {
                string connStr = "server=db4free.net;port=3306;username=siatek;password=projektps;database=projektps";
                conn = new MySqlConnection(connStr);
                try
                {
                    Komunikaty.Items.Add("Connecting to MySQL...");
                    conn.Open();

                }
                catch (Exception ee)
                {
                    Komunikaty.Items.Add("error \n");
                    Komunikaty.Items.Add(ee.ToString());
                }
            }
            catch (Exception ex)
            {
                Komunikaty.Items.Add(ex);
            }
        }
        public void db_close(MySqlConnection conn)
        {
            conn.Close();
        }
        public void db_send()
        {

        }
    }
}
