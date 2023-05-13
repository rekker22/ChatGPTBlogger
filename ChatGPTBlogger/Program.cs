using Google.Apis.Auth.OAuth2;
using Google.Apis.Blogger.v3;
using Google.Apis.Blogger.v3.Data;
using Google.Apis.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Configuration.Json;
using ChatGPTBlogger.Service;
using ChatGPTBlogger.Service.Models.DataDTO;
using ChatGPTBlogger.Service.Models.HttpRequestDTO;
using System.Xml;
using System.Xml.Linq;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Text.Json;
using System.Text;
using ChatGPTBlogger.Service.Models.HttpRequestDTO.Response;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.Console;
using Newtonsoft.Json;

namespace ChatGPTBlogger
{

    public static class ConfigurationFactory
    {


        /// <summary>
        /// Use for .NET Core Console applications.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="env"></param>
        /// <returns></returns>
        private static IConfigurationBuilder Configure(IConfigurationBuilder config, Microsoft.Extensions.Hosting.IHostingEnvironment env)
        {
            return Configure(config, env.EnvironmentName);
        }

        private static IConfigurationBuilder Configure(IConfigurationBuilder config, string environmentName)
        {
            return config
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
        }

        /// <summary>
        /// Use for .NET Core Console applications.
        /// </summary>
        /// <returns></returns>
        public static IConfiguration CreateConfiguration()
        {
            var env = new HostingEnvironment
            {
                EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                ApplicationName = AppDomain.CurrentDomain.FriendlyName,
                ContentRootPath = AppDomain.CurrentDomain.BaseDirectory,
                ContentRootFileProvider = new PhysicalFileProvider(AppDomain.CurrentDomain.BaseDirectory) //C:\Projects\ChatGPTBlogger\ChatGPTBlogger\appsettings.json
            };

            var config = new ConfigurationBuilder();
            var configured = Configure(config, env);

            return configured.Build();
        }
    }
    class BloggerAPI
    {

        private static BloggerService blgservice = Auth();
        private static BloggerService Auth()
        {

            UserCredential credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
            new ClientSecrets
            {
                ClientId = "196110353876-ohov8r2psq4cjgvfvnonalp6abntnilt.apps.googleusercontent.com",
                ClientSecret = "ecru4tBw1b7hHN9axJgneTZr"
            },
            new[] { BloggerService.Scope.Blogger },
            "Admin", CancellationToken.None).Result;
            //, 
            //CancellationToken.None, new FileDataStore("Books.ListMyLibrary")).Result;

            // Create the service.

            var service = new BloggerService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "BloggerPoster",
            });

            return service;
        }
        public async Task blogPoster(string Title, string Content)
        {
            Post post = new Post();

            post.Title = Title;
            post.Content = Content;

            post.Labels = new List<string>() { "Anime", "News" };

            var postsInsertAction = blgservice.Posts.Insert(post, "3771216211958786553");

            //postsInsertAction.IsDraft = true;

            var insertedPost = await postsInsertAction.ExecuteAsync();

            //Console.WriteLine(insertedPost.Title + " " + insertedPost.Content);

        }

    }


    class TwitterAPI
    {
        static public readonly HttpClient _twitterAPIHttpClient;
        public string twitterUserEndpoint = "2/users/1118144172307943425/tweets?expansions=attachments.media_keys&media.fields=preview_image_url,url&tweet.fields=referenced_tweets,entities";
        public string twitterTweetEndpoint = "2/tweets/";
        static public List<ChatGPTTweetData> chatGPTTweetDatas = new List<ChatGPTTweetData>();
        static private ILogger _logger;

        static TwitterAPI()
        {
            _twitterAPIHttpClient = new HttpClient()
            {
                BaseAddress = new("https://api.twitter.com")
            };
        }

        public TwitterAPI(ILogger logger)
        {
            _logger= logger;
        }

        public async Task<TwitterData> GetAllTweets(API api, string clientSecret, string? paginationToken)
        {
            string localTwitterUserEndpoint = twitterUserEndpoint;

            if (paginationToken != null)
            {
                localTwitterUserEndpoint += "&pagination_token=" + paginationToken;
            }

            TwitterData response = await api.GetAsync<TwitterData>(localTwitterUserEndpoint, _twitterAPIHttpClient, clientSecret);

            return response;

        }

        public async Task<TweetData> GetTweet(string tweetId, API api, string clientSecret)
        {
            string localTwitterTweetEndpoint = twitterTweetEndpoint + tweetId + "?tweet.fields=referenced_tweets,entities&expansions=attachments.media_keys&media.fields=preview_image_url,url";

            TweetData data = await api.GetAsync<TweetData>(localTwitterTweetEndpoint, _twitterAPIHttpClient, clientSecret);

            return data;
        }

        private bool GetAlreadyAddedTweets(string? id)
        {
            bool found = false;

            foreach (ChatGPTTweetData chtgptTweetdata in chatGPTTweetDatas)
            {
                if (chtgptTweetdata.id.Contains(id))
                {
                    found = true;
                    break;
                }
            }
            return found;
        }

        public async Task FormatChatGPTTweet(TwitterData allTweets, string LastAddedTweetId, API api, string clientSecret)
        {
            foreach (Data d in allTweets.data) // Adding tweets which has been added as replied
            {
                if (!GetAlreadyAddedTweets(d.id) && (d.referenced_tweets == null || d.referenced_tweets?.Where(x => x.type == "retweeted").Count() <= 0))
                // filtering out the tweets where it is already added as replied tweet or reference_tweet is null(which means user created tweet)
                // and is not retweeted(only retweet no text - best to avoid as it can only be japanese)
                {
                    ChatGPTTweetData chatGPTTweetData = new ChatGPTTweetData();
                    chatGPTTweetData.text = d.text;
                    chatGPTTweetData.id.Add(d.id);
                    if (d.entities != null && d.entities.urls != null)
                    { //replacing original Urls
                        foreach (Url url in d.entities.urls)
                        {
                            chatGPTTweetData.text = chatGPTTweetData.text.Replace(url.url, url.expanded_url);

                            chatGPTTweetData.Source.Add(url.expanded_url);

                            if (url.images != null)
                            {

                                foreach (Image image in url.images) //Adding the images
                                {
                                    if (image.url.Contains("name=orig"))
                                    {
                                        chatGPTTweetData.images.Add(image);
                                    }
                                }
                            }
                        }
                    }
                    if (d.attachments != null)
                    { //adding media urls
                        foreach (string m_keys in d.attachments.media_keys)
                        {
                            chatGPTTweetData.media.Add(allTweets.includes.media.Where(x => x.media_key == m_keys).FirstOrDefault());
                        }
                    }

                    if(d.attachments == null && chatGPTTweetData.images.Count == 0)
                    {
                        ReferencedTweet? tempQuotedTweet = d.referenced_tweets?.Where(x => x.type == "quoted").FirstOrDefault();
                        TweetData twdata = await GetTweet(tempQuotedTweet.id, api, clientSecret);

                        foreach (string m_keys in twdata.data.attachments.media_keys)
                        {
                            chatGPTTweetData.media.Add(twdata.includes.media.Where(x => x.media_key == m_keys).FirstOrDefault());
                        }
                    }

                    List<ReferencedTweet>? tempReferenceTweet = d.referenced_tweets;

                    while (tempReferenceTweet != null && tempReferenceTweet?.Where(x => x.type == "replied_to").Count() > 0) //if any replied_to tweet exists then add text images and id 
                    {
                        ReferencedTweet? rt = tempReferenceTweet?.Where(x => x.type == "replied_to").FirstOrDefault();
                        TweetData data = await GetTweet(rt.id, api, clientSecret);

                        chatGPTTweetData.text = chatGPTTweetData.text.Substring(0, 0) + data.data.text + '\n' + chatGPTTweetData.text.Substring(0);
                        chatGPTTweetData.id.Add(data.data.id);
                        tempReferenceTweet = data.data.referenced_tweets;
                        if (data.includes != null)
                        { //adding media urls of replied tweets
                            chatGPTTweetData.media.AddRange(data.includes.media);
                        }

                        if (data.data.entities != null && data.data.entities.urls != null)
                        { //replacing original Urls
                            foreach (Url url in data.data.entities.urls)
                            {
                                chatGPTTweetData.text = chatGPTTweetData.text.Replace(url.url, url.expanded_url);
                                chatGPTTweetData.Source.Add(url.expanded_url);

                                if (url.images != null)
                                {

                                    foreach (Image image in url.images) //Adding the images
                                    {
                                        if (image.url.Contains("name=orig"))
                                        {
                                            chatGPTTweetData.images.Add(image);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    chatGPTTweetDatas.Add(chatGPTTweetData);


                }
            }


        }

        public TwitterData GetLatestTweets(TwitterData allTweetsResponse, string lastAddedTweetId)
        {
            TwitterData tempTweetData = new TwitterData()
            {
                data = allTweetsResponse.data.Where(x => Convert.ToInt64(x.id) > Convert.ToInt64(lastAddedTweetId)).ToList(),
                meta = new Meta()
                {
                    result_count = allTweetsResponse.data.Where(x => Convert.ToInt64(x.id) > Convert.ToInt64(lastAddedTweetId)).Count(),
                    oldest_id = lastAddedTweetId,
                    newest_id = allTweetsResponse.meta.newest_id,
                    next_token = allTweetsResponse.meta.next_token
                },
                includes = allTweetsResponse.includes
            };

            return tempTweetData;
        }

        public void UpdateLastAddedTweetId(string lastAddedTweetId)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(AppDomain.CurrentDomain.BaseDirectory + "LastAddedTweetId.xml");
            xmlDoc.SelectSingleNode("LastAddedTweetId").InnerText = lastAddedTweetId;
            xmlDoc.Save(AppDomain.CurrentDomain.BaseDirectory + "LastAddedTweetId.xml");

        }

        public string GetLastAddedTweetId()
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(AppDomain.CurrentDomain.BaseDirectory + "LastAddedTweetId.xml");
            string? LastUpdatedId = xmlDoc.SelectSingleNode("LastAddedTweetId")?.InnerText;



            return LastUpdatedId;

        }

        public long GetMaxId(List<string> Ids)
        {
            long maxId = 0;

            List<long> ids = new List<long>();

            foreach (string id in Ids)
            {
                ids.Add(long.Parse(id));
            }

            foreach (long id in ids)
            {
                if (id > maxId)
                {
                    maxId = id;
                }
            }

            return maxId;
        }

    }

    class ChatGPTAPI
    {

        static private readonly HttpClient _chatGPTAPIHttpClient;
        private string endpoint = "v1/chat/completions";
        private ILogger _logger;

        static ChatGPTAPI()
        {
            _chatGPTAPIHttpClient = new HttpClient()
            {
                BaseAddress = new("https://api.openai.com")
            };
        }

        public ChatGPTAPI(ILogger logger)
        {
            _logger = logger;
        }

        static void logIt(ILogger logger, string message, bool completed)
        {
            string timeStamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff");

            logger.LogInformation(timeStamp + ' ' + message + " - " + (completed ? "Completed" : "Started"));

        }

        public async Task<string> GetTitle(ChatGPTTweetData chatGPTTweet, API api, string clientSecret)
        {
            ChatGPTPost chatGPTPost = new ChatGPTPost();

            chatGPTPost.messages = new List<Service.Models.HttpRequestDTO.Message>{
                new Service.Models.HttpRequestDTO.Message()
                {
                    role = "user",
                    content = chatGPTTweet.text.Replace('\n', ' ') + ". Write a article title based on provided information."
                }
            };

            ChatGPTPostResponse chatGPTPostResponse = await api.PostAsync<ChatGPTPost, ChatGPTPostResponse>(endpoint, _chatGPTAPIHttpClient, clientSecret, chatGPTPost);

            //logIt(_logger, "Title" + JsonConvert.SerializeObject(chatGPTPostResponse), false);

            return chatGPTPostResponse.choices.FirstOrDefault().message.content;

        }

        public async Task<string> GetBody(ChatGPTTweetData chatGPTTweet, API api, string clientSecret)
        {
            ChatGPTPost chatGPTPost = new ChatGPTPost();

            chatGPTPost.messages = new List<Service.Models.HttpRequestDTO.Message>{
                new Service.Models.HttpRequestDTO.Message()
                {
                    role = "user",
                    content = chatGPTTweet.text.Replace('\n', ' ') + ". I want you to act as a news article writer. Write a news article of maximum 500 words based on provided information only."
                } };

            ChatGPTPostResponse chatGPTPostResponse = await api.PostAsync<ChatGPTPost, ChatGPTPostResponse>(endpoint, _chatGPTAPIHttpClient, clientSecret, chatGPTPost);

            //logIt(_logger, "Body" + JsonConvert.SerializeObject(chatGPTPostResponse), false);

            return chatGPTPostResponse.choices.FirstOrDefault().message.content;

        }



    }



    internal class Program
    {

        static void logIt(ILogger logger, string message, bool completed)
        {
            string timeStamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff");

            logger.LogInformation(timeStamp + ' ' + message + " - " + (completed ? "Completed" : "Started"));

        }

        static async Task Main(string[] args)
        {

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddFilter("NonHostConsoleApp.Program", LogLevel.Debug)
                    .AddConsole();
            });

            ILogger logger = loggerFactory.CreateLogger<Program>();
            IConfiguration configuration = ConfigurationFactory.CreateConfiguration();
            string TwitterclientSecret = configuration.GetRequiredSection("Twitter:ClientSecret").Value;
            string ChatGPTClientSecret = configuration.GetRequiredSection("ChatGPT:ClientSecret").Value;

            API api = new API();
            TwitterAPI twitterAPI = new TwitterAPI(logger);

            logIt(logger, "twitterAPI.GetAllTweets", false);

            TwitterData allTweetsResponse = await twitterAPI.GetAllTweets(api, TwitterclientSecret, null); //Get all the response for first call

            logIt(logger, "twitterAPI.GetAllTweets", true);

            string lastAddedTweetId = twitterAPI.GetLastAddedTweetId();

            logIt(logger, "twitterAPI.GetLatestTweets", false);

            TwitterData allTweets = twitterAPI.GetLatestTweets(allTweetsResponse, lastAddedTweetId); //added required tweets only

            //allTweets.includes = allTweetsResponse.includes;

            string latestAddedTweetId = allTweets.meta.newest_id;

            while (allTweets != null && Convert.ToInt64(allTweets.meta.newest_id) > Convert.ToInt64(lastAddedTweetId)) //utilizing next_token to furhter get the tweets that we don't have yet
            {

                var response = await twitterAPI.GetAllTweets(api, TwitterclientSecret, allTweets.meta.next_token);

                TwitterData twData = twitterAPI.GetLatestTweets(response, lastAddedTweetId);

                allTweets.data.AddRange(twData.data);
                allTweets.meta = twData.meta;

                if (twData.includes != null)
                {
                    allTweets.includes?.media.AddRange(twData?.includes.media);
                }


            }

            logIt(logger, "twitterAPI.GetLatestTweets", true);

            //twitterAPI.UpdateLastAddedTweetId(latestAddedTweetId); //update XML that we got the latest data

            logIt(logger, "twitterAPI.FormatChatGPTTweet", false);

            await twitterAPI.FormatChatGPTTweet(allTweets, lastAddedTweetId, api, TwitterclientSecret);

            logIt(logger, "twitterAPI.FormatChatGPTTweet", true);

            ChatGPTAPI chatGPTAPI = new ChatGPTAPI(logger);

            BloggerAPI bloggerAPI = new BloggerAPI();

            //logIt(logger, "twitterAPI.chatGPTTweetDatas", false);

            foreach (ChatGPTTweetData tweetData in TwitterAPI.chatGPTTweetDatas.AsEnumerable().Reverse())
            {

                logIt(logger, "chatGPTAPI.GetTitle ", false);

                string temptitle = await chatGPTAPI.GetTitle(tweetData, api, ChatGPTClientSecret);

                string title = temptitle.Replace("\"", "");

                logIt(logger, "chatGPTAPI.GetTitle" + title, true);

                logIt(logger, "chatGPTAPI.GetBody - " + tweetData.id.FirstOrDefault().ToString(), false);

                string tempbody = await chatGPTAPI.GetBody(tweetData, api, ChatGPTClientSecret);

                List<string> anchorLinks = new List<string>();

                foreach (string url in tweetData.Source)
                {
                    string anchorLink = "";

                    if (!url.Contains("twitter") && !url.Contains("youtube"))
                    {
                        anchorLink += $"<a href=\"{url}\" rel=\"nofollow\" target=\"_blank\">Official Website</a>";
                    }

                    else if (url.Contains("twitter") && !url.Contains("AIR_News01"))
                    {
                        anchorLink += $"<a href=\"{url}\" rel=\"nofollow\" target=\"_blank\">Official Twitter</a>";
                    }

                    else if (url.Contains("youtube"))
                    {
                        anchorLink += $"<a href=\"{url}\" rel=\"nofollow\" target=\"_blank\">Youtube Video</a>";
                    }

                    if (anchorLink != "") { anchorLinks.Add(anchorLink); }


                }

                List<string> images = new List<string>();

                if (tweetData.media.Count > 0)
                {
                    foreach (Media media in tweetData.media)
                    {
                        string imagesHTML = $"<p style=\"text-align: center;\">  <img alt=\"{title}\" src=\"{media.url}\" width=\"500\" /></p>";

                        images.Add(imagesHTML);
                    }

                }

                else if (tweetData.media.Count == 0 && tweetData.images.Count > 0)
                {
                    foreach (Image image in tweetData.images)
                    {
                        string imagesHTML = $"<p style=\"text-align: center;\">  <img alt=\"{title}\" src=\"{image.url}\" width=\"500\" /></p>";

                        images.Add(imagesHTML);
                    }
                }

                string source = $"<p>Source -  {string.Join(", ", anchorLinks)}</p>"; //<a href=\"https://twitter.com/SugoiLITE/status/1503367245325488129\" rel=\"nofollow\" target=\"_blank\">Leak</a>

                string body = $"<h1>{title}</h1><span><!--more--></span><br />{string.Join(" ", images)}<p>{tempbody.Replace("\n\n", "</p><p>").Trim('\"')}</p>{source}";

                logIt(logger, "chatGPTAPI.GetBody" + body.Substring(0, 100), true);

                logIt(logger, "bloggerAPI.blogPoster", false);

                await bloggerAPI.blogPoster(title, body);

                logIt(logger, "bloggerAPI.blogPoster", true);





                twitterAPI.UpdateLastAddedTweetId(twitterAPI.GetMaxId(tweetData.id).ToString()); //update XML that we got the latest data

                string ChatGPTTimeOut = configuration.GetRequiredSection("ChatGPT:TimeOut").Value;

                logIt(logger, ChatGPTTimeOut + "ms Sleep Time", false);

                

                System.Threading.Thread.Sleep(Int32.Parse(ChatGPTTimeOut));

                logIt(logger, ChatGPTTimeOut + "ms Sleep Time", true);

            }




        }
    }
}