using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using Moq;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Model;
using Xunit;

namespace Hagalaz.Cache.Tests
{
    public class MemoryStreamSpy : MemoryStream
    {
        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }

    [Collection(CacheApiTelemetryCollection.Name)]
    public class CacheApiTests
    {
        private readonly Mock<IFileStore> _fileStoreMock;
        private readonly Mock<IReferenceTableProvider> _referenceTableProviderMock;
        private readonly Mock<ICacheWriter> _cacheWriterMock;
        private readonly Mock<IContainerDecoder> _containerDecoderMock;
        private readonly Mock<IReferenceTableCodec> _referenceTableCodecMock;
        private readonly Mock<IArchiveDecoder> _archiveDecoderMock;
        private readonly CacheApi _cacheApi;

        public CacheApiTests()
        {
            _fileStoreMock = new Mock<IFileStore>();
            _referenceTableProviderMock = new Mock<IReferenceTableProvider>();
            _cacheWriterMock = new Mock<ICacheWriter>();
            _containerDecoderMock = new Mock<IContainerDecoder>();
            _referenceTableCodecMock = new Mock<IReferenceTableCodec>();
            _archiveDecoderMock = new Mock<IArchiveDecoder>();
            _cacheApi = new CacheApi(_fileStoreMock.Object, _referenceTableProviderMock.Object, _cacheWriterMock.Object, _containerDecoderMock.Object, _referenceTableCodecMock.Object, _archiveDecoderMock.Object);
        }

        [Fact]
        public void GetFileId_ShouldReturnFileId()
        {
            // Arrange
            var referenceTableMock = new Mock<IReferenceTable>();
            referenceTableMock.Setup(rt => rt.GetFileId("test_file")).Returns(123);
            _referenceTableProviderMock.Setup(rtm => rtm.ReadReferenceTable(1)).Returns(referenceTableMock.Object);

            // Act
            var fileId = _cacheApi.GetFileId(1, "test_file");

            // Assert
            Assert.Equal(123, fileId);
        }

        [Fact]
        public void ReadContainer_ReturnsContainer()
        {
            // Arrange
            var expectedContainer = new Mock<IContainer>().Object;
            _containerDecoderMock.Setup(f => f.Decode(It.IsAny<System.IO.MemoryStream>())).Returns(expectedContainer);
            _fileStoreMock.SetupGet(fs => fs.IndexFileCount).Returns(1);
            _fileStoreMock.Setup(fs => fs.Read(It.IsAny<int>(), It.IsAny<int>())).Returns(new System.IO.MemoryStream());

            // Act
            var result = _cacheApi.ReadContainer(0, 0);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedContainer, result);
        }

        [Fact]
        public void ReadContainer_ShouldDisposeStream()
        {
            // Arrange
            var streamSpy = new MemoryStreamSpy();
            _fileStoreMock.SetupGet(fs => fs.IndexFileCount).Returns(1);
            _fileStoreMock.Setup(fs => fs.Read(It.IsAny<int>(), It.IsAny<int>())).Returns(streamSpy);
            _containerDecoderMock.Setup(f => f.Decode(It.IsAny<System.IO.MemoryStream>())).Returns(new Mock<IContainer>().Object);

            // Act
            _cacheApi.ReadContainer(0, 0);

            // Assert
            Assert.True(streamSpy.IsDisposed);
        }

        [Fact]
        public void ReadArchive_RecordsSuccessfulArchiveAndContainerDecodeMetrics()
        {
            SetupArchiveRead(CompressionType.Gzip);
            using var measurements = new CacheMeterListener();

            _cacheApi.ReadArchive(0, 0);

            var archiveLoad = Assert.Single(measurements.Measurements, measurement =>
                measurement.InstrumentName == "hagalaz.cache.archive.load");
            Assert.Equal(1d, archiveLoad.Value);
            Assert.Equal("success", archiveLoad.Tags["outcome"]);
            var archiveDuration = Assert.Single(measurements.Measurements, measurement =>
                measurement.InstrumentName == "hagalaz.cache.archive.load.duration");
            Assert.Equal("success", archiveDuration.Tags["outcome"]);
            var containerDuration = Assert.Single(measurements.Measurements, measurement =>
                measurement.InstrumentName == "hagalaz.cache.container.decode.duration");
            Assert.Equal("gzip", containerDuration.Tags["compression"]);
            Assert.Equal("success", containerDuration.Tags["outcome"]);
        }

        [Fact]
        public void ReadArchive_WhenContainerDecodeFails_RecordsFailureWithUnknownCompression()
        {
            SetupArchiveRead(CompressionType.Gzip);
            _containerDecoderMock
                .Setup(decoder => decoder.Decode(It.IsAny<System.IO.MemoryStream>()))
                .Throws<InvalidDataException>();
            using var measurements = new CacheMeterListener();

            Assert.Throws<InvalidDataException>(() => _cacheApi.ReadArchive(0, 0));

            var archiveLoad = Assert.Single(measurements.Measurements, measurement =>
                measurement.InstrumentName == "hagalaz.cache.archive.load");
            Assert.Equal("failure", archiveLoad.Tags["outcome"]);
            var archiveDuration = Assert.Single(measurements.Measurements, measurement =>
                measurement.InstrumentName == "hagalaz.cache.archive.load.duration");
            Assert.Equal("failure", archiveDuration.Tags["outcome"]);
            var containerDuration = Assert.Single(measurements.Measurements, measurement =>
                measurement.InstrumentName == "hagalaz.cache.container.decode.duration");
            Assert.Equal("unknown", containerDuration.Tags["compression"]);
            Assert.Equal("failure", containerDuration.Tags["outcome"]);
        }

        private void SetupArchiveRead(CompressionType compression)
        {
            _fileStoreMock.SetupGet(store => store.IndexFileCount).Returns(1);
            _fileStoreMock.Setup(store => store.Read(0, 0)).Returns(new System.IO.MemoryStream());

            var entry = new Mock<IReferenceTableEntry>();
            entry.SetupGet(reference => reference.Capacity).Returns(1);
            var table = new Mock<IReferenceTable>();
            table.SetupGet(reference => reference.Capacity).Returns(1);
            table.Setup(reference => reference.GetEntry(0)).Returns(entry.Object);
            _referenceTableProviderMock.Setup(provider => provider.ReadReferenceTable(0)).Returns(table.Object);

            var container = new Mock<IContainer>();
            container.SetupGet(decoded => decoded.CompressionType).Returns(compression);
            _containerDecoderMock
                .Setup(decoder => decoder.Decode(It.IsAny<System.IO.MemoryStream>()))
                .Returns(container.Object);
            _archiveDecoderMock
                .Setup(decoder => decoder.Decode(container.Object, 1))
                .Returns(new Mock<IArchive>().Object);
        }

        private sealed class CacheMeterListener : IDisposable
        {
            private readonly MeterListener _listener = new();

            public CacheMeterListener()
            {
                _listener.InstrumentPublished = (instrument, listener) =>
                {
                    if (instrument.Meter.Name == "Hagalaz.Cache")
                    {
                        listener.EnableMeasurementEvents(instrument);
                    }
                };
                _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => Record(instrument, value, tags));
                _listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => Record(instrument, value, tags));
                _listener.Start();
            }

            public List<CacheMeasurement> Measurements { get; } = [];

            public void Dispose() => _listener.Dispose();

            private void Record(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
            {
                var copiedTags = new Dictionary<string, object?>(tags.Length);
                foreach (var tag in tags)
                {
                    copiedTags[tag.Key] = tag.Value;
                }

                Measurements.Add(new CacheMeasurement(instrument.Name, value, copiedTags));
            }
        }

        private sealed record CacheMeasurement(string InstrumentName, double Value, IReadOnlyDictionary<string, object?> Tags);
    }
}
