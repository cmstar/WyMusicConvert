using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using TagLib;
using TagLibFile = TagLib.File;

namespace WyMusicConvert
{
    /// <summary>
    /// 用于解析NCM文件。
    /// </summary>
    public class NcmFile : IDisposable
    {
        /*
         * 算法参考：
         *
         * - C++原版 https://github.com/anonymous5l/ncmdump
         * - .net版  https://github.com/GameBelial/ncmdump
         *
         * NCM 文件定义（[域名称(字节长度)]）：
         *
         * - [FileMark(8)]                  一个文件标记，可用于校验是否是NCM文件格式。
         * - [2]                            偏移两个字节。
         * - [Key_Length(4)]                4字节，表示 Key_Cipher 域的字节长度。little-endian 编码。
         * - [Key_Cipher(Key_Length)]       解密得到 KeyBox，步骤见后文。
         * - [Meta_Length(4)]               元数据的长度。
         * - [Meta_Cipher(Meta_Length)]     元数据，加密的 JSON，包含歌曲名称、专辑、长度等各种信息，解密步骤见后文。
         * - [CRC(9)]                       CRC校验区，固定长度。
         * - [Image_Length(4)]              专辑图片的长度。
         * - [Image_Data(Image_Length)]     专辑图片。
         * - [Body]                         数据本体部分，需使用 Key_Cipher 解密出来的 KeyBox 解密。
         *
         * Key_Cipher 解密：
         *
         * 1. 逐字节异或 0x64；
         * 2. AES 解密得到明文；
         * 3. 忽略头部固定的 neteasecloudmusic （17字节）；
         * 4. 剩下的数据，通过 BuildKeyBox 方法获得加密因子 KeyBox 。
         *
         * Meta_Cipher 解密：
         *
         * 1. 逐字节异或 0x63 ；
         * 2. ASCII解码，得到一段文本，格式是： 163 key(Don't modify):BASE64_DATA；
         * 3. 忽略开头的“163 key(Don't modify):” （22字节），取之后的 BASE64 部分。
         * 4. 将 BASE64 的数据 AES 解密，得到明文；
         * 5. UTF-8解码，得到： music:{JSON_DATA}；
         * 6. 忽略开头 “music:”（6字节），得到元数据的 JSON。
         */

        private static readonly byte[] AesKeyBoxKey
            = { 0x68, 0x7A, 0x48, 0x52, 0x41, 0x6D, 0x73, 0x6F, 0x35, 0x6B, 0x49, 0x6E, 0x62, 0x61, 0x78, 0x57 };

        private static readonly byte[] AesMetaDataKey
            = { 0x23, 0x31, 0x34, 0x6C, 0x6A, 0x6B, 0x5F, 0x21, 0x5C, 0x5D, 0x26, 0x30, 0x55, 0x3C, 0x27, 0x28 };

        private static readonly byte[] EmptyVector = new byte[AesMetaDataKey.Length];

        private static readonly byte[] NcmFileMark
            = { 0x43, 0x54, 0x45, 0x4E, 0x46, 0x44, 0x41, 0x4d };

        private readonly FileStream _inputStream;
        private readonly byte[] _keyBox;

        /// <summary>
        /// 初始化<see cref="NcmFile"/>的新实例。
        /// </summary>
        /// <param name="path">NCM文件的完整路径。</param>
        public NcmFile(string path)
        {
            Path = path;

            _inputStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            // [FileMark(8)]
            ValidateFileMark();

            // [2]
            _inputStream.Seek(2, SeekOrigin.Current);

            // [KeyFactor]
            var keyFactor = ReadKeyFactor();
            _keyBox = BuildKeyBox(keyFactor);

            // [MetaData]
            var metaJson = ReadMetaJson();
            MetaData = JsonConvert.DeserializeObject<NcmMetaData>(metaJson);

            // [CRC(9)]
            _inputStream.Seek(9, SeekOrigin.Current);

            // 至此，文件流当前位置停留在专辑图片的开始位置。剩下的数据待输出时再读取。
        }

        /// <summary>
        /// 当前NCM文件的路径。
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// 当前文件的描述信息，包含歌曲名称、专辑、长度等各种信息。
        /// </summary>
        public NcmMetaData MetaData { get; }

        /// <inheritdoc />
        public void Dispose()
        {
            _inputStream.Dispose();
        }

        /// <summary>
        /// 解密当前NCM文件，并输出到指定的位置。
        /// </summary>
        /// <param name="outputFile">输出的目标文件的完整路径。</param>
        public void Dump(string outputFile)
        {
            const int bufferLength = 4096;

            // [Image]
            var imageBytes = ReadNextBlock();

            var box = _keyBox;
            var buf = new byte[bufferLength];

            using (var outputStream = new FileStream(outputFile, FileMode.Create))
            {
                while (true)
                {
                    var len = _inputStream.Read(buf);
                    if (len <= 0)
                        break;

                    for (int i = 0; i < len; i++)
                    {
                        var j = (byte)((i + 1) & 0xff);
                        buf[i] ^= box[box[j] + box[(box[j] + j) & 0xff] & 0xff];
                    }

                    outputStream.Write(buf, 0, len);
                }
            }

            // 转出来的文件已经自带名称、专辑等信息，但不带专辑图片，图片单独保存一下。
            using (var tagLibFile = TagLibFile.Create(outputFile))
            {
                tagLibFile.Tag.Pictures = new[]
                {
                    (IPicture)new Picture(new ByteVector(imageBytes, imageBytes.Length))
                };
                tagLibFile.Save();
            }
        }

        private void ValidateFileMark()
        {
            for (int i = 0; i < NcmFileMark.Length; i++)
            {
                if (_inputStream.ReadByte() != NcmFileMark[i])
                    throw new WyMusicConvertException($"{Path} is not a valid NPM file.");
            }
        }

        // 从文件流读取一个数据块：[Data_Length(4)][Data_Body(Data_Length)]
        private byte[] ReadNextBlock()
        {
            var buf = _inputStream.ForceRead(4);
            var len = BitConverter.ToInt32(buf, 0);

            buf = _inputStream.ForceRead(len);
            return buf;
        }

        private byte[] ReadKeyFactor()
        {
            const int headerLength = 17; // len("neteasecloudmusic")

            var keyBytes = ReadNextBlock();

            for (int i = 0; i < keyBytes.Length; i++)
            {
                keyBytes[i] ^= 0x64;
            }

            var data = AesDecrypt(AesKeyBoxKey, keyBytes);
            var keyFactor = Cut(data, headerLength);

            return keyFactor;
        }

        private string ReadMetaJson()
        {
            const int cipherHeaderLength = 22; // len("163 key(Don't modify):")
            const int jsonHeaderLength = 6; // len("music:")

            var modifyData = ReadNextBlock();

            for (int i = 0; i < modifyData.Length; i++)
            {
                modifyData[i] ^= 0x63;
            }

            var base64Bytes = Cut(modifyData, cipherHeaderLength);
            var base64String = Encoding.ASCII.GetString(base64Bytes);
            var cipherBytes = Convert.FromBase64String(base64String);
            var plainBytes = AesDecrypt(AesMetaDataKey, cipherBytes);
            var jsonBytes = Cut(plainBytes, jsonHeaderLength);
            var json = Encoding.UTF8.GetString(jsonBytes);

            return json;
        }

        private static byte[] AesDecrypt(byte[] key, byte[] data)
        {
            var aes = Aes.Create();
            if (aes == null)
                throw new CryptographicException("Can't create the AES base class.");

            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.ECB;

            using (var decryptor = aes.CreateDecryptor(key, EmptyVector))
            {
                var result = decryptor.TransformFinalBlock(data, 0, data.Length);
                return result;
            }
        }

        private static byte[] BuildKeyBox(byte[] key)
        {
            byte[] box = new byte[256];
            for (int i = 0; i < 256; ++i)
            {
                box[i] = (byte)i;
            }

            byte keyLength = (byte)key.Length;
            byte lastByte = 0;
            byte keyOffset = 0;

            for (int i = 0; i < 256; ++i)
            {
                var swap = box[i];
                var c = (byte)((swap + lastByte + key[keyOffset++]) & 0xff);

                if (keyOffset >= keyLength)
                {
                    keyOffset = 0;
                }

                box[i] = box[c];
                box[c] = swap;
                lastByte = c;
            }

            return box;
        }

        private static byte[] Cut(byte[] source, int offset)
        {
            var length = source.Length - offset;
            var result = new byte[length];
            Buffer.BlockCopy(source, offset, result, 0, length);
            return result;
        }
    }
}
