using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using OpenCVVideoRedactor.Pipeline;
using OpenCVVideoRedactor.Pipeline.Operations;

namespace OpenCVVideoRedactor.Helpers
{
    class OperationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var operation = value as IFrameOperation;
            if (operation != null)
            {
                return Convert(operation.Name);
            }
            var modelOperation = value as Model.Database.Operation;
            if(modelOperation != null)
            {
                return Convert(modelOperation.Name);
            }
            return value;
        }
        public static string Convert(string name)
        {
            switch (name)
            {
                case nameof(ApplyBlurFilter): return "Наложить блюр";
                case nameof(ApplyGrayFilter): return "Серый фильтр";
                case nameof(ChangeByCondition): return "Изменить по условию";
                case nameof(ChangeVariable): return "Изменить переменную";
                case nameof(CropFrame): return "Обрезать изображение";
                case nameof(DetectFaceOnFrame): return "Детектирование лица";
                case nameof(PerspectiveSkewFrame): return "Искривление изображения";
                case nameof(RemoveBackgroundColor): return "Удалить фон по цвету";
                case nameof(ResizeFrame): return "Изменить размер изображения";
                case nameof(RotateFrame): return "Поворот изображения";
                case nameof(ChangeFrameOpacity): return "Изменить прозрачность";

                case nameof(ChangeSaturation): return "Изменить насыщеность";
                case nameof(ChangeHue): return "Изменить оттеннок";
                case nameof(ChangeLightness): return "Изменить яркость";
            }
            return name;
        }
        public object ConvertBack(object value, Type targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
