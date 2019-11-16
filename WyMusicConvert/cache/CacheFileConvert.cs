using System;
using System.Diagnostics;
using System.IO;

namespace WyMusicConvert
{
    public static class CacheFileConvert
    {
        public static void Process(CacheFileConvertOption option)
        {
            foreach (var path in option.Paths)
            {
                if (Directory.Exists(path))
                {
                    foreach (var file in Directory.EnumerateFiles(path, "*.uc"))
                    {
                        ProcessFile(file, option.ForceConvert);
                    }
                }
                else if (File.Exists(path))
                {
                    ProcessFile(path, option.ForceConvert);
                }
                else
                {
                    Console.WriteLine($"The path is invalid: {path}");
                }
            }
        }

        private static void ProcessFile(string path, bool forceConvert)
        {
            Console.Write($"Converting... {path}");

            var fileInfo = new FileInfo(path);
            Trace.Assert(fileInfo.Directory != null);

            var targetFileName = $"{fileInfo.Name}.mp3";
            var targetFilePath = Path.Combine(fileInfo.Directory.FullName, targetFileName);

            if (!forceConvert && File.Exists(targetFilePath))
            {
                Console.WriteLine(" ...Skipped");
                return;
            }
             
            Decrypt(path, targetFilePath);
            Console.WriteLine(" ...Done");
        }

        // ref https://www.jianshu.com/p/5fb2bcaa79f8
        // 文章给出了解密过程和获取歌曲信息的方法。这里先实现一下解密。
        private static void Decrypt(string inputPath, string outputPath)
        {
            using (var input = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var output = new FileStream(outputPath, FileMode.OpenOrCreate))
            {
                var buf = new byte[1024];

                int len;
                while ((len = input.Read(buf, 0, buf.Length)) > 0)
                {
                    for (int i = 0; i < len; i++)
                    {
                        buf[i] ^= 0xA3;
                    }
                    output.Write(buf, 0, len);
                }
            }
        }
    }
}
