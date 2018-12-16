using System.IO;

namespace WyMusicConvert
{
    public static class FileStreamExtension
    {
        public static void Write(this FileStream f, byte[] buf)
        {
            f.Write(buf, 0, buf.Length);
        }

        public static int Read(this FileStream f, byte[] buf)
        {
            return f.Read(buf, 0, buf.Length);
        }

        public static void ForceRead(this FileStream f, byte[] buf)
        {
            if (f.Read(buf, 0, buf.Length) != buf.Length)
                throw new IOException("No more data in the given stream.");
        }

        public static byte[] ForceRead(this FileStream f, int length)
        {
            var buf = new byte[length];
            ForceRead(f, buf);
            return buf;
        }
    }
}
