
namespace src.Http
{

    public class HttpContext
    {
        public HttpRequest Request { get; set; }
        public HttpResponse Response { get; set; }
        public CancellationToken RequestAborted { get; set; }


        public HttpContext(HttpRequest request, HttpResponse response, CancellationToken cancellationToken)
        {
            Request = request;
            Response = response;
            RequestAborted = cancellationToken;
        }


        public HttpContext(HttpRequest request, HttpResponse response)
        {
            Request = request;
            Response = response;
        }


        public void SetResponse(HttpResponse response)
        {
            Response = response;
        }

        public void SetRequest(HttpRequest request)
        {
            Request = request;
            
        }


    }


}