﻿using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace WPF3D.ToolBox
{
    /// <summary>
    /// This class enables a Viewport3D to be enhanced by allowing UIElements to be placed
    /// behind and in front of the Viewport3D.  These can then be used for various enhancements.
    /// For examples see the Trackball, or InteractiveViewport3D.
    /// </summary>
    [ContentProperty("Content")]
    public abstract class Viewport3DDecorator
        : FrameworkElement
            , IAddChild
    {
        private readonly UIElementCollection _preViewportChildren;
        private readonly UIElementCollection _postViewportChildren;
        private UIElement _content;

        /// <summary>
        /// Creates the Viewport3DDecorator
        /// </summary>
        protected Viewport3DDecorator()
        {
            // create the two lists of children
            _preViewportChildren = new UIElementCollection(this, this);
            _postViewportChildren = new UIElementCollection(this, this);

            // no content yet
            _content = null;
        }

        /// <summary>
        /// The content/child of the Viewport3DDecorator.  A Viewport3DDecorator only has one
        /// child and this child must be either another Viewport3DDecorator or a Viewport3D.
        /// </summary>
        /// <value>
        /// The content.
        /// </value>
        public UIElement Content
        {
            get
            {
                return _content;
            }

            set
            {
                // check to make sure it is a Viewport3D or a Viewport3DDecorator                
                if (!(value is Viewport3D || value is Viewport3DDecorator))
                {
                    throw new ArgumentException("Not a valid child type", nameof(value));
                }

                // check to make sure we're attempting to set something new
                if (_content != value)
                {
                    UIElement oldContent = _content;
                    UIElement newContent = value;

                    // remove the previous child
                    RemoveVisualChild(oldContent);
                    RemoveLogicalChild(oldContent);

                    // set the private variable
                    _content = value;

                    // link in the new child
                    AddLogicalChild(newContent);
                    AddVisualChild(newContent);

                    // let anyone know that derives from us that there was a change
                    OnViewport3DDecoratorContentChange(oldContent, newContent);

                    // data bind to what is below us so that we have the same width/height
                    // as the Viewport3D being enhanced
                    // create the bindings now for use later
                    BindToContentsWidthHeight(newContent);

                    // Invalidate measure to indicate a layout update may be necessary
                    InvalidateMeasure();
                }
            }
        }

        /// <summary>
        /// Data binds the (Max/Min)Width and (Max/Min)Height properties to the same
        /// ones as the content.  This will make it so we end up being sized to be
        /// exactly the same ActualWidth and ActualHeight as waht is below us.
        /// </summary>
        /// <param name="newContent">What to bind to</param>
        private void BindToContentsWidthHeight(UIElement newContent)
        {
            // bind to width height
            Binding widthBinding = new Binding("Width");
            widthBinding.Mode = BindingMode.OneWay;
            Binding heightBinding = new Binding("Height");
            heightBinding.Mode = BindingMode.OneWay;

            widthBinding.Source = newContent;
            heightBinding.Source = newContent;

            BindingOperations.SetBinding(this, WidthProperty, widthBinding);
            BindingOperations.SetBinding(this, HeightProperty, heightBinding);


            // bind to max width and max height
            Binding _maxWidthBinding = new Binding("MaxWidth");
            _maxWidthBinding.Mode = BindingMode.OneWay;
            Binding _maxHeightBinding = new Binding("MaxHeight");
            _maxHeightBinding.Mode = BindingMode.OneWay;

            _maxWidthBinding.Source = newContent;
            _maxHeightBinding.Source = newContent;

            BindingOperations.SetBinding(this, MaxWidthProperty, _maxWidthBinding);
            BindingOperations.SetBinding(this, MaxHeightProperty, _maxHeightBinding);


            // bind to min width and min height
            Binding minWidthBinding = new Binding("MinWidth");
            minWidthBinding.Mode = BindingMode.OneWay;
            Binding minHeightBinding = new Binding("MinHeight");
            minHeightBinding.Mode = BindingMode.OneWay;

            minWidthBinding.Source = newContent;
            minHeightBinding.Source = newContent;

            BindingOperations.SetBinding(this, MinWidthProperty, minWidthBinding);
            BindingOperations.SetBinding(this, MinHeightProperty, minHeightBinding);
        }

        /// <summary>
        /// Extenders of Viewport3DDecorator can override this function to be notified
        /// when the Content property changes
        /// </summary>
        /// <param name="oldContent">The old value of the Content property</param>
        /// <param name="newContent">The new value of the Content property</param>
        protected virtual void OnViewport3DDecoratorContentChange(UIElement oldContent, UIElement newContent)
        {
        }

        /// <summary>
        /// Property to get the Viewport3D that is being enhanced.
        /// </summary>
        /// <value>
        /// The viewport3 d.
        /// </value>
        public Viewport3D Viewport3D
        {
            get
            {
                Viewport3D viewport3D = null;
                Viewport3DDecorator currEnhancer = this;

                // we follow the enhancers down until we get the
                // Viewport3D they are enhancing
                while (true)
                {
                    UIElement currContent = currEnhancer.Content;

                    if (currContent == null)
                    {
                        break;
                    }
                    else if (currContent is Viewport3D)
                    {
                        viewport3D = (Viewport3D)currContent;
                        break;
                    }
                    else
                    {
                        currEnhancer = (Viewport3DDecorator)currContent;
                    }
                }

                return viewport3D;
            }
        }

        /// <summary>
        /// The UIElements that occur before the Viewport3D
        /// </summary>
        /// <value>
        /// The pre viewport children.
        /// </value>
        protected UIElementCollection PreViewportChildren
        {
            get
            {
                return _preViewportChildren;
            }
        }

        /// <summary>
        /// The UIElements that occur after the Viewport3D
        /// </summary>
        /// <value>
        /// The post viewport children.
        /// </value>
        protected UIElementCollection PostViewportChildren
        {
            get
            {
                return _postViewportChildren;
            }
        }

        /// <summary>
        /// Returns the number of Visual children this element has.
        /// </summary>
        protected override int VisualChildrenCount
        {
            get
            {
                int contentCount = (Content == null ? 0 : 1);

                return PreViewportChildren.Count +
                       PostViewportChildren.Count +
                       contentCount;
            }
        }

        /// <summary>
        /// Returns the child at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the requested child element in the collection.</param>
        /// <returns>
        /// The requested child element. This should not return null; if the provided index is out of range, an exception is thrown.
        /// </returns>
        protected override Visual GetVisualChild(int index)
        {
            int orginalIndex = index;

            // see if index is in the pre viewport children
            if (index < PreViewportChildren.Count)
            {
                return PreViewportChildren[index];
            }
            index -= PreViewportChildren.Count;

            // see if it's the content
            if (Content != null && index == 0)
            {
                return Content;
            }
            index -= (Content == null ? 0 : 1);

            // see if it's the post viewport children
            if (index < PostViewportChildren.Count)
            {
                return PostViewportChildren[index];
            }

            // if we didn't return then the index is out of range - throw an error
            throw new ArgumentOutOfRangeException("index", orginalIndex, "Out of range visual requested");
        }

        /// <summary>
        /// Returns an enumertor to this element's logical children
        /// </summary>
        protected override IEnumerator LogicalChildren
        {
            get
            {
                Visual[] logicalChildren = new Visual[VisualChildrenCount];

                for (int i = 0; i < VisualChildrenCount; i++)
                {
                    logicalChildren[i] = GetVisualChild(i);
                }

                // return an enumerator to the ArrayList
                return logicalChildren.GetEnumerator();
            }
        }

        /// <summary>
        /// Updates the DesiredSize of the Viewport3DDecorator
        /// </summary>
        /// <param name="constraint">The "upper limit" that the return value should not exceed</param>
        /// <returns>
        /// The desired size of the Viewport3DDecorator
        /// </returns>
        protected override Size MeasureOverride(Size constraint)
        {
            Size finalSize = new Size();

            MeasurePreViewportChildren(constraint);

            // measure our Viewport3D(Enhancer)
            if (Content != null)
            {
                Content.Measure(constraint);
                finalSize = Content.DesiredSize;
            }

            MeasurePostViewportChildren(constraint);

            return finalSize;
        }

        /// <summary>
        /// Measures the size of all the PreViewportChildren.  If special measuring behavior is needed, this
        /// method should be overridden.
        /// </summary>
        /// <param name="constraint">The "upper limit" on the size of an element</param>
        protected virtual void MeasurePreViewportChildren(Size constraint)
        {
            // measure the pre viewport children
            MeasureUIElementCollection(PreViewportChildren, constraint);
        }

        /// <summary>
        /// Measures the size of all the PostViewportChildren.  If special measuring behavior is needed, this
        /// method should be overridden.
        /// </summary>
        /// <param name="constraint">The "upper limit" on the size of an element</param>
        protected virtual void MeasurePostViewportChildren(Size constraint)
        {
            // measure the post viewport children
            MeasureUIElementCollection(PostViewportChildren, constraint);
        }

        /// <summary>
        /// Measures all of the UIElements in a UIElementCollection
        /// </summary>
        /// <param name="collection">The collection to measure</param>
        /// <param name="constraint">The "upper limit" on the size of an element</param>
        private void MeasureUIElementCollection(UIElementCollection collection, Size constraint)
        {
            // measure the pre viewport visual visuals
            foreach (UIElement uiElem in collection)
            {
                uiElem.Measure(constraint);
            }
        }

        /// <summary>
        /// Arranges the Pre and Post Viewport children, and arranges itself
        /// </summary>
        /// <param name="arrangeSize">The final size to use to arrange itself and its children</param>
        /// <returns></returns>
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            ArrangePreViewportChildren(arrangeSize);

            // arrange our Viewport3D(Enhancer)
            if (Content != null)
            {
                Content.Arrange(new Rect(arrangeSize));
            }

            ArrangePostViewportChildren(arrangeSize);

            return arrangeSize;
        }

        /// <summary>
        /// Arranges all the PreViewportChildren.  If special measuring behavior is needed, this
        /// method should be overridden.
        /// </summary>
        /// <param name="arrangeSize">The final size to use to arrange each child</param>
        protected virtual void ArrangePreViewportChildren(Size arrangeSize)
        {
            ArrangeUIElementCollection(PreViewportChildren, arrangeSize);
        }

        /// <summary>
        /// Arranges all the PostViewportChildren.  If special measuring behavior is needed, this
        /// method should be overridden.
        /// </summary>
        /// <param name="arrangeSize">The final size to use to arrange each child</param>
        protected virtual void ArrangePostViewportChildren(Size arrangeSize)
        {
            ArrangeUIElementCollection(PostViewportChildren, arrangeSize);
        }

        /// <summary>
        /// Arranges all the UIElements in the passed in UIElementCollection
        /// </summary>
        /// <param name="collection">The collection that should be arranged</param>
        /// <param name="constraint">The final size that element should use to arrange itself and its children</param>
        private void ArrangeUIElementCollection(UIElementCollection collection, Size constraint)
        {
            // measure the pre viewport visual visuals
            foreach (UIElement uiElem in collection)
            {
                uiElem.Arrange(new Rect(constraint));
            }
        }
        
        void IAddChild.AddChild(Object value)
        {
            // check against null
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            // we only can have one child
            if (this.Content != null)
            {
                throw new ArgumentException("Viewport3DDecorator can only have one child");
            }

            // now we can actually set the content
            Content = (UIElement)value;
        }

        void IAddChild.AddText(string text)
        {
            // The only text we accept is whitespace, which we ignore.
            foreach (char c in text)
            {
                if (!char.IsWhiteSpace(c))
                {
                    throw new ArgumentException("Non whitespace in add text", text);
                }
            }
        }
    }
}