using System;
using System.Collections.Generic;

namespace Darwin.Shared.Results
{
    /// <summary>
    /// Lightweight functional-style result wrapper representing either success (with optional value)
    /// or failure (with error messages). Keeps Application handlers consistent and testable.
    /// </summary>
    public class Result
    {
        public bool Succeeded { get; }
        public IReadOnlyList<string> Errors { get; }

        protected Result(bool ok, IReadOnlyList<string>? errors)
        {
            Succeeded = ok;
            Errors = errors ?? Array.Empty<string>();
        }

        public static Result Ok() => new Result(true, Array.Empty<string>());
        public static Result Fail(params string[] errors) => new Result(false, errors ?? Array.Empty<string>());
        public static Result Fail(IEnumerable<string> errors) => new Result(false, new List<string>(errors));
    }

    /// <summary>
    /// Result with a payload. On failure, <see cref="Value"/> is default.
    /// </summary>
    public sealed class Result<T> : Result
    {
        public T? Value { get; }

        private Result(bool ok, T? value, IReadOnlyList<string>? errors) : base(ok, errors)
        {
            Value = value;
        }

        public static Result<T> Ok(T value) => new Result<T>(true, value, Array.Empty<string>());
        public static new Result<T> Fail(params string[] errors) => new Result<T>(false, default, errors ?? Array.Empty<string>());
        public static Result<T> Fail(IEnumerable<string> errors) => new Result<T>(false, default, new List<string>(errors));
    }
}
