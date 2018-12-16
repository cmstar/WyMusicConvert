using Newtonsoft.Json;

namespace WyMusicConvert
{
    /// <summary>
    /// 歌曲的描述信息。
    /// </summary>
    public class NcmMetaData
    {
        /*
          "musicId": 27979419,
          "musicName": "六等星の夜 (from Live at anywhere)",
          "artist": [["Aimer", 16152]],
          "albumId": 2706118,
          "album": "After Dark",
          "albumPicDocId": "109951163627370594",
          "albumPic": "https://p4.music.126.net/Lb7MTxYrLgBhtUpd5yxg8w==/109951163627370594.jpg",
          "bitrate": 320000,
          "mp3DocId": "051f78faac3f37ad8fb99636cef8a9ff",
          "duration": 337933,
          "mvId": 0,
          "alias": [],
          "transNames": [],
          "format": "mp3"
         */

        /// <summary>
        /// 歌曲ID，可对应到URL：https://music.163.com/song?id={MusicId} 。
        /// </summary>
        [JsonProperty("musicId")]
        public long MusicId;

        /// <summary>
        /// 歌曲名称。
        /// </summary>
        [JsonProperty("musicName")]
        public string MusicName;

        /// <summary>
        /// 专辑作者，可有多个。
        /// 第一层每个元素表示一个作者；
        /// 第二层索引0为作者名称，索引1为作者ID，对应URL：https://music.163.com/#/artist?id={ArtistId} 。
        /// </summary>
        [JsonProperty("artist")]
        public string[][] Artist;

        /// <summary>
        /// 专辑ID，对应URL：https://music.163.com/#/album?id={AlbumId}。
        /// </summary>
        [JsonProperty("albumId")]
        public long AlbumId;

        /// <summary>
        /// 专辑名称。
        /// </summary>
        [JsonProperty("album")]
        public string Album;

        /// <summary>
        /// 专辑图片的URL。
        /// </summary>
        [JsonProperty("albumPic")]
        public string AlbumPic;

        /// <summary>
        /// 比特率。如320000 （即320K）。
        /// </summary>
        [JsonProperty("bitrate")]
        public int Bitrate;
        
        /// <summary>
        /// 歌曲时间长度，毫秒。
        /// </summary>
        [JsonProperty("duration")]
        public int Duration;

        /// <summary>
        /// 歌曲格式，如“mp3”（不包含.）。
        /// </summary>
        [JsonProperty("format")]
        public string Format;
    }
}
