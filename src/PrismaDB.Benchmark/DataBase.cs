﻿using MySql.Data.MySqlClient;
using Npgsql;
using System;
using System.Data;
using System.Data.SqlClient;

namespace PrismaDB.Benchmark
{
    class DataBase
    {
        protected static CustomConfiguration conf;
        private SqlConnection msconnection = null;
        private MySqlConnection myconnection = null;
        private NpgsqlConnection pgconnection = null;
        private static string servertype;
        private const string MSSQL = "mssql";
        private const string MYSQL = "mysql";
        private const string PGSQL = "postgres";

        public DataBase(bool first = false)
        {
            conf = CustomConfiguration.LoadConfiguration();
            servertype = conf.ServerType;
            if (first)
                IsServerConnected();
            else
                Connect();
        }

        public void IsServerConnected()
        {
            switch (servertype)
            {
                case MSSQL:
                    IsMSConnected();
                    break;
                case MYSQL:
                    IsMYConnected();
                    break;
                case PGSQL:
                    IsPGConnected();
                    break;
            }
        }

        public void Connect()
        {
            switch (servertype)
            {
                case MSSQL:
                    MSConnect();
                    break;
                case MYSQL:
                    MYConnect();
                    break;
                case PGSQL:
                    PGConnect();
                    break;
            }
        }

        public long ExecuteNonQuery(string query)
        {
            switch (servertype)
            {
                case MSSQL:
                    return MSExecuteNonQuery(query);
                case MYSQL:
                    return MYExecuteNonQuery(query);
                case PGSQL:
                    return PGExecuteNonQuery(query);
            }
            return 0;
        }

        public string ExecuteReader(string query)
        {
            switch (servertype)
            {
                case MSSQL:
                    return MSExecuteReader(query);
                case MYSQL:
                    return MYExecuteReader(query);
                case PGSQL:
                    return PGExecuteReader(query);
            }
            return null;
        }

        public void Close()
        {
            switch (servertype)
            {
                case MSSQL:
                    msconnection.Close();
                    msconnection = null;
                    break;
                case MYSQL:
                    myconnection.Close();
                    myconnection = null;
                    break;
                case PGSQL:
                    pgconnection.Close();
                    pgconnection = null;
                    break;
            }
        }

        private void MSConnect()
        {
            var msbldr = new SqlConnectionStringBuilder
            {
                UserID = conf.userid,
                Password = conf.password,
                DataSource = conf.host + "," + conf.port,
                InitialCatalog = conf.database
            };
            try
            {
                msconnection = new SqlConnection(msbldr.ConnectionString);
                msconnection.Open();
            }
            catch (SqlException e)
            {
                Console.WriteLine("Cannot create connection:\n" + e.Message);
                msconnection = null;
            }
        }

        private void MYConnect()
        {
            var mybldr = new MySqlConnectionStringBuilder
            {
                ["user id"] = conf.userid,
                ["password"] = conf.password,
                ["server"] = conf.host,
                ["port"] = conf.port,
                ["database"] = conf.database,
            };
            try
            {
                myconnection = new MySqlConnection(mybldr.ConnectionString);
                myconnection.Open();
            }
            catch (MySqlException e)
            {
                Console.WriteLine("Cannot create connection:\n" + e.Message);
                myconnection = null;
            }
        }

        private void PGConnect()
        {
            var pgbldr = new NpgsqlConnectionStringBuilder
            {
                Username = conf.userid,
                Password = conf.password,
                Host = conf.host,
                Port = Int32.Parse(conf.port),
                Database = conf.database,
                ServerCompatibilityMode = ServerCompatibilityMode.NoTypeLoading
            };
            try
            {
                pgconnection = new NpgsqlConnection(pgbldr.ConnectionString);
                pgconnection.Open();
            }
            catch (NpgsqlException e)
            {
                Console.WriteLine("Cannot create connection:\n" + e.Message);
                myconnection = null;
            }
        }

        private bool Retry(int times = 3)
        {
            int retry = 0;
            do
            {
                if (retry > times) return false;
                retry++;
                Console.WriteLine("Trying to reconnect the {0} time ... ", retry);
                Connect();
            } while (msconnection == null && myconnection == null);
            return true;
        }

        private void IsMSConnected()
        {
            int attempt = 0;
            bool isConnected = false;
            var bldr = new SqlConnectionStringBuilder
            {
                UserID = conf.userid,
                Password = conf.password,
                DataSource = conf.host + "," + conf.port,
                InitialCatalog = conf.database
            };
            while (!isConnected)
            {
                if (attempt < 10)
                {
                    using (var l_oConnection = new SqlConnection(bldr.ConnectionString))
                    {
                        try
                        {
                            l_oConnection.Open();
                            isConnected = true;
                        }
                        catch (SqlException)
                        {
                            isConnected = false;
                        }
                    }
                    attempt++;
                    System.Threading.Thread.Sleep(5000);
                }
                else
                {
                    Console.WriteLine("Connect failed. Press any key to exit ...");
                    Console.ReadLine();
                    Environment.Exit(1);
                }
            }
        }

        private void IsMYConnected()
        {
            InitDatabase();
            int attempt = 0;
            bool isConnected = false;
            var mybldr = new MySqlConnectionStringBuilder
            {
                ["user id"] = conf.userid,
                ["password"] = conf.password,
                ["server"] = conf.host,
                ["port"] = conf.port,
                ["database"] = conf.database,
            };
            while (!isConnected)
            {
                if (attempt < 10)
                {
                    using (var l_oConnection = new MySqlConnection(mybldr.ConnectionString))
                    {
                        try
                        {
                            l_oConnection.Open();
                            isConnected = true;
                        }
                        catch (MySqlException)
                        {
                            isConnected = false;
                        }
                    }
                    attempt++;
                    System.Threading.Thread.Sleep(5000);
                }
                else
                {
                    Console.WriteLine("Connect failed. Press any key to exit ...");
                    Console.ReadLine();
                    Environment.Exit(1);
                }
            }
        }

        private void InitDatabase()
        {
            var mybldr = new MySqlConnectionStringBuilder
            {
                ["user id"] = "init",
                ["password"] = "init",
                ["server"] = conf.host,
                ["port"] = conf.port,
                ["database"] = conf.database,
            };
            using (var l_oConnection = new MySqlConnection(mybldr.ConnectionString))
            {
                try
                {
                    l_oConnection.Open();
                    string Register = $"PRISMADB REGISTER USER '{conf.userid}' PASSWORD '{conf.password}';";
                    MySqlCommand cmd = new MySqlCommand
                    {
                        CommandText = Register,
                        Connection = l_oConnection,
                        CommandType = CommandType.Text,
                        CommandTimeout = 300
                    };
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        reader.Close();
                    }
                }
                catch (MySqlException e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        private void IsPGConnected()
        {
            int attempt = 0;
            bool isConnected = false;
            var bldr = new NpgsqlConnectionStringBuilder
            {
                Username = conf.userid,
                Password = conf.password,
                Host = conf.host,
                Port = Int32.Parse(conf.port),
                Database = conf.database,
                ServerCompatibilityMode = ServerCompatibilityMode.NoTypeLoading

            };
            while (!isConnected)
            {
                if (attempt < 10)
                {
                    using (var l_oConnection = new NpgsqlConnection(bldr.ConnectionString))
                    {
                        try
                        {
                            l_oConnection.Open();
                            isConnected = true;
                        }
                        catch (NpgsqlException)
                        {
                            isConnected = false;
                        }
                    }
                    attempt++;
                    System.Threading.Thread.Sleep(5000);
                }
                else
                {
                    Console.WriteLine("Connect failed. Press any key to exit ...");
                    Console.ReadLine();
                    Environment.Exit(1);
                }
            }
        }

        private long MSExecuteNonQuery(string query)
        {
            if (msconnection == null)
            {
                Console.WriteLine("There is no connection!");
                if (!Retry())
                {
                    Console.WriteLine("Connect failed. Press any key to exit ...");
                    Console.ReadLine();
                    Environment.Exit(1);
                }
            }
            // execute query
            SqlCommand cmd = new SqlCommand
            {
                CommandText = query,
                Connection = msconnection,
                CommandType = CommandType.Text,
                CommandTimeout = 300
            };
            return cmd.ExecuteNonQuery();
        }

        private long MYExecuteNonQuery(string query)
        {
            if (myconnection == null)
            {
                Console.WriteLine("There is no connection!");
                if (!Retry())
                {
                    Console.WriteLine("Connect failed. Press any key to exit ...");
                    Console.ReadLine();
                    Environment.Exit(1);
                }
            }
            // execute query
            MySqlCommand cmd = new MySqlCommand
            {
                CommandText = query,
                Connection = myconnection,
                CommandType = CommandType.Text,
                CommandTimeout = 300
            };
            return cmd.ExecuteNonQuery();
        }

        private long PGExecuteNonQuery(string query)
        {
            if (pgconnection == null)
            {
                Console.WriteLine("There is no connection!");
                if (!Retry())
                {
                    Console.WriteLine("Connect failed. Press any key to exit ...");
                    Console.ReadLine();
                    Environment.Exit(1);
                }
            }
            // execute query
            NpgsqlCommand cmd = new NpgsqlCommand
            {
                CommandText = query,
                Connection = pgconnection,
                CommandType = CommandType.Text,
                CommandTimeout = 300
            };
            return cmd.ExecuteNonQuery();
        }

        private string MSExecuteReader(string query)
        {
            if (msconnection == null)
                return null;
            SqlCommand cmd = new SqlCommand
            {
                CommandText = query,
                Connection = msconnection,
                CommandType = CommandType.Text,
                CommandTimeout = 300
            };
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                    return reader[1].ToString();
            }
            return null;
        }

        private string MYExecuteReader(string query)
        {
            if (myconnection == null)
                return null;
            MySqlCommand cmd = new MySqlCommand
            {
                CommandText = query,
                Connection = myconnection,
                CommandType = CommandType.Text,
                CommandTimeout = 300
            };
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                    return reader[1].ToString();
            }
            return null;
        }

        private string PGExecuteReader(string query)
        {
            if (pgconnection == null)
                return null;
            NpgsqlCommand cmd = new NpgsqlCommand
            {
                CommandText = query,
                Connection = pgconnection,
                CommandType = CommandType.Text,
                CommandTimeout = 300
            };
            using (NpgsqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                    return reader[1].ToString();
            }
            return null;
        }
    }
}