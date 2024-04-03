using Microsoft.EntityFrameworkCore.Infrastructure;
using OpenCVVideoRedactor.Model.Database;
using System.ComponentModel;
using System.Windows.Media;

namespace OpenCVVideoRedactor.Model
{
    public class CreateProjectModel : INotifyPropertyChanged
    {
        private string _title = "";
        private string _dataFolder = "";
        private long _fps = 1;
        private long _width = 640;
        private long _height = 480;
        private Color _background = Colors.Black;

        public string Title
        {
            get { return _title; }
            set
            {
                _title = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
            }
        }
        public string DataFolder
        {
            get { return _dataFolder; }
            set
            {
                _dataFolder = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DataFolder)));
            }
        }
        public long VideoFps
        {
            get { return _fps; }
            set
            {
                _fps = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VideoFps)));
            }
        }
        public long VideoWidth
        {
            get { return _width; }
            set
            {
                _width = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VideoWidth)));
            }
        }
        public long VideoHeight
        {
            get { return _height; }
            set
            {
                _height = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VideoHeight)));
            }
        }
        public Color BackgroundColor
        {
            get { return _background; }
            set
            {
                _background = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BackgroundColor)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
