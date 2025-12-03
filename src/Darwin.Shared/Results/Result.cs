namespace Darwin.Shared.Results
{
    /// <summary>
    /// Lightweight functional-style result wrapper for use-case handlers.
    /// Use Ok/Fail factories to keep call-sites terse and consistent.
    /// </summary>
    public sealed class Result
    {
        public bool Succeeded { get; }
        public string? Error { get; }

        private Result(bool succeeded, string? error)
        {
            Succeeded = succeeded;
            Error = error;
        }

        public static Result Ok() => new Result(true, null);
        public static Result Fail(string error) => new Result(false, error);
    }

    /// <summary>
    /// Generic variant that carries a value on success.
    /// </summary>
    public sealed class Result<T>
    {
        public bool Succeeded { get; }
        public string? Error { get; }
        public T? Value { get; }

        private Result(bool succeeded, T? value, string? error)
        {
            Succeeded = succeeded;
            Value = value;
            Error = error;
        }

        public static Result<T> Ok(T value) => new Result<T>(true, value, null);
        public static Result<T> Fail(string error) => new Result<T>(false, default, error);
    }
}
