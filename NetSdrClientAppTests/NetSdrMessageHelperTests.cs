using Xunit;
using NetSdrClientApp.Messages;
using System;
using System.Linq;
using System.Collections.Generic;

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

        // 2. Тест успішного створення DataItem (Покриває GetDataItemMessage і GetMessage)
        [Fact]
        public void GetDataItemMessage_ShouldGenerateCorrectBytes()
        {
            var type = MsgTypes.DataItem0;
            byte[] data = new byte[] { 0xAA, 0xBB, 0xCC };

            // Цей метод викликає той самий приватний GetMessage, але з іншими параметрами
            byte[] result = NetSdrMessageHelper.GetDataItemMessage(type, data);

            Assert.NotNull(result);
            // Header (2) + Type (2) + Data (3) = 7 bytes
            Assert.Equal(7, result.Length);
            Assert.Equal(0xAA, result[4]); // Перевіряємо, що дані записались
        }

        // 3. Тест на помилку розміру (Покриває Exception в GetHeader)
        [Fact]
        public void GetControlItemMessage_ShouldThrow_WhenPayloadIsTooLarge()
        {
            // Створюємо масив, більший за 8192 байти (MaxMessageLength)
            byte[] hugePayload = new byte[9000]; 

            // Це має викликати ArgumentException у приватному методі GetHeader
            Assert.Throws<ArgumentException>(() => 
                NetSdrMessageHelper.GetControlItemMessage(MsgTypes.SetControlItem, ControlItemCodes.None, hugePayload));
        }

        // 4. Тест успішного парсингу (Happy Path для TranslateMessage)
        [Fact]
        public void TranslateMessage_ShouldParseValidMessage()
        {
            // Імітуємо пакет: Length=4, Type=Ack (0)
            // Заголовок (Length) = 4 (0x04 0x00)
            // Тип (Type) = 0 (0x00 0x00) - Ack
            byte[] message = new byte[] { 0x04, 0x00, 0x00, 0x00 };

            bool success = NetSdrMessageHelper.TranslateMessage(message, out MsgTypes type, out ControlItemCodes code, out ushort seq, out byte[] body);

            Assert.True(success);
            Assert.Equal(MsgTypes.Ack, type);
            Assert.Empty(body);
        }

        // 5. Тест на невідомий ControlItemCode (Покриває else { success = false } в TranslateMessage)
        [Fact]
        public void TranslateMessage_ShouldReturnFalse_WhenItemCodeIsInvalid()
        {
            // Формуємо пакет: 
            // Length = 6 (Header 2 + Type 2 + InvalidCode 2)
            // Type = SetControlItem (це важливо, щоб код зайшов у switch case)
            // InvalidCode = 9999 (якого немає в Enum)
            
            ushort len = 6;
            ushort typeVal = (ushort)MsgTypes.SetControlItem;
            ushort invalidCode = 9999;

            var lenBytes = BitConverter.GetBytes(len);
            var typeBytes = BitConverter.GetBytes(typeVal);
            var codeBytes = BitConverter.GetBytes(invalidCode);

            byte[] message = lenBytes.Concat(typeBytes).Concat(codeBytes).ToArray();

            // Act
            bool success = NetSdrMessageHelper.TranslateMessage(message, out MsgTypes type, out ControlItemCodes code, out ushort seq, out byte[] body);

            // Assert
            Assert.False(success, "Should fail because ControlItemCode is invalid");
        }

        // 6. Тест семплів (Happy Path)
        [Fact]
        public void GetSamples_ShouldConvertBytesToShorts()
        {
            ushort sampleSize = 16; // 16 біт
            byte[] body = new byte[] { 0x01, 0x00, 0x02, 0x00 };

            var samples = NetSdrMessageHelper.GetSamples(sampleSize, body).ToList();

            Assert.Equal(2, samples.Count);
            Assert.Equal(1, samples[0]);
            Assert.Equal(2, samples[1]);
        }

        // 7. Тест помилки семплів (Покриває throw ArgumentOutOfRangeException в GetSamples)
        [Fact]
        public void GetSamples_ShouldThrow_WhenSampleSizeIsTooLarge()
        {
            // sampleSize > 4 (тобто більше 32 біт), код має викинути помилку
            ushort invalidSize = 40; // 5 байт
            byte[] body = new byte[10];

            Assert.Throws<ArgumentOutOfRangeException>(() => 
                NetSdrMessageHelper.GetSamples(invalidSize, body));
        }
    }
}