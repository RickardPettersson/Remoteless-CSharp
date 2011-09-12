using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Remoteless
{
    /// <summary>
    /// Class to hold information about a playlist
    /// </summary>
    public class Playlist
    {
        public string Name { get; set; }
        public string SpotifyURL { get; set; }
        public List<Song> Songs { get; set; }
    }

    /// <summary>
    /// Class to hold information about a song in a playlist, artist and track info
    /// </summary>
    public class Song
    {
        public SongInfo Album { get; set; }
        public SongInfo Artist { get; set; }
        public SongInfo Track { get; set; }
    }

    /// <summary>
    /// Class to hold information about a artist or a track
    /// </summary>
    public class SongInfo
    {
        public string Name { get; set; }
        public string SpotifyURL { get; set; }
    }

    /// <summary>
    /// Class to hold information of Spotify client state
    /// </summary>
    public class State
    {
        public bool Alive { get; set; }
        public bool Visible { get; set; }
        public bool Playing { get; set; }
        public bool Activated { get; set; }
        public int Volume { get; set; }
        public int SongPos { get; set; }
        public bool CovertArt { get; set; }
        public int CovertID { get; set; }
        public string Artist { get; set; }
        public string Track { get; set; }
    }

    /// <summary>
    /// Main class to hold functionality for Remoteless
    /// </summary>
    public class Controller
    {
        /// <summary>
        /// URL to the HTTP interface for Remoteless
        /// </summary>
        private static string URL = "http://localhost:14387";

        /// <summary>
        /// Function to get the result from a HTTP GET from example Remoteless HTTP interface
        /// </summary>
        /// <param name="url">URL to where to get information</param>
        /// <returns>String with data from example Remoteless HTTP interface</returns>
        private static string GetSite(string url)
        {
            // used to build entire input
            StringBuilder sb = new StringBuilder();

            // used on each read operation
            byte[] buf = new byte[8192];

            // prepare the web page we will be asking for
            HttpWebRequest request = (HttpWebRequest)
                WebRequest.Create(url);

            // execute the request
            HttpWebResponse response = (HttpWebResponse)
                request.GetResponse();

            // we will read data via the response stream
            Stream resStream = response.GetResponseStream();

            string tempString = null;
            int count = 0;

            do
            {
                // fill the buffer with data
                count = resStream.Read(buf, 0, buf.Length);

                // make sure we read some data
                if (count != 0)
                {
                    // translate from bytes to ASCII text
                    tempString = Encoding.UTF8.GetString(buf, 0, count);

                    // continue building the string
                    sb.Append(tempString);
                }
            }
            while (count > 0); // any more data to read?

            // print out page source
            return sb.ToString();
        }

        /// <summary>
        /// Function to get a list of playlists and the playlists songs
        /// </summary>
        /// <returns>List of playlists</returns>
        public static List<Playlist> GetPlaylistWithSongs()
        {
            return GetPlaylistWithSongs(0);
        }

        /// <summary>
        /// Function to get a list of playlists and the playlists songs
        /// </summary>
        /// <param name="from">Number the list should start from</param>
        /// <returns>List of playlists</returns>
        public static List<Playlist> GetPlaylistWithSongs(int startFrom)
        {
            string data = GetSite(URL + "/playlists?from=" + startFrom);

            string[] separator = { "\n" };
            string[] splitedData = data.Split(separator, StringSplitOptions.RemoveEmptyEntries);

            string[] separator2 = { "|" };
            string[] separator3 = { "=" };

            List<Playlist> playlists = new List<Playlist>();

            Playlist lastPlaylist = new Playlist();

            Song lastSongInfo = new Song();

            int from = 0;
            int to = 0;
            int remaining = 0;

            for (int i = 0; i < splitedData.Length; i++)
            {
                if (splitedData[i].Contains("from") || splitedData[i].Contains("to") || splitedData[i].Contains("remaining"))
                {
                    string[] rowData = splitedData[i].Split(separator3, StringSplitOptions.RemoveEmptyEntries);

                    switch (rowData[0].ToLower())
                    {
                        case "from":
                            from = Convert.ToInt32(rowData[1]);
                            break;
                        case "to":
                            to = Convert.ToInt32(rowData[1]);
                            break;
                        case "remaining":
                            remaining = Convert.ToInt32(rowData[1]);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    string[] rowData = splitedData[i].Split(separator2, StringSplitOptions.RemoveEmptyEntries);

                    switch (rowData[0].ToLower())
                    {
                        case "playlist":
                            if ((lastPlaylist.Name != string.Empty) && (lastPlaylist.Songs != null) && (lastPlaylist.Songs.Count > 0))
                            {
                                playlists.Add(lastPlaylist);
                            }

                            lastPlaylist = new Playlist();
                            lastPlaylist.SpotifyURL = rowData[2];
                            lastPlaylist.Name = rowData[3];
                            lastPlaylist.Songs = new List<Song>();
                            break;
                        case "track":
                            lastSongInfo = new Song();
                            lastSongInfo.Track = new SongInfo()
                            {
                                SpotifyURL = rowData[2],
                                Name = rowData[3]
                            };
                            break;
                        case "artist":
                            lastSongInfo.Artist = new SongInfo()
                            {
                                SpotifyURL = rowData[2],
                                Name = rowData[3]
                            };
                            lastPlaylist.Songs.Add(lastSongInfo);
                            break;
                        case "from":
                            from = Convert.ToInt32(rowData[1]);
                            break;
                        case "to":
                            to = Convert.ToInt32(rowData[1]);
                            break;
                        case "remaining":
                            remaining = Convert.ToInt32(rowData[1]);
                            break;
                        default:
                            break;
                    }
                }
            }

            if ((remaining > 0) && (to != from))
            {
                playlists.AddRange(GetPlaylistWithSongs(to));
            }

            return playlists;
        }

        /// <summary>
        /// Get a state object
        /// </summary>
        /// <returns>State object with information about the client</returns>
        public static State GetState()
        {
            string data = GetSite(URL + "/state");

            State state = new State();

            string[] separator = { "\n" };
            string[] separator2 = { "=" };

            string[] splitedData = data.Split(separator, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < splitedData.Length; i++)
            {
                string[] splitedRowData = splitedData[i].Split(separator2, StringSplitOptions.RemoveEmptyEntries);

                if (splitedRowData.Length > 0)
                {
                    switch (splitedRowData[0])
                    {
                        case "alive":
                            state.Alive = Convert.ToBoolean(splitedRowData[1]);
                            break;
                        case "visible":
                            state.Visible = Convert.ToBoolean(splitedRowData[1]);
                            break;
                        case "playing":
                            state.Playing = Convert.ToBoolean(splitedRowData[1]);
                            break;
                        case "activated":
                            state.Activated = Convert.ToBoolean(splitedRowData[1]);
                            break;
                        case "volume":
                            state.Volume = Convert.ToInt32(splitedRowData[1]);
                            break;
                        case "songpos":
                            state.SongPos = Convert.ToInt32(splitedRowData[1]);
                            break;
                        case "coverart":
                            state.CovertArt = Convert.ToBoolean(splitedRowData[1]);
                            break;
                        case "artist":
                            state.Artist = splitedRowData[1];
                            break;
                        case "track":
                            state.Track = splitedRowData[1];
                            break;
                    }
                }
            }

            return state;
        }

        public static List<Song> Search(string searchValue)
        {
            List<Song> songList = new List<Song>();

            Song lastSong = null;

            string data = GetSite(URL + "/search?cc=NO&query=" + searchValue);

            string[] separator = { "\n" };
            string[] splitedData = data.Split(separator, StringSplitOptions.RemoveEmptyEntries);

            string[] separator2 = { "|" };

            for (int i = 0; i < splitedData.Length; i++)
            {
                string[] splitedDataRow = splitedData[i].Split(separator2, StringSplitOptions.RemoveEmptyEntries);

                if (splitedDataRow[1] == "0")
                {
                    if (lastSong != null)
                    {
                        songList.Add(lastSong);
                    }

                    lastSong = new Song();
                }

                switch (splitedDataRow[0])
                {
                    case "artist":
                        lastSong.Artist = new SongInfo()
                        {
                            Name = splitedDataRow[3],
                            SpotifyURL = splitedDataRow[2]
                        };
                        break;
                    case "track":
                        lastSong.Track = new SongInfo()
                        {
                            Name = splitedDataRow[3],
                            SpotifyURL = splitedDataRow[2]
                        };
                        break;
                    case "album":
                        lastSong.Album = new SongInfo()
                        {
                            Name = splitedDataRow[3],
                            SpotifyURL = splitedDataRow[2]
                        };
                        break;
                }
            }

            return songList;
        }

        /// <summary>
        /// Start Spotify
        /// </summary>
        public static void Start()
        {
            string data = GetSite(URL + "/action/start");
        }

        /// <summary>
        /// Play previous track
        /// </summary>
        public static void Prev()
        {
            string data = GetSite(URL + "/action/prev");
        }

        /// <summary>
        /// Play or pause the track
        /// </summary>
        public static void PlayPause()
        {
            string data = GetSite(URL + "/action/playpause");
        }

        /// <summary>
        /// Play next track
        /// </summary>
        public static void Next()
        {
            string data = GetSite(URL + "/action/next");
        }

        /// <summary>
        /// Put volume up
        /// </summary>
        public static void VolUp()
        {
            string data = GetSite(URL + "/action/volup");
        }

        /// <summary>
        /// Put volume down
        /// </summary>
        public static void VolDown()
        {
            string data = GetSite(URL + "/action/voldown");
        }

        /// <summary>
        /// Set volume
        /// </summary>
        /// <param name="vol">% of volume to set it to</param>
        public static void VolSet(int vol)
        {
            string data = GetSite(URL + "/action/volset?vol=" + vol);
        }

        /// <summary>
        /// Play a playlist, artist or track
        /// </summary>
        /// <param name="url"></param>
        public static void Play(string url)
        {
            string data = GetSite(URL + "/action/play?uri=spotify:" + url);
        }
    }
}