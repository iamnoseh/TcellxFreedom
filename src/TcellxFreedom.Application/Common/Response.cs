using System.Net;

namespace TcellxFreedom.Application.Common;

public sealed class Response<T>
{
    public T? Data { get; set; }
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public HttpStatusCode StatusCode { get; set; }

    public Response(T data)
    {
        Data = data;
        IsSuccess = true;
        StatusCode = HttpStatusCode.OK;
    }

    public Response(HttpStatusCode statusCode, string message)
    {
        IsSuccess = false;
        StatusCode = statusCode;
        Message = message;
    }
}
