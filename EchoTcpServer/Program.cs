using System;
using System.Threading.Tasks;
using EchoServer;

class Program
{
    public static async Task Main(string[] args)
    {
        // 1. Створюємо логіку
        var handler = new EchoMessageHandler();
        // 2. Створюємо сервер
        Server server = new Server(5000, handler);

        // Запускаємо сервер
        var serverTask = Task.Run(() => server.StartAsync());

        // Запускаємо UDP Sender (твоя стара логіка)
        string host = "127.0.0.1";
        int port = 60000;
        
        using (var sender = new UdpTimedSender(host, port))
        {
            Console.WriteLine("Press 'Q' to quit...");
            sender.StartSending(3000); // 3 секунди інтервал

            while (Console.ReadKey(true).Key != ConsoleKey.Q) { }

            sender.StopSending();
            server.Stop();
        }
        
        try { await serverTask; } catch { }
    }
}