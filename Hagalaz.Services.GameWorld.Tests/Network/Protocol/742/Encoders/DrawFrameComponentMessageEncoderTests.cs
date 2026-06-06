using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Services.GameWorld.Network.Protocol._742.Encoders;
using Raido.Common.Protocol;
using Hagalaz.Services.GameWorld.Extensions;
using Raido.Server.Extensions;
using Raido.Common.Buffers;

namespace Hagalaz.Services.GameWorld.Tests.Network.Protocol._742.Encoders
{
    [TestClass]
    public class DrawFrameComponentMessageEncoderTests
    {
        private DrawFrameComponentMessageEncoder _encoder;
        private IRaidoMessageBinaryWriter _outputMock;

        [TestInitialize]
        public void Setup()
        {
            _encoder = new DrawFrameComponentMessageEncoder();
            _outputMock = Substitute.For<IRaidoMessageBinaryWriter>();

            _outputMock.SetOpcode(Arg.Any<int>()).Returns(_outputMock);
            _outputMock.SetSize(Arg.Any<RaidoMessageSize>()).Returns(_outputMock);
            _outputMock.WriteByte(Arg.Any<byte>()).Returns(_outputMock);
        }

        [TestMethod]
        public void EncodeMessage_ForceRedrawTrue_WritesTwo()
        {
            // Arrange
            var message = new DrawFrameComponentMessage { Id = 1, ForceRedraw = true };

            // Act
            _encoder.EncodeMessage(message, _outputMock);

            // Assert
            // WriteByteS(2) calls WriteByte(128 - 2) = WriteByte(126)
            _outputMock.Received(1).WriteByte(126);
        }

        [TestMethod]
        public void EncodeMessage_ForceRedrawFalse_WritesZero()
        {
            // Arrange
            var message = new DrawFrameComponentMessage { Id = 1, ForceRedraw = false };

            // Act
            _encoder.EncodeMessage(message, _outputMock);

            // Assert
            // WriteByteS(0) calls WriteByte(128 - 0) = WriteByte(128)
            _outputMock.Received(1).WriteByte(128);
        }
    }
}
