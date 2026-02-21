import sys
filename = 'Hagalaz.Cache.Tests/FileStoreTests.cs'
with open(filename, 'r') as f:
    lines = f.readlines()

# Find the last closing brace of the FileStoreTests class
last_brace_idx = -1
for i in range(len(lines)-1, -1, -1):
    if lines[i].strip() == '}':
        last_brace_idx = i
        break

# I'll just rewrite the end of the file
new_lines = lines[:last_brace_idx]
new_lines.append("""
        [Fact]
        public void Write_And_Read_LargeFile_Succeeds()
        {
            // Arrange
            const int indexId = 0;
            const int fileId = 10;
            var fileData = new byte[10000];
            new Random().NextBytes(fileData);

            // Act
            _fileStore.Write(indexId, fileId, new MemoryStream(fileData));
            var resultStream = _fileStore.Read(indexId, fileId);

            // Assert
            var resultData = resultStream.ToArray();
            Assert.Equal(fileData, resultData);
        }
    }
}
""")

with open(filename, 'w') as f:
    f.writelines(new_lines)
