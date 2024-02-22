using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Set default IP - but also prompt for IP in case user cannot use our default
            string ip = "localhost";

            Console.Write($"Please enter IP to use, default IP is {ip}: ");
            string newIp = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(newIp))
            {
                ip = newIp;
            }

            // Set default port - but also prompt for port in case user cannot use our default
            int port = 27015;

            Console.Write($"Please enter port to use (default port is {port}): ");
            string newPort = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(newPort))
            {
                while (!int.TryParse(newPort, out port))
                {
                    Console.Write($"{newPort} is not a valid integer, please try again: ");
                    newPort = Console.ReadLine();
                }
            }

            string uri = $"http://{ip}:{port}/";

            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls(new string[] { uri });
                    Console.WriteLine($"Listening on {uri}");
                    webBuilder.UseStartup<Startup>();
                })
                .Build()
                .Run();
        }
    }
}