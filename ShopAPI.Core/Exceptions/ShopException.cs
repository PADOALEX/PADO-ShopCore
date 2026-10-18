namespace ShopAPI.Core.Exceptions;

public sealed class ShopException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public static ShopException BadRequest(string message) => new(400, message);
    public static ShopException NotFound(string message) => new(404, message);
    public static ShopException Conflict(string message) => new(409, message);
}
