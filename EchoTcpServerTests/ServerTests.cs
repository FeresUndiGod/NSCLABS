using Xunit;
using Moq;
using EchoServer;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace EchoTcpServerTests
{
    public class ServerTests
    {
        [Fact]
        public async Task Server_ShouldCallHandler()
        {
            int port = 45678;
            var mockHandler = new Mock<IMessageHandler>();
            mockHandler.Setup(h => h.Process(It.IsAny<byte[]>()))
                       .Returns(new byte[] { 0x01 });

            var server = new Server(port, mockHandler.Object);
            
            // Запуск сервера
            var task = Task.Run(() => server.StartAsync());
            await Task.Delay(200);

            // Клієнт
            using (var client = new TcpClient())
            {
                await client.ConnectAsync("127.0.0.1", port);
                await client.GetStream().WriteAsync(new byte[] { 1 });
                
                // Чекаємо обробки
                await Task.Delay(200);
            }

            server.Stop();
            try { await task; } catch {}

            // Перевірка, що Process викликався
            mockHandler.Verify(h => h.Process(It.IsAny<byte[]>()), Times.AtLeastOnce);
        }
    }
}