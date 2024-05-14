using OpenCvSharp;
using OpenCVVideoRedactor.Model;
using OpenCVVideoRedactor.Model.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCVVideoRedactor.Pipeline
{
    internal class MediaStorage
    {
        private Mat? _image = null;
        private VideoCapture? _video = null;
        public MediaStorage(ResourceInUse resource, Project info)
        {
            if(resource.Resource.Type == (long)ResourceType.VIDEO)
            {
                _video = new VideoCapture(Path.Combine(Path.Combine(info.DataFolder, "videos"), resource.Resource.Name));
            }
            else if(resource.Resource.Type == (long)ResourceType.IMAGE)
            {
                _image = new Mat(Path.Combine(Path.Combine(info.DataFolder, "images"), resource.Resource.Name), ImreadModes.Unchanged);
            }
            else
            {
                throw new Exception("MediaStorage не поддерживает данный тип ресурса");
            }
        }
        private readonly object lockGetFrame = new object();
        public Mat? GetFrame(TimeSpan time)
        {
            if (_image != null) return _image.Clone();
            if(_video != null)
            {
                lock (lockGetFrame)
                {
                    Mat result = new Mat();
                    _video.PosMsec = (int)time.TotalMilliseconds;
                    if (_video.Read(result))
                    {
                        return result;
                    }
                    _video.PosFrames = _video.FrameCount - 1;
                    return _video.Read(result) ? result : null;
                }
            }
            return null;
        }
        public void Dispose()
        {
            if (_video != null) { _video.Dispose(); _video = null; }
            if (_image != null) { _image.Dispose(); _image = null; }
        }
        ~MediaStorage()
        {
            if (_video != null) _video.Dispose();
            if (_image != null) _image.Dispose();
        }
    }
}
