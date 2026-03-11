namespace Application.Core.Validators;

public static class CnpjValidator
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value.Where(char.IsDigit).ToArray());
    }

    public static bool IsValidBasic(string? value)
    {
        var normalized = Normalize(value);
        return normalized.Length == 14;
    }
}