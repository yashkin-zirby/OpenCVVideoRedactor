using System;
using System.Windows.Controls;
using System.Windows;
using OpenCVVideoRedactor.Helpers;
using System.Windows.Media;
using System.Windows.Threading;
using System.IO;
using FFMpegCore.Extensions.System.Drawing.Common;
using OpenCVVideoRedactor.Model.Database;

namespace OpenCVVideoRedactor.PopUpWindows
{
    public class MediaViewer
    {
        public static void ShowDialog(string filePath, long type, SolidColorBrush? backgroud = null)
        {
            var showTrack = true;
            var width = 800;
            var height = 600;
            if (type == (int)ResourceType.IMAGE) { showTrack = false; }
            if (type != (int)ResourceType.AUDIO) {
                var snapshot = FFMpegImage.Snapshot(filePath);
                width = snapshot.Width;
                height = snapshot.Height;
                snapshot.Dispose();
            }
            App.Current.Dispatcher.Invoke(() =>
            {
                Window Box = new Window();
                Box.Title = Path.GetFileName(filePath);
                Box.Width = width;
                Box.Height = height + (showTrack?30:0);
                Box.Background = backgroud != null ? backgroud : new SolidColorBrush(Colors.Black);
                Grid grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition());
                if (showTrack)
                {
                    var rowDefinition = new RowDefinition();
                    rowDefinition.Height = new GridLength(30);
                    grid.RowDefinitions.Add(rowDefinition);
                }
                Box.Content = grid;
                Box.WindowStyle = WindowStyle.SingleBorderWindow;
                Box.WindowStartupLocation = WindowStartupLocation.Manual;

                MediaElement content = new MediaElement();
                content.LoadedBehavior = MediaState.Manual;
                content.Source = new Uri(filePath);
                content.Stretch = Stretch.Uniform;
                content.StretchDirection = StretchDirection.DownOnly;
                grid.Children.Add(content);
                var timer = new DispatcherTimer();
                var paused = false;
                if (showTrack)
                {
                    Slider timeController = new Slider();
                    timeController.Margin = new Thickness(30, 0, 0, 0);
                    timeController.Minimum = TimeSpan.Zero.TotalSeconds;
                    timeController.VerticalAlignment = VerticalAlignment.Center;
                    timeController.ValueChanged += (sender, args) =>
                    {
                        var time = TimeSpan.FromSeconds(timeController.Value);
                        if(!paused)content.Pause();
                        content.Position = time;
                        if(!paused)content.Play();
                    };
                    Grid.SetRow(timeController, 1);
                    content.MediaOpened += (sender, args) =>
                    {
                        timeController.Maximum = content.NaturalDuration.TimeSpan.TotalSeconds;
                        timer.Interval = TimeSpan.FromSeconds(0.25);
                        timer.Tick += (sender, args) =>
                        {
                            if (content.NaturalDuration.TimeSpan.TotalSeconds > 0)
                            {
                                timeController.Value = content.Position.TotalSeconds;
                            }
                        };
                        timer.Start();
                    };
                    Button start = new Button();
                    start.Width = 30;
                    start.Height = 30;
                    start.HorizontalAlignment = HorizontalAlignment.Left;
                    start.Content = "▶️";
                    start.Visibility = Visibility.Collapsed;
                    Grid.SetRow(start, 1);
                    grid.Children.Add(start);
                    Button stop = new Button();
                    stop.Width = 30;
                    stop.Height = 30;
                    stop.HorizontalAlignment = HorizontalAlignment.Left;
                    stop.Content = "◼";
                    Grid.SetRow(stop, 1);
                    grid.Children.Add(stop);
                    start.Click += (sender, args) =>
                    {
                        start.Visibility = Visibility.Collapsed;
                        stop.Visibility = Visibility.Visible;
                        content.Play();
                        paused = false;
                    };
                    stop.Click += (sender, args) =>
                    {
                        stop.Visibility = Visibility.Collapsed;
                        start.Visibility = Visibility.Visible;
                        content.Pause();
                        paused = true;
                    };
                    grid.Children.Add(timeController);
                }
                content.Play();
                Box.Left = 600;
                Box.Top = 100;
                Box.ShowDialog();
                timer.Stop();
            });
        }
    }
}
