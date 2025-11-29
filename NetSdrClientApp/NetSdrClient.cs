using NetSdrClientApp.Messages;
using NetSdrClientApp.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetSdrClientApp
{
    public class NetSdrClient
    {
        private ITcpClient _tcpClient;
        private IUdpClient _udpClient;

        public bool IQStarted { get; private set; }

        public NetSdrClient(ITcpClient tcpClient, IUdpClient udpClient)
        {
            _tcpClient = tcpClient;
            _udpClient = udpClient;

            _tcpClient.MessageReceived += _tcpClient_MessageReceived;
            _udpClient.MessageReceived += _udpClient_MessageReceived;
        }

        public async Task ConnectAsync()
        {
            if (!_tcpClient.Connected)
            {
                _tcpClient.Connect();
            }

            // Init setup
            var sampleRate = BitConverter.GetBytes((long)100000).Take(5).ToArray();
            var automaticFilterMode = BitConverter.GetBytes((ushort)0).ToArray();
            var adMode = new byte[] { 0x00, 0x03 };

            // Host pre setup
            var msgs = new List<byte[]>
            {
                NetSdrMessageHelper.GetControlItemMessage(MsgTypes.SetControlItem, ControlItemCodes.IQOutputDataSampleRate, sampleRate),
                NetSdrMessageHelper.GetControlItemMessage(MsgTypes.SetControlItem, ControlItemCodes.RFFilter, automaticFilterMode),
                NetSdrMessageHelper.GetControlItemMessage(MsgTypes.SetControlItem, ControlItemCodes.ADModes, adMode),
            };

            foreach (var msg in msgs)
            {
                await SendTcpRequest(msg);
            }
        }

        // --- ОСЬ ЦЕЙ МЕТОД, ЯКОГО НЕ ВИСТАЧАЛО ---
        public void Disconnect()
        {
            _tcpClient.Disconnect();
        }
        // ------------------------------------------

        public async Task StartIQAsync()
        {
            if (!_tcpClient.Connected)
            {
                Console.WriteLine("No active connection.");
                return;
            }

            var iqDataMode = (byte)0x80;
            var start = (byte)0x02;
            var fifo16bitCaptureMode = (byte)0x01;
            var n = (byte)1;

            var args = new[] { iqDataMode, start, fifo16bitCaptureMode, n };
            var msg = NetSdrMessageHelper.GetControlItemMessage(MsgTypes.SetControlItem, ControlItemCodes.ReceiverState, args);

            await SendTcpRequest(msg);
            
            // Запускаємо UDP
            _ = _udpClient.StartListeningAsync();
            
            IQStarted = true;
        }

        public async Task StopIQAsync()
        {
            if (!_tcpClient.Connected)
            {
                Console.WriteLine("No active connection.");
                return;
            }

            var stop = (byte)0x01;
            var args = new byte[] { 0, stop, 0, 0 };
            var msg = NetSdrMessageHelper.GetControlItemMessage(MsgTypes.SetControlItem, ControlItemCodes.ReceiverState, args);

            await SendTcpRequest(msg);
            
            // Зупиняємо UDP
            _udpClient.StopListening();
            
            IQStarted = false;
        }

        public async Task ChangeFrequencyAsync(long hz, int channel)
        {
            var channelArg = (byte)channel;
            var frequencyArg = BitConverter.GetBytes(hz).Take(5);
            var args = new[] { channelArg }.Concat(frequencyArg).ToArray();

            var msg = NetSdrMessageHelper.GetControlItemMessage(MsgTypes.SetControlItem, ControlItemCodes.ReceiverFrequency, args);

            await SendTcpRequest(msg);
        }

        private async Task<byte[]> SendTcpRequest(byte[] msg)
        {
            if (!_tcpClient.Connected)
            {
                Console.WriteLine("No active connection.");
                return null;
            }

            // У реальному коді тут може бути TaskCompletionSource для очікування відповіді,
            // але для спрощення ми просто відправляємо.
            await _tcpClient.SendMessageAsync(msg);
            return null; 
        }

        private void _tcpClient_MessageReceived(object? sender, byte[] e)
        {
            // Обробка вхідних повідомлень від TCP
            Console.WriteLine($"TCP Message received: {e.Length} bytes");
        }

        private void _udpClient_MessageReceived(object? sender, byte[] e)
        {
            // Обробка UDP пакетів (IQ даних)
            // Тут можна викликати NetSdrMessageHelper.GetSamples(...)
        }
    }
}