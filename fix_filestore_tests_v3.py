import sys

filename = 'Hagalaz.Cache.Tests/FileStoreTests.cs'
with open(filename, 'r') as f:
    content = f.read()

# Find the last '}'
last_brace = content.rfind('}')
second_last_brace = content.rfind('}', 0, last_brace)

new_test = """
        [Fact]
        public void Write_And_Read_ExtendedFile_Succeeds()
        {
            // Arrange
            const int indexId = 0;
            const int fileId = ushort.MaxValue + 1;
            var fileData = System.Text.Encoding.UTF8.GetBytes("This is a test file for extended file IDs.");

            // Act
            _fileStore.Write(indexId, fileId, new MemoryStream(fileData));
            var resultStream = _fileStore.Read(indexId, fileId);

            // Assert
            var resultData = resultStream.ToArray();
            Assert.Equal(fileData, resultData);
        }
"""

final_content = content[:second_last_brace] + new_test + content[second_last_brace:]
with open(filename, 'w') as f:
    f.write(final_content)
