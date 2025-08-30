

namespace src.Http
{
 public static class HttpStatusMessage
{
    public static string GetMessage(int statusCode)
    {
        return statusCode switch
        {
            200 => "OK",
            201 => "Created",
            400 => "Bad Request",
            404 => "Not Found",
            500 => "Internal Server Error",
            _ => "Unknown Status"
        };
    }
}
}