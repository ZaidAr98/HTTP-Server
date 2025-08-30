
using System.Text;

namespace src.Http
{
    public class HttpResponse
    {
        public int StatusCode { get; set; }
        public string StatusMessage => HttpStatusMessage.GetMessage(StatusCode);
        public string Body { get; set; } = string.Empty;
        public Dictionary<string, string> Headers { get; set; }

        public byte[] ToByteArray()
        {
            StringBuilder responseBuilder = new StringBuilder();
            responseBuilder.Append($"HTTP/1.1 {StatusCode} {StatusMessage}\r\n");
            if (Headers != null)
            {
                foreach (var header in Headers)
                {
                    responseBuilder.Append($"{header.Key}:{header.Value}\r\n");
                }
            }

            responseBuilder.Append("\r\n");
            if (!string.IsNullOrEmpty(Body))
            {
                responseBuilder.Append(Body);
            }
            return Encoding.UTF8.GetBytes(responseBuilder.ToString());
        }
        public void AddHeader(string key, string value)
        {
            if (Headers == null)
            {
                Headers = new Dictionary<string, string>();
            }
            Headers.Add(key, value);
        }
    }
}