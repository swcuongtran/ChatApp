using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BuildingBlock.Exception
{
    public readonly record struct Result(bool IsSuccess, Error? Error = null)
    {
        public static Result Ok() => new(true, null);
        public static Result Fail(Error error) => new(false, error);
    }

    public readonly record struct Result<T>(bool IsSuccess, T? Value, Error? Error)
    {
        public static Result<T> Ok(T value) => new(true, value, null);
        public static Result<T> Fail(Error error) => new(false, default, error);
    }
}
