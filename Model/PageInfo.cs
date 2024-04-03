using Microsoft.Extensions.Primitives;
using OpenCVVideoRedactor.View;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace OpenCVVideoRedactor.Model
{
    public class PageInfo : INotifyPropertyChanged
    {
        private Page? _currentPage = null;
        public Page? CurrentPage { 
            get { return _currentPage; } 
            set {
                var oldValue = _currentPage;
                _currentPage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentPage)));
            } 
        } 
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
//Файл