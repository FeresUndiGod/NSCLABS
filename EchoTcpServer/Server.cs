using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace EchoServer
{
    public class Server
    {
        private readonly int _port;
        private readonly IMessageHandler _handler;
        private TcpListener? _listener;
        private CancellationTokenSource _cts;

        public Server(int port, IMessageHandler handler)
        {
            _port = port;
            _handler = handler;
            _cts = new CancellationTokenSource();
        }

        public async Task StartAsync()
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            Console.WriteLine($"TCP Server started on {_port}.");

            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClientAsync(client, _cts.Token));
                }
            }
            catch { /* Ignored */ }
            finally { Stop(); }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            using (client)
            using (NetworkStream stream = client.GetStream())
            {
                try
                {
                    byte[] buffer = new byte[8192];
                    int bytesRead;

                    while (!token.IsCancellationRequested && 
                          (bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                    {
                        byte[] receivedData = new byte[bytesRead];
                        Array.Copy(buffer, receivedData, bytesRead);

                        // Використовуємо наш Handler!
                        byte[] response = _handler.Process(receivedData);

                        await stream.WriteAsync(response, 0, response.Length, token);
                        Console.WriteLine($"Echoed {response.Length} bytes.");
                    }
                }
                catch { /* Ignored */ }
            }
        }

        public void Stop()
        {
            if (!_cts.IsCancellationRequested)
            {
                _cts.Cancel();
                _listener?.Stop();
                Console.WriteLine("Server stopped.");
            }
        }
    }
}