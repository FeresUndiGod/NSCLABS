using Xunit;
using NetSdrClientApp.Messages;
using System;
using System.Linq;
using System.Collections.Generic;

// === ВАЖЛИВО: Цей рядок виправляє помилку CS0103 ===
using static NetSdrClientApp.Messages.NetSdrMessageHelper;

namespace NetSdrClientAppTests
{
    public class NetSdrMessageHelperTests
    {
        // 1. Тест успішного створення ControlItem (Happy Path)
        [Fact]
        public void GetControlItemMessage_ShouldGenerateCorrectHeaderAndBody()
        {
            var type = MsgTypes.SetControlItem;
            var itemCode = ControlItemCodes.ReceiverState;
            byte[] paramsData = new byte[] { 0x01, 0x02 };

            byte[] result = NetSdrMessageHelper.GetControlItemMessage(type, itemCode, paramsData);

            Assert.NotNull(result);
            Assert.True(result.Length >= 4);
            ushort lengthInHeader = BitConverter.ToUInt16(result, 0);
            Assert.Equal(result.Length, lengthInHeader);
        }

        // 2. Тест успішного створення DataItem
        [Fact]
        public void GetDataItemMessage_ShouldGenerateCorrectBytes()
        {
            var type = MsgTypes.DataItem0;
            byte[] data = new byte[] { 0xAA, 0xBB, 0xCC };

            byte[] result = NetSdrMessageHelper.GetDataItemMessage(type, data);

            Assert.NotNull(result);
            Assert.Equal(7, result.Length);
            Assert.Equal(0xAA, result[4]); 
        }

        // 3. Тест на помилку розміру
        [Fact]
        public void GetControlItemMessage_ShouldThrow_WhenPayloadIsTooLarge()
        {
            byte[] hugePayload = new byte[9000]; 

            Assert.Throws<ArgumentException>(() => 
                NetSdrMessageHelper.GetControlItemMessage(MsgTypes.SetControlItem, ControlItemCodes.None, hugePayload));
        }

        // 4. Тест успішного парсингу
        [Fact]
        public void TranslateMessage_ShouldParseValidMessage()
        {
            // Length=4, Type=Ack(0)
            byte[] message = new byte[] { 0x04, 0x00, 0x00, 0x00 };

            bool success = NetSdrMessageHelper.TranslateMessage(message, out MsgTypes type, out ControlItemCodes code, out ushort seq, out byte[] body);

            Assert.True(success);
            Assert.Equal(MsgTypes.Ack, type);
            Assert.Empty(body);
        }

        // 5. Тест на невідомий ControlItemCode
        [Fact]
        public void TranslateMessage_ShouldReturnFalse_WhenItemCodeIsInvalid()
        {
            ushort len = 6;
            ushort typeVal = (ushort)MsgTypes.SetControlItem;
            ushort invalidCode = 9999;

            var lenBytes = BitConverter.GetBytes(len);
            var typeBytes = BitConverter.GetBytes(typeVal);
            var codeBytes = BitConverter.GetBytes(invalidCode);

            byte[] message = lenBytes.Concat(typeBytes).Concat(codeBytes).ToArray();

            bool success = NetSdrMessageHelper.TranslateMessage(message, out MsgTypes type, out ControlItemCodes code, out ushort seq, out byte[] body);

            Assert.False(success, "Should fail because ControlItemCode is invalid");
        }

        // 6. Тест семплів
        [Fact]
        public void GetSamples_ShouldConvertBytesToShorts()
        {
            ushort sampleSize = 16; 
            byte[] body = new byte[] { 0x01, 0x00, 0x02, 0x00 };

            var samples = NetSdrMessageHelper.GetSamples(sampleSize, body).ToList();

            Assert.Equal(2, samples.Count);
            Assert.Equal(1, samples[0]);
            Assert.Equal(2, samples[1]);
        }

        // 7. Тест помилки семплів
        [Fact]
        public void GetSamples_ShouldThrow_WhenSampleSizeIsTooLarge()
        {
            ushort invalidSize = 40; 
            byte[] body = new byte[10];

            Assert.Throws<ArgumentOutOfRangeException>(() => 
                NetSdrMessageHelper.GetSamples(invalidSize, body));
        }
    }
}