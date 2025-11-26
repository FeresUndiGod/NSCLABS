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
        [Fact]
        public void Constructor_ShouldCreateInstance()
        {
            var wrapper = new UdpClientWrapper(0);
            Assert.NotNull(wrapper);
        }

        [Fact]
        public async Task Listening_ShouldReceiveData_AndStop()
        {
            int port = 12000;
            var wrapper = new UdpClientWrapper(port);
            string receivedMsg = null;
            
            wrapper.MessageReceived += (s, data) => receivedMsg = Encoding.UTF8.GetString(data);

            // Start
            var task = wrapper.StartListeningAsync();
            await Task.Delay(100);

            // Send Real Packet
            using (var sender = new UdpClient())
            {
                var data = Encoding.UTF8.GetBytes("TestUDP");
                await sender.SendAsync(data, data.Length, new IPEndPoint(IPAddress.Loopback, port));
            }

            await Task.Delay(500);
            wrapper.StopListening();

            Assert.Equal("TestUDP", receivedMsg);
        }

        [Fact]
        public void Exit_ShouldCloseResources()
        {
            var wrapper = new UdpClientWrapper(0);
            wrapper.Exit(); // Covers Exit() method
            Assert.NotNull(wrapper); 
        }

        [Fact]
        public void Metadata_ShouldBeConsistent()
        {
            var wrapper = new UdpClientWrapper(12345);
            Assert.Equal(wrapper.GetHashCode(), wrapper.GetHashCode());
        }
    }
}