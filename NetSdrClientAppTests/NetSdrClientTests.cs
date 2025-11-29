using Xunit;
using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;
using NetSdrClientApp.Messages;
using System.Threading.Tasks;
using System.Threading;
using System;

// Цей рядок важливий, щоб бачити Enums
using static NetSdrClientApp.Messages.NetSdrMessageHelper;

namespace NetSdrClientAppTests
{
    public class NetSdrClientTests
    {
        private readonly Mock<ITcpClient> _mockTcp;
        private readonly Mock<IUdpClient> _mockUdp;
        private NetSdrClient _client;

        public NetSdrClientTests()
        {
            _mockTcp = new Mock<ITcpClient>();
            _mockUdp = new Mock<IUdpClient>();

            // Базова ініціалізація
            _client = new NetSdrClient(_mockTcp.Object, _mockUdp.Object);
        }

        [Fact]
        public async Task ConnectAsync_ShouldCallConnect_WhenNotConnected()
        {
            // Arrange
            // === ВИПРАВЛЕННЯ ТУТ ===
            // Ми використовуємо змінну, щоб імітувати зміну стану підключення
            bool isConnected = false;
            
            // Коли питають Connected, повертаємо значення змінної
            _mockTcp.Setup(t => t.Connected).Returns(() => isConnected);
            
            // Коли викликають Connect(), змінюємо змінну на true
            _mockTcp.Setup(t => t.Connect()).Callback(() => isConnected = true);

            // Налаштування для SendMessageAsync
            _mockTcp.Setup(t => t.SendMessageAsync(It.IsAny<byte[]>()))
                .Returns(Task.CompletedTask);

            // Act
            await _client.ConnectAsync();

            // Assert
            _mockTcp.Verify(t => t.Connect(), Times.Once);
            // Тепер це пройде, бо Connected стало true і код дійшов до відправки
            _mockTcp.Verify(t => t.SendMessageAsync(It.IsAny<byte[]>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task StartIQAsync_ShouldStartUdp_WhenConnected()
        {
            // Arrange
            _mockTcp.Setup(t => t.Connected).Returns(true);
            
            _mockTcp.Setup(t => t.SendMessageAsync(It.IsAny<byte[]>()))
                   .Returns(Task.CompletedTask);

            // Act
            await _client.StartIQAsync();

            // Assert
            _mockUdp.Verify(u => u.StartListeningAsync(), Times.Once);
            _mockTcp.Verify(t => t.SendMessageAsync(It.IsAny<byte[]>()), Times.AtLeastOnce);
            Assert.True(_client.IQStarted);
        }

        [Fact]
        public async Task StartIQAsync_ShouldNotStart_WhenDisconnected()
        {
            // Arrange
            _mockTcp.Setup(t => t.Connected).Returns(false);

            // Act
            await _client.StartIQAsync();

            // Assert
            _mockUdp.Verify(u => u.StartListeningAsync(), Times.Never);
        }

        [Fact]
        public async Task StopIQAsync_ShouldStopUdp_WhenConnected()
        {
            // Arrange
            _mockTcp.Setup(t => t.Connected).Returns(true);
            _mockTcp.Setup(t => t.SendMessageAsync(It.IsAny<byte[]>()))
                   .Returns(Task.CompletedTask);

            // Act
            await _client.StopIQAsync();

            // Assert
            _mockUdp.Verify(u => u.StopListening(), Times.Once);
            Assert.False(_client.IQStarted);
        }

        [Fact]
        public async Task ChangeFrequencyAsync_ShouldSendTcpCommand()
        {
            // Arrange
            _mockTcp.Setup(t => t.Connected).Returns(true);
            _mockTcp.Setup(t => t.SendMessageAsync(It.IsAny<byte[]>()))
                   .Returns(Task.CompletedTask);

            // Act
            await _client.ChangeFrequencyAsync(1000000, 0);

            // Assert
            _mockTcp.Verify(t => t.SendMessageAsync(It.IsAny<byte[]>()), Times.Once);
        }

        [Fact]
        public void Disconnect_ShouldCallWrappersDisconnect()
        {
            // Act
            _client.Disconnect();

            // Assert
            _mockTcp.Verify(t => t.Disconnect(), Times.Once);
        }
    }
}