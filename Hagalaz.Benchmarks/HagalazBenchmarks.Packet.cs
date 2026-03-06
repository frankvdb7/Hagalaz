using BenchmarkDotNet.Attributes;
using Hagalaz.Network.Common;
using System;
using System.Linq;
using System.Text;

namespace Hagalaz.Benchmarks
{
    public partial class HagalazBenchmarks
    {
        private byte[] _packetData = null!;
        private string _testString = null!;
        private byte[] _stringInPacket = null!;

        private void SetupPacket()
        {
            _packetData = Enumerable.Range(0, 100).Select(i => (byte)i).ToArray();
            _testString = "This is a test string for benchmarking Packet operations.";

            var composer = new GenericPacketComposer();
            composer.AppendString(_testString);
            _stringInPacket = composer.SerializeBuffer();
        }

        [Benchmark]
        public byte[] PacketComposer_Serialize_Fixed()
        {
            var composer = new GenericPacketComposer(128);
            composer.SetOpcode(10);
            composer.SetType(SizeType.Fixed);
            composer.AppendBytes(_packetData);
            return composer.Serialize();
        }

        [Benchmark]
        public byte[] PacketComposer_Serialize_VarByte()
        {
            var composer = new GenericPacketComposer(128);
            composer.SetOpcode(10);
            composer.SetType(SizeType.Byte);
            composer.AppendBytes(_packetData);
            return composer.Serialize();
        }

        [Benchmark]
        public string Packet_ReadString()
        {
            using var packet = new Packet(_stringInPacket);
            return packet.ReadString();
        }

        [Benchmark]
        public void PacketComposer_AppendString()
        {
            var composer = new GenericPacketComposer(128);
            composer.AppendString(_testString);
        }

        [Benchmark]
        public string Packet_ToString()
        {
            using var packet = new Packet(10, SizeType.Fixed, _packetData);
            return packet.ToString();
        }
    }
}
