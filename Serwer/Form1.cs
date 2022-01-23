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
using System.Text.RegularExpressions;

internal class MyTcpListener : TcpListener
{
    public MyTcpListener(IPEndPoint localEP) : base(localEP)
    {
    }

    public MyTcpListener(IPAddress localaddr, int port) : base(localaddr, port)
    {
    }

    public MyTcpListener(int port) : base(port)
    {
    }

    public new bool Active
    {
        get { return base.Active; }
    }
}
namespace Serwer
{

    public partial class Form1 : Form
    {
        private MyTcpListener server = new MyTcpListener(IPAddress.Parse("127.0.0.1"), Convert.ToInt32("11000"));
        private TcpClient klient = new TcpClient();
        private NetworkStream netStream;
        private delegate void Update(string s);
        MySqlDataAdapter adapter;
        MySqlConnection conn;
        bool flaga_s = true;
        bool flaga = true;
        //MySqlConnection connection = new MySqlConnection("server=db4free.net;port=3306;username=siatek;password=projektps;database=projektps");
        public class Ranking
        {
            public string User { get; set; }
            public int Points { get; set; }
        }
        public class Kolejka
        {
            public string MatchNumber { get; set; }
            public string RoundNumber { get; set; }
            public string Date { get; set; }

            public string HomeTeam { get; set; }
            public string AwayTeam { get; set; }
            public string Result { get; set; }
        }
        public  Form1()
        {
            InitializeComponent();
        }


        async private void Start_Click(object sender, EventArgs e)
        {
            //tworzenie serwera
            if (!server.Active)
            {
                try
                {
                    server.Start();
                    setText("Serwer rozpoczął pracę");
                    
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Błąd");
                }
                if (server.Server.Available > 0)
                {
                    MessageBox.Show("dupa");
                }
            }
            flaga_s = false;
            // połączenie do sql
            db_connect();

            //pobranie danych csv i wgranie danych do db
            string term_file = @"C:\Users\mbuko\source\repos\PROJEKT PS SERWER\bin\Debug\epl-2021-GMTStandardTime.csv";
            setText("pobieram najnowaszą baze danych...");
            string term_path = pobierz_terminarz();
            setText("uaktualniam baze danych...");
            //update_terminarz_db(term_path);

            //tworzenie klienta

            //nasłuchiwanie wiadomości
            if (backgroundWorker1.IsBusy != true)
            {
                // Start the asynchronous operation.
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
            while (server.Active)
            {
                if (flaga)
                {
                    klient = await server.AcceptTcpClientAsync();
                    setText("Klient połączył się");
                }
                flaga = false;
                while (klient.Connected)
                {
                    try
                    {
                        dynamic json = await getJson();
                        //MessageBox.Show(json.ToString());
                        if (json.Type.ToString().Equals("Login"))
                        {
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
                                message_send("Correct");
                            }
                            else
                            {
                                message_send("Error");
                            }
                        }
                        else if (json.Type.ToString().Equals("Register"))
                        {
                            DB db = new DB();
                            MySqlCommand command = new MySqlCommand("INSERT INTO users (username, password, email) values ('" + json.Username.ToString() + "', '" + json.Password.ToString() + "', '" + json.Email.ToString() + "' )", db.getConnection());

                            command.Parameters.Add("@usn", MySqlDbType.VarChar).Value = json.Username.ToString();
                            command.Parameters.Add("@pass", MySqlDbType.VarChar).Value = json.Password.ToString();
                            command.Parameters.Add("@email", MySqlDbType.VarChar).Value = json.Email.ToString();

                            db.otworzPolaczenie();
                            //czy user sie powtarza w bazie
                            if (SprawdzDuplikaty(json.Username.ToString()))
                            {
                                message_send("Username_Exists");
                            }
                            //czy email sie powtarza w bazie
                            else if (SprawdzDuplikaty2(json.Email.ToString()))
                            {
                                message_send("Email_Exists");
                            }
                            //czy hasla sie zgadzaja

                            else
                            {
                                if (command.ExecuteNonQuery() == 1)
                                {
                                    message_send("Username_Created");
                                }
                                else
                                {
                                    message_send("Failure");
                                }
                            }
                            db.zamknijPolaczenie();
                        }
                        else if (json.Type.ToString().Equals("Ranking"))
                        {
                            DB db = new DB();

                            DataTable table = new DataTable();

                            MySqlDataAdapter adapter = new MySqlDataAdapter();
                            MySqlCommand command = new MySqlCommand("SELECT username, points FROM users ORDER BY points DESC", db.getConnection());
                            db.otworzPolaczenie();
                            MySqlDataReader rdr = command.ExecuteReader();
                            List<Ranking> dataList = new List<Ranking>();
                            while (rdr.Read())
                            {
                                dataList.Add(new Ranking() { User = rdr.GetString(0), Points = rdr.GetInt32(1) });
                            }
                            Byte[] data = System.Text.Encoding.ASCII.GetBytes(JsonSerializer.Serialize(dataList));
                            netStream = klient.GetStream();
                            netStream.Write(data, 0, data.Length);
                        }
                        else if (json.Type.ToString().Equals("Kolejka_info"))
                        {
                            DB db = new DB();
                            string Kolejka = json.kolejka.ToString();
                            string kolejka_nr = Kolejka.Remove(0, 9);

                            DataTable table = new DataTable();

                            MySqlDataAdapter adapter = new MySqlDataAdapter();
                            MySqlCommand command = new MySqlCommand("SELECT * FROM terminarz WHERE RoundNumber = '" + kolejka_nr + "'", db.getConnection());
                            db.otworzPolaczenie();
                            MySqlDataReader rdr = command.ExecuteReader();
                            List<Kolejka> dataList = new List<Kolejka>();
                            while (rdr.Read())
                            {
                                dataList.Add(new Kolejka()
                                {
                                    MatchNumber = rdr.GetString(0),
                                    RoundNumber = rdr.GetString(1),
                                    Date = rdr.GetString(2),
                                    HomeTeam = rdr.GetString(3),
                                    AwayTeam = rdr.GetString(4),
                                    Result = rdr.GetString(5)
                                });
                            }
                            Byte[] data = System.Text.Encoding.ASCII.GetBytes(JsonSerializer.Serialize(dataList));
                            netStream = klient.GetStream();
                            netStream.Write(data, 0, data.Length);
                        }
                        else if (json.Type.ToString().Equals("Bet"))
                        {
                            DB db = new DB();
                            MySqlCommand command = new MySqlCommand("INSERT INTO Bet values ('"
                                + json.User.ToString() + "', '" + json.MatchNumber.ToString()
                                + "', '" + json.Result.ToString() + "')", db.getConnection());
                            db.otworzPolaczenie();
                            if (command.ExecuteNonQuery() == 1)
                            {
                                message_send("OBSTAWIONO");
                            }
                            else
                            {
                                message_send("Wystąpił problem, spróbuj ponownie później");
                            }
                            db.zamknijPolaczenie();
                        }

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString(), "Błąd");
                    }
                    flaga = true;
                }
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
                var path = Application.StartupPath + @"\" + fileName; // Habeeb, "Dubai Media City, Dubai"
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
                        string sql = "INSERT INTO terminarz VALUES('" + values[0].ToString() + "','" + values[1].ToString() + "','" + values[2].ToString() + "','" + values[4].ToString() + "','" + values[5].ToString() + "','" + values[6].ToString() + "')";

                        MySqlCommand cmd = new MySqlCommand(sql, conn);
                        cmd.ExecuteNonQuery();
                    }

                }
            }
            catch (Exception ex)
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

        public async Task<dynamic> getJson()
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
                    return json;
                }
                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Błąd");
                netStream.Close();
                return null;
            }
        }

        public List<Bet> getBets(string user)
        {
            DB db = new DB();

            DataTable table = new DataTable();

            MySqlDataAdapter adapter = new MySqlDataAdapter();
            MySqlCommand command = new MySqlCommand($"SELECT matchID, Result, user FROM Bet WHERE user = '{user}' ORDER BY matchID ASC", db.getConnection());
            db.otworzPolaczenie();
            MySqlDataReader rdr = command.ExecuteReader();
            List<Bet> dataList = new List<Bet>();
            while (rdr.Read())
            {
                dataList.Add(new Bet() { ID = rdr.GetInt16(0), Zaklad = rdr.GetString(1), User = rdr.GetString(2) });
            }

            Console.WriteLine(JsonSerializer.Serialize(dataList));

            return dataList;

        }

        public List<Score> getScores()
        {
            DB db = new DB();

            DataTable table = new DataTable();

            MySqlDataAdapter adapter = new MySqlDataAdapter();
            MySqlCommand command = new MySqlCommand("SELECT matchnumber, result FROM terminarz", db.getConnection());
            db.otworzPolaczenie();
            MySqlDataReader rdr = command.ExecuteReader();
            List<Score> dataList = new List<Score>();
            while (rdr.Read())
            {
                dataList.Add(new Score() { ID = rdr.GetInt16(0), Wynik = rdr.GetString(1)});
            }

            return dataList;
        }
        public void message_send(string message)
        {
            Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
            netStream = klient.GetStream();
            netStream.Write(data, 0, data.Length);
        }

        public int VerifyBet()
        {
            int points = 0;
            Regex rx1 = new Regex(@"^[0-9]{1,}");
            Regex rx2 = new Regex(@"[0-9]{1,}$");
            List<Score> scores = getScores();
            foreach (Bet bet in getBets("Wojtek"))
            {
                string score = scores.Find(x => x.ID.Equals(bet.ID)).Wynik;
                MatchCollection matches = rx1.Matches(score);
                int HS = Int32.Parse(matches[0].Value);
                matches = rx2.Matches(getScores().Find(x => x.ID.Equals(bet.ID)).Wynik);
                int AS = Int32.Parse(matches[0].Value);
                string result;
                if (HS>AS)
                {
                    result = "H";
                }
                else if (AS > HS)
                {
                    result = "A";
                }
                else
                {
                    result="D";
                }
                if(result == bet.Zaklad) { points += 10; }

            }
            listBox1.Items.Add(points);
            return points;
        }

        public Boolean SprawdzDuplikaty(string username)
        {
            DB db = new DB();


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

        public Boolean SprawdzDuplikaty2(string email)
        {
            DB db = new DB();

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
        public class Bet
        {
            public int ID { get; set; }
            public string Zaklad { get; set; }
            public string User { get; set; }
        }

        public class Score
        {
            public int ID { get; set; }
            public string Wynik { get; set; }
        }
    }
}