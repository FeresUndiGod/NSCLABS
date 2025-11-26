using System;
using System.Linq;
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
        [Fact]
        public async Task FullLifecycle_ShouldWorkCorrectly()
        {

            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            
            var wrapper = new TcpClientWrapper("127.0.0.1", port);
            bool messageReceived = false;
            wrapper.MessageReceived += (s, data) => messageReceived = true;


            wrapper.Connect();
            Assert.True(wrapper.Connected);


            using var serverClient = await listener.AcceptTcpClientAsync();
            var serverStream = serverClient.GetStream();


            await wrapper.SendMessageAsync("Hello");
            await wrapper.SendMessageAsync(new byte[] { 0x01, 0x02 });


            byte[] buffer = new byte[1024];
            int bytesRead = await serverStream.ReadAsync(buffer, 0, buffer.Length);
            Assert.True(bytesRead > 0);


            await serverStream.WriteAsync(Encoding.UTF8.GetBytes("Ack"), 0, 3);
            await Task.Delay(500); // Чекаємо обробки
            Assert.True(messageReceived, "Client should receive message from server");


            wrapper.Disconnect();
            Assert.False(wrapper.Connected);
            
            listener.Stop();
        }


        [Fact]
        public void Connect_WhenAlreadyConnected_ShouldNotReconnect()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            var wrapper = new TcpClientWrapper("127.0.0.1", port);
            
            wrapper.Connect();
            Assert.True(wrapper.Connected);
            

            wrapper.Connect(); 
            Assert.True(wrapper.Connected);

            wrapper.Disconnect();
            listener.Stop();
        }

        [Fact]
        public async Task Actions_WhenNotConnected_ShouldHandleGracefully()
        {
            var wrapper = new TcpClientWrapper("127.0.0.1", 8080);


            wrapper.Disconnect();
            Assert.False(wrapper.Connected);


            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await wrapper.SendMessageAsync("Fail")
            );
        }
    }
}