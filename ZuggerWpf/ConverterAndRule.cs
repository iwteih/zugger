using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows;

namespace ZuggerWpf
{
    public class Count2Background : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            int num = int.Parse(value.ToString());

            if (num == 0)
            {
                return Brushes.Transparent;
            }
            else if (num <= 5)
            {
                return Brushes.Orange;
            }
            else if (num > 5 && num <= 10)
            {
                return Brushes.Tomato;
            }
            else
            {
                return Brushes.Red;
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class Date2Color : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            DateTime date;

            if (!DateTime.TryParse(value.ToString(), out date))
            {
                date = DateTime.Now;
            }

            int days = (DateTime.Now - date).Days;

            if (days <= 3)
            {
                return Brushes.Black;
            }
            else if (days > 3 && days < 7)
            {
                return Brushes.Orange;
            }
            else
            {
                return Brushes.Red;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class TaskDate2Color : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            DateTime dateline;

            if (DateTime.TryParse(value.ToString(), out dateline))
            {
                if (dateline < DateTime.Now)
                {
                    return Brushes.Red;
                }
            }

            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class Datasource2Visible : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            if (value == null)
            {
                return Visibility.Collapsed;
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class Bool2Visible : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            bool v;
            if (bool.TryParse(value.ToString(), out v))
            {
                return !v;
            }

            return v;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            bool v;

            if(bool.TryParse(value.ToString(), out v))
            {
                return v;
            }

            return v;
        }
    }

    public class Count2Visible : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            int v;
            if (int.TryParse(value.ToString(), out v))
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class HeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            double maxHeight;

            if (double.TryParse(value.ToString(), out maxHeight))
            {
                double para;
                if (parameter != null && double.TryParse(parameter.ToString(), out para))
                {
                    return maxHeight * para;
                }
            }

            return maxHeight;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class MinuteRule : ValidationRule
    {
        private int _min;
        private int _max;

        public MinuteRule()
        {
        }

        public int Min
        {
            get { return _min; }
            set { _min = value; }
        }

        public int Max
        {
            get { return _max; }
            set { _max = value; }
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            int age = 0;

            try
            {
                if (((string)value).Length > 0)
                    age = Int32.Parse(value.ToString());
            }
            catch
            {
                return new ValidationResult(false, "请输入数字");
            }

            if ((age < Min) || (age > Max))
            {
                return new ValidationResult(false,
                  "请输入正确的分钟数，范围: " + Min + " - " + Max + "");
            }
            else
            {
                return new ValidationResult(true, null);
            }
        }
    }
}
