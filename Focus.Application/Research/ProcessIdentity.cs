namespace Focus.Application.Research;

public static class ProcessIdentity
{
    public static bool TryNormalize(string? input, out string? normalized)
    {
        normalized = string.IsNullOrWhiteSpace(input) ? null : input.Trim().ToLowerInvariant();
        return normalized is null || (normalized.Length <= 200 &&
            char.IsAsciiLetterOrDigit(normalized[0]) &&
            normalized.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-'));
    }
}
