using System;

namespace SSTournamentsBot
{
    public static class SystemExtensionMethods
    {
        public static string FormatWith(this string self, object arg)
        {
            return string.Format(self, arg);
        }

        public static string FormatWith(this string self, object arg, object arg2)
        {
            return string.Format(self, arg, arg2);
        }

        public static string FormatWith(this string self, object arg, object arg2, object arg3)
        {
            return string.Format(self, arg, arg2, arg3);
        }

        public static string FormatWith(this string self, params object[] args)
        {
            return string.Format(self, args);
        }

        public static string PrettyShortDatePrint(this DateTime date)
        {
            return date.ToString("dd.MM.yyyy");
        }

        public static string PrettyShortDateAndTimePrint(this DateTime date)
        {
            return date.ToString("HH:mm (dd.MM.yyyy)");
        }

        public static string PrettyShortTimePrint(this DateTime date)
        {
            return date.ToString("HH:mm");
        }

        public static string PrettyPrint(this TimeSpan time)
        {
            if (time.TotalMinutes < 1.0)
                return $"1 минуту";
            if (time.TotalHours < 1.0)
                return PrintMinutes(time.Minutes);

            return $"{PrintHours(time.Hours)} {PrintMinutes(time.Minutes)}";
        }

        private static string PrintMinutes(int minutes)
        {
            switch (minutes)
            {
                case 0: return $"{minutes} минут";
                case 1: return $"{minutes} минуту";
                case int m when m > 1 && m < 5: return $"{minutes} минуты";
                case int m when m > 4 && m < 21: return $"{minutes} минут";
                case 21: return $"{minutes} минуту";
                case int m when m > 21 && m < 25: return $"{minutes} минуты";
                case int m when m > 24 && m < 31: return $"{minutes} минут";
                case 31: return $"{minutes} минуту";
                case int m when m > 31 && m < 35: return $"{minutes} минуты";
                case int m when m > 34 && m < 41: return $"{minutes} минут";
                case 41: return $"{minutes} минуту";
                case int m when m > 41 && m < 45: return $"{minutes} минуты";
                case int m when m > 44 && m < 51: return $"{minutes} минут";
                case 51: return $"{minutes} минуту";
                case int m when m > 51 && m < 55: return $"{minutes} минуты";
                case int m when m > 54 && m < 61: return $"{minutes} минут";
                default: return $"{minutes} минут";
            }
        }

        private static string PrintHours(int hours)
        {
            switch (hours)
            {
                case 0: return $"{hours} часов";
                case 1: return $"{hours} час";
                case int m when m > 1 && m < 5: return $"{hours} часа";
                case 21: return $"{hours} час";
                case int m when m > 21 && m < 25: return $"{hours} часа";
                default: return $"{hours} часов";
            }
        }
    }
}
