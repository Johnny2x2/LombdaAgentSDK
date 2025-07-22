using Markdig;
using Microsoft.Maui.Controls;
using System.Text.RegularExpressions;

namespace LombdaAgentMAUI.Controls;

/// <summary>
/// A custom view that renders markdown content using styled labels and other controls
/// </summary>
public class MarkdownView : ContentView
{
    public static readonly BindableProperty MarkdownProperty = BindableProperty.Create(
        nameof(Markdown),
        typeof(string),
        typeof(MarkdownView),
        string.Empty,
        propertyChanged: OnMarkdownChanged);

    public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
        nameof(TextColor),
        typeof(Color),
        typeof(MarkdownView),
        Colors.Black);

    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(
        nameof(FontSize),
        typeof(double),
        typeof(MarkdownView),
        14.0);

    public string Markdown
    {
        get => (string)GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    private static void OnMarkdownChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MarkdownView markdownView)
        {
            markdownView.UpdateContent();
        }
    }

    private void UpdateContent()
    {
        if (string.IsNullOrWhiteSpace(Markdown))
        {
            Content = null;
            return;
        }

        try
        {
            // Parse markdown to HTML first to get structure
            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();

            var html = Markdig.Markdown.ToHtml(Markdown, pipeline);
            
            // Create a simplified renderer for MAUI
            var stackLayout = new StackLayout
            {
                Spacing = 5
            };

            // Split content by lines and process each
            var lines = Markdown.Split('\n', StringSplitOptions.None);
            foreach (var line in lines)
            {
                var processedLine = line.Trim();
                if (string.IsNullOrEmpty(processedLine))
                {
                    // Add small spacing for empty lines
                    stackLayout.Children.Add(new BoxView { HeightRequest = 5, BackgroundColor = Colors.Transparent });
                    continue;
                }

                var element = CreateElementFromLine(processedLine);
                if (element != null)
                {
                    stackLayout.Children.Add(element);
                }
            }

            Content = stackLayout;
        }
        catch (Exception)
        {
            // Fallback to plain text if markdown parsing fails
            Content = new Label
            {
                Text = Markdown,
                TextColor = TextColor,
                FontSize = FontSize,
                LineBreakMode = LineBreakMode.WordWrap
            };
        }
    }

    private View? CreateElementFromLine(string line)
    {
        // Handle different markdown elements
        
        // Headers
        if (line.StartsWith("# "))
        {
            return new Label
            {
                Text = line.Substring(2),
                TextColor = TextColor,
                FontSize = FontSize + 8,
                FontAttributes = FontAttributes.Bold,
                LineBreakMode = LineBreakMode.WordWrap
            };
        }
        else if (line.StartsWith("## "))
        {
            return new Label
            {
                Text = line.Substring(3),
                TextColor = TextColor,
                FontSize = FontSize + 6,
                FontAttributes = FontAttributes.Bold,
                LineBreakMode = LineBreakMode.WordWrap
            };
        }
        else if (line.StartsWith("### "))
        {
            return new Label
            {
                Text = line.Substring(4),
                TextColor = TextColor,
                FontSize = FontSize + 4,
                FontAttributes = FontAttributes.Bold,
                LineBreakMode = LineBreakMode.WordWrap
            };
        }
        
        // Code blocks
        if (line.StartsWith("```"))
        {
            return new Frame
            {
                BackgroundColor = Color.FromArgb("#F5F5F5"),
                Padding = new Thickness(10),
                CornerRadius = 5,
                Content = new Label
                {
                    Text = line.Replace("```", ""),
                    TextColor = Color.FromArgb("#333333"),
                    FontFamily = "Courier New",
                    FontSize = FontSize - 1,
                    LineBreakMode = LineBreakMode.WordWrap
                }
            };
        }

        // Inline code
        if (line.Contains("`"))
        {
            return CreateFormattedLabel(line);
        }

        // Bullet points
        if (line.StartsWith("- ") || line.StartsWith("* "))
        {
            return new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Children =
                {
                    new Label
                    {
                        Text = "•",
                        TextColor = TextColor,
                        FontSize = FontSize,
                        VerticalOptions = LayoutOptions.Start,
                        Margin = new Thickness(0, 0, 5, 0)
                    },
                    new Label
                    {
                        Text = line.Substring(2),
                        TextColor = TextColor,
                        FontSize = FontSize,
                        LineBreakMode = LineBreakMode.WordWrap,
                        HorizontalOptions = LayoutOptions.FillAndExpand
                    }
                }
            };
        }

        // Numbered lists
        var numberedMatch = Regex.Match(line, @"^(\d+)\.\s(.*)");
        if (numberedMatch.Success)
        {
            return new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Children =
                {
                    new Label
                    {
                        Text = $"{numberedMatch.Groups[1].Value}.",
                        TextColor = TextColor,
                        FontSize = FontSize,
                        VerticalOptions = LayoutOptions.Start,
                        Margin = new Thickness(0, 0, 5, 0)
                    },
                    new Label
                    {
                        Text = numberedMatch.Groups[2].Value,
                        TextColor = TextColor,
                        FontSize = FontSize,
                        LineBreakMode = LineBreakMode.WordWrap,
                        HorizontalOptions = LayoutOptions.FillAndExpand
                    }
                }
            };
        }

        // Bold and italic formatting
        if (line.Contains("**") || line.Contains("*") || line.Contains("`"))
        {
            return CreateFormattedLabel(line);
        }

        // Regular paragraph
        return new Label
        {
            Text = line,
            TextColor = TextColor,
            FontSize = FontSize,
            LineBreakMode = LineBreakMode.WordWrap
        };
    }

    private Label CreateFormattedLabel(string text)
    {
        // For now, create a simple label that handles basic formatting
        // This is a simplified approach - a full implementation would use FormattedString
        var processedText = text;
        
        // Remove markdown formatting for display (simplified)
        processedText = Regex.Replace(processedText, @"\*\*(.*?)\*\*", "$1"); // Bold
        processedText = Regex.Replace(processedText, @"\*(.*?)\*", "$1"); // Italic
        processedText = Regex.Replace(processedText, @"`(.*?)`", "$1"); // Inline code

        return new Label
        {
            Text = processedText,
            TextColor = TextColor,
            FontSize = FontSize,
            LineBreakMode = LineBreakMode.WordWrap
        };
    }
}