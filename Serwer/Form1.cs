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
        bool flaga = false;
        MySqlConnection connection = new MySqlConnection("server=db4free.net;port=3306;username=siatek;password=projektps;database=projektps");
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
            //pobranie danych csv
            string term_file = @"C:\Users\mbuko\source\repos\PROJEKT PS SERWER\bin\Debug\netcoreapp3.1\England-Championship-fixture-2021-2022.csv";
            pobierz_wyniki();
            if (!(File.Exists(term_file)))
            {
                pobierz_terminarz();
            }
            else
            {
                var path = term_file; // Habeeb, "Dubai Media City, Dubai"
                wyswietl_csv(term_file);
            }
            db_connect();
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

                    MySqlDataAdapter adapter = new MySqlDataAdapter();
                    MySqlCommand command = new MySqlCommand("select * from `users` where username ='" + json.Username.ToString() + "' and password = '" + json.Password.ToString() + "'", db.getConnection());

                    command.Parameters.Add("@usn", MySqlDbType.VarChar).Value = json.Username.ToString();
                    command.Parameters.Add("@pass", MySqlDbType.VarChar).Value = json.Password.ToString();

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
            string remoteUri = "https://cdn.bettingexpert.com/assets/";
            string fileName = "England-Championship-fixture-2021-2022.csv", myStringWebResource = null;
            string filePath;
            // Create a new WebClient instance.
            using (WebClient myWebClient = new WebClient())
            {
                myStringWebResource = remoteUri + fileName;
                Komunikaty.Items.Add("Downloading File" + fileName + "  from " + myStringWebResource + " .......\n\n");
                // Download the Web resource and save it into the current filesystem folder.
                myWebClient.DownloadFile(myStringWebResource, fileName);
                //Komunikaty.Items.Add("Successfully Downloaded File " + fileName + " from: " + myStringWebResource);
                //Komunikaty.Items.Add("\nDownloaded file saved in the following file system folder:\n\t" + Application.StartupPath);
                var path = Application.StartupPath + @"\"+  fileName; // Habeeb, "Dubai Media City, Dubai"
                Komunikaty.Items.Add("Path: " + path);
                filePath = path;
            }
            return filePath;
        }
        public string pobierz_wyniki()
        {
            string remoteUri = "https://www.football-data.co.uk/mmz4281/2122/";
            string fileName = "E0.csv", myStringWebResource = null;
            string filePath;
            // Create a new WebClient instance.
            using (WebClient myWebClient = new WebClient())
            {
                // Concatenate the domain with the Web resource filename.
                myStringWebResource = remoteUri + fileName;
                Komunikaty.Items.Add("Downloading File" + fileName + "  from " + myStringWebResource + " .......\n\n");
                // Download the Web resource and save it into the current filesystem folder.
                myWebClient.DownloadFile(myStringWebResource, fileName);
                Komunikaty.Items.Add("Successfully Downloaded File " + fileName + " from: " + myStringWebResource);
                Komunikaty.Items.Add("\nDownloaded file saved in the following file system folder:\n\t" + Application.StartupPath);


                var path = Application.StartupPath + fileName; // Habeeb, "Dubai Media City, Dubai"
                Komunikaty.Items.Add(path);
                filePath = path;
            }
            return filePath;
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
            MySqlConnection connection = new MySqlConnection("server=db4free.net;port=3306;username=siatek;password=projektps;database=projektps");
        }
        public void db_send()
        {

        }
    }
}
