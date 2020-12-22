using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace ytbrowser.common {
    public class NegBoolConverter : IValueConverter {
        #region IValueConverter Members

        /**
         * bool --> !bool
         */
        public object Convert(object value, Type targetType, object parameter, string language) {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return DependencyProperty.UnsetValue;
        }

        #endregion
    }

    /**
     * bool --> Visibility
     */
    public class BoolVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if ((bool)value) {
                return Visibility.Visible;
            } else {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return DependencyProperty.UnsetValue;
        }
    }

    /**
     * bool --> Visibility
     */
    public class NegBoolVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (!(bool)value) {
                return Visibility.Visible;
            } else {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return DependencyProperty.UnsetValue;
        }
    }

    /**
     * enum値 <--> Visibility
     * ConverterParameter で渡されたenum値と一致すればVisible
     */
    public class EnumVisibilityConverter : IValueConverter {
        /**
         * enum値(value)とparameterが等しければ Visible, 等しくなければ Collaplsedを返す
         */
        public object Convert(object value, Type targetType, object parameter, string culture) {

            string ParameterString = parameter as string;
            if (ParameterString == null) {
                return DependencyProperty.UnsetValue;
            }

            if (Enum.IsDefined(value.GetType(), value) == false) {
                return DependencyProperty.UnsetValue;
            }

            object paramvalue = Enum.Parse(value.GetType(), ParameterString);

            if (paramvalue.Equals(value)) {
                return Visibility.Visible;
            } else {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string culture) {
            string ParameterString = parameter as string;
            if (ParameterString == null) {
                return DependencyProperty.UnsetValue;
            }

            return Enum.Parse(targetType, ParameterString);
        }
    }

    public class NegEnumVisibilityConverter : IValueConverter {
        /**
         * enum値(value)とparameterが等しくなければ Visible, 等しければ Collaplsedを返す
         */
        public object Convert(object value, Type targetType, object parameter, string culture) {

            string ParameterString = parameter as string;
            if (ParameterString == null) {
                return DependencyProperty.UnsetValue;
            }

            if (Enum.IsDefined(value.GetType(), value) == false) {
                return DependencyProperty.UnsetValue;
            }

            object paramvalue = Enum.Parse(value.GetType(), ParameterString);

            if (!paramvalue.Equals(value)) {
                return Visibility.Visible;
            } else {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string culture) {
            return DependencyProperty.UnsetValue;
        }
    }
}
