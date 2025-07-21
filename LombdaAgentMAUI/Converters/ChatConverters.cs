using System.Globalization;

namespace LombdaAgentMAUI.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isUser)
            {
                return isUser 
                    ? Color.FromArgb("#512BD4") // User message color
                    : Color.FromArgb("#E0E0E0"); // Agent message color
            }
            return Color.FromArgb("#E0E0E0");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToTextColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isUser)
            {
                return isUser 
                    ? Colors.White // User message text color
                    : Colors.Black; // Agent message text color
            }
            return Colors.Black;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToLayoutOptionsConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isUser)
            {
                return isUser 
                    ? LayoutOptions.End // User messages align right
                    : LayoutOptions.Start; // Agent messages align left
            }
            return LayoutOptions.Start;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}