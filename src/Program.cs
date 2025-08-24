using System;
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
        Console.WriteLine("Listening on port 4221");

        while (true)
        {
            Socket clientSocket = server.AcceptSocket();
            Console.WriteLine("Client connected");
            Thread clientThread = new Thread(() => HandleClient(clientSocket));
            clientThread.Start();
        }
        server.Stop();
    }

    static void HandleClient(Socket clientSocket)
    {
        Console.WriteLine("Handling client connection");
        byte[] buffer = new byte[4096];

        try
        {
            var received = clientSocket.Receive(buffer);
            string requestText = Encoding.UTF8.GetString(buffer, 0, received);
            Console.WriteLine($"Request:\n{requestText}");

            var splitted = requestText.Split("\r\n");
            var url = splitted[1].Split(" ")[1];
            Console.WriteLine($"Url -> {url}");

            var route = splitted[0].Split(" ")[1];

            byte[] responseBytes;
            if (route == "/")
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
                string userAgent = splitted[2].Split(": ")[1];
                string response = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {userAgent.Length}\r\n\r\n" + userAgent;
                responseBytes = Encoding.UTF8.GetBytes(response);
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


