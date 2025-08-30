using src.Http;

namespace src.Middleware.Middlewares
{
    public class AuthenticationMiddleware : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var authHeader = context.Request.Headers.GetValueOrDefault("Authorization");
            if (authHeader != null && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length);
                context.Request.Headers["IsAuthenticated"] = "True"; // Foydalanuvchini contextga joylash
            }

            await next(context);
        }
    }
}