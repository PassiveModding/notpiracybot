using System;
using Passive.Discord.Setup;

namespace notpiracybot
{
    public class DbConnection
    {
        private static string port;

        public static string DatabaseName { get; set; }

        public static string DbConnectionString => $"Server={Host};Port={port};Database={DatabaseName};User Id={Username};Password={Password};";

        public static string Host { get; set; }

        public static string Password { get; set; }

        public static string Port
        {
            get
            {
                return port;
            }

            set
            {
                if (int.TryParse(value, out var _))
                {
                    port = value;
                }
                else
                {
                    throw new ArgumentException("Port must be an integer.");
                }
            }
        }

        public static string Username { get; set; }

        public static bool IsInitialized { get; private set; } = false;

        public static void Initialize(Config config)
        {
            Host = config.GetOrAddEntry("DbHost", () =>
            {
                Console.WriteLine("Please enter database server IP:");
                var server = Console.ReadLine();
                return server;
            });
            Port = config.GetOrAddEntry("DbPort", () =>
            {
                Console.WriteLine("Please enter database port:");
                var dbPort = Console.ReadLine();
                return dbPort;
            });
            DatabaseName = config.GetOrAddEntry("DbName", () =>
            {
                Console.WriteLine("Please enter database name:");
                var dbName = Console.ReadLine();
                return dbName;
            });
            Username = config.GetOrAddEntry("DbUsername", () =>
            {
                Console.WriteLine("Please enter database login username:");
                var username = Console.ReadLine();
                return username;
            });
            Password = config.GetOrAddEntry("DbPass", () =>
            {
                Console.WriteLine("Please enter database login password:");
                var password = Console.ReadLine();
                return password;
            });
            IsInitialized = true;
        }
    }
}