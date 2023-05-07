using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatGPTBlogger.Service.Models.HttpRequestDTO
{
    public class ChatGPTPost
    {
        [JsonPropertyName("model")]
        public string model { get; set; } = "gpt-3.5-turbo-0301";


        [JsonPropertyName("messages")]
        public List<Message> messages { get; set; }


        [JsonPropertyName("temperature")]
        public double temperature { get; set; } = 0.5;
    }

    public class Message
    {
        [JsonPropertyName("role")]
        public string role { get; set; }

        [JsonPropertyName("content")]
        public string content { get; set; }
    }
}
