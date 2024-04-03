using FFMpegCore;
using Microsoft.VisualBasic.Devices;
using OpenCVVideoRedactor.Model.Database;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OpenCVVideoRedactor.Helpers
{
    public class FileAlreadyExists : Exception { public FileAlreadyExists(string message) : base(message) { } }
    class ResourceHelper
    {
        public static object? GetDataFromItemsControl(ItemsControl source, Point point)
        {
            UIElement? element = source.InputHitTest(point) as UIElement;
            if (element != null)
            {
                object data = DependencyProperty.UnsetValue;
                while (data == DependencyProperty.UnsetValue)
                {
                    data = source.ItemContainerGenerator.ItemFromContainer(element);
                    if (data == DependencyProperty.UnsetValue)
                    {
                        element = VisualTreeHelper.GetParent(element) as UIElement;
                    }
                    if (element == source)
                    {
                        return null;
                    }
                }
                if (data != DependencyProperty.UnsetValue)
                {
                    return data;
                }
            }

            return null;
        }
        public static Resource CreateResourceFromVideo(string file, Project project, out Resource? ref_audio, string? newName=null)
        {
            var info = FFProbe.Analyse(file);
            var audioStream = info.AudioStreams.FirstOrDefault();
            var videosDir = Path.Combine(project.DataFolder, "videos");
            var audiosDir = Path.Combine(project.DataFolder, "audios");
            if (!Directory.Exists(videosDir)) Directory.CreateDirectory(videosDir);
            if (!Directory.Exists(audiosDir)) Directory.CreateDirectory(audiosDir);
            newName = newName ?? Path.GetFileName(file);
            var videoFileName = Path.Combine(videosDir, newName);
            if (File.Exists(videoFileName)) throw new FileAlreadyExists("Медиа-файл с таким именем уже существует");
            ref_audio = null;
            if (audioStream != null)
            {
                var audioFileName = Path.Combine(audiosDir, Path.GetFileNameWithoutExtension(newName) + "." + audioStream.CodecName);
                for (int i = 0; File.Exists(audioFileName); i++) {
                    audioFileName = Path.Combine(audiosDir,Path.GetFileNameWithoutExtension(newName) + i + "." + audioStream.CodecName);
                }
                FFMpegArguments.FromFileInput(file, true, args => args.WithCustomArgument("-hwaccel cuda"))
                    .OutputToFile(videoFileName, true, args => {
                        args.WithCustomArgument($"-map 0:a \"{audioFileName}\" -map 0:v");
                        args.WithFramerate(project.VideoFps);
                    }).ProcessSynchronously();
                ref_audio = new Resource();
                ref_audio.Name = Path.GetFileName(audioFileName);
                ref_audio.StartTime = null;
                ref_audio.ProjectId = project.Id;
                ref_audio.Duration = audioStream.Duration.Ticks;
                ref_audio.PossitionX = 0;
                ref_audio.PossitionY = 0;
                ref_audio.Type = 2;
            }
            else
            {
                FFMpegArguments.FromFileInput(file, true, args => args.WithCustomArgument("-hwaccel cuda"))
                    .OutputToFile(videoFileName, true, args => args.WithFramerate(project.VideoFps))
                    .ProcessSynchronously();
            }
            var video = new Resource();
            video.Name = Path.GetFileName(videoFileName);
            video.StartTime = null;
            video.ProjectId = project.Id;
            video.Duration = info.Duration.Ticks;
            video.PossitionX = 0;
            video.PossitionY = 0;
            video.Type = 1;
            return video;
        }
        public static Resource CreateResourceFromAudio(string file, Project project, string? newName = null)
        {
            var info = FFProbe.Analyse(file);
            var audiosDir = Path.Combine(project.DataFolder, "audios");
            if (!Directory.Exists(audiosDir)) Directory.CreateDirectory(audiosDir);
            newName = newName ?? Path.GetFileName(file);
            var newFileName = Path.Combine(audiosDir, newName);
            if(File.Exists(newFileName)) throw new FileAlreadyExists("Медиа-файл с таким именем уже существует");
            File.Copy(file, newFileName, false);
            Resource audio = new Resource();
            audio.StartTime = null;
            audio.ProjectId = project.Id;
            audio.Name = Path.GetFileName(newFileName);
            audio.PossitionX = 0;
            audio.PossitionY = 0;
            audio.Type = 2;
            audio.Duration = info.Duration.Ticks;
            return audio;
        }
        public static Resource CreateResourceFromImage(string file, Project project, string? newName = null)
        {
            var imagesDir = Path.Combine(project.DataFolder, "images");
            if (!Directory.Exists(imagesDir)) Directory.CreateDirectory(imagesDir);
            newName = newName ?? Path.GetFileName(file);
            var newFileName = Path.Combine(imagesDir, newName);
            if (File.Exists(newFileName)) throw new FileAlreadyExists("Медиа-файл с таким именем уже существует");
            File.Copy(file, newFileName, false);
            Resource image = new Resource();
            image.StartTime = null;
            image.ProjectId = project.Id;
            image.Name = Path.GetFileName(newFileName);
            image.PossitionX = 0;
            image.PossitionY = 0;
            image.Type = 0;
            image.Duration = TimeSpan.FromMilliseconds(1000.0 / project.VideoFps).Ticks;
            return image;
        }
        public static void DropNotExistingResources(string dir, DatabaseContext context)
        {
            var resources = context.Resources;
            foreach (var resource in resources)
            {
                var file = GetPathByType(dir, resource.Type, resource.Name);
                if (!File.Exists(file))
                {
                    MessageBox.Show(file);
                    context.Resources.Remove(resource);
                }
            }
            context.SaveChanges();
        }
        public static string GetPathByType(string dataDir,long type,string fileName)
        {
            string dirType = "";
            switch((ResourceType)type) {
                case ResourceType.IMAGE: dirType = "images"; break;
                case ResourceType.VIDEO: dirType = "videos"; break;
                case ResourceType.AUDIO: dirType = "audios"; break;
            }
            return Path.Combine(dataDir,dirType,fileName);
        }
    }
}
