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
        [Fact]
        public void Constructor_ShouldInitializeWithHostAndPort()
        {
            var wrapper = new TcpClientWrapper("127.0.0.1", 8080);
            Assert.NotNull(wrapper);
        }

        [Fact]
        public async Task FullLifecycle_ShouldWorkCorrectly()
        {
            // 1. Setup Server
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            
            var wrapper = new TcpClientWrapper("127.0.0.1", port);
            bool messageReceived = false;
            wrapper.MessageReceived += (s, data) => messageReceived = true;

            // 2. Connect
            wrapper.Connect();
            Assert.True(wrapper.Connected);

            // Accept client on server side
            using var serverClient = await listener.AcceptTcpClientAsync();
            var serverStream = serverClient.GetStream();

            // 3. Send (Client -> Server)
            await wrapper.SendMessageAsync("Hello");
            await wrapper.SendMessageAsync(new byte[] { 0x01, 0x02 });

            // Verify server received data
            byte[] buffer = new byte[1024];
            int bytesRead = await serverStream.ReadAsync(buffer, 0, buffer.Length);
            Assert.True(bytesRead > 0);

            // 4. Receive (Server -> Client)
            await serverStream.WriteAsync(Encoding.UTF8.GetBytes("Ack"), 0, 3);
            
            // Wait for client to process message
            await Task.Delay(2000); 
            Assert.True(messageReceived);

            // 5. Disconnect
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
            
            // Second call
            wrapper.Connect(); 
            Assert.True(wrapper.Connected);

            wrapper.Disconnect();
            listener.Stop();
        }

        [Fact]
        public async Task Actions_WhenNotConnected_ShouldHandleGracefully()
        {
            var wrapper = new TcpClientWrapper("127.0.0.1", 8080);

            // Disconnect without connection
            wrapper.Disconnect();
            Assert.False(wrapper.Connected);

            // Send without connection
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await wrapper.SendMessageAsync("Fail")
            );
        }
    }
}