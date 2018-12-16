using System;
using System.Diagnostics;
using System.IO;

namespace WyMusicConvert
{
    public static class NcmConvert
    {
        public static void Process(NcmConvertOption option)
        {
            foreach (var path in option.Paths)
            {
                if (Directory.Exists(path))
                {
                    // 客户端下载的文件都是直接放在根目录下的，没有子目录，不用递归。
                    foreach (var file in Directory.EnumerateFiles(path, "*.ncm"))
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

            using (var ncm = new NcmFile(path))
            {
                var targetFileName = $"{Path.GetFileNameWithoutExtension(fileInfo.Name)}.{ncm.MetaData.Format}";
                var targetFilePath = Path.Combine(fileInfo.Directory.FullName, targetFileName);

                if (!forceConvert && File.Exists(targetFilePath))
                {
                    Console.WriteLine(" ...Skipped");
                    return;
                }

                ncm.Dump(targetFilePath);
            }

            Console.WriteLine(" ...Done");
        }
    }
}
