using System.Diagnostics.CodeAnalysis;

namespace WebUI.Endpoints.Resources;

public record struct Field<T>(T? Value, Exception? Error)
{
    [MemberNotNullWhen(true, nameof(Value)), MemberNotNullWhen(false, nameof(Error))] public readonly bool HasValue => Error == null;
    public static implicit operator T(Field<T> input) => input.HasValue ? input.Value ?? throw new InvalidOperationException() : throw new InvalidOperationException();
    public static implicit operator Field<T>(T value) => new(value, null);
}