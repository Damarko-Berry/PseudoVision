using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace PVChannelManager
{
    public class OmniButton<T> : Button
    {
        public OmniButton(T param, OmniClick<T> omniClick)
        {
            Click += SrtingClicked;
            ob = param;
            GetClick += omniClick;
        }
        public OmniClick<T> GetClick;
        T ob;
        private void SrtingClicked(object sender, RoutedEventArgs e)
        {
            GetClick.Invoke(ob);
        }

    }
    public delegate void OmniClick<T>(T x);
    
}
