using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace OpenCVVideoRedactor.PopUpWindows
{
    public class SelectionBox
    {
        public static object? ShowDialog(string title, string text, Dictionary<string, object> list, int defaultElement = 0)
        {
            string result = list.Keys.ElementAt(defaultElement);
            App.Current.Dispatcher.Invoke(() => {
                Window Box = new Window();
                FontFamily font = new FontFamily("Avenir");
                int FontSize = 14;
                StackPanel stackPanel = new StackPanel();
                Brush BoxBackgroundColor = Brushes.WhiteSmoke;
                Brush InputBackgroundColor = Brushes.Ivory;
                ComboBox input = new ComboBox();
                Button okButton = new Button();
                Button cancelButton = new Button();
                Box.Height = 200;
                Box.Width = 450;
                Box.Background = BoxBackgroundColor;
                Box.Title = title;
                Box.Content = stackPanel;
                Box.WindowStyle = WindowStyle.SingleBorderWindow;
                Box.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                TextBlock content = new TextBlock();
                content.TextWrapping = TextWrapping.Wrap;
                content.Background = null;
                content.HorizontalAlignment = HorizontalAlignment.Center;
                content.Text = text;
                content.Margin = new Thickness(10);
                content.FontFamily = font;
                content.FontSize = FontSize;
                stackPanel.Children.Add(content);

                input.Background = InputBackgroundColor;
                input.FontFamily = font;
                input.FontSize = FontSize;
                input.HorizontalAlignment = HorizontalAlignment.Center;
                input.Text = result;
                input.MinWidth = 200;
                input.Margin = new Thickness(10);
                input.ItemsSource = list.Keys;
                input.SelectedIndex = defaultElement;
                input.SelectionChanged += (e, args) =>
                {
                    input.Text = (string)input.SelectedValue;
                    var val = input.SelectedValue;
                };
                input.KeyDown += (e, args) => {
                    switch (args.Key)
                    {
                        case Key.Enter:
                            {
                                Box.Close();
                            }
                            break;
                        case Key.Escape:
                            {
                                input.Text = "";
                                Box.Close();
                            }
                            break;
                    }
                };
                stackPanel.Children.Add(input);

                okButton.Width = 70;
                okButton.Height = 30;
                okButton.Click += (e, args) => {
                    Box.Close();
                };
                okButton.Margin = new Thickness(20);
                okButton.Content = "Ok";

                cancelButton.Width = 70;
                cancelButton.Height = 30;
                cancelButton.Click += (e, args) =>
                {
                    input.Text = "";
                    Box.Close();
                };
                cancelButton.Margin = new Thickness(20);
                cancelButton.Content = "Отмена";

                WrapPanel gboxContent = new WrapPanel();
                gboxContent.HorizontalAlignment = HorizontalAlignment.Center;
                gboxContent.VerticalAlignment = VerticalAlignment.Bottom;

                stackPanel.Children.Add(gboxContent);
                gboxContent.Children.Add(okButton);
                gboxContent.Children.Add(cancelButton);

                input.Focus();
                Box.ShowDialog();
                result = input.Text;
            });
            if (result == "" || result == null) return null;
            return list[result];
        }
    }
}
