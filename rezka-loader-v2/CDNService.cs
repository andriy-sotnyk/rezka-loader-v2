using System;
using System.Collections;
using System.Linq;
using System.Text;
using HtmlAgilityPack;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace rezka_loader_v2
{
    internal class CDNService
    {
        private RezkaClient _client;
        public CDNService()
        {
            _client = new RezkaClient();
        }

        public MoviePageData GetMovieDownloadOptionsWithSelenium(string url)
        {
            // Налаштовуємо фоновий режим (headless)
            ChromeOptions options = new ChromeOptions();
            //options.AddArgument("--headless");
           // options.AddArgument("--disable-gpu");

            using (IWebDriver driver = new ChromeDriver(options))
            {
                try
                {
                    driver.Navigate().GoToUrl(url);

                    WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

                    wait.Until(d => d.Title != null && d.Title.Contains("Смотреть"));

                    string pageSource = driver.PageSource;

                    if (string.IsNullOrEmpty(pageSource))
                    {
                        return null;
                    }

                    HtmlAgilityPack.HtmlDocument html = new HtmlAgilityPack.HtmlDocument();
                    html.LoadHtml(pageSource);

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
                                string translatorName = translators.GetAttributeValue("title", "");
                                string translatorId = translators.GetAttributeValue("data-translator_id", "");
                                
                                if (translatorId == "")
                                {
                                    HtmlNode a = translators.FirstChild;
                                    translatorName = a.GetAttributeValue("title", "");
                                    translatorId = a.GetAttributeValue("data-translator_id", "");
                                }

                                availableTranslators.Add(new Translator(translatorName, int.Parse(translatorId)));
                            }
                        }
                    }

                    if (seasonContainer != null)
                    {
                        foreach (HtmlNode season in seasonContainer.ChildNodes)
                        {
                            if (season.NodeType == HtmlNodeType.Element)
                            {
                                string seasonNumber = season.InnerText;
                                string seasonID = season.GetAttributeValue("data-tab_id", "");
                                availableSeasons.Add(new Season(seasonNumber, int.Parse(seasonID)));
                            }
                        }
                    }

                    if (episodesContainer != null)
                    {
                        foreach (HtmlNode episode in episodesContainer.ChildNodes)
                        {
                            if (episode.NodeType == HtmlNodeType.Element)
                            {
                                string episodeNumber = episode.InnerText;
                                string episodeID = episode.GetAttributeValue("data-episode_id", "");
                                availableEpisodes.Add(new Episode(episodeNumber, int.Parse(episodeID)));
                            }
                        }
                    }

                    if (favContainer != null)
                    {
                        movieId = int.Parse(favContainer.GetAttributeValue("value", "-1"));
                    }

                    if (movieId == -1)
                    {
                        return null;
                    }

                    if (availableTranslators.Count == 0)
                    {
                        HtmlNodeCollection scriptNodes = html.DocumentNode.SelectNodes("//script");
                        if (scriptNodes != null)
                        {
                            foreach (HtmlNode node in scriptNodes)
                            {
                                if (node.InnerHtml.Contains("initCDNMoviesEvents"))
                                {
                                    string[] scriptItems = node.InnerHtml.Split(',');
                                    if (scriptItems.Length > 1 && int.TryParse(scriptItems[1], out int _))
                                    {
                                        availableTranslators.Add(new Translator("Default", int.Parse(scriptItems[1])));
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    return new MoviePageData(availableTranslators, availableSeasons, availableEpisodes, movieId);
                }
                catch (Exception)
                {
                    // Сюди програма потрапить також у випадку, якщо за 10 секунд тайтл так і не змінився (вилетить WebDriverTimeoutException)
                    return null;
                }
            }
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
