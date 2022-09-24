namespace Extensions.Toolkit;

public static class DateTimeExt
{
    public static string ToReadableString(this TimeSpan span)
    {
        var formatted = string.Format("{0}{1}{2}{3}",
            span.Duration().Days > 0 ? $"{span.Days:0} day{(span is {Days: 1} ? string.Empty : "s")}, " : string.Empty,
            span.Duration().Hours > 0 ? $"{span.Hours:0} hour{(span is {Hours: 1} ? string.Empty : "s")}, " : string.Empty,
            span.Duration().Minutes > 0 ? $"{span.Minutes:0} minute{(span is {Minutes: 1} ? string.Empty : "s")}, " : string.Empty,
            span.Duration().Seconds > 0 ? $"{span.Seconds:0} second{(span is {Seconds: 1} ? string.Empty : "s")}" : string.Empty);

        if (formatted.EndsWith(", ")) formatted = formatted[..^2];

        if (string.IsNullOrEmpty(formatted)) formatted = "0 seconds";

        return formatted;
    }
}