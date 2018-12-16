using System.Collections.Generic;
using CommandLine;

namespace WyMusicConvert
{
    [Verb("ncm", HelpText = "Decrypt NCM files.")]
    public class NcmConvertOption
    {
        [Value(0, MetaName = "paths", Required = true, 
            HelpText = "A group of files or directories.")]
        public IEnumerable<string> Paths { get; set; }

        [Option('f', "force", 
            HelpText = "Do convert even if the target file already exists.")]
        public bool ForceConvert { get; set; }
    }
}