namespace ChatGPTBlogger.Service.Models.DataDTO
{
    public class ChatGPTTweetData
    {
        public string text { get; set; }
        public List<string> id =new List<string>();
        public List<Media> media = new List<Media>();
        public List<string> Source =new List<string>();
        public List<Image> images = new List<Image>();

        

    }
}
