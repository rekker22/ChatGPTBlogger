using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ChatGPTBlogger.Service.Models.DataDTO
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    //public class Data
    //{
    //    public List<string> edit_history_tweet_ids { get; set; }
    //    public string text { get; set; }
    //    public List<ReferencedTweet>? referenced_tweets { get; set; }
    //    public string id { get; set; }
    //}

    //public class Meta
    //{
    //    public int result_count { get; set; }
    //    public string newest_id { get; set; }
    //    public string oldest_id { get; set; }
    //    public string next_token { get; set; }
    //}

    //public class ReferencedTweet
    //{
    //    public string type { get; set; }

    //    public string id { get; set; }

    //}

    //public class TwitterData
    //{
    //    public List<Data> data { get; set; }
    //    public Meta meta { get; set; }
    //}

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Annotation
    {
        public int start { get; set; }
        public int end { get; set; }
        public double probability { get; set; }
        public string type { get; set; }
        public string normalized_text { get; set; }
    }

    public class Attachments
    {
        public List<string> media_keys { get; set; }
    }

    public class Data
    {
        public Entities entities { get; set; }
        public Attachments attachments { get; set; }
        public string id { get; set; }
        public List<ReferencedTweet> referenced_tweets { get; set; }
        public List<string> edit_history_tweet_ids { get; set; }
        public string text { get; set; }
    }

    public class Entities
    {
        public List<Url> urls { get; set; }
        public List<Annotation> annotations { get; set; }

        public List<Hashtag> hashtags { get; set; }
    }

    public class Hashtag
    {
        public int? start { get; set; }
        public int? end { get; set; }
        public string tag { get; set; }
    }

    public class Image
    {
        public string url { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }

    public class Includes
    {
        public List<Media> media = new List<Media>();
    }

    public class Media
    {
        [JsonPropertyName("media_key")]
        public string media_key { get; set; }

        [JsonPropertyName("type")]
        public string type { get; set; }

        [JsonPropertyName("url")]
        public string? url { get; set; }
    }

    public class Meta
    {
        public int result_count { get; set; }
        public string newest_id { get; set; }
        public string oldest_id { get; set; }
        public string next_token { get; set; }
    }

    public class ReferencedTweet
    {
        public string type { get; set; }
        public string id { get; set; }
    }

    public class TwitterData
    {
        public List<Data> data { get; set; }
        public Includes includes { get; set; }
        public Meta meta { get; set; }
    }

    public class Url
    {
        public int start { get; set; }
        public int end { get; set; }
        public string url { get; set; }
        public string expanded_url { get; set; }
        public string display_url { get; set; }
        public int status { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string unwound_url { get; set; }
        public string media_key { get; set; }
        public List<Image> images { get; set; }
    }



    public class TweetData
    {
        public Data data { get; set; }
        public Includes includes { get; set; }
    }


}
