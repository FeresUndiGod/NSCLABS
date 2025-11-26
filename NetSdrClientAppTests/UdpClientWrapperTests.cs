using System;
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
            // Arrange
            int port = 0;

            // Act
            var wrapper = new UdpClientWrapper(port);

            // Assert
            Assert.NotNull(wrapper);
        }

        [Fact]
        public void StopListening_WhenNotStarted_ShouldNotThrow()
        {
            // Arrange
            var wrapper = new UdpClientWrapper(0);

            // Act & Assert
            wrapper.StopListening();
        }

        [Fact]
        public void GetHashCode_ShouldReturnConsistentValue()
        {
            // Arrange
            var wrapper = new UdpClientWrapper(12345);

            // Act
            int hash1 = wrapper.GetHashCode();
            int hash2 = wrapper.GetHashCode();

            // Assert
            Assert.Equal(hash1, hash2);
        }
    }
}