﻿using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace CSO2_ComboLauncher
{
    static class QQMail
    {
        public static string Filename { get; private set; }

        /// <summary>
        /// get download link from qqmail, if success global variable '<see cref="Filename"/>' will be set with the same filename from current link, else it's '<see cref="string.Empty"/>'.
        /// </summary>
        /// <returns>if link start with 'http' it's a download link, else error message.</returns>
        public static async Task<string> GetLink(string code, string sha1, string k)
        {
            Filename = string.Empty;

            using (Web Web = new Web())
            {
                string page = await Web.Client.DownloadStringTaskAsync($"https://mail.qq.com/cgi-bin/ftnExs_download?t=exs_ftn_download&code={code}&k={k}");
                Regex regex = new Regex("http[^\"]+");
                MatchCollection links = regex.Matches(page);
                foreach (Match link in links)
                {
                    if (link.Value.EndsWith(sha1.ToLower()))
                    {
                        Filename = new Regex("(?<=fname=).+?(?=&)").Match(link.Value).Value;
                        return link.Value;
                    }
                }

                regex = new Regex("(?<=\"ft_d_error infobar error\" ><p>).+?(?=<)|(?<=\"ft_d_error infobar error\"><p>).+?(?=<)");
                Match match = regex.Match(page);
                if (match.Success)
                    return match.Value;

                return "address is not found.";
            }
        }

        public static async Task<string> DownloadString(string code, string sha1, string k)
        {
            string address;
            using (Web Web = new Web())
            {
                address = await GetLink(code, sha1, k);
                if (!address.StartsWith("http"))
                    return null;
            }
            return await Downloader.StringFromHttp(address);
        }

        public static async Task<string> DownloadString(string[] code, string sha1, string[] k)
        {
            for (int i=0;i<k.Count();i++)
            {
                string result = await DownloadString(code[i], sha1, k[i]);
                if (!string.IsNullOrEmpty(result))
                    return result;
            }

            return null;
        }

        /// <param name="path">if a folder is given, file name will be from QQ mail server.</param>
        public static async Task<bool> DownloadFile(string path, int threads, string code, string sha1, string k)
        {
            string address;
            using (Web Web = new Web())
            {
                address = await GetLink(code, sha1, k);

                if (!address.StartsWith("http"))
                    return false;
            }
            return await Downloader.FileFromHttp(address, (Path.GetFileName(path) == string.Empty) ? path + Filename : path, threads, "sha1", sha1);
        }

        /// <param name="path">if a folder is given, file name will be from QQ mail server.</param>
        public static async Task<bool> DownloadFile(string path, int threads, string[] code, string sha1, string[] k)
        {
            for (int i = 0; i < k.Count(); i++)
                if (await DownloadFile(path, threads, code[i], sha1, k[i]))
                    return true;

            return false;
        }
    }
}