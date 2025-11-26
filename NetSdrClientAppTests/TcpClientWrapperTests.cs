using System;
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
            // Arrange
            string host = "127.0.0.1";
            int port = 8080;

            // Act
            var wrapper = new TcpClientWrapper(host, port);

            // Assert
            Assert.NotNull(wrapper);
        }

        [Fact]
        public void Connected_Initially_ShouldReturnFalse()
        {
            // Arrange
            var wrapper = new TcpClientWrapper("127.0.0.1", 8080);

            // Act
            bool isConnected = wrapper.Connected;

            // Assert
            Assert.False(isConnected);
        }

        [Fact]
        public async Task SendMessageAsync_WhenNotConnected_ShouldThrowException()
        {
            // Arrange
            var wrapper = new TcpClientWrapper("127.0.0.1", 8080);
            byte[] data = new byte[] { 1, 2, 3 };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await wrapper.SendMessageAsync(data)
            );
        }
    }
}