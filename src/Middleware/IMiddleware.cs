


using src.Http;

namespace src.Middleware.Middlewares
{
    public interface IMiddleware
    {
        Task InvokeAsync(HttpContext context, RequestDelegate next);        
    }
}