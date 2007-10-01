using System.Globalization;
using System.IO;
using System.Text;

namespace MiniHttpd
{
    internal class ChunkedStream : ImmediateResponseStream
    {
        public ChunkedStream(Stream outputStream) : base(outputStream)
        {
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            byte[] lengthLine = Encoding.UTF8.GetBytes(count.ToString("x", CultureInfo.InvariantCulture) + "\r\n");
            _outputStream.Write(lengthLine, 0, lengthLine.Length);
            Write(buffer, offset, count, false);
            _outputStream.Write(new byte[] {13, 10}, 0, 2);
            _outputStream.Flush();
        }
    }
}