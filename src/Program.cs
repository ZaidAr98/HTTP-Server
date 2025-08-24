using System;
using System.Net;
using System.Net.Sockets;
using System.Text;


Console.WriteLine("Logs from your program will appear here!");

TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();
Console.WriteLine("Listening on port 4221");

Socket clientSocket = server.AcceptSocket();
Console.WriteLine("Client  Connected");


byte[] buffer = new byte[4096];
var received = clientSocket.Receive(buffer);


string requestText = Encoding.UTF8.GetString(buffer, 0, received);


Console.WriteLine($"Request:\n{requestText}");

var splitted = requestText.Split("\r\n");

var url = splitted[0].Split(" ")[1];

Console.WriteLine($"URL -> {url}");

byte[] responseBytes;
if (url == "/")
{
    string response = "HTTP/1.1 200 OK\r\n\r\n";
    responseBytes = Encoding.UTF8.GetBytes(response);
}
else
{
    string response = "HTTP/1.1 404 Not Found\r\n\r\n";
    responseBytes = Encoding.UTF8.GetBytes(response);
}

clientSocket.Send(responseBytes);
clientSocket.Close();
server.Stop();


