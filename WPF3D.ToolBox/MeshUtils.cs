using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace WPF3D.ToolBox
{
    #region Mesh IValueConverters

    /// <summary>
    /// Abstract base class for all type converters that have a MeshGeometry3D source
    /// </summary>
    public abstract class MeshConverter<TArgetType>
        : IValueConverter
    {
        /// <summary>
        /// IValueConverter.Convert
        /// </summary>
        /// <param name="value">The binding source (should be a MeshGeometry3D)</param>
        /// <param name="targetType">The binding target</param>
        /// <param name="parameter">Optionaly parameter to the converter</param>
        /// <param name="culture">(ignored)</param>
        /// <returns>
        /// The converted value
        /// </returns>
        /// <exception cref="System.ArgumentException">MeshConverter can only convert from a MeshGeometry3D</exception>
        object IValueConverter.Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
            {
                return DependencyProperty.UnsetValue;
            }

            if (targetType != typeof(TArgetType))
            {
                throw new ArgumentException($"MeshConverter must target a {typeof(TArgetType).Name}");
            }

            MeshGeometry3D mesh = value as MeshGeometry3D;
            if (mesh == null)
            {
                throw new ArgumentException("MeshConverter can only convert from a MeshGeometry3D");
            }

            return Convert(mesh, parameter);
        }

        /// <summary>
        /// IValueConverter.ConvertBack
        /// Not implemented
        /// </summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Subclasses should override this to do conversion
        /// </summary>
        /// <param name="mesh">The mesh source</param>
        /// <param name="parameter">Optional converter argument</param>
        /// <returns>
        /// The converted value
        /// </returns>
        public abstract object Convert(MeshGeometry3D mesh, object parameter);
    }

    /// <summary>
    /// MeshTextureCoordinateConverter
    /// A MeshConverter that returns a PointCollection and takes an optional direction argument
    /// </summary>
    public abstract class MeshTextureCoordinateConverter
        : MeshConverter<PointCollection>
    {
        /// <summary>
        /// Subclasses should override this to do conversion
        /// </summary>
        /// <param name="mesh">The source mesh</param>
        /// <param name="parameter">The optional parameter</param>
        /// <returns>
        /// The converted value
        /// </returns>
        /// <exception cref="System.ArgumentException">Parameter must be a string.</exception>
        public override object Convert(MeshGeometry3D mesh, object parameter)
        {
            string paramAsString = parameter as string;
            if (parameter != null && paramAsString == null)
            {
                throw new ArgumentException("Parameter must be a string.");
            }

            // Default to the positive Y axis
            Vector3D dir = MathUtils.YAxis;

            if (paramAsString != null)
            {
                dir = Vector3D.Parse(paramAsString);
                MathUtils.TryNormalize(ref dir);
            }

            return Convert(mesh, dir);
        }

        /// <summary>
        /// Subclasses should override this to do conversion
        /// </summary>
        /// <param name="mesh">The source mesh</param>
        /// <param name="dir">The normalized direction parameter</param>
        /// <returns></returns>
        public abstract object Convert(MeshGeometry3D mesh, Vector3D dir);
    }

    /// <summary>
    /// IValueConverter that generates texture coordinates for a plane.
    /// </summary>
    public class PlanarTextureCoordinateGenerator
        : MeshTextureCoordinateConverter
    {
        public override object Convert(MeshGeometry3D mesh, Vector3D dir)
        {
            return MeshUtils.GeneratePlanarTextureCoordinates(mesh, dir);
        }
    }

    /// <summary>
    /// IValueConverter that generates texture coordinates for a sphere.
    /// </summary>
    public class SphericalTextureCoordinateGenerator
        : MeshTextureCoordinateConverter
    {
        public override object Convert(MeshGeometry3D mesh, Vector3D dir)
        {
            return MeshUtils.GenerateSphericalTextureCoordinates(mesh, dir);
        }
    }

    /// <summary>
    /// IValueConverter that generates texture coordinates for a cylinder
    /// </summary>
    public class CylindricalTextureCoordinateGenerator
        : MeshTextureCoordinateConverter
    {
        public override object Convert(MeshGeometry3D mesh, Vector3D dir)
        {
            return MeshUtils.GenerateCylindricalTextureCoordinates(mesh, dir);
        }
    }

    #endregion

    public static class MeshUtils
    {
        #region Texture Coordinate Generation Methods

        /// <summary>
        /// Generates texture coordinates as if mesh were a cylinder.
        /// Notes:
        /// 1) v is flipped for you automatically
        /// 2) 'mesh' is not modified. If you want the generated coordinates to be assigned to mesh, do: mesh.TextureCoordinates = GenerateCylindricalTextureCoordinates(mesh, foo)
        /// </summary>
        /// <param name="mesh">The mesh</param>
        /// <param name="dir">The axis of rotation for the cylinder</param>
        /// <returns>
        /// The generated texture coordinates
        /// </returns>
        public static PointCollection GenerateCylindricalTextureCoordinates(MeshGeometry3D mesh, Vector3D dir)
        {
            if (mesh == null)
            {
                return null;
            }

            Rect3D bounds = mesh.Bounds;
            int count = mesh.Positions.Count;
            PointCollection texcoords = new PointCollection(count);
            IEnumerable<Point3D> positions = TransformPoints(ref bounds, mesh.Positions, ref dir);

            foreach (Point3D vertex in positions)
            {
                texcoords.Add(new Point(GetUnitCircleCoordinate(-vertex.Z, vertex.X),
                                        1.0 - GetPlanarCoordinate(vertex.Y, bounds.Y, bounds.SizeY)));
            }

            return texcoords;
        }

        /// <summary>
        /// Generates texture coordinates as if mesh were a sphere.
        /// Notes:
        /// 1) v is flipped for you automatically
        /// 2) 'mesh' is not modified. If you want the generated coordinates to be assigned to mesh, do: mesh.TextureCoordinates = GenerateSphericalTextureCoordinates(mesh, foo)
        /// </summary>
        /// <param name="mesh">The mesh</param>
        /// <param name="dir">The axis of rotation for the sphere</param>
        /// <returns>
        /// The generated texture coordinates
        /// </returns>
        public static PointCollection GenerateSphericalTextureCoordinates(MeshGeometry3D mesh, Vector3D dir)
        {
            if (mesh == null)
            {
                return null;
            }

            Rect3D bounds = mesh.Bounds;
            int count = mesh.Positions.Count;
            PointCollection texcoords = new PointCollection(count);
            IEnumerable<Point3D> positions = TransformPoints(ref bounds, mesh.Positions, ref dir);

            foreach (Point3D vertex in positions)
            {
                // Don't need to do 'vertex - center' since TransformPoints put us
                // at the origin
                Vector3D radius = new Vector3D(vertex.X, vertex.Y, vertex.Z);
                MathUtils.TryNormalize(ref radius);

                texcoords.Add(new Point(GetUnitCircleCoordinate(-radius.Z, radius.X),
                                        1.0 - (Math.Asin(radius.Y) / Math.PI + 0.5)));
            }

            return texcoords;
        }

        /// <summary>
        /// Generates texture coordinates as if mesh were a plane.
        /// Notes:
        /// 1) v is flipped for you automatically
        /// 2) 'mesh' is not modified. If you want the generated coordinates to be assigned to mesh, do: mesh.TextureCoordinates = GeneratePlanarTextureCoordinates(mesh, foo)
        /// </summary>
        /// <param name="mesh">The mesh</param>
        /// <param name="dir">The normal of the plane</param>
        /// <returns>
        /// The generated texture coordinates
        /// </returns>
        public static PointCollection GeneratePlanarTextureCoordinates(MeshGeometry3D mesh, Vector3D dir)
        {
            if (mesh == null)
            {
                return null;
            }

            Rect3D bounds = mesh.Bounds;
            int count = mesh.Positions.Count;
            PointCollection texcoords = new PointCollection(count);
            IEnumerable<Point3D> positions = TransformPoints(ref bounds, mesh.Positions, ref dir);

            foreach (Point3D vertex in positions)
            {
                // The plane is looking along positive Y, so Z is really Y
                texcoords.Add(new Point(GetPlanarCoordinate(vertex.X, bounds.X, bounds.SizeX),
                                        GetPlanarCoordinate(vertex.Z, bounds.Z, bounds.SizeZ)));
            }

            return texcoords;
        }

        #endregion

        internal static double GetPlanarCoordinate(double end, double start, double width)
        {
            return (end - start) / width;
        }

        internal static double GetUnitCircleCoordinate(double y, double x)
        {
            return Math.Atan2(y, x) / (2.0 * Math.PI) + 0.5;
        }

        /// <summary>
        /// Transforms the points.
        /// </summary>
        /// <param name="bounds">The bounds.</param>
        /// <param name="points">The points.</param>
        /// <param name="dir">The dir.</param>
        /// <returns></returns>
        internal static IEnumerable<Point3D> TransformPoints(ref Rect3D bounds, Point3DCollection points, ref Vector3D dir)
        {
            if (dir == MathUtils.YAxis)
            {
                return points;
            }

            Vector3D rotAxis = Vector3D.CrossProduct(dir, MathUtils.YAxis);
            double rotAngle = Vector3D.AngleBetween(dir, MathUtils.YAxis);
            Quaternion q;

            if (Math.Abs(rotAxis.X) > 0.1 || Math.Abs(rotAxis.Y) > 0.1 || Math.Abs(rotAxis.Z) > 0.1)
            {
                Debug.Assert(Math.Abs(rotAngle) > 0.1);

                q = new Quaternion(rotAxis, rotAngle);
            }
            else
            {
                Debug.Assert(dir == -MathUtils.YAxis);

                q = new Quaternion(MathUtils.XAxis, rotAngle);
            }

            Vector3D center = new Vector3D(bounds.X + bounds.SizeX / 2,
                                           bounds.Y + bounds.SizeY / 2,
                                           bounds.Z + bounds.SizeZ / 2);

            Matrix3D t = Matrix3D.Identity;
            t.Translate(-center);
            t.Rotate(q);

            int count = points.Count;
            Point3D[] transformedPoints = new Point3D[count];

            for (int i = 0; i < count; i++)
            {
                transformedPoints[i] = t.Transform(points[i]);
            }

            // Finally, transform the bounds too
            bounds = MathUtils.TransformBounds(bounds, t);

            return transformedPoints;
        }
    }
}