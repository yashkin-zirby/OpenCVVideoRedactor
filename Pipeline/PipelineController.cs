using DevExpress.Mvvm;
using OpenCvSharp;
using OpenCVVideoRedactor.Model;
using OpenCVVideoRedactor.Model.Database;
using OpenCVVideoRedactor.Pipepline;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Text;
using OpenCvSharp.WpfExtensions;
using FFMpegCore;
using FFMpegCore.Extensions.System.Drawing.Common;
using OpenCvSharp.Extensions;
using OpenCVVideoRedactor.Helpers;
using DevExpress.Mvvm.Native;

namespace OpenCVVideoRedactor.Pipeline
{
    public class PipelineController : BindableBase
    {
        private List<ResourceInUse> _resources = new List<ResourceInUse>();
        private Dictionary<long, FrameProcessingPipeline> _pipelines = new Dictionary<long, FrameProcessingPipeline>();
        private Mat _background;
        private bool _videoIsReady = false;
        private string _videoDir;
        private string _audioDir;
        private string _imageDir;
        private double _fps;
        public PipelineController(CurrentProjectInfo projectInfo) {
            if (projectInfo.ProjectInfo == null) throw new Exception("Не найден проект");
            _resources = projectInfo.ResourcesInUse.Where(n=>n.Resource.Type != (int)ResourceType.AUDIO).ToList();

            foreach (var resource in _resources)
            {
                _pipelines[resource.Resource.Id] = new FrameProcessingPipeline(resource.Resource);
            }
            _imageDir = Path.Combine(projectInfo.ProjectInfo.DataFolder,"images");
            _audioDir = Path.Combine(projectInfo.ProjectInfo.DataFolder, "audios");
            _videoDir = Path.Combine(projectInfo.ProjectInfo.DataFolder, "videos");
            _fps = projectInfo.ProjectInfo.VideoFps;
            var r = (byte)((projectInfo.ProjectInfo.Background >> 16) & 255);
            var g = (byte)((projectInfo.ProjectInfo.Background >> 8) & 255);
            var b = (byte)(projectInfo.ProjectInfo.Background & 255);
            _background = new Mat((int)projectInfo.ProjectInfo.VideoHeight, (int)projectInfo.ProjectInfo.VideoWidth,
                MatType.CV_8UC3,new Scalar(r,g,b));
        }
        public bool UpdateResourceList(IEnumerable<ResourceInUse> resources)
        {
            var changed = false;
            foreach(var resource in resources)
            {
                var resourcePrev = _resources.FirstOrDefault();
                if(resourcePrev == null || resource.Resource.IsPropertiesChanged(resourcePrev.Resource))
                {
                    _pipelines[resource.Resource.Id] = new FrameProcessingPipeline(resource.Resource);
                    changed = true;
                }
            }
            _resources = resources.ToList();
            return changed;
        }
        public void UpdatePipelineFor(Resource resource)
        {
            _pipelines[resource.Id] = new FrameProcessingPipeline(resource);
        }
        public BitmapSource GetFrame(TimeSpan time)
        {
            var frame = _background.Clone();
            foreach(var overlayFrame in _resources.Select(n => ConvertToFrame(n, time)))
            {
                if(overlayFrame != null)
                {
                    var resultFrame = _pipelines[overlayFrame.Id].Apply(overlayFrame);
                    var x = resultFrame.Variables["x"];
                    var y = resultFrame.Variables["y"];
                    frame = frame.DrawMat(resultFrame.Image, new Point(x, y));
                }
            }
            return BitmapSourceConverter.ToBitmapSource(frame);
        }
        private Frame? ConvertToFrame(ResourceInUse resource, TimeSpan time)
        {
            if (time.Ticks >= resource.Resource.StartTime && time.Ticks <= resource.Resource.StartTime + resource.Resource.Duration)
            {
                long id = resource.Resource.Id;
                var variables = new Dictionary<string, double>
                {
                    { "x", resource.Resource.PossitionX },
                    { "y", resource.Resource.PossitionY },
                    { "time", time.Ticks },
                    { "frame",  (int)((time.Ticks-resource.Resource.StartTime.Value)*_fps/10000000)},
                };
                resource.Resource.Variables.ForEach(n => variables.Add(n.Name,n.Value));
                switch ((ResourceType)resource.Resource.Type)
                {
                    case ResourceType.IMAGE:
                        return new Frame(id,new Mat(Path.Combine(_imageDir, resource.Resource.Name), ImreadModes.Unchanged), variables);
                    case ResourceType.VIDEO:
                        if (resource.ActualDuration != null)
                        {
                            var duration = resource.ActualDuration.Value;
                            duration = duration - TimeSpan.FromMilliseconds(1000 / _fps);
                            time = new TimeSpan(Math.Min(duration.Ticks, time.Ticks - resource.Resource.StartTime.Value));
                        }
                        else
                        {
                            var duration = FFProbe.Analyse(Path.Combine(_videoDir, resource.Resource.Name)).Duration;
                            duration = duration - TimeSpan.FromMilliseconds(1000 / _fps);
                            time = new TimeSpan(Math.Min(duration.Ticks, time.Ticks - resource.Resource.StartTime.Value));
                        }
                        var image = FFMpegImage.Snapshot(Path.Combine(_videoDir, resource.Resource.Name), null, time);
                        return new Frame(id,BitmapConverter.ToMat(image), variables);
                }
            }
            return null;
        }
        public void GenerateVideo() {
            
        }
    }
}
