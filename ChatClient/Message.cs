using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatClient
{
    [Serializable]
    public class Message
    {
        [JsonPropertyName("author")]
        public string Author { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        public Message()
        {
            Timestamp = DateTime.Now;
        }

        public Message(string author, string text)
        {
            Author = author;
            Text = text;
            Timestamp = DateTime.Now;
        }

        public string ToJson()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            return JsonSerializer.Serialize(this, options);
        }

        public static Message FromJson(string json)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<Message>(json, options);
        }

        public override string ToString()
        {
            return $"[{Timestamp:HH:mm:ss}] {Author}: {Text}";
        }
    }
}