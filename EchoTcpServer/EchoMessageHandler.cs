using System;

namespace EchoServer
{
    public class EchoMessageHandler : IMessageHandler
    {
        public byte[] Process(byte[] data)
        {
            if (data == null || data.Length == 0) 
                return Array.Empty<byte>();
            
            return data;
        }
    }
}