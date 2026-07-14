using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace rezka_loader_v2
{
    public partial class DownloadConfigurationForm : Form
    {
        private const int WM_NCHITTEST = 0x84;
        private const int HT_CLIENT = 0x1;
        private const int HT_CAPTION = 0x2;
        private String url = "";
        private int movieId = -1;
        private CDNService cdnService;

        public DownloadConfigurationForm(String url)
        {
            InitializeComponent();
            this.url = url;
            this.cdnService = new CDNService();
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == WM_NCHITTEST)
                m.Result = (IntPtr)(HT_CAPTION);
        }

        private void DownloadConfigurationForm_Load(object sender, EventArgs e)
        {
            MoviePageData data = this.cdnService.GetMovieDownloadOptionsWithSelenium(url);

            if (data == null)
            {
                MessageBox.Show("Couldn't fetch movie data", "Error");
                return;
            }

            this.movieId = data.MovieId;

            foreach (Translator translator in data.Translations)
            {
                translationSelector.Items.Add(translator.getName() + " [" + translator.getId() + "]");
            }
            translationSelector.SelectedIndex = 0; 

            if (data.Seasons == null || data.Seasons.Count == 0)
            {
                seasonSelector.Enabled = false;
            } else
            {
                foreach (Season season in data.Seasons)
                {
                    seasonSelector.Items.Add(season.GetSeasonTitle() + " [" + season.GetSeasonId() + "]");
                }
                seasonSelector.SelectedIndex = seasonSelector.Items.Count - 1;
            }

            if (data.Episodes == null || data.Episodes.Count == 0)
            {
                episodeSelector.Enabled = false;
            }
            else
            {
                foreach (Episode episode in data.Episodes)
                {
                    episodeSelector.Items.Add(episode.GetEpisodeTitle() + " [" + episode.GetEpisodeId() + "]");
                }
                episodeSelector.SelectedIndex = episodeSelector.Items.Count - 1;
            }
        }

        private void searchLabel_Click(object sender, EventArgs e)
        {

        }

        private void close_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void nextBtn_Click(object sender, EventArgs e)
        {
            int translatorId = GetId(this.translationSelector.Items[translationSelector.SelectedIndex].ToString());
            System.Collections.Generic.Dictionary<string, string> links = null;

            if (!seasonSelector.Enabled)
            {
                links = cdnService.GetCDNLinks(this.movieId, translatorId);
            } else
            {
                int season = GetId(this.seasonSelector.Items[seasonSelector.SelectedIndex].ToString());
                int episode = GetId(this.episodeSelector.Items[episodeSelector.SelectedIndex].ToString());

                links = cdnService.GetCDNLinks(this.movieId, translatorId, season, episode);
            }

            if (links != null)
            {
                nextBtn.Visible = false;
                downloadBtn.Visible = true;
                qualityLabel.Visible = true;
                bulkTitle.Visible = true;
                demoTitle.Visible = true;
                bulk.Visible = true;
                bulkGuide.Visible = true;

                qualityList.DataSource = new BindingSource(links, null);
                qualityList.DisplayMember = "Key";
                qualityList.ValueMember = "Value";

                qualityList.Visible = true;
            }
        }

        private int GetId(String str)
        {
            return Int32.Parse(str.Split('[')[1].Split(']')[0]);
        }

        private void downloadBtn_Click(object sender, EventArgs e)
        {
            if (bulk.Text != "")
            {
                handleBulk();
                return;
            }
            String link = qualityList.SelectedValue.ToString();

            string episode = episodeSelector.Text;

            if (manualSeriesInput.Text != "")
            {
                episode = manualSeriesInput.Text;
            }

            startDownload(link, getFilename(link, translationSelector.Text, seasonSelector.Text, episode));
        }

        private String getFilename(String link, String translator, String season, String episode)
        {
            string fileName = translator + "_" + season + "_" + episode;

            MovieSearch search = new MovieSearch();
            String filmName = search.getNameFromUrl(url);

            fileName = fileName.Replace(" ", "_");

            String extension = link.Split('.').Last();
            if (filmName != "")
            {
                fileName = filmName + "_" + fileName;
            }

            fileName = fileName + "." + extension;

            fileName = string.Concat(fileName.Split(Path.GetInvalidFileNameChars()));

            return fileName;
        }

        private void startDownload(String link, String fileName)
        {
            SaveFileDialog oSaveFileDialog = new SaveFileDialog();
            oSaveFileDialog.Filter = "All files (*.*) | *.*";
            oSaveFileDialog.FileName = fileName.Trim();

            if (oSaveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    new RezkaClient().DownloadFile(link, oSaveFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error occured: " + ex.Message, "Error");
                }
                this.Close();
            }
        }

        private void handleBulk()
        {
            String translator = translationSelector.Text;
            String season = seasonSelector.Text;
            int translatorId = GetId(this.translationSelector.Items[translationSelector.SelectedIndex].ToString());
            int seasonId = GetId(this.seasonSelector.Items[seasonSelector.SelectedIndex].ToString());
            string[] episodeRangeText = bulk.Text.Split('-');
            int[] episodeRange = new int[2];
            Int32.TryParse(episodeRangeText[0], out episodeRange[0]);
            Int32.TryParse(episodeRangeText[1], out episodeRange[1]);

            List<int> episodeIds = new List<int>();

            for(int i = episodeRange[0] - 1; i <= episodeRange[1] - 1; i++)
            {
                episodeIds.Add(GetId(this.episodeSelector.Items[i].ToString()));
            }

            String quality = qualityList.Text;

            foreach (int episodeId in episodeIds)
            {
                Dictionary<string, string> links = cdnService.GetCDNLinks(this.movieId, translatorId, seasonId, episodeId);
                if(!links.ContainsKey(quality))
                {
                    MessageBox.Show("Quality " + qualityList.Text + " is not avaialble for the episode " + (episodeId + 1) + ". Skipping.", "Quality is not available.");
                    continue;
                }
                String link = links[quality];
                startDownload(link, getFilename(link, translator, season, episodeId.ToString()));
            }
        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            manualSeriesInput.Visible = true;
        }
    }
}
