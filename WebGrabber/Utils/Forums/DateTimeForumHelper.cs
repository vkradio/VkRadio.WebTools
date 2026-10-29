using System;
using System.Linq;

namespace VkRadio.WebTools.WebGrabber.Utils.Forums;

/*
 * Based on formatting on the PHPBB OM website. It has date in one of the following formats:
 * 
 * Сегодня в 12:55
 * Вчера в 12:55
 * Вс 6 Ноя 2016 - 12:55
 * 
 * The last example corresponds to a formatting PHP function date(): D j M Y - H:i
 * Details: http://php.net/manual/ru/function.date.php
 * That format is used for parsing sp-orenmama.ru that based on PHPBB + spcript
 */
public static class DateTimeForumHelper
{
    static string[] s_months = { "Янв", "Фев", "Мар", "Апр", "Май", "Июн", "Июл", "Авг", "Сен", "Окт", "Ноя", "Дек" };

    public static void ParseHoursAndMinutes(string value, out int out_hours, out int out_minutes)
    {
        var parts = value.Split(new char[] { ':' });
        out_hours = int.Parse(parts[0]);
        out_minutes = int.Parse(parts[1]);
    }

    /// <summary>
    ///  Parse month number (starting from 1) by its 3-char Russian short name
    /// </summary>
    /// <param name="stringNameRus">Russian 3-char short name of month</param>
    /// <returns>Month number from 1 to 12</returns>
    public static int ParseMonth(string stringNameRus)
    {
        var monthNumber = s_months
            .Select((x, i) => new { Number = i + 1, IsMatch = x.Equals(stringNameRus, StringComparison.CurrentCultureIgnoreCase) })
            .Where(x => x.IsMatch)
            .Select(x => x.Number)
            .FirstOrDefault();

        if (monthNumber == 0)
        {
            throw new ArgumentException($"Invalid string value: {stringNameRus ?? "<NULL>"}; expected one of: {string.Join(", ", s_months)}");
        }

        return monthNumber;
    }

    public static DateTime Parse(string dateTimeString, DateTime currentDateTime)
    {
        var result = currentDateTime.Date;

        if (dateTimeString.Contains("Сегодня") || dateTimeString.Contains("Вчера"))
        {
            if (dateTimeString.Contains("Вчера"))
            {
                result = result.AddDays(-1);
            }

            var parts = dateTimeString.Split(new char[] { ' ' });

            int hours, minutes;

            ParseHoursAndMinutes(parts[parts.Length - 1], out hours, out minutes);

            result = result.AddHours(hours);
            result = result.AddMinutes(minutes);
        }
        else
        {
            var useDateTimeString = dateTimeString;

            if (useDateTimeString.IndexOf("в ") == 0)
            {
                useDateTimeString = useDateTimeString.Substring(2);
            }

            var parts = useDateTimeString.Split(new char[] { ' ' });

            int
                day = int.Parse(parts[1]),
                year = int.Parse(parts[3]),
                month = ParseMonth(parts[2]);

            int hours, minutes;

            ParseHoursAndMinutes(parts[5], out hours, out minutes);

            result = new DateTime(year, month, day, hours, minutes, 0);
        }

        return result;
    }

    /// <summary>
    ///  Parse date from a forum format DD Mon YYYY (used in email grabber)
    /// </summary>
    /// <param name="dateAsString">Date string or null</param>
    /// <returns>Parsed date or null</returns>
    public static DateTime? ParseDate(string? dateAsString)
    {
        if (string.IsNullOrWhiteSpace(dateAsString))
        {
            return null;
        }

        var useDateAsString = dateAsString!.Trim();

        if (useDateAsString == string.Empty)
        {
            return null;
        }

        var parts = useDateAsString.Split(new char[] { ' ' });
        var day = int.Parse(parts[0]);
        var year = int.Parse(parts[2]);
        var month = parts[1];

        var monthIndex = -1;

        for (var i = 0; i < s_months.Length; i++)
        {
            if (month == s_months[i])
            {
                monthIndex = i;
                break;
            }
        }

        return new DateTime(year, monthIndex + 1, day);
    }
}
