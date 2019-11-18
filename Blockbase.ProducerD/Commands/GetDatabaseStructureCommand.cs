using Blockbase.ProducerD.Commands.Interfaces;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace Blockbase.ProducerD.Commands
{
    public class GetDatabaseStructureCommand : IHelperCommand
    {
        private DataSet ds = new DataSet();
        private DataTable dt = new DataTable();

        public async Task ExecuteAsync()
        {
            try
            {
                // PostgeSQL-style connection string
                string connstring = "Server=localhost;Port=5432;User Id=postgres;Password=qwerty123;database=test";

                // Making connection with Npgsql provider
                NpgsqlConnection conn = new NpgsqlConnection(connstring);
                conn.Open();

                //string sql = "CREATE DATABASE test;";
                //NpgsqlCommand command = new NpgsqlCommand(sql, conn);
                //command.ExecuteNonQuery();

                //sql = "CREATE TABLE table_name ( column1 SERIAL PRIMARY KEY, column2 TEXT);";
                //command = new NpgsqlCommand(sql, conn);
                //command.ExecuteNonQuery();
             
                string sql = "SELECT TABLE_NAME FROM information_schema.tables WHERE table_schema = 'public';";
                NpgsqlCommand command = new NpgsqlCommand(sql, conn);
                var reader = command.ExecuteReader();
                Console.WriteLine("Database tables: ");
                while (reader.Read())
                {
                    Console.WriteLine(reader[0]);
                }
                Console.WriteLine("Database columns: ");
                //use "WHERE table_name = X" when you want the columns of a specific table X
                sql = "SELECT column_name FROM information_schema.columns WHERE table_schema = 'public';";
                command = new NpgsqlCommand(sql, conn);
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Console.WriteLine(reader[0]);
                }

                conn.Close();
            }
            catch (Exception e)
            {
                // something went wrong, and you wanna know why
                Console.WriteLine(e.Message);
                throw;
            }
        }

        public string GetCommandHelp()
        {
            return "gds = get database structure";
        }

        public bool TryParseCommand(string commandStr)
        {
            return "gds" == commandStr;
        }
    }
}
