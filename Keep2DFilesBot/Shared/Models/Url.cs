using Keep2DFilesBot.Shared.Results;

namespace Keep2DFilesBot.Shared.Models;

public readonly record struct Url
{
    private readonly Uri _value;

    private Url(Uri value) => _value = value;

    public static Result<Url> Create(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return Result<Url>.Failure("Адрес не может быть пустым");

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return Result<Url>.Failure("Некорректный формат адреса");

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return Result<Url>.Failure("Поддерживаются только HTTP/HTTPS");

        return Result<Url>.Success(new Url(uri));
    }

    public static implicit operator Uri(Url url) => url._value;

    public override string ToString() => _value.ToString();
}
