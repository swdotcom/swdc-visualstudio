

using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SoftwareCo
{
    class SoftwareSpotifyManager
    {
        private static LocalSpotifyTrackInfo CurrentTrackInfo;

        protected static async Task HandleTrackInfoAsync(LocalSpotifyTrackInfo localTrackInfo)
        {
            if (!SoftwareHttpManager.HasSpotifyAccessToken())
            {
                // initialize the token
                await SoftwareHttpManager.InitializeSpotifyClientGrantAsync();
            }

            bool hasLocalTrackData = (localTrackInfo.name != null && localTrackInfo.artist != null)
                ? true : false;
            bool hasCurrentTrackData = (CurrentTrackInfo != null && CurrentTrackInfo.name != null && CurrentTrackInfo.artist != null)
                ? true : false;
            bool isNewTrack = true;
            if (hasLocalTrackData && hasCurrentTrackData &&
                localTrackInfo.name.Equals(CurrentTrackInfo.name) &&
                localTrackInfo.artist.Equals(CurrentTrackInfo.artist))
            {
                isNewTrack = false;
            }

            HttpResponseMessage response = null;
            try
            {
                if (isNewTrack && hasLocalTrackData)
                {
                    if (hasCurrentTrackData)
                    {
                        // close the previous track
                        CurrentTrackInfo.end = SoftwareCoUtil.getNowInSeconds();
                        // send it to the app server
                        response = await SoftwareHttpManager.SendRequestAsync(
                                    HttpMethod.Post, "/data/music", CurrentTrackInfo.GetAsJson());
                    }
                    // fill in the missing attributes from the spotify API
                    await SoftwareHttpManager.GetSpotifyTrackInfoAsync(localTrackInfo);
                    // send it to the app server
                    response = await SoftwareHttpManager.SendRequestAsync(
                                HttpMethod.Post, "/data/music", localTrackInfo.GetAsJson());
                    CurrentTrackInfo = localTrackInfo.Clone();
                }
                else if (hasCurrentTrackData && !hasLocalTrackData)
                {
                    // send this to close it
                    CurrentTrackInfo.end = SoftwareCoUtil.getNowInSeconds();
                    // send it to the app server
                    response = await SoftwareHttpManager.SendRequestAsync(
                                HttpMethod.Post, "/data/music", CurrentTrackInfo.GetAsJson());
                    CurrentTrackInfo = null;
                }
            } catch (Exception e) {
                Logger.Error("Software.com: Unable to process track information, error: " + e.Message);
            }

            if (response != null && !SoftwareHttpManager.IsOk(response)) {
                Logger.Error(response.ToString());
            }
        }

        public static async Task GetLocalSpotifyTrackInfoAsync()
        {
            Process proc = Process.GetProcessesByName("Spotify").FirstOrDefault
                (p => !string.IsNullOrWhiteSpace(p.MainWindowTitle));

            LocalSpotifyTrackInfo localTrackInfo = new LocalSpotifyTrackInfo();
            if (proc != null)
            {
                // spotify is open, get the track info
                string spotifyTrackInfo = proc.MainWindowTitle;
                // split it
                string[] stringParts = spotifyTrackInfo.Split('-');
                
                if (stringParts != null && stringParts.Length == 2)
                {
                    localTrackInfo.artist = stringParts[0].Trim();
                    localTrackInfo.name = stringParts[1].Trim();
                }
            }

            await HandleTrackInfoAsync(localTrackInfo);
        }
    }

    class LocalSpotifyTrackInfo
    {
        public long start { get; set; }
        public long local_start { get; set; }
        public string artist { get; set; }
        public string name { get; set; }

        public long end { get; set; }
        public string genre { get; set; }
        public string type { get; set; }
        public long duration { get; set; }
        public string state { get; set; }
        public string id { get; set; }

        public string GetAsJson()
        {
            if (this.type == null)
            {
                this.type = "spotify";
            }
            JsonObject jsonObj = new JsonObject();
            jsonObj.Add("start", this.start);
            jsonObj.Add("local_start", this.local_start);
            jsonObj.Add("end", this.end);
            jsonObj.Add("genre", this.genre);
            jsonObj.Add("type", this.type);
            jsonObj.Add("duration", this.duration);
            jsonObj.Add("state", this.state);
            jsonObj.Add("artist", this.artist);
            jsonObj.Add("name", this.name);
            jsonObj.Add("id", this.id);
            return jsonObj.ToString();
        }

        public LocalSpotifyTrackInfo Clone()
        {
            LocalSpotifyTrackInfo info = new LocalSpotifyTrackInfo();
            info.start = this.start;
            info.local_start = this.local_start;
            info.end = this.end;
            info.genre = this.genre;
            info.type = this.type;
            info.duration = this.duration;
            info.state = this.state;
            info.artist = this.artist;
            info.name = this.name;
            info.id = this.id;
            return info;
        }

    }
}
