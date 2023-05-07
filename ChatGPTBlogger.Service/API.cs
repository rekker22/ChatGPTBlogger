using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ChatGPTBlogger.Service
{
    public class API
    {

        public async Task<T> GetAsync<T>(string endpoint, HttpClient _httpClient, string _clientSecret)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _clientSecret);

            using HttpResponseMessage response = await _httpClient.GetAsync(endpoint);

            string Response = await response.Content.ReadAsStringAsync();

            //T jsonResponse = JsonSerializer.Deserialize<T>(Response);

            T jsonResponse = JsonConvert.DeserializeObject<T>(Response);

            return jsonResponse;
            
            
        }

        public async Task<R> PostAsync<T, R>(string endpoint, HttpClient _httpClient, string _clientSecret, T PostBody)
        {

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _clientSecret);

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));


            using StringContent jsonContent = new(
                JsonConvert.SerializeObject(PostBody),
                Encoding.UTF8,
                "application/json");

            using HttpResponseMessage response = await _httpClient.PostAsync(
                endpoint,
                jsonContent);
    

            string Response = await response.Content.ReadAsStringAsync();

            //R jsonResponse = JsonSerializer.Deserialize<R>(Response);


            R jsonResponse = JsonConvert.DeserializeObject<R>(Response);

            return jsonResponse;

        }

    }
}
