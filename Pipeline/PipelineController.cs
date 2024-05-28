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
using Microsoft.Win32;
using OpenCVVideoRedactor.PopUpWindows;
using System.Windows;

namespace OpenCVVideoRedactor.Pipeline
{
    public class PipelineController
    {
        private List<ResourceInUse> _resources = new List<ResourceInUse>();
        private Dictionary<long, FrameProcessingPipeline> _pipelines = new Dictionary<long, FrameProcessingPipeline>();
        private Dictionary<long, MediaStorage> _media = new Dictionary<long, MediaStorage>();
        private Mat _background;
        private Project project;
        public bool VideoIsReady { get; private set; } = false;
        public PipelineController(CurrentProjectInfo projectInfo) {
            if (projectInfo.ProjectInfo == null) throw new Exception("Не найден проект");
            _resources = projectInfo.ResourcesInUse.ToList();
            project = projectInfo.ProjectInfo;
            foreach (var resource in _resources.Where(n => n.Resource.IsNotAudio))
            {
                _pipelines[resource.Resource.Id] = new FrameProcessingPipeline(resource.Resource);
                _media[resource.Resource.Id] = new MediaStorage(resource, project);
            }
            var r = (byte)((projectInfo.ProjectInfo.Background >> 16) & 255);
            var g = (byte)((projectInfo.ProjectInfo.Background >> 8) & 255);
            var b = (byte)(projectInfo.ProjectInfo.Background & 255);
            _background = new Mat((int)projectInfo.ProjectInfo.VideoHeight, (int)projectInfo.ProjectInfo.VideoWidth,
                MatType.CV_8UC3,new Scalar(r,g,b));
        }
        public bool UpdateResourceList(IEnumerable<ResourceInUse> resources)
        {
            var changed = false;
            foreach(var resource in resources.Where(n=>n.Resource.IsNotAudio))
            {
                if(!_media.ContainsKey(resource.Resource.Id)) _media[resource.Resource.Id] = new MediaStorage(resource, project);
                var resourcePrev = _resources.FirstOrDefault(n=>n.Resource.Id == resource.Resource.Id);
                if(resourcePrev == null || resource.Resource.IsPropertiesChanged(resourcePrev.Resource))
                {
                    _pipelines[resource.Resource.Id] = new FrameProcessingPipeline(resource.Resource);
                    changed = true;
                }
            }
            _resources = resources.ToList();
            foreach (var key in _media.Keys.ToList())
            {
                if (!_resources.Any(n => n.Resource.Id == key))
                {
                    _media[key].Dispose();
                    _media.Remove(key);
                    changed = true;
                }
            }
            if (changed) VideoIsReady = false;
            return changed;
        }
        public void UpdatePipelineFor(Resource resource)
        {
            if (resource.IsNotAudio)
            {
                _pipelines[resource.Id] = new FrameProcessingPipeline(resource);
                VideoIsReady = false;
            }
        }
        public BitmapSource GetFrame(TimeSpan time, long selectedRectId = 0)
        {
            var frame = _background.Clone();
            var frameRect = new OpenCvSharp.Rect(0, 0, 0, 0);
            foreach(var overlayFrame in _resources.Where(n=>n.Resource.IsNotAudio).Select(n => ConvertToFrame(n, time, project)))
            {
                if(overlayFrame != null)
                {
                    var resultFrame = overlayFrame;
                    try
                    {
                        resultFrame = _pipelines[overlayFrame.Id].Apply(overlayFrame);
                    }catch(Exception e) {
                        System.Windows.MessageBox.Show("Один из операторов конвеера выдал ошибку:"+e.Message);
                    }
                    if (resultFrame != null)
                    {
                        var x = resultFrame.Variables["x"];
                        var y = resultFrame.Variables["y"];
                        frame = frame.DrawMat(resultFrame.Image, new OpenCvSharp.Point(x, y));
                        if (resultFrame.Id == selectedRectId) frameRect = new OpenCvSharp.Rect((int)x, (int)y, resultFrame.Image.Width,resultFrame.Image.Height);
                    }
                }
            }
            if(frameRect.Width > 0 && frameRect.Height > 0)frame.Rectangle(frameRect, Scalar.Aqua, 1);
            return BitmapSourceConverter.ToBitmapSource(frame);
        }
        public static List<string> GetVariables(ResourceInUse resource)
        {
            var result = new List<string>() {"x","y","time","duration","frame","width","height" };
            resource.Resource.Variables.ForEach(n => result.Add(n.Name));
            return result;
        }
        public Frame? ConvertToFrame(ResourceInUse resource, TimeSpan time, Project info)
        {
            if (time.Ticks >= resource.Resource.StartTime && time.Ticks <= resource.Resource.StartTime + resource.Resource.Duration)
            {
                long id = resource.Resource.Id;
                var variables = new Dictionary<string, double>
                {
                    { "x", resource.Resource.PossitionX },
                    { "y", resource.Resource.PossitionY },
                    { "time", time.TotalSeconds },
                    { "duration", resource.Resource.Duration},
                    { "frame",  (int)((time.Ticks-resource.Resource.StartTime.Value)*info.VideoFps/10000000)},
                };
                resource.Resource.Variables.ForEach(n => variables.Add(n.Name,n.Value));
                var mat = _media[id].GetFrame(time);
                if (mat == null) return null;
                variables["width"] = mat.Width;
                variables["height"] = mat.Height;
                return new Frame(id, mat, variables);
            }
            return null;
        }
        public async void GenerateVideo(Action<long, long> callback) {
            if (_resources.Count == 0) return;
            var tempFile = System.IO.Path.Combine(project.DataFolder, "temp_output.mp4");
            var output = System.IO.Path.Combine(project.DataFolder, "output.mp4");
            Dictionary<string, object> codecs = new Dictionary<string, object>()
                {
                    { "H264 (рекомендуемый)", FourCC.H264 },
                    { "MJPG", FourCC.MJPG },
                    { "XVID", FourCC.XVID },
                    { "MP42", FourCC.MP42 },
                    { "DIVX", FourCC.DIVX },
                    { "AVC", FourCC.AVC },
                    { "CVID", FourCC.CVID },
                };
            int? selectedCodec = (int?)SelectionBox.ShowDialog("Экспорт видео", "Выбирите кодек для кодирования видео", codecs);
            if (selectedCodec == null)
            {
                return;
            }
            if(VideoIsReady && File.Exists(output))
            {
                var codecName = FFProbe.Analyse(output).PrimaryVideoStream?.CodecName ?? "";
                int codec = FourCC.FromString(codecName);
                if (codec / 100 != selectedCodec.Value / 100)
                {
                    File.Delete(output);
                    VideoIsReady = false;
                }
            }
            if (!VideoIsReady)
            {
                var Duration = _resources[0].MaxDuration;
                using (var writer = new VideoWriter())
                {
                    // H264
                    if (!writer.Open(tempFile, selectedCodec.Value, project.VideoFps, new OpenCvSharp.Size(project.VideoWidth, project.VideoHeight))){
                        writer.Open(tempFile, FourCC.H264, project.VideoFps, new OpenCvSharp.Size(project.VideoWidth, project.VideoHeight));
                    }
                    long ticksPerFrame = (long)10000000.0 / project.VideoFps;
                    int frameI = 0;
                    for (long i = 0; i < Duration; i += ticksPerFrame)
                    {
                        Mat frame = _background.Clone();
                        foreach (var resource in _resources.Where(n => n.Resource.IsNotAudio))
                        {
                            if (i >= resource.Resource.StartTime && i <= resource.Resource.StartTime + resource.Resource.Duration)
                            {
                                long id = resource.Resource.Id;
                                var variables = new Dictionary<string, double>
                            {
                                { "x", resource.Resource.PossitionX },
                                { "y", resource.Resource.PossitionY },
                                { "time", TimeSpan.FromTicks(i).TotalSeconds },
                                { "duration", resource.Resource.Duration},
                                { "frame",  frameI},
                            };
                                frameI++;
                                resource.Resource.Variables.ForEach(n => variables.Add(n.Name, n.Value));
                                var mat = _media[id].GetFrame(TimeSpan.FromTicks(i));
                                if (mat != null)
                                {
                                    variables["width"] = mat.Width;
                                    variables["height"] = mat.Height;
                                    var applied = _pipelines[id].Apply(new Frame(id, mat, variables));
                                    if (applied != null)
                                    {
                                        frame = frame.DrawMat(applied.Image, new OpenCvSharp.Point(applied.Variables["x"], applied.Variables["y"]));
                                    }
                                }
                            }
                            if(i!=Duration)callback.Invoke(i, Duration);
                        }
                        writer.Write(frame);
                    }
                }
                if (File.Exists(tempFile))
                {
                    var args = FFMpegArguments.FromFileInput(tempFile, true, (arg) => { arg.WithCustomArgument("-f lavfi -i anullsrc=channel_layout=stereo:sample_rate=44100"); })
                             .OutputToFile(output, true, options =>
                         options.WithCustomArgument("-c:v copy -c:a aac -shortest"));
                    await args.ProcessAsynchronously();
                    File.Delete(tempFile);
                    foreach (var audio in _resources.Where(n => !n.Resource.IsNotAudio))
                    {
                        var delay = audio.Resource.StartTime != null ? TimeSpan.FromTicks(audio.Resource.StartTime.Value) : TimeSpan.Zero;
                        var fileName = System.IO.Path.Combine(project.DataFolder, "audios", audio.Resource.Name);
                        var audioArgs = FFMpegArguments.FromFileInput(output, true)
                                 .AddFileInput(fileName)
                                 .OutputToFile(tempFile, true, options =>
                             options.WithCustomArgument($"-filter_complex \"[1:a]adelay={delay.TotalSeconds}s:all=1[a1];[0:a][a1]amix\""));
                        await audioArgs.ProcessAsynchronously();
                        File.Delete(output);
                        File.Move(tempFile, output);
                    }
                }
            }
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "video files|*.mp4;*.avi";
            var value = saveFileDialog.ShowDialog();
            if (value.HasValue && value.Value)
            {
                if (output == saveFileDialog.FileName) return;
                await FFMpegArguments.FromFileInput(output).OutputToFile(saveFileDialog.FileName, true).ProcessAsynchronously();
            }
            callback.Invoke(1, 1);
            VideoIsReady = true;
        }
    }
}
