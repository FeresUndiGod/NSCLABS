using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests.Networking
{
    public class TcpClientWrapperTests
    {
        // ТЕСТ 1: "Щасливий шлях" - повний цикл роботи
        // Покриває: Connect, StartListening, SendMessageAsync, Receive, Disconnect
        [Fact]
        public async Task FullFlow_Connect_Send_Receive_Disconnect()
        {
            // 1. ПІДГОТОВКА СЕРВЕРА (імітуємо реальний пристрій)
            var listener = new TcpListener(IPAddress.Loopback, 0); // Порт 0 = автовибір
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            // Створюємо твій клас
            var wrapper = new TcpClientWrapper("127.0.0.1", port);
            
            // Підписуємось на подію, щоб перевірити отримання
            string receivedFromServer = null;
            wrapper.MessageReceived += (sender, bytes) => 
            {
                receivedFromServer = Encoding.UTF8.GetString(bytes);
            };

            // 2. CONNECT
            wrapper.Connect();
            Assert.True(wrapper.Connected, "Клієнт має бути підключений");

            // Приймаємо клієнта на стороні "сервера"
            var serverClient = await listener.AcceptTcpClientAsync();
            var serverStream = serverClient.GetStream();

            // 3. SEND (Клієнт -> Сервер) - String
            await wrapper.SendMessageAsync("Hello String");
            
            // 4. SEND (Клієнт -> Сервер) - Bytes
            byte[] byteData = new byte[] { 0xAA, 0xBB };
            await wrapper.SendMessageAsync(byteData);

            // Читаємо на сервері, щоб переконатися, що дані дійшли
            byte[] buffer = new byte[1024];
            int bytesRead = await serverStream.ReadAsync(buffer, 0, buffer.Length);
            Assert.True(bytesRead > 0, "Сервер має отримати дані");

            // 5. RECEIVE (Сервер -> Клієнт)
            // Це змусить спрацювати StartListeningAsync і подію MessageReceived
            byte[] response = Encoding.UTF8.GetBytes("ServerResponse");
            await serverStream.WriteAsync(response, 0, response.Length);

            // Чекаємо трохи, бо це асинхронно
            await Task.Delay(1000); 
            Assert.Equal("ServerResponse", receivedFromServer);

            // 6. DISCONNECT & DISPOSE
            wrapper.Disconnect();
            Assert.False(wrapper.Connected);
            
            wrapper.Dispose(); // Покриває метод Dispose()

            // Чистка
            listener.Stop();
        }

        // ТЕСТ 2: Обробка помилок (Negative Test)
        // Покриває: Exception throw, Disconnect без з'єднання
        [Fact]
        public async Task ExceptionHandling_And_NoConnection()
        {
            var wrapper = new TcpClientWrapper("127.0.0.1", 9999);

            // Спроба відправити без підключення -> має бути помилка
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await wrapper.SendMessageAsync("Fail"));

            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await wrapper.SendMessageAsync(new byte[] { 1, 2 }));

            // Disconnect коли не підключені (покриває гілку else)
            wrapper.Disconnect(); 
            Assert.False(wrapper.Connected);
        }

        // ТЕСТ 3: Невдале з'єднання (Catch block coverage)
        // Покриває: try-catch у Connect
        [Fact]
        public void Connect_ToInvalidPort_ShouldCatchException()
        {
            // Пробуємо підключитися до порту, де нічого немає
            var wrapper = new TcpClientWrapper("127.0.0.1", 55555);
            
            // Цей метод не кидає Exception назовні, а пише в Console (catch block)
            // Тому ми просто викликаємо його, щоб пройти по коду catch
            wrapper.Connect(); 

            Assert.False(wrapper.Connected);
        }

        // ТЕСТ 4: Повторне з'єднання
        // Покриває: if (Connected) return;
        [Fact]
        public void Connect_AlreadyConnected_ShouldReturnEarly()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            var wrapper = new TcpClientWrapper("127.0.0.1", port);
            wrapper.Connect();
            Assert.True(wrapper.Connected);

            // Другий виклик
            wrapper.Connect(); 
            Assert.True(wrapper.Connected);

            listener.Stop();
        }
    }
}