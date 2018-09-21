
using Commons.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
// using SpotifyAPI.Local;
// using SpotifyAPI.Local.Enums;
// using SpotifyAPI.Local.Models;

namespace SoftwareCo
{
    class SoftwareCoUtil
    {
        /**
        private SpotifyLocalAPI _spotify = null;

        public IDictionary<string, string> getTrackInfo()
        {
            IDictionary<string, string> dict = new Dictionary<string, string>();

            if (_spotify == null)
            {
                _spotify = new SpotifyLocalAPI();
            }

            if (SpotifyLocalAPI.IsSpotifyRunning() && _spotify.Connect())
            {
                StatusResponse status = _spotify.GetStatus();
                Logger.Info("got spotify status: " + status.ToString());
            }

            return dict;
        }
        **/

        public IDictionary<string, string> GetResourceInfo(string projectDir)
        {
            IDictionary<string, string> dict = new Dictionary<string, string>();
            string identifier = RunCommand("git config remote.origin.url", projectDir);
            if (identifier != null && !identifier.Equals(""))
            {
                dict.Add("identifier", identifier);

                // only get these since identifier is available
                string email = RunCommand("git config user.email", projectDir);
                if (email != null && !email.Equals(""))
                {
                    dict.Add("email", email);
                }
                string branch = RunCommand("git symbolic-ref --short HEAD", projectDir);
                if (branch != null && !branch.Equals(""))
                {
                    dict.Add("branch", branch);
                }
                string tag = RunCommand("git describe --all", projectDir);

                if (tag != null && !tag.Equals(""))
                {
                    dict.Add("tag", tag);
                }
            }
            
            return dict;
        }

        private string RunCommand(String cmd, String dir)
        {
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/c " + cmd;
            process.StartInfo.WorkingDirectory = dir;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();
            //* Read the output (or the error)
            string output = process.StandardOutput.ReadToEnd();
            string err = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (output != null)
            {
                return output.Trim();
            }
            return "";
        }
    }

    
}
