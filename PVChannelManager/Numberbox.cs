using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Controls;
using System.Windows.Input;

namespace PVChannelManager
{
    public class NumberBox : TextBox
    {
        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            base.OnPreviewTextInput(e);
            e.Handled = !IsTextAllowed(e.Text);
        }

        private static bool IsTextAllowed(string text)
        {
            foreach (char c in text)
            {
                if (!char.IsDigit(c) && c != '.')
                {
                    return false;
                }
            }
            return true;
        }
        public double ToDouble()
        {
            var D = double.Parse(Text);
            return Convert.ToDouble(Text);
        }
        protected override void OnTextChanged(TextChangedEventArgs e)
        {
            base.OnTextChanged(e);
            if (!IsTextAllowed(this.Text))
            {
                this.Text = string.Empty;
            }
        }

        public static implicit operator double(NumberBox box)
        {
            var D = double.Parse(box.Text);
            return D;
        }
    }
}
