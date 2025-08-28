using System.Net;
using System.Net.Sockets;
using System.Text;

public class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Starting HTTP Server...");

        TcpListener server = new TcpListener(IPAddress.Any, 4221);
        server.Start();
        Console.WriteLine("Listening on port 4221...");

        try
        {
            while (true)
            {
                Socket clientSocket = server.AcceptSocket();
                Console.WriteLine("Client connected");
                Thread clientThread = new Thread(() => HandleClient(clientSocket, args));
                clientThread.Start();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Server error: {ex.Message}");
        }
        finally
        {
            server.Stop();
        }
    }

    static void HandleClient(Socket clientSocket, string[] args)
    {
        Console.WriteLine("Handling client connection");
        byte[] buffer = new byte[4096];

        try
        {
            var received = clientSocket.Receive(buffer);
            string requestText = Encoding.UTF8.GetString(buffer, 0, received);
            Console.WriteLine($"Request:\n{requestText}");

            var request = HttpRequest.Parse(requestText);
            var response = ProcessRequest(request, args);

            byte[] responseBytes = response.ToByteArray();
            clientSocket.Send(responseBytes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling client: {ex.Message}");
            var errorResponse = new HttpResponse
            {
                StatusCode = 500,
                Body = "Internal Server Error"
            };
            errorResponse.AddHeader("Content-Type", "text/plain");
            errorResponse.AddHeader("Content-Length", errorResponse.Body.Length.ToString());
            
            try
            {
                clientSocket.Send(errorResponse.ToByteArray());
            }
            catch (Exception sendEx)
            {
                Console.WriteLine($"Error sending error response: {sendEx.Message}");
            }
        }
        finally
        {
            try
            {
                clientSocket.Close();
            }
            catch (Exception closeEx)
            {
                Console.WriteLine($"Error closing socket: {closeEx.Message}");
            }
        }
    }

    static HttpResponse ProcessRequest(HttpRequest request, string[] args)
    {
        var response = new HttpResponse();

        try
        {
            switch (request.Method)
            {
                case HttpMethod.GET:
                    return ProcessGetRequest(request, args);
                case HttpMethod.POST:
                    return ProcessPostRequest(request, args);
                default:
                    response.StatusCode = 405; // Method Not Allowed
                    response.Body = "Method Not Allowed";
                    response.AddHeader("Content-Type", "text/plain");
                    response.AddHeader("Content-Length", response.Body.Length.ToString());
                    return response;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing request: {ex.Message}");
            response.StatusCode = 500;
            response.Body = "Internal Server Error";
            response.AddHeader("Content-Type", "text/plain");
            response.AddHeader("Content-Length", response.Body.Length.ToString());
            return response;
        }
    }

    static HttpResponse ProcessGetRequest(HttpRequest request, string[] args)
    {
        var response = new HttpResponse();

        if (request.Path == "/")
        {
            response.StatusCode = 200;
            return response;
        }
        else if (request.Path.StartsWith("/echo/"))
        {
            string message = request.Path.Substring(6);
            response.StatusCode = 200;
            response.Body = message;
            response.AddHeader("Content-Type", "text/plain");
            response.AddHeader("Content-Length", message.Length.ToString());
            return response;
        }
        else if (request.Path == "/user-agent")
        {
            string userAgent = request.Headers.GetValueOrDefault("User-Agent", "");
            response.StatusCode = 200;
            response.Body = userAgent;
            response.AddHeader("Content-Type", "text/plain");
            response.AddHeader("Content-Length", userAgent.Length.ToString());
            return response;
        }
        else if (request.Path.StartsWith("/files/"))
        {
            return ProcessFileGet(request, args);
        }
        else
        {
            response.StatusCode = 404;
            return response;
        }
    }

    static HttpResponse ProcessPostRequest(HttpRequest request, string[] args)
    {
        var response = new HttpResponse();

        if (request.Path.StartsWith("/files/"))
        {
            return ProcessFilePost(request, args);
        }
        else
        {
            response.StatusCode = 404;
            return response;
        }
    }

    static HttpResponse ProcessFileGet(HttpRequest request, string[] args)
    {
        var response = new HttpResponse();

        try
        {
            if (args.Length < 1)
            {
                throw new ArgumentException("Directory argument not provided");
            }

            string fileName = request.Path.Substring(7);
            string fullPath = Path.Combine(args[0], fileName);
            
            if (!File.Exists(fullPath))
            {
                response.StatusCode = 404;
                return response;
            }

            string fileContent = File.ReadAllText(fullPath);
            response.StatusCode = 200;
            response.Body = fileContent;
            response.AddHeader("Content-Type", "application/octet-stream");
            response.AddHeader("Content-Length", fileContent.Length.ToString());
            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading file: {ex.Message}");
            response.StatusCode = 404;
            return response;
        }
    }

    static HttpResponse ProcessFilePost(HttpRequest request, string[] args)
    {
        var response = new HttpResponse();

        try
        {
            if (args.Length < 1)
            {
                throw new ArgumentException("Directory argument not provided");
            }

            string fileName = request.Path.Substring(7);
            string fullPath = Path.Combine(args[0], fileName);
            
            // Ensure directory exists
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(fullPath, request.Body);
            response.StatusCode = 201;
            response.Body = request.Body;
            response.AddHeader("Content-Type", "text/plain");
            response.AddHeader("Content-Length", request.Body.Length.ToString());
            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing file: {ex.Message}");
            response.StatusCode = 500;
            response.Body = "Internal Server Error";
            response.AddHeader("Content-Type", "text/plain");
            response.AddHeader("Content-Length", response.Body.Length.ToString());
            return response;
        }
    }
}

public class HttpRequest
{
    public HttpMethod Method { get; set; }
    public string Path { get; set; }
    public string Body { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

    public static HttpRequest Parse(string requestText)
    {
        var request = new HttpRequest();
        
        if (string.IsNullOrWhiteSpace(requestText))
        {
            throw new ArgumentException("Request text cannot be empty");
        }

        string[] sections = requestText.Split(new[] { "\r\n\r\n" }, StringSplitOptions.None);
        string headerSection = sections[0];
        request.Body = sections.Length > 1 ? sections[1] : "";

        string[] lines = headerSection.Split(new[] { "\r\n" }, StringSplitOptions.None);
        
        if (lines.Length == 0)
        {
            throw new ArgumentException("Invalid HTTP request format");
        }

        // Parse request line
        string[] requestLineParts = lines[0].Split(' ');
        if (requestLineParts.Length < 3)
        {
            throw new ArgumentException("Invalid HTTP request line");
        }

        if (Enum.TryParse<HttpMethod>(requestLineParts[0], true, out HttpMethod method))
        {
            request.Method = method;
        }
        else
        {
            throw new ArgumentException($"Unsupported HTTP method: {requestLineParts[0]}");
        }

        request.Path = requestLineParts[1];

        // Parse headers
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            int colonIndex = line.IndexOf(':');
            if (colonIndex > 0)
            {
                string key = line.Substring(0, colonIndex).Trim();
                string value = line.Substring(colonIndex + 1).Trim();
                request.Headers[key] = value;
            }
        }

        return request;
    }
}

public class HttpResponse
{
    public int StatusCode { get; set; } = 200;
    public string StatusMessage => HttpStatusMessages.GetMessage(StatusCode);
    public string Body { get; set; } = "";
    public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

    public byte[] ToByteArray()
    {
        var responseBuilder = new StringBuilder();

        responseBuilder.Append($"HTTP/1.1 {StatusCode} {StatusMessage}\r\n");
        
        foreach (var header in Headers)
        {
            responseBuilder.Append($"{header.Key}: {header.Value}\r\n");
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
        Headers[key] = value;
    }
}

public enum HttpMethod
{
    GET,
    POST,
    PUT,
    DELETE,
    HEAD,
    OPTIONS,
    PATCH
}

public static class HttpStatusMessages
{
    public static string GetMessage(int statusCode)
    {
        return statusCode switch
        {
            200 => "OK",
            201 => "Created",
            400 => "Bad Request",
            404 => "Not Found",
            405 => "Method Not Allowed",
            500 => "Internal Server Error",
            _ => "Unknown Status"
        };
    }
}