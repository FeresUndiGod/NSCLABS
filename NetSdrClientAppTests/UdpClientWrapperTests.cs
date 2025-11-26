using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests.Networking
{
    public class UdpClientWrapperTests
    {
        // ТЕСТ 1: Робота UDP (Start -> Receive -> Stop)
        // Покриває: StartListeningAsync, ReceiveAsync, Invoke, StopListening
        [Fact]
        public async Task Udp_Lifecycle_ShouldWork()
        {
            int port = 15000; // Вибираємо вільний порт
            var wrapper = new UdpClientWrapper(port);
            
            string receivedText = null;
            wrapper.MessageReceived += (s, data) => receivedText = Encoding.UTF8.GetString(data);

            // 1. START
            var listenTask = wrapper.StartListeningAsync();
            await Task.Delay(200); // Даємо час запуститися

            // 2. SEND REAL PACKET (Зовні)
            // Це критично для покриття рядка await _udpClient.ReceiveAsync(...)
            using (var sender = new UdpClient())
            {
                byte[] data = Encoding.UTF8.GetBytes("UdpHello");
                await sender.SendAsync(data, data.Length, new IPEndPoint(IPAddress.Loopback, port));
            }

            // Чекаємо обробки
            await Task.Delay(1000); 

            // 3. STOP
            wrapper.StopListening();
            
            // 4. EXIT (покриває метод Exit)
            wrapper.Exit();

            Assert.Equal("UdpHello", receivedText);
        }

        // ТЕСТ 2: Перевірка Equals та GetHashCode (були червоні на скріні)
        [Fact]
        public void Equals_And_HashCode_Coverage()
        {
            var w1 = new UdpClientWrapper(1000);
            var w2 = new UdpClientWrapper(1000); // Такий же порт
            var w3 = new UdpClientWrapper(2000); // Інший порт

            // Покриває Equals(obj) -> true
            Assert.True(w1.Equals(w2));
            
            // Покриває Equals(obj) -> false (різні порти)
            Assert.False(w1.Equals(w3));

            // Покриває Equals(obj) -> false (null або інший тип) - ЦЕ БУЛО ЧЕРВОНИМ
            Assert.False(w1.Equals(null));
            Assert.False(w1.Equals("SomeString"));

            // Покриває GetHashCode
            Assert.Equal(w1.GetHashCode(), w2.GetHashCode());
        }

        // ТЕСТ 3: Dispose (Явне викликання)
        [Fact]
        public void Dispose_ShouldRunWithoutError()
        {
            var wrapper = new UdpClientWrapper(3000);
            
            // Викликаємо Dispose, щоб покрити _cts?.Cancel() та _udpClient?.Close()
            wrapper.Dispose(); 
            
            Assert.NotNull(wrapper);
        }
    }
}