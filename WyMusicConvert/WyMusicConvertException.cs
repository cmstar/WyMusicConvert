using System;

namespace WyMusicConvert
{
    public class WyMusicConvertException : Exception
    {
        public WyMusicConvertException(string message)
            : base(message)
        {
        }

        public WyMusicConvertException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
