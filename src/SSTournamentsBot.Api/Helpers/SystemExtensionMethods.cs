using System;

namespace SSTournamentsBot
{
    public static class SystemExtensionMethods
    {
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
