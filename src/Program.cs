using System.Net;
using System.Net.Sockets;
using System.Text;


public class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Logs from your program will appear here!");

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

            var splitted = requestText.Split("\r\n");
            string[] sections = requestText.Split("\r\n\r\n");
            string headers = sections[0];
            string body = sections.Length > 1 ? sections[1] : "";
            var url = splitted[0].Split(" ")[1];
            Console.WriteLine($"Url -> {url}");

            var method = splitted[0].Split(" ")[0];
            var route = splitted[0].Split(" ")[1];

            byte[] responseBytes;

            if (method == "POST" && route.StartsWith("/files/"))
            {
                try
                {
                    if (args.Length < 1)
                    {
                        throw new ArgumentException("Directory argument not provided");
                    }

                    string fileName = route.Substring(7, route.Length - 7);
                    string fullPath = Path.Combine(args[0], fileName);
                    using StreamWriter writer = new StreamWriter(fullPath);
                    writer.Write(body);
                    string response = $"HTTP/1.1 201 Created\r\nContent-Type: text/plain\r\nContent-Length: {body.Length}\r\n\r\n{body}";
                    responseBytes = Encoding.UTF8.GetBytes(response);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    string response = "HTTP/1.1 404 Not Found\r\n\r\n";
                    responseBytes = Encoding.UTF8.GetBytes(response);
                }
            }
            else if (route == "/")
            {
                string response = "HTTP/1.1 200 OK\r\n\r\n";
                responseBytes = Encoding.UTF8.GetBytes(response);
            }
            else if (route.StartsWith("/echo/"))
            {
                string message = route.Substring(6, route.Length - 6);
                string response = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {message.Length}\r\n\r\n" + message;
                responseBytes = Encoding.UTF8.GetBytes(response);
            }
            else if (route.StartsWith("/user-agent"))
            {
                string userAgent = "";
                foreach (string line in splitted)
                {
                    if (line.StartsWith("User-Agent:", StringComparison.OrdinalIgnoreCase))
                    {
                        userAgent = line.Substring(12).Trim();
                        break;
                    }
                }
                string response = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {userAgent.Length}\r\n\r\n" + userAgent;
                responseBytes = Encoding.UTF8.GetBytes(response);
            }
            else if (route.StartsWith("/files/"))
            {
                try
                {
                    if (args.Length < 1)
                    {
                        throw new ArgumentException("Directory argument not provided");
                    }

                    string fileName = route.Substring(7, route.Length - 7);
                    string fullPath = Path.Combine(args[0], fileName);
                    using StreamReader reader = new StreamReader(fullPath);
                    string fileContent = reader.ReadToEnd();
                    string response = $"HTTP/1.1 200 OK\r\nContent-Type: application/octet-stream\r\nContent-Length: {fileContent.Length}\r\n\r\n{fileContent}";
                    responseBytes = Encoding.UTF8.GetBytes(response);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    string response = "HTTP/1.1 404 Not Found\r\n\r\n";
                    responseBytes = Encoding.UTF8.GetBytes(response);
                }
            }
            else
            {
                string response = "HTTP/1.1 404 Not Found\r\n\r\n";
                responseBytes = Encoding.UTF8.GetBytes(response);
            }

            clientSocket.Send(responseBytes);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        clientSocket.Close();
    }
}
