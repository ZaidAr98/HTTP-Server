using System.Net;
using System.Net.Sockets;
using src.Http;
using src.Middleware.Middlewares;
using CancellationToken = src.Http.CancellationToken;

public class Program
{

    private static void Main(string[] args)
    {
        TcpListener server = new TcpListener(IPAddress.Any, 4221);
        server.Start();
        Console.WriteLine("4221 Establish Connection");

        while (true)
        {
            Socket clientSocket = server.AcceptSocket();
            Console.WriteLine("Client Start");
            _ = Task.Run(() => HandleClient(clientSocket,args));
        }
    }

    private static async Task HandleClient(Socket clienSocket, string[] args)
{
    Console.WriteLine("Connection to the client has started.");
    try
    {
        if (clienSocket.Connected)
        {
            await ProcessRequest(clienSocket, args);
        }
    }
    finally
    {
        try
        {
            if (clienSocket.Connected)
            {
                clienSocket.Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error closing socket: {ex.Message}");
        }
    }
}

    private static async Task ProcessRequest(Socket clientSocket, string[] args)
    {
        try
        {
            var request = new HttpRequest(clientSocket);
            var response = new HttpResponse(); 
            var cancellationToken = new CancellationToken();
            _ = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(100);
                    bool disconnected = clientSocket.Poll(1000, SelectMode.SelectRead) && clientSocket.Available == 0;
                    if (disconnected)
                    {
                        cancellationToken.Cancel();
                        Console.WriteLine("Client disconnected");
                        break;
                    }
                }
            });

            var httpContext = new HttpContext(request, response, cancellationToken);
            var middlewareBuilder = new MiddlewareBuilder();
            middlewareBuilder.UseMiddleware<AuthenticationMiddleware>();
            var app = middlewareBuilder.Run(httpContext);
            await app(httpContext);
            
              }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            
        }
     }

    
}