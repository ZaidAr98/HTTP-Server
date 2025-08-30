using System.Net.Sockets;
using System.Text;
namespace src.Http
{


    public class HttpRequest
    {
        public HttpRequest(Socket clientSocket)
        {
            ClientSocket = clientSocket;
            Parse(clientSocket);
        }

        public HttpMethod Method { get; set; }
        public Host Host { get; set; }

        public string Path { get; set; }

        public string Body { get; set; }

        public Socket ClientSocket { get; set; }


        public Dictionary<string, string> Headers { get; set; }


        public void Parse(Socket socket)
        {
            byte[] buffer = new byte[4096];
            var received = socket.Receive(buffer);

            string requestText = Encoding.UTF8.GetString(buffer, 0, received);
            Console.WriteLine($"Request:\n{requestText}");
            this.Method = GetMethod(requestText);
            this.Host = GetHost(requestText);
            this.Path = GetPath(requestText);
            this.Body = GetBody(requestText);
            this.Headers = GetHeaders(requestText);

        }


       private Host GetHost(string requestText)
    {
        var splitted = requestText.Split("\r\n");
        var host = splitted[1].Split(": ")[1];
        var protocol = splitted[0].Split(" ")[2];
        var port = 80;
        if (host.Contains(":"))
        {
            port = int.Parse(host.Split(":")[1]);
            host = host.Split(":")[0];
        }

        return new Host(host, port, protocol);
    }



        private Dictionary<string, string> GetHeaders(string requestText)
        {
            string[] sections = requestText.Split("\r\n\r\n");
            string[] lines = sections[0].Split("\r\n");
              string[] headerLines = lines[1..];
            Dictionary<string, string> headers = new Dictionary<string, string>();


            foreach (var line in headerLines)
            {
                var header = line.Trim().Split(": ");
                if (header.Length == 2)
                {
                    headers.Add(header[0], header[1]);
                }
            }
            return headers;
        }

        private string GetBody(string requestText)
        {
            string[] sections = requestText.Split("\r\n\r\n");
            string body = sections.Length > 1 ? sections[1] : "";
            return body.Trim();
    }    
        private string GetPath(string requestText)
        {
            var splitted = requestText.Split("\r\n");
            var route = splitted[0].Split(" ")[1];
            return route;
        }

        private HttpMethod GetMethod(string request)
        {
            var method = request.Split(" ")[0];
            return method switch
            {
                "GET" => HttpMethod.GET,
                "POST" => HttpMethod.POST,
                "PUT" => HttpMethod.PUT,
                "DELETE" => HttpMethod.DELETE,
                _ => throw new NotImplementedException()
            }; 
       }

          





    }

}