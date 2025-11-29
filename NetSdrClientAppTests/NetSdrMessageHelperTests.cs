using Xunit;
using NetSdrClientApp.Messages;
using System;
using System.Linq;
using System.Collections.Generic;
using static NetSdrClientApp.Messages.NetSdrMessageHelper;

namespace NetSdrClientAppTests
{
    public class NetSdrMessageHelperTests
    {
        // ==================== EXISTING TESTS ====================
        
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
            Assert.Equal(result.Length, lengthInHeader & 0x1FFF);
        }

        [Fact]
        public void GetDataItemMessage_ShouldGenerateCorrectBytes()
        {
            var type = MsgTypes.DataItem0;
            byte[] data = new byte[] { 0xAA, 0xBB, 0xCC };

            byte[] result = NetSdrMessageHelper.GetDataItemMessage(type, data);

            Assert.NotNull(result);
            Assert.Equal(5, result.Length); // 2 header + 3 data
            Assert.Equal(0xAA, result[2]); // Data starts at index 2
        }

        [Fact]
        public void GetDataItemMessage_DoesNotIncludeSequenceNumber()
        {
            // GetDataItemMessage should NOT include sequence number
            // Sequence number is added at protocol level, not message level
            var type = MsgTypes.DataItem1;
            byte[] data = new byte[] { 0x01, 0x02 };

            byte[] result = NetSdrMessageHelper.GetDataItemMessage(type, data);

            // Should be: 2 (header) + 2 (data) = 4 bytes total
            Assert.Equal(4, result.Length);
            
            // Verify header encoding
            ushort header = BitConverter.ToUInt16(result, 0);
            MsgTypes parsedType = (MsgTypes)(header >> 13);
            int length = header & 0x1FFF;
            
            Assert.Equal(type, parsedType);
            Assert.Equal(4, length); // Total message length
        }

        [Fact]
        public void GetControlItemMessage_ShouldThrow_WhenPayloadIsTooLarge()
        {
            byte[] hugePayload = new byte[9000]; 

            Assert.Throws<ArgumentException>(() => 
                NetSdrMessageHelper.GetControlItemMessage(MsgTypes.SetControlItem, ControlItemCodes.None, hugePayload));
        }

        [Fact]
        public void TranslateMessage_ShouldParseValidMessage()
        {
            // Create a proper control item message
            byte[] message = NetSdrMessageHelper.GetControlItemMessage(
                MsgTypes.Ack, 
                ControlItemCodes.None, 
                Array.Empty<byte>());

            bool success = NetSdrMessageHelper.TranslateMessage(message, out MsgTypes type, out ControlItemCodes code, out ushort seq, out byte[] body);

            Assert.True(success);
            Assert.Equal(MsgTypes.Ack, type);
            Assert.Empty(body);
        }

        [Fact]
        public void TranslateMessage_ShouldReturnFalse_WhenItemCodeIsInvalid()
        {
            // Create message with invalid item code
            byte[] header = BitConverter.GetBytes((ushort)6);
            byte[] invalidCode = BitConverter.GetBytes((ushort)9999);
            byte[] message = header.Concat(invalidCode).ToArray();

            bool success = NetSdrMessageHelper.TranslateMessage(message, out MsgTypes type, out ControlItemCodes code, out ushort seq, out byte[] body);

            Assert.False(success);
        }

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

        [Fact]
        public void GetSamples_ShouldThrow_WhenSampleSizeIsTooLarge()
        {
            ushort invalidSize = 40; 
            byte[] body = new byte[10];

            Assert.Throws<ArgumentOutOfRangeException>(() => 
                NetSdrMessageHelper.GetSamples(invalidSize, body).ToList());
        }

        // ==================== NEW TESTS FOR COVERAGE ====================

        [Theory]
        [InlineData(MsgTypes.SetControlItem)]
        [InlineData(MsgTypes.CurrentControlItem)]
        [InlineData(MsgTypes.ControlItemRange)]
        [InlineData(MsgTypes.Ack)]
        public void GetControlItemMessage_ShouldHandleAllControlTypes(MsgTypes type)
        {
            byte[] result = NetSdrMessageHelper.GetControlItemMessage(type, ControlItemCodes.ReceiverFrequency, new byte[] { 0x01 });
            
            Assert.NotNull(result);
            Assert.True(result.Length > 2);
        }

        [Theory]
        [InlineData(MsgTypes.DataItem0)]
        [InlineData(MsgTypes.DataItem1)]
        [InlineData(MsgTypes.DataItem2)]
        [InlineData(MsgTypes.DataItem3)]
        public void GetDataItemMessage_ShouldHandleAllDataTypes(MsgTypes type)
        {
            byte[] result = NetSdrMessageHelper.GetDataItemMessage(type, new byte[] { 0xFF });
            
            Assert.NotNull(result);
            ushort header = BitConverter.ToUInt16(result, 0);
            MsgTypes parsedType = (MsgTypes)(header >> 13);
            Assert.Equal(type, parsedType);
        }

        [Theory]
        [InlineData(ControlItemCodes.IQOutputDataSampleRate)]
        [InlineData(ControlItemCodes.RFFilter)]
        [InlineData(ControlItemCodes.ADModes)]
        [InlineData(ControlItemCodes.ReceiverState)]
        [InlineData(ControlItemCodes.ReceiverFrequency)]
        public void TranslateMessage_ShouldParseAllValidItemCodes(ControlItemCodes itemCode)
        {
            byte[] message = NetSdrMessageHelper.GetControlItemMessage(MsgTypes.SetControlItem, itemCode, new byte[] { 0x01 });
            
            bool success = NetSdrMessageHelper.TranslateMessage(message, out MsgTypes type, out ControlItemCodes code, out ushort seq, out byte[] body);
            
            Assert.True(success);
            Assert.Equal(itemCode, code);
        }

        [Fact]
        public void TranslateMessage_ShouldParseDataItemWithSequenceNumber()
        {
            // Manually create a DataItem message with sequence number
            byte[] data = new byte[] { 0xAA, 0xBB };
            ushort sequenceNum = 0x1234;
            
            // Header: length=6 (2+2+2), type=DataItem2 (6)
            // 6 + (6 << 13) = 6 + 49152 = 49158 = 0xC006
            byte[] header = BitConverter.GetBytes((ushort)0xC006);
            byte[] seqBytes = BitConverter.GetBytes(sequenceNum);
            byte[] message = header.Concat(seqBytes).Concat(data).ToArray();
            
            bool success = NetSdrMessageHelper.TranslateMessage(message, out MsgTypes type, out ControlItemCodes code, out ushort seq, out byte[] body);
            
            Assert.True(success);
            Assert.Equal(MsgTypes.DataItem2, type);
            Assert.Equal(ControlItemCodes.None, code);
            Assert.Equal(sequenceNum, seq);
            Assert.Equal(2, body.Length);
            Assert.Equal(0xAA, body[0]);
            Assert.Equal(0xBB, body[1]);
        }

        [Fact]
        public void GetSamples_ShouldHandle8BitSamples()
        {
            ushort sampleSize = 8;
            byte[] body = new byte[] { 0x01, 0x02, 0x03, 0x04 };
            
            var samples = NetSdrMessageHelper.GetSamples(sampleSize, body).ToList();
            
            Assert.Equal(4, samples.Count);
            Assert.Equal(1, samples[0]);
            Assert.Equal(2, samples[1]);
        }

        [Fact]
        public void GetSamples_ShouldHandle24BitSamples()
        {
            ushort sampleSize = 24;
            byte[] body = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 };
            
            var samples = NetSdrMessageHelper.GetSamples(sampleSize, body).ToList();
            
            Assert.Equal(2, samples.Count);
        }

        [Fact]
        public void GetSamples_ShouldHandle32BitSamples()
        {
            ushort sampleSize = 32;
            byte[] body = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF };
            
            var samples = NetSdrMessageHelper.GetSamples(sampleSize, body).ToList();
            
            Assert.Single(samples);
            Assert.Equal(-1, samples[0]);
        }

        [Fact]
        public void GetSamples_ShouldHandlePartialData()
        {
            ushort sampleSize = 16;
            byte[] body = new byte[] { 0x01, 0x00, 0x02 }; // Only 3 bytes, last sample incomplete
            
            var samples = NetSdrMessageHelper.GetSamples(sampleSize, body).ToList();
            
            Assert.Single(samples); // Should only return 1 complete sample
        }

        [Fact]
        public void GetControlItemMessage_ShouldHandleEmptyParameters()
        {
            byte[] result = NetSdrMessageHelper.GetControlItemMessage(MsgTypes.Ack, ControlItemCodes.None, Array.Empty<byte>());
            
            Assert.NotNull(result);
            Assert.Equal(2, result.Length); // Only header
        }

        [Fact]
        public void GetControlItemMessage_ShouldHandleLargeValidPayload()
        {
            byte[] payload = new byte[8000];
            
            byte[] result = NetSdrMessageHelper.GetControlItemMessage(MsgTypes.SetControlItem, ControlItemCodes.ReceiverState, payload);
            
            Assert.NotNull(result);
        }

        [Fact]
        public void TranslateMessage_ShouldReturnFalse_WhenBodyLengthMismatch()
        {
            // Create message with incorrect length in header
            byte[] header = BitConverter.GetBytes((ushort)10); // Says 10 bytes
            byte[] code = BitConverter.GetBytes((ushort)ControlItemCodes.ReceiverState);
            byte[] shortBody = new byte[] { 0x01 }; // But body is only 1 byte (total would be 5)
            byte[] message = header.Concat(code).Concat(shortBody).ToArray();
            
            bool success = NetSdrMessageHelper.TranslateMessage(message, out MsgTypes type, out ControlItemCodes itemCode, out ushort seq, out byte[] body);
            
            Assert.False(success);
        }

        [Fact]
        public void GetDataItemMessage_ShouldHandleMaxDataItemLength()
        {
            // Max data for DataItem: 8191 (max message) - 2 (header) = 8189 bytes
            byte[] maxData = new byte[8189];
            
            byte[] result = NetSdrMessageHelper.GetDataItemMessage(MsgTypes.DataItem0, maxData);
            
            Assert.NotNull(result);
            Assert.Equal(8191, result.Length); // 2 header + 8189 data
        }

        [Fact]
        public void TranslateMessage_ShouldHandleMinimumValidMessage()
        {
            // Create proper minimum message using helper
            byte[] message = NetSdrMessageHelper.GetControlItemMessage(
                MsgTypes.Ack, 
                ControlItemCodes.None, 
                Array.Empty<byte>());
            
            bool success = NetSdrMessageHelper.TranslateMessage(message, out MsgTypes type, out ControlItemCodes code, out ushort seq, out byte[] body);
            
            Assert.True(success);
            Assert.Equal(MsgTypes.Ack, type);
            Assert.Empty(body);
        }
    }
}