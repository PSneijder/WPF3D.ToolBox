using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace WPF3D.ToolBox.Controls
{
    /// <summary>
    /// Trackport3D loads a Model3D from a xaml file and displays it.  The user
    /// may rotate the view by dragging the mouse with the left mouse button.
    /// Dragging with the right mouse button will zoom in and out.
    /// Trackport3D is primarily an example of how to use the Trackball utility
    /// class, but it may be used as a custom control in your own applications.
    /// </summary>
    public partial class Trackport3D
    {
        private readonly Trackball _trackball = new Trackball();
        private readonly ScreenSpaceLines3D _wireframe = new ScreenSpaceLines3D();

        private ViewMode _viewMode;

        public Trackport3D()
        {
            InitializeComponent();

            Viewport.Children.Add(_wireframe);
            Camera.Transform = _trackball.Transform;
            Headlight.Transform = _trackball.Transform;
        }

        public Color HeadlightColor
        {
            get { return Headlight.Color; }
            set { Headlight.Color = value; }
        }

        public Color AmbientLightColor
        {
            get { return AmbientLight.Color; }
            set { AmbientLight.Color = value; }
        }

        public ViewMode ViewMode
        {
            get { return _viewMode; }
            set
            {
                _viewMode = value;
                SetupScene();
            }
        }

        private void SetupScene()
        {
            switch (ViewMode)
            {
                case ViewMode.Solid:
                    _wireframe.Points.Clear();
                    break;

                case ViewMode.Wireframe:
                    Root.Content = null;
                    _wireframe.MakeWireframe(Root.Content);
                    break;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Viewport3Ds only raise events when the mouse is over the rendered 3D geometry.
            // In order to capture events whenever the mouse is over the client are we use a
            // same sized transparent Border positioned on top of the Viewport3D.

            _trackball.EventSource = Viewport;
        }
    }
}