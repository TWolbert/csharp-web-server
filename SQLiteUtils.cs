using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using BCrypt.Net;

namespace C__webserver
{
    public static class SQLiteUtils
    {

        public static SQLiteConnection loginConn;
        public static void init()
        {
            // Create table and database if not exists
            loginConn = CreateConnectionLogin();

            var cmd = new SQLiteCommand(loginConn)
            {
                CommandText = $"CREATE TABLE IF NOT EXISTS users(id INTEGER PRIMARY KEY, username TEXT, password TEXT)"
            };

            cmd.ExecuteNonQuery();
        }
        private static SQLiteConnection CreateConnectionLogin()
        {
            SQLiteConnection sqlite_conn;
            // Create a new database connection:
            sqlite_conn = new SQLiteConnection("Data Source= login.db; Version = 3; New = True; Compress = True; ");
            // Open the connection:
            try
            {
                sqlite_conn.Open();
            }
            catch (Exception ex)
            {
                throw new Exception("Couldnt create database");
            }
            return sqlite_conn;
        }
        public async static Task<string> registerUser(string username, string password)
        {
            string checkQuery = "SELECT COUNT(*) FROM users WHERE username = @username";
            string insertQuery = "INSERT INTO users (username, password) VALUES (@username, @password)";

            try
            {
                SQLiteCommand checkCmd = new SQLiteCommand(loginConn)
                {
                    CommandText = checkQuery
                };
                checkCmd.Parameters.AddWithValue("@username", username);
                int userCount = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                if (userCount == 0)  // Username doesn't exist, so proceed with insertion
                {
                    string pw = encryptPassword(password);

                    SQLiteCommand insertCmd = new SQLiteCommand(loginConn)
                    {
                        CommandText = insertQuery
                    };
                    insertCmd.Parameters.AddWithValue("@username", username);
                    insertCmd.Parameters.AddWithValue("@password", pw);

                    int result = await insertCmd.ExecuteNonQueryAsync();

                    if (result == 0)
                    {
                        return "Something went wrong";
                    }
                    else
                    {
                        return "User added!";
                    }
                }
                else
                {
                    return "Username already exists";
                }
                
            }
            catch (Exception ex)
            {
                // Handle exceptions
                Console.WriteLine("Error: " + ex.Message);
                return "Something went wrong";
            }
        }
        
        public static string verifyLogin(string username, string password)
        {
            var cmd = new SQLiteCommand(loginConn);
            cmd.CommandText = "SELECT * FROM users WHERE username = ?";
            cmd.Parameters.AddWithValue("@username", username);

            var reader = cmd.ExecuteReader();
            string passHash = "";

            while (reader.Read())
            {
                passHash = reader["password"].ToString();
            }

            if (string.IsNullOrEmpty(passHash))
            {
                return "User doesn't exist!";
            }

            if (verifyPassword(password, passHash))
            {
                return "Login successful!";
            }
            else
            {
                return "Password incorrect";
            }
        }
        
        private static bool verifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        private static string encryptPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, 10);
        }
    }

}
