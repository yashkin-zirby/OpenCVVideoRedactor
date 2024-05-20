using OpenCVVideoRedactor.Parser;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.UI.Native;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using OpenCVVideoRedactor.Model;
using OpenCVVideoRedactor.Model.Database;
using OpenCVVideoRedactor.Pipeline;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static System.Net.Mime.MediaTypeNames;

namespace OpenCVVideoRedactor.ViewModel
{
    public class ModifyOperationViewModel : BindableBase
    {
        private CurrentProjectInfo _projectInfo;
        private DatabaseContext _dbContext;
        private Action? _close = null;
        private Window? window;
        private Operation? _operation;
        private List<Parameter> _inputs = new List<Parameter>();
        private List<Parameter> _outputs = new List<Parameter>();
        private MathParser parser = new MathParser();
        public Operation? Operation { get { return _operation; } }
        private List<string> _variables = new List<string>();
        public string ActionType { get { return _operation != null ? (_operation.Id > 0 ? "Модификация" : "Создание") : "Просмотр"; } }
        private Style _textStyle = new Style();
        public ModifyOperationViewModel(CurrentProjectInfo projectInfo,DatabaseContext dbContext) {
            _projectInfo = projectInfo;
            _dbContext = dbContext;
            _operation = projectInfo.SelectedOperation;
            if (_operation == null) return;
            _inputs = _operation.Parameters.Where(n => (n.Type & 1) == (long)ParameterType.INPUT)
                .OrderByDescending(n => n.Type).Select(n=>new Parameter(n)).ToList();
            _outputs = _operation.Parameters.Where(n => (n.Type & 1) == (long)ParameterType.OUTPUT)
                .OrderByDescending(n => n.Type).Select(n => new Parameter(n)).ToList();
            var ticks = projectInfo.SelectedResource?.StartTime;
            if(ticks != null && projectInfo.ProjectInfo != null && projectInfo.SelectedResourceInUse != null)
            {
                _variables = PipelineController.GetVariables(projectInfo.SelectedResourceInUse);
            }
            else
            {
                _operation = null;
            }
            _textStyle.Setters.Add(new Setter() { Property = TextBlock.ForegroundProperty, Value = new SolidColorBrush(Color.FromRgb(240, 240, 240)) });
            _textStyle.Setters.Add(new Setter() { Property = TextBlock.TextAlignmentProperty, Value = TextAlignment.Center });
            _textStyle.Setters.Add(new Setter() { Property = TextBlock.VerticalAlignmentProperty, Value = VerticalAlignment.Center});
        }
        private UIElement GenerateParameterView(Parameter parameter)
        {
            StackPanel panel = new StackPanel();
            panel.Margin = new Thickness(0, 5, 0, 5);
            panel.Orientation = Orientation.Horizontal;
            panel.HorizontalAlignment = HorizontalAlignment.Center;
            panel.Focusable = true;
            panel.MouseLeftButtonDown += (e, args) =>
            {
                args.Handled = false;
                panel.Focus();
            };
            switch ((ParameterType)(parameter.Type & ((int)ParameterType.REQUIRED - 1)))
            {
                case ParameterType.COLOR:
                    {
                        var colorPicker = new Xceed.Wpf.Toolkit.ColorPicker();
                        var rgba = parameter.Value.Split(",").Select(n=>byte.Parse(n)).ToArray();
                        colorPicker.SelectedColor = rgba.Length == 4?Color.FromArgb(rgba[3], rgba[0], rgba[1], rgba[2])
                            : Color.FromRgb(rgba[0], rgba[1], rgba[2]);
                        colorPicker.SelectedColorChanged += (e, args) =>
                        {
                            if(args.NewValue != null) 
                                parameter.Value = $"{args.NewValue.Value.R},{args.NewValue.Value.G},{args.NewValue.Value.B},{args.NewValue.Value.A}";
                        };
                        var varName = new TextBlock();
                        varName.Text = parameter.Name + " := ";
                        varName.Style = _textStyle;
                        varName.FontSize = 14;
                        panel.Children.Add(varName);
                        panel.Children.Add(colorPicker);
                    }break;
                case ParameterType.FLAG:
                    {
                        var checkBox = new CheckBox();
                        checkBox.IsChecked = parameter.Value == "True";
                        checkBox.Click += (e, args) =>
                        {
                            parameter.Value = checkBox.IsChecked.ToString() ?? "False";
                        };
                        var varName = new TextBlock();
                        varName.Text = parameter.Name + " := ";
                        varName.Style = _textStyle;
                        varName.FontSize = 14;
                        panel.Children.Add(varName);
                        panel.Children.Add(checkBox);
                    }
                    break;
                case ParameterType.CONDITION: {
                        var regex = new Regex(@"(?<![<>=≠])(<|>|<=|>=|=|≠)(?![<>=≠])");
                        var defParts = regex.Split(parameter.Value);
                        var selectOperation= new ComboBox();
                        selectOperation.Width = 40;
                        selectOperation.ItemsSource = new string[] { "<", ">", "<=", ">=", "=", "≠" };
                        selectOperation.SelectedValue = defParts[1];
                        selectOperation.SelectionChanged += (obj, args) => {
                            defParts[1] = selectOperation.SelectedValue as string ?? "=";
                            parameter.Value = defParts[0] + defParts[1] + defParts[2];
                        };
                        var leftPart = new TextBox();
                        leftPart.MinWidth = 100;
                        leftPart.Text = defParts[0];
                        leftPart.KeyDown += (o, a) => {
                            if (a.Key == Key.Enter)
                            {
                                panel.Focus();
                            }
                        };
                        leftPart.LostKeyboardFocus += (sender, args) => {
                            try
                            {
                                var expr = parser.Parse(leftPart.Text);
                                if (expr != null)
                                {
                                    defParts[0] = expr.ToString();
                                }
                                leftPart.Text = defParts[0];
                            }
                            catch
                            {
                                leftPart.Text = defParts[0];
                            }
                            parameter.Value = defParts[0] + defParts[1] + defParts[2];
                        };
                        leftPart.Style = _textStyle;
                        leftPart.Background = new SolidColorBrush(Color.FromRgb(105, 105, 110));
                        leftPart.FontSize = 14;

                        var rightPart = new TextBox();
                        rightPart.MinWidth = 100;
                        rightPart.Text = defParts[2];
                        rightPart.KeyDown += (o, a) => {
                            if (a.Key == Key.Enter)
                            {
                                panel.Focus();
                            }
                        };
                        rightPart.LostKeyboardFocus += (sender, args) => {
                            try
                            {
                                var expr = parser.Parse(rightPart.Text);
                                if (expr != null)
                                {
                                    defParts[2] = expr.ToString();
                                }
                                rightPart.Text = defParts[2];
                            }
                            catch
                            {
                                rightPart.Text = defParts[2];
                            }
                            parameter.Value = defParts[0] + defParts[1] + defParts[2];
                        };
                        rightPart.Style = _textStyle;
                        rightPart.Background = new SolidColorBrush(Color.FromRgb(105, 105, 110));
                        rightPart.FontSize = 14;
                        panel.Children.Add(leftPart);
                        panel.Children.Add(selectOperation);
                        panel.Children.Add(rightPart);
                    }
                    break;
                case ParameterType.DEFINITION:
                    {
                        var defParts = parameter.Value.Split(":=");
                        var selectVariable = new ComboBox();
                        selectVariable.MinWidth = 50;
                        selectVariable.ItemsSource = _variables.Except(new string[] { "time", "frame", "width", "height", "duration" });
                        selectVariable.SelectedValue = defParts[0];
                        selectVariable.SelectionChanged += (obj, args) => {
                            defParts[0] = selectVariable.SelectedValue != null ? (string)selectVariable.SelectedValue : "";
                            parameter.Value = defParts[0]+":="+defParts[1];
                        };
                        var equals = new TextBlock();
                        equals.Text = ":=";
                        equals.Margin = new Thickness(5, 0, 5, 0);
                        equals.Style = _textStyle;
                        equals.FontSize = 14;
                        var expression = new TextBox();
                        expression.MinWidth = 160;
                        expression.Text = defParts[1];
                        expression.KeyDown += (o, a) => { if (a.Key == Key.Enter) {
                                panel.Focus();
                            } 
                        };
                        expression.LostKeyboardFocus += (sender, args) => {
                            try
                            {
                                var expr = parser.Parse(expression.Text);
                                if(expr != null)
                                {
                                    defParts[1] = expr.ToString();
                                }
                                expression.Text = defParts[1];
                            }
                            catch
                            {
                                expression.Text = defParts[1];
                            }
                            parameter.Value = defParts[0] + ":=" + defParts[1];
                        };
                        expression.Style = _textStyle;
                        expression.Background = new SolidColorBrush(Color.FromRgb(105,105,110));
                        expression.FontSize = 14;
                        panel.Children.Add(selectVariable);
                        panel.Children.Add(equals);
                        panel.Children.Add(expression);
                    }
                    break;
                case ParameterType.EXPRESSION:
                    {
                        var varName = new TextBlock();
                        varName.Text = parameter.Name + " := ";
                        varName.Style = _textStyle;
                        varName.FontSize = 14;

                        var expression = new TextBox();
                        expression.MinWidth = 160;
                        expression.Text = parameter.Value;
                        expression.Margin = new Thickness(5, 0, 0, 0);
                        expression.KeyDown += (o, a) => {
                            if (a.Key == Key.Enter)
                            {
                                panel.Focus();
                            }
                        };
                        expression.LostKeyboardFocus += (sender, args) => {
                            try
                            {
                                var expr = parser.Parse(expression.Text);
                                if (expr != null)
                                {
                                    parameter.Value = expr.ToString();
                                }
                                expression.Text = parameter.Value;
                            }
                            catch
                            {
                                expression.Text = parameter.Value;
                            }
                        };
                        expression.Style = _textStyle;
                        expression.Background = new SolidColorBrush(Color.FromRgb(105, 105, 110));
                        expression.FontSize = 14;
                        panel.Children.Add(varName);
                        panel.Children.Add(expression);
                    }
                    break;
                case ParameterType.INPUT:
                    {
                        var varName = new TextBlock();
                        varName.Text = parameter.Name + " := ";
                        varName.Style = _textStyle;
                        varName.FontSize = 14;

                        var selectVariable = new ComboBox();
                        selectVariable.Margin = new Thickness(5,0,0,0);
                        selectVariable.MinWidth = 50;
                        selectVariable.ItemsSource = _variables.Union(new string[] { "" });
                        selectVariable.SelectedValue = parameter.Value;
                        selectVariable.SelectionChanged += (obj, args) => {
                            parameter.Value = selectVariable.SelectedValue != null ? (string)selectVariable.SelectedValue : "";
                        };
                        panel.Children.Add(varName);
                        panel.Children.Add(selectVariable);
                    }
                    break;
            }
            return panel;
        }
        private UIElement GenerateOutputParameterView(Parameter parameter)
        {
            StackPanel panel = new StackPanel();
            panel.HorizontalAlignment = HorizontalAlignment.Center;
            panel.Margin = new Thickness(0, 5, 0, 5);
            panel.Orientation = Orientation.Horizontal;
            switch ((ParameterType)(parameter.Type & ((int)ParameterType.REQUIRED - 1)))
            {
                case ParameterType.OUTPUT_DEFINITION:
                    {
                        var defParts = parameter.Value.Split(":=");
                        var selectVariable = new ComboBox();
                        selectVariable.MinWidth = 50;
                        selectVariable.ItemsSource = _variables.Except(new string[] { "time", "frame", "width", "height", "duration" });
                        selectVariable.SelectedValue = defParts[0];
                        selectVariable.SelectionChanged += (obj, args) => {
                            defParts[0] = selectVariable.SelectedValue != null ? (string)selectVariable.SelectedValue : "";
                            parameter.Value = defParts[0] + ":=" + defParts[1];
                        };
                        var equals = new TextBlock();
                        equals.Text = ":=";
                        equals.Margin = new Thickness(5, 0, 5, 0);
                        equals.Style = _textStyle;
                        equals.FontSize = 14;
                        var expression = new TextBox();
                        expression.MinWidth = 160;
                        expression.Text = defParts[1];
                        expression.KeyDown += (o, a) => {
                            if (a.Key == Key.Enter)
                            {
                                panel.Focus();
                            }
                        };
                        expression.LostKeyboardFocus += (sender, args) => {
                            try
                            {
                                var expr = parser.Parse(expression.Text);
                                if (expr != null)
                                {
                                    defParts[1] = expr.ToString();
                                }
                                expression.Text = defParts[1];
                            }
                            catch
                            {
                                expression.Text = defParts[1];
                            }
                            parameter.Value = defParts[0] + ":=" + defParts[1];
                        };
                        expression.Style = _textStyle;
                        expression.Background = new SolidColorBrush(Color.FromRgb(105, 105, 110));
                        expression.FontSize = 14;
                        panel.Children.Add(selectVariable);
                        panel.Children.Add(equals);
                        panel.Children.Add(expression);
                    }
                    break;
                case ParameterType.OUTPUT:
                    {
                        var varName = new TextBlock();
                        varName.Text = parameter.Name + " =>";
                        varName.FontSize = 14;
                        varName.Style = _textStyle;
                        varName.Margin = new Thickness(0, 0, 5, 0);
                        var selectVariable = new ComboBox();
                        selectVariable.MinWidth = 50;
                        selectVariable.ItemsSource = _variables.Union(new string[] { "" }).Except(new string[] { "time", "frame", "duration", "width", "height" });
                        selectVariable.SelectedValue = parameter.Value;
                        selectVariable.SelectionChanged += (obj, args) => {
                            parameter.Value = selectVariable.SelectedValue != null ? (string)selectVariable.SelectedValue : "";
                        };
                        panel.Children.Add(varName);
                        panel.Children.Add(selectVariable);
                    }
                    break;
            }
            return panel;
        }
        public ICommand GenerateView
        {
            get
            {
                return new DelegateCommand<RoutedEventArgs>(e => {
                    window = e.Source as Window;
                    if (window != null) {
                        if (_operation == null) { window.Close(); _projectInfo.SelectedOperation = null;return; }
                        _close = ()=>window.Close();
                        var inputs = window.FindName("Inputs") as StackPanel;
                        if(inputs != null)
                        {
                            for(int i = 0; i < _inputs.Count; i++)
                            {
                                inputs.Children.Add(GenerateParameterView(_inputs[i]));
                            }
                        }
                        var outputs = window.FindName("Outputs") as StackPanel;
                        if(outputs != null)
                        {
                            for (int i = 0; i < _outputs.Count; i++)
                            {
                                outputs.Children.Add(GenerateOutputParameterView(_outputs[i]));
                            }
                        }
                    }
                });
            }
        }
        public ICommand ApplyCommand
        {
            get
            {
                return new DelegateCommand(() => {
                    var modelValid = true;
                    int num = 0;
                    foreach(var param in _inputs)
                    {
                        var type = (ParameterType)(param.Type & ((int)ParameterType.REQUIRED-1));
                        num++;
                        if (type == ParameterType.CONDITION)
                        {
                            modelValid = modelValid && IsConditionValid(param, num);
                        }else if (type == ParameterType.DEFINITION)
                        {
                            modelValid = modelValid && IsDefinitionValid(param, num);
                        }
                        else if (type == ParameterType.EXPRESSION)
                        {
                            modelValid = modelValid && IsExpressionValid(param, num);
                        }
                        else if (type == ParameterType.INPUT)
                        {
                            var isRequired = (param.Type & (int)ParameterType.REQUIRED) == (int)ParameterType.REQUIRED;
                            var valid = (isRequired ? param.Value.Length > 0 : true);
                            if (!valid) MessageBox.Show($"Ошибка в параметре №{num}\nПараметр {param.Name} обязательный");
                            modelValid = modelValid && valid;
                        }
                    }
                    foreach (var param in _outputs)
                    {
                        var type = (ParameterType)(param.Type & 15);
                        num++;
                        if (type == ParameterType.OUTPUT_DEFINITION)
                        {
                            modelValid = modelValid && IsDefinitionValid(param, num);
                        }
                        else if (type == ParameterType.OUTPUT)
                        {
                            var isRequired = (param.Type & (int)ParameterType.REQUIRED) == (int)ParameterType.REQUIRED;
                            var valid = (isRequired ? param.Value.Length > 0 : true);
                            if (!valid) MessageBox.Show($"Ошибка в параметре №{num}\nВыходной параметр {param.Name} обязательный");
                            modelValid = modelValid && valid;
                        }
                    }
                    if (modelValid)
                    {
                        if (_projectInfo.SelectedResource != null)
                        {
                            var operation = _dbContext.Operations.Include(n => n.Parameters).FirstOrDefault(n => n.Id == Operation.Id);
                            if (operation != null)
                            {
                                foreach(var param in operation.Parameters)
                                {
                                    if((param.Type&1) == (int)ParameterType.INPUT)
                                    {
                                        param.Value = _inputs.First(n => n.Name == param.Name).Value;
                                    }
                                    else
                                    {
                                        param.Value = _outputs.First(n => n.Name == param.Name).Value;
                                    }
                                }
                                _dbContext.SaveChanges();
                            }
                            else
                            {
                                var index = _dbContext.Operations.Where(n => n.Source == _projectInfo.SelectedResource.Id).Count()+1;
                                _operation.Source = _projectInfo.SelectedResource.Id;
                                _operation.Parameters = new List<Parameter>();
                                _operation.Index = index;
                                _dbContext.Operations.Add(_operation);
                                _dbContext.SaveChanges();
                                for(int i = 0; i < _inputs.Count; i++)
                                {
                                    _inputs[i].Operation = _operation.Id;
                                }
                                for (int i = 0; i < _outputs.Count; i++)
                                {
                                    _outputs[i].Operation = _operation.Id;
                                }
                                _dbContext.Parameters.AddRange(_inputs);
                                _dbContext.Parameters.AddRange(_outputs);
                                _dbContext.SaveChanges();
                            }
                        }
                        _projectInfo.NoticeResourceUpdated(null);
                        if (_close != null)_close();
                    }
                });
            }
        }
        private bool IsConditionValid(Parameter parameter, int num)
        {
            var regex = new Regex(@"(?<![<>=≠])(<|>|<=|>=|=|≠)(?![<>=≠])");
            var defParts = regex.Split(parameter.Value);
            var expressionLeft = parser.Parse(defParts[0]);
            var expressionRight = parser.Parse(defParts[2]);
            var leftUndefinedVars = expressionLeft.GetVariables()
                                .Except(_variables).DefaultIfEmpty(string.Empty).Aggregate((m, n) => m + ", " + n);
            string error = "";
            if (leftUndefinedVars.Length > 0) error = $"Переменные в левой части условия ({leftUndefinedVars}) не определены\n";
            var rightUndefinedVars = expressionLeft.GetVariables()
                                .Except(_variables).DefaultIfEmpty(string.Empty).Aggregate((m, n) => m + ", " + n);
            if (leftUndefinedVars.Length > 0) error += $"Переменные в правой части условия ({rightUndefinedVars}) не определены";
            if (error.Length > 0) MessageBox.Show($"Ошибка в параметре №{num}\n{error}");
            return leftUndefinedVars.Length == 0 && rightUndefinedVars.Length == 0;
        }
        private bool IsDefinitionValid(Parameter parameter, int num)
        {
            var defParts = parameter.Value.Split(":=");
            var variable = defParts[0];
            var expression = parser.Parse(defParts[1]);
            bool result = true;
            string Error = "";
            if (!_variables.Contains(variable))
            {
                Error = "Не выбрана переменная для записи значения выражения\n";
                result = false;
            }
            var undefinedVars = expression.GetVariables()
                                .Except(_variables).DefaultIfEmpty(string.Empty).Aggregate((m, n) => m + ", " + n);
            if (undefinedVars.Length > 0)
            {
                Error += $"Переменные ({undefinedVars}) не определены";
                result = false;
            }
            if (Error.Length > 0) MessageBox.Show($"Ошибка в параметре №{num}\n{Error}");
            return result;
        }
        private bool IsExpressionValid(Parameter parameter, int num)
        {
            var expression = parser.Parse(parameter.Value);
            var undefinedVars = expression.GetVariables()
                                .Except(_variables).DefaultIfEmpty(string.Empty).Aggregate((m, n) => m + ", " + n);
            if (undefinedVars.Length > 0)
            {
                MessageBox.Show($"Ошибка в параметре №{num}\nПеременные ({undefinedVars}) не определены");
            }
            return undefinedVars.Length == 0;
        }
    }
}
