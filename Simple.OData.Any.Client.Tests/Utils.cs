using System;
using System.IO;
using System.Text;

namespace Simple.OData.Client
{
    static class Utils
    {
        public static string StreamToString(Stream stream, bool disposeStream = false)
        {
            if (!disposeStream && stream.CanSeek)
                stream.Seek(0, SeekOrigin.Begin);
            var result = new StreamReader(stream).ReadToEnd();
            if (disposeStream)
                stream.Dispose();
            return result;
        }

        public static byte[] StreamToByteArray(Stream stream, bool disposeStream = false)
        {
            if (!disposeStream && stream.CanSeek)
                stream.Seek(0, SeekOrigin.Begin);
            var bytes = new byte[stream.Length];
            var result = new BinaryReader(stream).ReadBytes(bytes.Length);
            if (disposeStream)
                stream.Dispose();
            return result;
        }

        public static Stream StringToStream(string text)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(text));
        }

        public static Stream ByteArrayToStream(byte[] bytes)
        {
            return new MemoryStream(bytes);
        }

        public static Stream CloneStream(Stream stream)
        {
            stream.Position = 0;
            var clonedStream = new MemoryStream();
            stream.CopyTo(clonedStream);
            return clonedStream;
        }
    }
}
