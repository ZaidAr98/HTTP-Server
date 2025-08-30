using System.IO.Compression;
using System.Text;
using src.Middleware.Middlewares;

namespace src.Http
{
    public class MiddlewareBuilder
    {
        private readonly List<Func<HttpContext, RequestDelegate, Task>> _middlewares = new();

        public MiddlewareBuilder Use(Func<HttpContext, RequestDelegate, Task> middleware)
        {
            _middlewares.Add(middleware);
            return this;
        }

        public MiddlewareBuilder UseMiddleware<T>() where T : IMiddleware, new()
        {
            return Use(async (context, next) =>
            {
                var middleware = new T();
                await middleware.InvokeAsync(context, next);
            });
        }

        public RequestDelegate Run(HttpContext httpContext)
        {
            RequestDelegate app = FinalHandler;

            foreach (var middleware in _middlewares.AsEnumerable().Reverse())
            {
                var next = app;
                app = async (context) => await middleware(context, next);
            }

            return app;
        }

        private async Task FinalHandler(HttpContext httpContext)
        {
            byte[] responseBytes;

            #region Handlers
            if (httpContext.Request.Path == "/")
            {
                httpContext.Response.StatusCode = 200;
            }
            else if(httpContext.Request.Path == "/long-running-job")
            {
                var cancellationToken = httpContext.RequestAborted;
                for (int i = 0; i < 1000; i++)
                {
                    if(cancellationToken.IsCancellationRequested)
                    {
                        Console.WriteLine("Finished!");
                        httpContext.Response.StatusCode = 499;
                        httpContext.Response.AddHeader("Content-Type", "text/plain");
                        httpContext.Response.Body = "Client Closed Request";
                    }

                    Console.WriteLine($"Job {i} is running...");
                    await Task.Delay(1000);
                }

                httpContext.Response.StatusCode = 200;
                httpContext.Response.AddHeader("Content-Type", "text/plain");
                httpContext.Response.Body = "Success!";
            }
            else if (httpContext.Request.Path.StartsWith("/echo/"))
            {
                string message = httpContext.Request.Path[6..];
                httpContext.Response.StatusCode = 200;
                httpContext.Response.AddHeader("Content-Type", "text/plain");
                httpContext.Response.AddHeader("Content-Length", message.Length.ToString());
                httpContext.Response.Body = message;
            }
            else if (httpContext.Request.Path.StartsWith("/user-agent"))
            {
                httpContext.Response.StatusCode = 200;
                string userAgent = httpContext.Request.Headers["User-Agent"];
                httpContext.Response.AddHeader("Content-Type", "text/plain");
                httpContext.Response.AddHeader("Content-Length", userAgent.Length.ToString());
                httpContext.Response.Body = userAgent;
            }
            else if (httpContext.Request.Path.StartsWith("/files/"))
            {
                try
                {
                    string fileName = httpContext.Request.Path[7..];
                    string fullPath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
                    
                    if (httpContext.Request.Method.ToString() == "POST")
                    {
                        using StreamWriter writer = new StreamWriter(fullPath);
                        writer.Write(httpContext.Request.Body);
                        httpContext.Response.StatusCode = 201;
                    }
                    else
                    {
                        using StreamReader reader = new StreamReader(fullPath);
                        string fileContent = reader.ReadToEnd();
                        httpContext.Response.StatusCode = 200;
                        httpContext.Response.AddHeader("Content-Type", "application/octet-stream");
                        httpContext.Response.AddHeader("Content-Length", fileContent.Length.ToString());
                        httpContext.Response.Body = fileContent;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    httpContext.Response.StatusCode = 404;
                }
            }
            else
            {
                httpContext.Response.StatusCode = 404;
            }
            
            if (httpContext.Request.Headers.ContainsKey("Connection"))
            {
                var connection = httpContext.Request.Headers["Connection"].Trim();
                if (connection == "close")
                {
                    httpContext.Response.AddHeader("Connection", "close");
                }
            }
            
            if (httpContext.Request.Headers.ContainsKey("Accept-Encoding"))
            {
                var encodings = httpContext.Request.Headers["Accept-Encoding"].Split(",").Select(x => x.Trim()).ToList();
                if (encodings.Contains("gzip"))
                {
                    httpContext.Response.AddHeader("Content-Encoding", "gzip");
                    using (var compressedStream = new MemoryStream())
                    {
                        using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Compress))
                        {
                            byte[] bodyBytes = Encoding.UTF8.GetBytes(httpContext.Response.Body);
                            gzipStream.Write(bodyBytes, 0, bodyBytes.Length);
                        }
                        var compressedBytes = compressedStream.ToArray();
                        httpContext.Response.Body = null;
                        httpContext.Response.Headers["Content-Length"] = compressedBytes.Length.ToString();
                        responseBytes = httpContext.Response.ToByteArray();
                        httpContext.Request.ClientSocket.Send(responseBytes);
                        httpContext.Request.ClientSocket.Send(compressedBytes);

                        if (httpContext.Request.Headers.ContainsKey("Connection"))
                        {
                            var connection = httpContext.Request.Headers["Connection"].Trim();
                            if (connection == "close")
                            {
                                httpContext.Request.ClientSocket.Close();
                                return;
                            }
                        }
                    }
                }
            }
            #endregion

            responseBytes = httpContext.Response.ToByteArray();
            httpContext.Request.ClientSocket.Send(responseBytes);

            if (httpContext.Request.Headers.ContainsKey("Connection"))
            {
                var connection = httpContext.Request.Headers["Connection"].Trim();
                if (connection == "close")
                {
                    httpContext.Request.ClientSocket.Close();
                    return;
                }
            }
        }
    }
}