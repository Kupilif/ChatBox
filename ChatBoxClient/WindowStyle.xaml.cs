using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ChatBoxClient
{
        public partial class WindowStyle : ResourceDictionary
        {
            public WindowStyle()
            {
                InitializeComponent();
            }

            private void Close_Click(object sender, RoutedEventArgs e)
            {
                SystemCommands.CloseWindow(Window.GetWindow(sender as Button));
            }

            private void Maximize_Click(object sender, RoutedEventArgs e)
            {
                Window window = Window.GetWindow(Window.GetWindow(sender as Button));
                if (window.WindowState == WindowState.Normal)
                {
                    SystemCommands.MaximizeWindow(window);
                }
                else if (window.WindowState == WindowState.Maximized)
                {
                    SystemCommands.RestoreWindow(window);
                }
            }

            private void Minimize_Click(object sender, RoutedEventArgs e)
            {
                SystemCommands.MinimizeWindow(Window.GetWindow(sender as Button));
            }
        }
    }
