using System.Globalization;

namespace LombdaAgentMAUI.Converters
{
    /// <summary>
    /// Converts boolean to Color for message background styling
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isUser && isUser)
            {
                // User messages - blue background
                return Color.FromArgb("#512BD4");
            }
            else
            {
                // Agent messages - light gray background
                return Color.FromArgb("#E0E0E0");
            }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts boolean to text Color for message text styling
    /// </summary>
    public class BoolToTextColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isUser && isUser)
            {
                // User messages - white text (on blue background)
                return Colors.White;
            }
            else
            {
                // Agent messages - black text (on light background)
                return Colors.Black;
            }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts boolean to LayoutOptions for message alignment
    /// </summary>
    public class BoolToLayoutOptionsConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isUser && isUser)
            {
                // User messages - align to the right
                return LayoutOptions.End;
            }
            else
            {
                // Agent messages - align to the left
                return LayoutOptions.Start;
            }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts boolean IsMarkdown property to visibility for markdown/plain text rendering
    /// </summary>
    public class MarkdownVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isMarkdown)
            {
                // Parameter can be "markdown" or "plain" to indicate which view to show
                var viewType = parameter?.ToString()?.ToLower();
                
                if (viewType == "markdown")
                {
                    return isMarkdown;
                }
                else if (viewType == "plain")
                {
                    return !isMarkdown;
                }
            }
            
            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}