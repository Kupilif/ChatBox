using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace CommonClasses
{
    public class CColorsLoader
    {
        private static CColorsLoader instance;
        
        public static CColorsLoader GetInstance()
        {
            if (instance == null)
            {
                instance = new CColorsLoader();
            }
            return instance;
        }

        public Color[] DefaultColors = new Color[]
        {
            Colors.Aqua, Colors.Blue, Colors.BlueViolet, Colors.Brown, Colors.Chocolate, Colors.Crimson, Colors.DarkBlue, Colors.DarkCyan,
            Colors.DarkGreen, Colors.DarkMagenta, Colors.DarkOliveGreen, Colors.DarkOrange, Colors.DarkOrchid, Colors.DarkRed, Colors.DarkSlateBlue,
            Colors.DarkSlateGray, Colors.DarkViolet, Colors.DeepPink, Colors.DeepSkyBlue, Colors.Firebrick, Colors.ForestGreen, Colors.Fuchsia,
            Colors.Green, Colors.Indigo, Colors.Lime, Colors.Magenta, Colors.Maroon, Colors.Navy, Colors.OrangeRed, Colors.Purple
        };

        public Color ClrError { get; private set; }
        public Color ClrDefault { get; private set; }
        public Color ClrLabelText { get; private set; }
        public Color ClrIPandPort { get; private set; }
        public Color ClrServerStatusOpen { get; private set; }
        public Color ClrServerStatusClosed { get; private set; }
        public Color ClrServerStatusStopped { get; private set; }
        public Color ClrClientStatusActive { get; private set; }
        public Color ClrClientStatusBanned { get; private set; }
        public Color ClrClientStatusDisconnected { get; private set; }
        public Color clrOK { get; private set; }
        public Color clrWRONG { get; private set; }
        public sbyte ClrErrorInd { get; private set; }
        public sbyte ClrDefaultInd { get; private set; }

        private const byte ALPHA_CHANNEL = 32;
        private readonly Color ACTIVE_USER_BTN_COLOR = Colors.Lime;
        private readonly Color BANNED_USER_BTN_COLOR = Colors.Red;

        private List<Color> palette;

        private CColorsLoader()
        {
            palette = new List<Color>();
            ClrDefault = Colors.Black;
            ClrError = Colors.Red;
            ClrClientStatusActive = Colors.Green;
            ClrClientStatusBanned = Colors.Orange;
            ClrClientStatusDisconnected = Colors.Red;
            ClrLabelText = Colors.Blue;
            ClrIPandPort = Colors.Brown;
            ClrServerStatusClosed = Colors.Orange;
            ClrServerStatusStopped = Colors.Red;
            ClrServerStatusOpen = Colors.Green;
            clrOK = Color.FromArgb(ALPHA_CHANNEL, Colors.Lime.R, Colors.Lime.G, Colors.Lime.B);
            clrWRONG = Color.FromArgb(ALPHA_CHANNEL, Colors.Red.R, Colors.Red.G, Colors.Red.B);
            ClrErrorInd = -1;
            ClrDefaultInd = -2;

            SetStandartPalette();
        }

        /*  Подпрограмма перевода заданного числа в цвет  */
        public Color GetColorForID(sbyte id)
        {
            Color res;

            res = ClrDefault;
            if (ClrDefaultInd == id)
            {
                return ClrDefault;
            }
            if (ClrErrorInd == id)
            {
                return ClrError;
            }

            if ((id >= 0) && (id < palette.Count))
            {
                return palette[id];
            }
            else
            {
                return ClrDefault;
            }
        }

        public void SetPalette(Color[] colors)
        {
            palette.Clear();
            for (int i = 0; i < colors.Length; i++)
            {
                palette.Add(colors[i]);
            }
        }

        public Color[] GetCurrentPalette()
        {
            return palette.ToArray();
        }

        private void SetStandartPalette()
        {
            palette.Clear();
            for (int i = 0; i < DefaultColors.Length; i++)
            {
                palette.Add(DefaultColors[i]);
            }
        }

        public Grid GetPicForBannedUser(double size)
        {
            Grid grid = new Grid();
            grid.Width = size - 5;
            grid.Height = size - 5;

            Ellipse face = new Ellipse();
            face.Height = size - 5;
            face.Width = size - 5;
            face.Fill = new SolidColorBrush(BANNED_USER_BTN_COLOR);
            face.VerticalAlignment = VerticalAlignment.Center;
            face.HorizontalAlignment = HorizontalAlignment.Center;
            face.Stroke = new SolidColorBrush(Colors.Black);

            grid.Children.Add(face);
            return grid;
        }

        public Grid GetPicForActiveUser(double size)
        {
            Grid grid = new Grid();
            grid.Width = size - 5;
            grid.Height = size - 5;

            Ellipse face = new Ellipse();
            face.Height = size - 5;
            face.Width = size - 5;
            face.Fill = new SolidColorBrush(ACTIVE_USER_BTN_COLOR);
            face.VerticalAlignment = VerticalAlignment.Center;
            face.HorizontalAlignment = HorizontalAlignment.Center;
            face.Stroke = new SolidColorBrush(Colors.Black);

            grid.Children.Add(face);
            return grid;
        }

        public Grid GetSmileDraw(double size)
        {
            Grid grid = new Grid();
            grid.Width = size - 5;
            grid.Height = size - 5;

            Ellipse face = new Ellipse();
            face.Height = size - 5;
            face.Width = size - 5;
            face.Fill = new SolidColorBrush(Colors.Yellow);
            face.VerticalAlignment = VerticalAlignment.Center;
            face.HorizontalAlignment = HorizontalAlignment.Center;
            face.Stroke = new SolidColorBrush(Colors.Black);

            Ellipse eye1 = new Ellipse();
            eye1.Height = size / 7;
            eye1.Width = size / 7;
            eye1.Fill = new SolidColorBrush(Colors.Black);
            eye1.VerticalAlignment = VerticalAlignment.Center;
            eye1.HorizontalAlignment = HorizontalAlignment.Center;
            eye1.Margin = new Thickness(-(size / 4), -(size / 4), 0, 0);

            Ellipse eye2 = new Ellipse();
            eye2.Height = size / 7;
            eye2.Width = size / 7;
            eye2.Fill = new SolidColorBrush(Colors.Black);
            eye2.VerticalAlignment = VerticalAlignment.Center;
            eye2.HorizontalAlignment = HorizontalAlignment.Center;
            eye2.Margin = new Thickness(0, -(size / 4), -(size / 4), 0);

            Line mouth = new Line();
            mouth.X1 = 0;
            mouth.Y1 = 0;
            mouth.X2 = size / 3;
            mouth.Y2 = 0;
            mouth.Stroke = new SolidColorBrush(Colors.Black);
            mouth.VerticalAlignment = VerticalAlignment.Center;
            mouth.HorizontalAlignment = HorizontalAlignment.Center;
            mouth.Margin = new Thickness(0, 0, 0, (-size / 4));

            grid.Children.Add(face);
            grid.Children.Add(eye1);
            grid.Children.Add(eye2);
            grid.Children.Add(mouth);
            return grid;
        }

        public Grid GetPicForDisconnectBtn(double size)
        {
            Grid grid = new Grid();
            grid.Width = size - 5;
            grid.Height = size - 5;

            Line line1 = new Line();
            line1.X1 = 0;
            line1.Y1 = 0;
            line1.X2 = size - 5;
            line1.Y2 = size - 5;
            line1.Stroke = new SolidColorBrush(Colors.Red);
            line1.StrokeThickness = 3;

            Line line2 = new Line();
            line2.X1 = size - 5;
            line2.Y1 = 0;
            line2.X2 = 0;
            line2.Y2 = size - 5;
            line2.Stroke = new SolidColorBrush(Colors.Red);
            line2.StrokeThickness = 3;

            grid.Children.Add(line1);
            grid.Children.Add(line2);
            return grid;
        }
    }
}
