﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using FFMpegCore;
using Microsoft.EntityFrameworkCore;
using OpenCVVideoRedactor.Model.Database;
using OpenCVVideoRedactor.View;

namespace OpenCVVideoRedactor.Model
{
    public class CurrentProjectInfo : INotifyPropertyChanged
    {
        private Project? _projectInfo = null;
        private List<Resource> _resources = new List<Resource>();
        private Resource? _selectedResource = null;
        private Operation? _selectedOperation = null;
        private ResourceInUse? _selectedResourceInUse = null;
        private TimeSpan _currentTime = TimeSpan.Zero;
        public string? ImagesDir { get { return _projectInfo != null ? Path.Combine(_projectInfo.DataFolder, "images") : null; } }
        public string? VideosDir { get { return _projectInfo != null ? Path.Combine(_projectInfo.DataFolder, "videos") : null; } }
        public string? AudiosDir { get { return _projectInfo != null ? Path.Combine(_projectInfo.DataFolder, "audios") : null; } }
        public TimeSpan CurrentTime
        {
            get { return _currentTime; }
            set
            {
                _currentTime = value;
                RaisePropertiesChanged(nameof(CurrentTime));
            }
        }
        public Project? ProjectInfo {
            get { return _projectInfo; }
            set {
                _projectInfo = value;
                _currentTime = TimeSpan.Zero;
                Resources = new List<Resource>();
                RaisePropertiesChanged(nameof(ProjectInfo));
            }
        }
        private bool _resourcesVisible = true;
        private bool _propertiesVisible = true;
        public bool IsPropertiesColumnVisible
        {
            get { return _propertiesVisible; }
            set
            {
                _propertiesVisible = value;
                RaisePropertiesChanged(nameof(IsPropertiesColumnVisible));
            }
        }
        public bool IsResourcesColumnVisible
        {
            get { return _resourcesVisible; }
            set
            {
                _resourcesVisible = value;
                RaisePropertiesChanged(nameof(IsResourcesColumnVisible));
            }
        }
        public Resource? SelectedResource
        {
            get { return _selectedResource; }
            set
            {
                _selectedResource = value;
                _selectedResourceInUse = _resourceInUses.Count() > 0 && value != null ? _resourceInUses.FirstOrDefault(n => n.Resource.Id == value.Id) : null;
                RaisePropertiesChanged(nameof(SelectedResource), nameof(MaxDuration));
            }
        }
        public Operation? SelectedOperation
        {
            get { return _selectedOperation; }
            set {
                
                _selectedOperation = value;
                if (_selectedOperation != null)
                {
                    var changed = new ModifyOperationView().ShowDialog();
                    _selectedOperation = null;
                    if(changed.HasValue && changed.Value)NoticeResourceUpdated(null);
                }
            }
        }
        public ResourceInUse? SelectedResourceInUse
        {
            get
            {
                return _selectedResourceInUse;
            }
        }
        
        public void NoticeResourceUpdated(DatabaseContext? context)
        {
            var selected = SelectedResource;
            if (context != null && ProjectInfo != null)
                Resources = context.Resources.Where(n => n.ProjectId == ProjectInfo.Id)
                    .Include(n => n.Variables).Include(n => n.Operations).ThenInclude(n => n.Parameters).ToList();                
            else
                Resources = Resources.Select(x => x).ToList();
            SelectedResource = selected;
        }
        public long MaxDuration
        {
            get
            {
                var r = Resources;
                return r.Count()>0? r.Max(n => n.StartTime == null? 0 : n.StartTime.Value + n.Duration):0;
            }
        }
        public List<Resource> Resources
        {
            get { return _resources; }
            set
            {
                _resources = value;
                SelectedResource = null;
                RaisePropertiesChanged(nameof(Resources),nameof(Images), nameof(Videos), nameof(Audios));
                ResourcesInUse = _resources.Where(n => n.IsInUse).OrderBy(n => n.Layer).Select(n => new ResourceInUse(n, MaxDuration));
            }
        }
        private IEnumerable<ResourceInUse> _resourceInUses = new List<ResourceInUse>();
        public IEnumerable<ResourceInUse> ResourcesInUse
        {
            get { return _resourceInUses; }
            set
            {
                var result = new List<ResourceInUse>();
                foreach(var resource in value)
                {
                    var actual = _resourceInUses.FirstOrDefault(n => n.Resource.Id == resource.Resource.Id);
                    if (actual != null)
                    {
                        resource.ActualDuration = actual.ActualDuration;
                        resource.ActualWidth = actual.ActualWidth;
                        resource.ActualHeight = actual.ActualHeight;
                    }
                    else
                    {
                        try{
                            var dir = "";
                            switch((ResourceType)resource.Resource.Type)
                            {
                                case ResourceType.IMAGE: dir = ImagesDir; break;
                                case ResourceType.AUDIO: dir = AudiosDir; break;
                                case ResourceType.VIDEO: dir = VideosDir; break;
                            }
                            var info = FFProbe.Analyse(Path.Combine(dir, resource.Resource.Name));
                            if(info.PrimaryVideoStream != null)
                            {
                                resource.ActualDuration = info.PrimaryVideoStream.Duration;
                                resource.ActualWidth = info.PrimaryVideoStream.Width;
                                resource.ActualHeight = info.PrimaryVideoStream.Height;
                            }
                            if(info.PrimaryAudioStream != null)
                            {
                                resource.ActualDuration = info.PrimaryAudioStream.Duration;
                            }
                        }
                        catch(Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }
                    result.Add(resource);
                }
                _resourceInUses = result;
                RaisePropertiesChanged(nameof(ResourcesInUse));
            }
        }
        public IEnumerable<Resource> Images
        {
            get { return _resources.Where(n=>n.Type == (int)ResourceType.IMAGE); }
        }
        public IEnumerable<Resource> Videos
        {
            get { return _resources.Where(n => n.Type == (int)ResourceType.VIDEO); }
        }
        public IEnumerable<Resource> Audios
        {
            get { return _resources.Where(n => n.Type == (int)ResourceType.AUDIO); }
        }
        private void RaisePropertiesChanged(params string[] properties)
        {
            if(PropertyChanged != null)
            {
                foreach (string property in properties)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(property));
                }
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? VideoCompileEvent;
        public void CompileVideo()
        {
            if (VideoCompileEvent != null) VideoCompileEvent.Invoke(null,new EventArgs());
        }
    }
}
