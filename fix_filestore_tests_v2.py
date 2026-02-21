import sys

filename = 'Hagalaz.Cache.Tests/FileStoreTests.cs'
with open(filename, 'r') as f:
    content = f.read()

# Find the last occurrence of '}' which should be the end of the namespace
# Then find the second to last '}' which should be the end of the class
# But wait, there might be other classes.
# I'll just find where FileStoreTests ends.

# Let's find the class definition
class_start = content.find('public class FileStoreTests')
if class_start == -1:
    print("Could not find class FileStoreTests")
    sys.exit(1)

# Find the end of the class by counting braces starting from class_start
brace_count = 0
class_end = -1
found_first_brace = False
for i in range(class_start, len(content)):
    if content[i] == '{':
        brace_count += 1
        found_first_brace = True
    elif content[i] == '}':
        brace_count -= 1
        if found_first_brace and brace_count == 0:
            class_end = i
            break

if class_end == -1:
    print("Could not find end of class")
    sys.exit(1)

new_test = """
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
"""

new_content = content[:class_end] + new_test + content[class_end:]
with open(filename, 'w') as f:
    f.write(new_content)
