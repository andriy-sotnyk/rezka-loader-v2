using AltoHttp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;

namespace rezka_loader_v2
{
    internal class RezkaClient
    {
        private HttpClient client;
        public static String domain = "https://rezka.ag";
        private const string REZKA_SEARCH_URL = "/search/?do=search&subaction=search&q=";
        private const string REZKA_GET_CDN_URL = "/ajax/get_cdn_series/";
        private static string HOMEPAGE_URL = domain;

        public RezkaClient()
        {
            this.client = new HttpClient(new HttpClientHandler{AutomaticDecompression = System.Net.DecompressionMethods.GZip});
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/81.0.4044.138 Safari/537.36");
        }

        public String search(String request)
        {
            try
            {
                return client.GetStringAsync(domain + REZKA_SEARCH_URL + request).Result;
            } 
            catch (Exception e)
            {
                return null;
            }      
        }

        public String GetMoviePage(String url)
        {
            try
            {
                return client.GetStringAsync(url).Result;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public CDNResponse GetCDNSeries(int movieId, int translatorId, int season = -1, int episode = -1)
        {
            Dictionary<string, string> requestBody;

            if (season != -1)
            {
                requestBody = new Dictionary<string, string>
                {
                    { "id", movieId.ToString() },
                    { "translator_id", translatorId.ToString() },
                    { "season", season.ToString() },
                    { "episode", episode.ToString() },
                    { "action", "get_stream" }
                };
            } else
            {
                requestBody = new Dictionary<string, string>
                {
                    { "id", movieId.ToString() },
                    { "translator_id", translatorId.ToString() },
                    { "action", "get_movie" }
                };
            }

            var content = new FormUrlEncodedContent(requestBody);

            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            content.Headers.ContentType.CharSet = "UTF-8";

            var response = client.PostAsync(domain + REZKA_GET_CDN_URL, content).Result;
            var responseString = response.Content.ReadAsStringAsync().Result;

            var responseParsed = JsonSerializer.Deserialize<CDNResponse>(responseString);

            if (!responseParsed.success)
            {
                return null;
            }

            return responseParsed;
        }
        public void DownloadFile(String url, String filepath)
        {
            var client = new WebClient();
            client.DownloadFileCompleted += Client_DownloadFileCompleted;
            client.QueryString.Add("file", filepath);
            client.DownloadFileAsync(new Uri(url), filepath);

            DownloadStatus.Get().AddFile(filepath, "In progress...");
        }

        private void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            var statusFiles = DownloadStatus.Get().GetFiles();
            string filename = ((System.Net.WebClient)(sender)).QueryString["file"];

            filename = filename.Split('\\').Last();

            if (e.Error != null)
            {
                statusFiles[filename][1] = "Error";
                MessageBox.Show("Error occured while downloading " + filename + ". Error was: " + e.Error.ToString());
            } else
            {
                statusFiles[filename][1] = "Done";
                MessageBox.Show("Download completed: " + filename);
            }
        }

        private void Downloader_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                var statusFiles = DownloadStatus.Get().GetFiles();
                var filename = (sender as AltoHttp.HttpDownloader).FileName;
                var status = statusFiles[filename][1];

                if (status != "Done" && e.Progress - float.Parse(status.Remove(status.Length - 1, 1)) >= 3.75)
                {
                    statusFiles[filename][1] = Math.Round(e.Progress, 2).ToString() + "%";
                }

                if (status != "Done" &&  e.Progress >= 99.9)
                {
                    statusFiles[filename][1] = "Done";
                }
            } catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }


    }
}
