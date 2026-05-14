using System.Text.RegularExpressions;
using System.Windows;
using WpfControl = System.Windows.Controls.Control;
using WpfRichTextBox = System.Windows.Controls.RichTextBox;
using System.Windows.Documents;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;

namespace HRB.Platform.Client.WPF.PaymentAppModule.Core.Controls
{
    /// <summary>
    /// Markdown查看器控件
    /// 支持基本的Markdown语法渲染
    /// </summary>
    public class MarkdownViewer : WpfControl
    {
        private WpfRichTextBox? _richTextBox;

        static MarkdownViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MarkdownViewer), new FrameworkPropertyMetadata(typeof(MarkdownViewer)));
        }

        public static readonly DependencyProperty MarkdownProperty =
            DependencyProperty.Register(
                nameof(Markdown),
                typeof(string),
                typeof(MarkdownViewer),
                new PropertyMetadata(string.Empty, OnMarkdownChanged));

        public string Markdown
        {
            get => (string)GetValue(MarkdownProperty);
            set => SetValue(MarkdownProperty, value);
        }

        private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MarkdownViewer viewer)
            {
                viewer.RenderMarkdown();
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _richTextBox = GetTemplateChild("PART_RichTextBox") as WpfRichTextBox;
            RenderMarkdown();
        }

        private void RenderMarkdown()
        {
            if (_richTextBox == null || string.IsNullOrEmpty(Markdown))
                return;

            var document = new FlowDocument
            {
                FontFamily = new FontFamily("Microsoft YaHei, Segoe UI"),
                FontSize = 14,
                LineHeight = 22,
                PagePadding = new Thickness(20)
            };

            var lines = Markdown.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            bool inCodeBlock = false;
            Paragraph currentParagraph = null;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // 代码块
                if (line.StartsWith("```"))
                {
                    inCodeBlock = !inCodeBlock;
                    if (!inCodeBlock && currentParagraph != null)
                    {
                        document.Blocks.Add(currentParagraph);
                        currentParagraph = null;
                    }
                    continue;
                }

                if (inCodeBlock)
                {
                    if (currentParagraph == null)
                    {
                        currentParagraph = new Paragraph
                        {
                            Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                            Padding = new Thickness(10),
                            FontFamily = new FontFamily("Consolas, Courier New"),
                            FontSize = 13
                        };
                    }
                    currentParagraph.Inlines.Add(new Run(line + "\n"));
                    continue;
                }

                // 空行
                if (string.IsNullOrWhiteSpace(line))
                {
                    if (currentParagraph != null)
                    {
                        document.Blocks.Add(currentParagraph);
                        currentParagraph = null;
                    }
                    continue;
                }

                // 标题
                if (line.StartsWith("# "))
                {
                    var heading = new Paragraph(new Run(line.Substring(2)))
                    {
                        FontSize = 24,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                        Margin = new Thickness(0, 20, 0, 10)
                    };
                    document.Blocks.Add(heading);
                }
                else if (line.StartsWith("## "))
                {
                    var heading = new Paragraph(new Run(line.Substring(3)))
                    {
                        FontSize = 20,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                        Margin = new Thickness(0, 16, 0, 8)
                    };
                    document.Blocks.Add(heading);
                }
                else if (line.StartsWith("### "))
                {
                    var heading = new Paragraph(new Run(line.Substring(4)))
                    {
                        FontSize = 18,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                        Margin = new Thickness(0, 14, 0, 7)
                    };
                    document.Blocks.Add(heading);
                }
                else if (line.StartsWith("#### "))
                {
                    var heading = new Paragraph(new Run(line.Substring(5)))
                    {
                        FontSize = 16,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                        Margin = new Thickness(0, 12, 0, 6)
                    };
                    document.Blocks.Add(heading);
                }
                // 分隔线
                else if (line.Trim() == "---")
                {
                    var separator = new Paragraph
                    {
                        BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
                        BorderThickness = new Thickness(0, 1, 0, 0),
                        Margin = new Thickness(0, 10, 0, 10),
                        Padding = new Thickness(0)
                    };
                    document.Blocks.Add(separator);
                }
                // 无序列表
                else if (line.TrimStart().StartsWith("- ") || line.TrimStart().StartsWith("* "))
                {
                    var listItem = line.TrimStart().Substring(2);
                    var paragraph = new Paragraph();
                    paragraph.Inlines.Add(new Run("• ") { Foreground = new SolidColorBrush(Color.FromRgb(0, 120, 215)) });
                    ParseInlineMarkdown(paragraph, listItem);
                    paragraph.Margin = new Thickness(20, 2, 0, 2);
                    document.Blocks.Add(paragraph);
                }
                // 有序列表
                else if (Regex.IsMatch(line.TrimStart(), @"^\d+\.\s"))
                {
                    var match = Regex.Match(line.TrimStart(), @"^(\d+)\.\s(.*)");
                    if (match.Success)
                    {
                        var number = match.Groups[1].Value;
                        var content = match.Groups[2].Value;
                        var paragraph = new Paragraph();
                        paragraph.Inlines.Add(new Run($"{number}. ") { Foreground = new SolidColorBrush(Color.FromRgb(0, 120, 215)) });
                        ParseInlineMarkdown(paragraph, content);
                        paragraph.Margin = new Thickness(20, 2, 0, 2);
                        document.Blocks.Add(paragraph);
                    }
                }
                // 引用块
                else if (line.StartsWith("> "))
                {
                    var quote = new Paragraph(new Run(line.Substring(2)))
                    {
                        Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(0, 120, 215)),
                        BorderThickness = new Thickness(4, 0, 0, 0),
                        Padding = new Thickness(10),
                        Margin = new Thickness(0, 5, 0, 5),
                        Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102))
                    };
                    document.Blocks.Add(quote);
                }
                // 普通段落
                else
                {
                    var paragraph = new Paragraph();
                    ParseInlineMarkdown(paragraph, line);
                    paragraph.Margin = new Thickness(0, 5, 0, 5);
                    document.Blocks.Add(paragraph);
                }
            }

            _richTextBox.Document = document;
        }

        private void ParseInlineMarkdown(Paragraph paragraph, string text)
        {
            // 粗体 **text**
            text = Regex.Replace(text, @"\*\*(.+?)\*\*", match =>
            {
                paragraph.Inlines.Add(new Run(match.Groups[1].Value) { FontWeight = FontWeights.Bold });
                return "\u0001"; // 占位符
            });

            // 斜体 *text*
            text = Regex.Replace(text, @"\*(.+?)\*", match =>
            {
                paragraph.Inlines.Add(new Run(match.Groups[1].Value) { FontStyle = FontStyles.Italic });
                return "\u0001";
            });

            // 行内代码 `code`
            text = Regex.Replace(text, @"`(.+?)`", match =>
            {
                paragraph.Inlines.Add(new Run(match.Groups[1].Value)
                {
                    Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 13
                });
                return "\u0001";
            });

            // 添加剩余文本
            var parts = text.Split('\u0001');
            int inlineIndex = 0;
            foreach (var part in parts)
            {
                if (!string.IsNullOrEmpty(part))
                {
                    paragraph.Inlines.Add(new Run(part));
                }
                inlineIndex++;
            }
        }
    }
}
