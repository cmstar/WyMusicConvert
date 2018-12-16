using System;
using CommandLine;

namespace WyMusicConvert
{
    internal static class Program
    {
        private static readonly Type[] Options =
        {
            typeof(NcmConvertOption),
        };

        public static void Main(string[] args)
        {
            using (var parser = new EnhancedCommandLineParser())
            {
                parser.ParseArguments(args, Options)
                    .WithParsed<NcmConvertOption>(option => Run(NcmConvert.Process, option));
            }
        }

        private static void Run<T>(Action<T> act, T option)
        {
            try
            {
                act(option);
            }
            catch (WyMusicConvertException ex)
            {
                Console.WriteLine(ex.Message);
                Environment.ExitCode = 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Environment.ExitCode = 2;
            }
        }
    }
}
