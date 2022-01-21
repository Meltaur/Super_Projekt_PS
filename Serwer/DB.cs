using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace Serwer
{
    class DB
    {
        //private MySqlConnection connection = new MySqlConnection("server=localhost;port=3306;username=root;password=;database=users_db");
        private MySqlConnection connection = new MySqlConnection("server=db4free.net;port=3306;username=siatek;password=projektps;database=projektps");

        //funkja otwierająca połączenie
        public void otworzPolaczenie()
        {
            if (connection.State == System.Data.ConnectionState.Closed)
            {
                connection.Open();
            }
        }

        //funkcja zamykajaca polaczenie
        public void zamknijPolaczenie()
        {
            if (connection.State == System.Data.ConnectionState.Open)
            {
                connection.Close();
            }
        }

        //zwrot 
        public MySqlConnection getConnection()
        {
            return connection;
        }

    }
}
