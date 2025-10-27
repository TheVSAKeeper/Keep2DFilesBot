using System.Globalization;

namespace Keep2DFilesBot.Shared.Models;

public readonly record struct UserId
{
    private readonly long _value;

    public UserId(long value)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Значение должно быть положительным");

        _value = value;
    }

    public static implicit operator long(UserId userId) => userId._value;

    public static explicit operator UserId(long value) => new(value);

    public override string ToString() => _value.ToString(CultureInfo.InvariantCulture);
}
