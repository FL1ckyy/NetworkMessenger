using System;
using System.Text.Json;

namespace Common
{
    [Serializable]
    public class Message
    {
        public string Author { get; set; }
        public string Text { get; set; }
        public DateTime Timestamp { get; set; }

        public Message() { }

        public Message(string author, string text)
        {
            Author = author;
            Text = text;
            Timestamp = DateTime.Now;
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }

        public static Message FromJson(string json)
        {
            return JsonSerializer.Deserialize<Message>(json);
        }

        public override string ToString()
        {
            return $"[{Timestamp:HH:mm:ss}] {Author}: {Text}";
        }
    }
}