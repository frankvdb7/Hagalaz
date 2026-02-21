import sys

filename = 'Hagalaz.Cache.Tests/FileStoreTests.cs'
with open(filename, 'r') as f:
    content = f.read()

# Find the first 'public class FileStoreTests'
class_start = content.find('public class FileStoreTests')
# Keep everything before the class
pre_class = content[:class_start]

# Find the start of the class body
body_start = content.find('{', class_start)
# Keep the body but we need to find the end correctly.
# I'll just use the brace counting again on the WHOLE content starting from body_start

brace_count = 0
body_end = -1
for i in range(body_start, len(content)):
    if content[i] == '{':
        brace_count += 1
    elif content[i] == '}':
        brace_count -= 1
        if brace_count == 0:
            body_end = i
            break

if body_end == -1:
    print("Failed to find body end")
    sys.exit(1)

body_content = content[body_start+1:body_end]

# Now, body_content might contain my previous failed attempts at the end.
# I'll look for the Dispose method which was likely the last thing in the original class.
dispose_idx = body_content.rfind('public void Dispose()')
if dispose_idx != -1:
    # Find the end of Dispose
    dispose_end = body_content.find('}', dispose_idx)
    if dispose_end != -1:
        body_content = body_content[:dispose_end+1]

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

final_content = pre_class + "public class FileStoreTests : IDisposable\n    {" + body_content + new_test + "\n    }\n}"
with open(filename, 'w') as f:
    f.write(final_content)
