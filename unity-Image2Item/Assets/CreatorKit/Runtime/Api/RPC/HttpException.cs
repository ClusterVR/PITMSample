public class HttpException : System.Runtime.InteropServices.ExternalException
{
    int _httpCode;

    public HttpException(int httpCode, string message) : base(message)
    {
        _httpCode = httpCode;
    }

    public int GetHttpCode()
    {
        return _httpCode;
    }
}
