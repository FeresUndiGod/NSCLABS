using Xunit;
using EchoServer;
using System.Text;

namespace EchoTcpServerTests
{
    public class EchoHandlerTests
    {
        [Fact]
        public void Process_ShouldReturnSameData()
        {
            var handler = new EchoMessageHandler();
            var input = Encoding.UTF8.GetBytes("Test");
            var result = handler.Process(input);
            Assert.Equal(input, result);
        }
    }
}