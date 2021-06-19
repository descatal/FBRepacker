
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
namespace Util.Behaviours
{
    public class OnlyDigitalBehavior : Behavior<TextBox>
    {
        private string lastRight = null;

        public Type DigitalType
        {
            get
            {
                return (Type)GetValue(DigitalTypeProperty);
            }
            set
            {
                SetValue(DigitalTypeProperty, value);
            }
        }

        public static readonly DependencyProperty DigitalTypeProperty =
            DependencyProperty.Register("DigitalType", typeof(Type), typeof(OnlyDigitalBehavior), new PropertyMetadata());


        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.TextChanged += AssociatedObject_TextChanged;
            InputMethod.SetIsInputMethodEnabled(this.AssociatedObject, false);
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            this.AssociatedObject.TextChanged -= AssociatedObject_TextChanged;
        }

        private void AssociatedObject_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            Type digitalType = this.DigitalType;
            if (textBox == null)
            {
                return;
            }
            if ((IsDigital(digitalType, textBox.Text) || string.IsNullOrEmpty(textBox.Text)) && lastRight != textBox.Text)
            {
                lastRight = textBox.Text;
            }
            else if (textBox.Text != lastRight)
            {
                textBox.Text = lastRight;
                textBox.SelectionStart = textBox.Text.Length;
            }
        }

        private bool IsDigital(Type targetType, string digitalString)
        {
            if (digitalString == "-")
            {
                return true;
            }
            if (targetType == typeof(Int16))
            {
                Int16 i = 0;
                if (Int16.TryParse(digitalString, out i))
                {
                    return true;
                }
            }
            else if (targetType == typeof(Int32))
            {
                Int32 i = 0;
                if (Int32.TryParse(digitalString, out i))
                {
                    return true;
                }
            }
            else if (targetType == typeof(Int64))
            {
                Int64 i = 0;
                if (Int64.TryParse(digitalString, out i))
                {
                    return true;
                }
            }
            else if (targetType == typeof(float))
            {
                float f = 0;
                if (float.TryParse(digitalString, out f))
                {
                    return true;
                }
            }
            else if (targetType == typeof(double))
            {
                double d = 0;
                if (double.TryParse(digitalString, out d))
                {
                    return true;
                }
            }
            else if (targetType == typeof(decimal))
            {
                decimal d = 0;
                if (decimal.TryParse(digitalString, out d))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
