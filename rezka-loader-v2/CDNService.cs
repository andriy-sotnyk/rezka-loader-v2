using System;
using System.Collections;
using System.Linq;
using System.Text;
using HtmlAgilityPack;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace rezka_loader_v2
{
    internal class CDNService
    {
        private RezkaClient _client;
        public CDNService()
        {
            _client = new RezkaClient();
        }

        public MoviePageData GetMovieDownloadOptions(String url)
        {
            String moviePageHTML = _client.GetMoviePage(url);

            if (moviePageHTML == null)
            {
                return null;
            }

            HtmlAgilityPack.HtmlDocument html = new HtmlAgilityPack.HtmlDocument();
            html.LoadHtml(moviePageHTML);

            HtmlNode translatorsContainer = html.DocumentNode.SelectSingleNode("//ul[contains(@class, 'b-translators__list')]");
            HtmlNode seasonContainer = html.DocumentNode.SelectSingleNode("//ul[contains(@class, 'b-simple_seasons__list')]");
            HtmlNode episodesContainer = html.DocumentNode.SelectSingleNode("//ul[contains(@class, 'b-simple_episodes__list')]");
            HtmlNode favContainer = html.DocumentNode.SelectSingleNode("//input[contains(@id, 'post_id')]");

            int movieId = -1;
            ArrayList availableTranslators = new ArrayList();
            ArrayList availableSeasons = new ArrayList();
            ArrayList availableEpisodes = new ArrayList();

            if (translatorsContainer != null)
            {
                foreach (HtmlNode translators in translatorsContainer.ChildNodes)
                {
                    if (translators.NodeType == HtmlNodeType.Element)
                    {
                        String translatorName = translators.GetAttributeValue("title", "");
                        String translatorId = translators.GetAttributeValue("data-translator_id", "");

                        availableTranslators.Add(new Translator(translatorName, Int32.Parse(translatorId)));
                    }
                }
            }

            if (seasonContainer != null)
            {
                foreach (HtmlNode season in seasonContainer.ChildNodes)
                {
                    if (season.NodeType == HtmlNodeType.Element)
                    {
                        String seasonNumber = season.InnerText;
                        String seasonID = season.GetAttributeValue("data-tab_id", "");

                        availableSeasons.Add(new Season(seasonNumber, Int32.Parse(seasonID)));
                    }
                }
            }

            if (episodesContainer != null)
            {
                foreach (HtmlNode episode in episodesContainer.ChildNodes)
                {
                    if (episode.NodeType == HtmlNodeType.Element)
                    {
                        String episodeNumber = episode.InnerText;
                        String episodeID = episode.GetAttributeValue("data-episode_id", "");

                        availableEpisodes.Add(new Episode(episodeNumber, Int32.Parse(episodeID)));
                    }
 
                }
            }

            if (favContainer != null)
            {
                movieId = Int32.Parse(favContainer.GetAttributeValue("value", "-1"));
            }

            if (movieId == -1)
            {
                return null;
            }

            if (availableTranslators.Count == 0)
            {
                var scriptNodes = html.DocumentNode.SelectNodes("//script");
                
                foreach(var node in scriptNodes)
                {
                    if (node.InnerHtml.Contains("initCDNMoviesEvents"))
                    {
                        var scriptItems = node.InnerHtml.Split(',');
                        if (scriptItems.Length > 0 && int.TryParse(scriptItems[1], out _))
                        {
                            availableTranslators.Add(new Translator("Default", int.Parse(scriptItems[1])));
                            break;
                        }
                    }
                }
            }

            return new MoviePageData(availableTranslators, availableSeasons, availableEpisodes, movieId);
        }

        public System.Collections.Generic.Dictionary<string, string> GetCDNLinks(int movieId, int translatorId, int season = -1, int episode = -1)
        {
            CDNResponse rawCDNData = this._client.GetCDNSeries(movieId, translatorId, season, episode);
            
            if (rawCDNData == null)
            {
                return null;
            }

            return DecryptURLs(rawCDNData.url);
        }

        private System.Collections.Generic.Dictionary<string, string> ParseCDNLinks(String combinedLinks)
        {
            var CDNLinks = new System.Collections.Generic.Dictionary<string, string>();

            var CDNLinksSeparated = combinedLinks.Split(',');

            ArrayList keys = new ArrayList();
            ArrayList values = new ArrayList();
            String[] separators = { "[", "]"};

            foreach (String linkset in CDNLinksSeparated)
            {
                var CDNLinksSplitted = linkset.Split(separators, StringSplitOptions.RemoveEmptyEntries);

                foreach (String item in CDNLinksSplitted)
                {

                    if (!item.Contains("http"))
                    {
                        keys.Add(item);
                    }
                    else
                    {
                        String[] separator = { " or " };
                        var options = item.Split(separator, StringSplitOptions.RemoveEmptyEntries);

                        foreach (String option in options)
                        {
                            if (option.EndsWith("mp4"))
                            {
                               values.Add(option);
                               break;
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < keys.Count; i++)
            {
                CDNLinks.Add(keys[i].ToString(), values[i].ToString());
            }



            return CDNLinks;
        }

        // Algorithm by https://github.com/SuperZombi/HdRezkaApi
        private System.Collections.Generic.Dictionary<string, string> DecryptURLs(String trashUrl)
        {
            return ParseCDNLinks(trashUrl);
            String[] trashCombos = { "@@", "@#", "@!", "@^", "@$", "#@", "##", "#!", "#^", "#$", "!@", "!#", "!!", "!^", "!$", "^@", "^#", "^!", "^^", "^$", "$@", "$#", "$!", "$^", "$$", "@@@", "@@#", "@@!", "@@^", "@@$", "@#@", "@##", "@#!", "@#^", "@#$", "@!@", "@!#", "@!!", "@!^", "@!$", "@^@", "@^#", "@^!", "@^^", "@^$", "@$@", "@$#", "@$!", "@$^", "@$$", "#@@", "#@#", "#@!", "#@^", "#@$", "##@", "###", "##!", "##^", "##$", "#!@", "#!#", "#!!", "#!^", "#!$", "#^@", "#^#", "#^!", "#^^", "#^$", "#$@", "#$#", "#$!", "#$^", "#$$", "!@@", "!@#", "!@!", "!@^", "!@$", "!#@", "!##", "!#!", "!#^", "!#$", "!!@", "!!#", "!!!", "!!^", "!!$", "!^@", "!^#", "!^!", "!^^", "!^$", "!$@", "!$#", "!$!", "!$^", "!$$", "^@@", "^@#", "^@!", "^@^", "^@$", "^#@", "^##", "^#!", "^#^", "^#$", "^!@", "^!#", "^!!", "^!^", "^!$", "^^@", "^^#", "^^!", "^^^", "^^$", "^$@", "^$#", "^$!", "^$^", "^$$", "$@@", "$@#", "$@!", "$@^", "$@$", "$#@", "$##", "$#!", "$#^", "$#$", "$!@", "$!#", "$!!", "$!^", "$!$", "$^@", "$^#", "$^!", "$^^", "$^$", "$$@", "$$#", "$$!", "$$^", "$$$" };
            ArrayList trashCombosEncoded = new ArrayList();
            String[] separator = { "//_//" };

            foreach (String trash in trashCombos)
            {
                trashCombosEncoded.Add(System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(trash)));
            }

            trashUrl = trashUrl.Replace("#h", "");
            trashUrl = String.Join("", trashUrl.Split(separator, StringSplitOptions.RemoveEmptyEntries));

            foreach (String trash in trashCombosEncoded)
            {
                var temp = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(trash));
                trashUrl = trashUrl.Replace(temp, "");
            }

            try
            {
                String clearedURLs = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(trashUrl));
                return ParseCDNLinks(clearedURLs);
            } catch
            {
                return null;
            }
        }
    }
}
