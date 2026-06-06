namespace FigurasQE_WebClient.Models;

public static class SessionDateHelper
{
    private static readonly TimeZoneInfo MexicoTimeZone = FindMexicoTimeZone();

    public static List<SessionDto> NormalizeWebSessionDates(IEnumerable<SessionDto> sessions)
    {
        return sessions.Select(NormalizeWebSessionDates).ToList();
    }

    public static SessionDto NormalizeWebSessionDates(SessionDto session)
    {
        if (!string.Equals(session.Device, "web", StringComparison.OrdinalIgnoreCase))
        {
            return session;
        }

        session.BeginningDate = NormalizeUtcLikeTimestamp(session.BeginningDate);
        session.EndDate = NormalizeUtcLikeTimestamp(session.EndDate);
        return session;
    }

    public static DateTime MexicoNow()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, MexicoTimeZone);
    }

    private static DateTime? NormalizeUtcLikeTimestamp(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        var utcValue = value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
            _ => value.Value.ToUniversalTime()
        };

        return DateTime.SpecifyKind(
            TimeZoneInfo.ConvertTimeFromUtc(utcValue, MexicoTimeZone),
            DateTimeKind.Unspecified);
    }

    private static TimeZoneInfo FindMexicoTimeZone()
    {
        foreach (var timeZoneId in new[] { "America/Mexico_City", "Central Standard Time (Mexico)" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Local;
    }
}
