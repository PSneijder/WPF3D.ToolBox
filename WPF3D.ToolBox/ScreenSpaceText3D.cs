using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace WPF3D.ToolBox
{
    public sealed class ScreenSpaceText3D
        : ModelVisual3D
    {
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register("Color", typeof(Color), typeof(ScreenSpaceText3D), new PropertyMetadata(Colors.White, OnColorChanged));

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(ScreenSpaceText3D), new PropertyMetadata(string.Empty, OnTextChanged));

        public static readonly DependencyProperty PointProperty =
            DependencyProperty.Register("Point", typeof(Point3D), typeof(ScreenSpaceText3D), new PropertyMetadata(default(Point3D), OnPointChanged));

        public Color Color
        {
            get { return (Color)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        public string Text
        {
            get { return (string) GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public Point3D Point
        {
            get { return (Point3D) GetValue(PointProperty); }
            set { SetValue(PointProperty, value); }
        }

        private static void OnColorChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {

        }

        private static void OnTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            ((ScreenSpaceText3D) sender).SetText((string) args.NewValue);
        }

        private static void OnPointChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            
        }

        private void SetText(string value)
        {
            Model3D model = CreateTextLabel3D(value, new SolidColorBrush(Color), true, 1, Point, new Vector3D(0,0,1), new Vector3D(0,1,0));
            Content = model;
        }

        /// <summary>
        /// Creates a Model3D containing a text label.
        /// </summary>
        /// <param name="text">The string</param>
        /// <param name="color">The color of the text.</param>
        /// <param name="isDoubleSided">Visible from both sides?</param>
        /// <param name="height">Height of the characters</param>
        /// <param name="center">The center of the label</param>
        /// <param name="over">Horizontal direction of the label</param>
        /// <param name="up">Vertical direction of the label</param>
        /// <returns>Suitable for adding to your Viewport3D</returns>
        public static Model3D CreateTextLabel3D(string text, Brush color, bool isDoubleSided, double height, Point3D center, Vector3D over, Vector3D up)
        {
            TextBlock textBlock = new TextBlock(new Run(text))
            {
                Foreground = color,
                FontFamily = new FontFamily("Arial")
            };

            DiffuseMaterial material = new DiffuseMaterial
            {
                Brush = new VisualBrush(textBlock)
            };

            // We just assume the characters are square
            double width = text.Length * height;

            // Since the parameter coming in was the center of the label,
            // we need to find the four corners
            // p0 is the lower left corner
            // p1 is the upper left
            // p2 is the lower right
            // p3 is the upper right
            Point3D p0 = center - width / 2 * over - height / 2 * up;
            Point3D p1 = p0 + up * 1 * height;
            Point3D p2 = p0 + over * width;
            Point3D p3 = p0 + up * 1 * height + over * width;

            // Now build the geometry for the sign.  It's just a
            // rectangle made of two triangles, on each side.
            MeshGeometry3D geometry = new MeshGeometry3D
            {
                Positions = new Point3DCollection
                {
                    // 0
                    // 1
                    // 2
                    // 3
                    p0, p1, p2, p3
                }
            };

            if (isDoubleSided)
            {
                geometry.Positions.Add(p0);    // 4
                geometry.Positions.Add(p1);    // 5
                geometry.Positions.Add(p2);    // 6
                geometry.Positions.Add(p3);    // 7
            }

            geometry.TriangleIndices.Add(0);
            geometry.TriangleIndices.Add(3);
            geometry.TriangleIndices.Add(1);
            geometry.TriangleIndices.Add(0);
            geometry.TriangleIndices.Add(2);
            geometry.TriangleIndices.Add(3);

            if (isDoubleSided)
            {
                geometry.TriangleIndices.Add(4);
                geometry.TriangleIndices.Add(5);
                geometry.TriangleIndices.Add(7);
                geometry.TriangleIndices.Add(4);
                geometry.TriangleIndices.Add(7);
                geometry.TriangleIndices.Add(6);
            }

            // These texture coordinates basically stretch the
            // TextBox brush to cover the full side of the label.
            geometry.TextureCoordinates.Add(new Point(0, 1));
            geometry.TextureCoordinates.Add(new Point(0, 0));
            geometry.TextureCoordinates.Add(new Point(1, 1));
            geometry.TextureCoordinates.Add(new Point(1, 0));

            if (isDoubleSided)
            {
                geometry.TextureCoordinates.Add(new Point(1, 1));
                geometry.TextureCoordinates.Add(new Point(1, 0));
                geometry.TextureCoordinates.Add(new Point(0, 1));
                geometry.TextureCoordinates.Add(new Point(0, 0));
            }

            return new GeometryModel3D(geometry, material); ;
        }
    }
}