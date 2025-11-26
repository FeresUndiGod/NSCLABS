namespace EchoServer
{
    public interface IMessageHandler
    {
        byte[] Process(byte[] data);
    }
}