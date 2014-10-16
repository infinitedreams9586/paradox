﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using SiliconStudio.Core.Mathematics;

using System.Windows.Media.Imaging;
using System.Windows.Input;
using SiliconStudio.Presentation.Extensions;

using Color = SiliconStudio.Core.Mathematics.Color;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace SiliconStudio.Presentation.Controls
{
    /// <summary>
    /// Represents a color picker control.
    /// </summary>
    [TemplatePart(Name = "PART_ColorPickerSelector", Type = typeof(Canvas))]
    [TemplatePart(Name = "PART_ColorPickerRenderSurface", Type = typeof(Rectangle))]
    [TemplatePart(Name = "PART_ColorPreviewRenderSurface", Type = typeof(Rectangle))]
    [TemplatePart(Name = "PART_HuePickerSelector", Type = typeof(FrameworkElement))]
    [TemplatePart(Name = "PART_HuePickerRenderSurface", Type = typeof(Rectangle))]
    public sealed class ColorPicker : Control
    {
        static ColorPicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorPicker), new FrameworkPropertyMetadata(typeof(ColorPicker)));
        }

        private Canvas colorPickerSelector;
        private Rectangle colorPickerRenderSurface;
        private Rectangle colorPreviewRenderSurface;
        private FrameworkElement huePickerRenderSurface;
        private Rectangle huePickerSelector;
        private bool interlock;
        private Color4 internalColor;
        private bool suspendBindingUpdates;

        /// <summary>
        /// Identifies the <see cref="Color"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register("Color", typeof(Color4), typeof(ColorPicker), new FrameworkPropertyMetadata(default(Color4), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnColorPropertyChanged, CoreceColorValue, false, UpdateSourceTrigger.Explicit));

        /// <summary>
        /// Identifies the <see cref="Hue"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HueProperty = DependencyProperty.Register("Hue", typeof(float), typeof(ColorPicker), new FrameworkPropertyMetadata(0.0f, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnHSVPropertyChanged, CoerceHueValue));

        /// <summary>
        /// Identifies the <see cref="Saturation"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SaturationProperty = DependencyProperty.Register("Saturation", typeof(float), typeof(ColorPicker), new FrameworkPropertyMetadata(0.0f, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnHSVPropertyChanged, CoercePercentageValue));

        /// <summary>
        /// Identifies the <see cref="Brightness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BrightnessProperty = DependencyProperty.Register("Brightness", typeof(float), typeof(ColorPicker), new FrameworkPropertyMetadata(0.0f, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnHSVPropertyChanged, CoercePercentageValue));

        /// <summary>
        /// Identifies the <see cref="Red"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RedProperty = DependencyProperty.Register("Red", typeof(byte), typeof(ColorPicker), new FrameworkPropertyMetadata((byte)0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnRGBAPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="Green"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GreenProperty = DependencyProperty.Register("Green", typeof(byte), typeof(ColorPicker), new FrameworkPropertyMetadata((byte)0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnRGBAPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="Blue"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BlueProperty = DependencyProperty.Register("Blue", typeof(byte), typeof(ColorPicker), new FrameworkPropertyMetadata((byte)0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnRGBAPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="Alpha"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AlphaProperty = DependencyProperty.Register("Alpha", typeof(byte), typeof(ColorPicker), new FrameworkPropertyMetadata((byte)0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnRGBAPropertyChanged));

        /// <summary>
        /// The color associated to this color picker.
        /// </summary>
        /// <remarks>The float values of each component of the color are always equals to the float conversion of a <see cref="byte"/> value divided by <b>255</b>.</remarks>
        public Color4 Color { get { return (Color4)GetValue(ColorProperty); } set { SetValue(ColorProperty, value); } }

        /// <summary>
        /// The hue of the color associated to this color picker.
        /// </summary>
        public float Hue { get { return (float)GetValue(HueProperty); } set { SetValue(HueProperty, value); } }

        /// <summary>
        /// The saturation of the color associated to this color picker.
        /// </summary>
        public float Saturation { get { return (float)GetValue(SaturationProperty); } set { SetValue(SaturationProperty, value); } }

        /// <summary>
        /// The brightness of the color associated to this color picker.
        /// </summary>
        public float Brightness { get { return (float)GetValue(BrightnessProperty); } set { SetValue(BrightnessProperty, value); } }

        /// <summary>
        /// The red component of the color associated to this color picker.
        /// </summary>
        public byte Red { get { return (byte)GetValue(RedProperty); } set { SetValue(RedProperty, value); } }

        /// <summary>
        /// The green component of the color associated to this color picker.
        /// </summary>
        public byte Green { get { return (byte)GetValue(GreenProperty); } set { SetValue(GreenProperty, value); } }

        /// <summary>
        /// The blue component of the color associated to this color picker.
        /// </summary>
        public byte Blue { get { return (byte)GetValue(BlueProperty); } set { SetValue(BlueProperty, value); } }

        /// <summary>
        /// The alpha component of the color associated to this color picker.
        /// </summary>
        public byte Alpha { get { return (byte)GetValue(AlphaProperty); } set { SetValue(AlphaProperty, value); } }

        /// <summary>
        /// An internal representation of the color associated to this color picker. Its value never rounded to match a byte division by 255.
        /// </summary>
        private Color4 InternalColor { get { return internalColor; } set { internalColor = value; interlock = true; Color = value; interlock = false; } }

        /// <inheritdoc/>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (colorPickerRenderSurface != null)
            {
                colorPickerRenderSurface.MouseDown -= OnColorPickerRenderSurfaceMouseDown;
                colorPickerRenderSurface.MouseUp -= OnColorPickerRenderSurfaceMouseUp;
                colorPickerRenderSurface.MouseMove -= OnColorPickerRenderSurfaceMouseMove;
            }

            if (huePickerRenderSurface != null)
            {
                huePickerRenderSurface.MouseDown -= OnHuePickerRenderSurfaceMouseDown;
                huePickerRenderSurface.MouseUp -= OnHuePickerRenderSurfaceMouseUp;
                huePickerRenderSurface.MouseMove -= OnHuePickerRenderSurfaceMouseMove;

            }

            colorPickerRenderSurface = FrameworkElementExtensions.CheckTemplatePart<Rectangle>(GetTemplateChild("PART_ColorPickerRenderSurface"));
            colorPreviewRenderSurface = FrameworkElementExtensions.CheckTemplatePart<Rectangle>(GetTemplateChild("PART_ColorPreviewRenderSurface"));
            colorPickerSelector = FrameworkElementExtensions.CheckTemplatePart<Canvas>(GetTemplateChild("PART_ColorPickerSelector"));
            huePickerSelector = FrameworkElementExtensions.CheckTemplatePart<Rectangle>(GetTemplateChild("PART_HuePickerSelector"));
            huePickerRenderSurface = FrameworkElementExtensions.CheckTemplatePart<FrameworkElement>(GetTemplateChild("PART_HuePickerRenderSurface"));
     
            if (colorPickerRenderSurface != null)
            {
                colorPickerRenderSurface.MouseDown += OnColorPickerRenderSurfaceMouseDown;
                colorPickerRenderSurface.MouseUp += OnColorPickerRenderSurfaceMouseUp;
                colorPickerRenderSurface.MouseMove += OnColorPickerRenderSurfaceMouseMove;
            }

            if (huePickerRenderSurface != null)
            {
                huePickerRenderSurface.MouseDown += OnHuePickerRenderSurfaceMouseDown;
                huePickerRenderSurface.MouseUp += OnHuePickerRenderSurfaceMouseUp;
                huePickerRenderSurface.MouseMove += OnHuePickerRenderSurfaceMouseMove;
            }

            RenderColorPickerSurface();

            if (colorPickerSelector != null && colorPickerRenderSurface != null)
            {
                Canvas.SetLeft(colorPickerSelector, Saturation * colorPickerRenderSurface.Width / 100.0);
                Canvas.SetTop(colorPickerSelector, Brightness * colorPickerRenderSurface.Height / 100.0);
            }
            if (huePickerSelector != null && huePickerRenderSurface != null)
            {
                Canvas.SetLeft(huePickerSelector, Hue * huePickerRenderSurface.Width / 360.0);
            }
        }

        /// <summary>
        /// Handles the <see cref="Rectangle.MouseDown"/> event of the color surface.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
        private void OnColorPickerRenderSurfaceMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                colorPickerRenderSurface.CaptureMouse();
                suspendBindingUpdates = true;
                UpdateColorPickerFromMouse(e.GetPosition(colorPickerRenderSurface));
            }
        }

        /// <summary>
        /// Handles the <see cref="Rectangle.MouseUp"/> event of the color surface.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
        private void OnColorPickerRenderSurfaceMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
            {
                colorPickerRenderSurface.ReleaseMouseCapture();
                suspendBindingUpdates = false;
                UpdateAllBindings();
            }
        }

        /// <summary>
        /// Handles the <see cref="Rectangle.MouseMove"/> event of the color surface.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
        private void OnColorPickerRenderSurfaceMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && colorPickerRenderSurface.IsMouseCaptured)
                UpdateColorPickerFromMouse(e.GetPosition(colorPickerRenderSurface));
        }

        /// <summary>
        /// Updates the <see cref="Color"/> value from the given position in the color surface.
        /// </summary>
        /// <param name="position">The position of the cursor in the color surface.</param>
        private void UpdateColorPickerFromMouse(Point position)
        {
            var x = (float)(position.X / colorPickerRenderSurface.Width);
            var y = (float)(position.Y / colorPickerRenderSurface.Height);

            x = MathUtil.Clamp(x, 0.0f, 1.0f);
            y = MathUtil.Clamp(y, 0.0f, 1.0f);

            var colorHSV = new ColorHSV(Hue, x, y, Alpha / 255.0f);
            Color = colorHSV.ToColor();
        }


        /// <summary>
        /// Handles the <see cref="Rectangle.MouseDown"/> event of the hue surface.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
        private void OnHuePickerRenderSurfaceMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                huePickerRenderSurface.CaptureMouse();
                suspendBindingUpdates = true;
                UpdateHuePickerFromMouse(e.GetPosition(huePickerRenderSurface));
            }
        }

        /// <summary>
        /// Handles the <see cref="Rectangle.MouseUp"/> event of the hue surface.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
        private void OnHuePickerRenderSurfaceMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
            {
                huePickerRenderSurface.ReleaseMouseCapture();
                suspendBindingUpdates = false;
                UpdateAllBindings();
            }
        }

        /// <summary>
        /// Handles the <see cref="Rectangle.MouseMove"/> event of the hue surface.
        /// </summary>
        /// <param name="sender">The object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
        private void OnHuePickerRenderSurfaceMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && huePickerRenderSurface.IsMouseCaptured)
                UpdateHuePickerFromMouse(e.GetPosition(huePickerRenderSurface));
        }

        /// <summary>
        /// Updates the <see cref="Color"/> value from the given position in the hue surface.
        /// </summary>
        /// <param name="position">The position of the cursor in the hue surface.</param>
        private void UpdateHuePickerFromMouse(Point position)
        {
            var x = (float)(position.X / huePickerRenderSurface.Width);
            x = (float)(360.0 * MathUtil.Clamp(x, 0.0f, 1.0f));
            var colorHSV = new ColorHSV(x, Saturation / 100.0f, Brightness / 100.0f, Alpha / 255.0f);
            Color = colorHSV.ToColor();
        }

        /// <summary>
        /// Update the color surface brush according to the current <see cref="Hue"/> value.
        /// </summary>
        private void RenderColorPickerSurface()
        {
            if (colorPreviewRenderSurface != null)
            {
                colorPreviewRenderSurface.Fill = new SolidColorBrush(Color.ToSystemColor());
            }
            if (colorPickerRenderSurface != null)
            {
                // Ensure the color picker is loaded 
                if (!double.IsNaN(colorPickerRenderSurface.Width) && !double.IsNaN(colorPickerRenderSurface.Height))
                {
                    var width = (int)colorPickerRenderSurface.Width;
                    var height = (int)colorPickerRenderSurface.Height;

                    PixelFormat pf = PixelFormats.Bgr32;
                    int rawStride = (width * pf.BitsPerPixel + 7) / 8;
                    var rawImage = new byte[rawStride * height];

                    for (int j = 0; j < height; ++j)
                    {
                        float y = j / (float)(height - 1);

                        for (int i = 0; i < width; ++i)
                        {
                            float x = i / (float)(width - 1);

                            System.Windows.Media.Color color = new ColorHSV(Hue, x, y, 1.0f).ToSystemColor();
                            rawImage[(i + j * width) * 4 + 0] = color.B;
                            rawImage[(i + j * width) * 4 + 1] = color.G;
                            rawImage[(i + j * width) * 4 + 2] = color.R;
                        }
                    }

                    colorPickerRenderSurface.Fill = new DrawingBrush(new ImageDrawing(BitmapSource.Create(width, height, 96, 96, pf, null, rawImage, rawStride), new Rect(0.0f, 0.0f, width, height)));
                }
            }
        }

        /// <summary>
        /// Raised when the <see cref="Color"/> property is modified.
        /// </summary>
        private void OnColorChanged()
        {
            if (!interlock)
            {
                internalColor = Color;
                interlock = true;

                SetCurrentValue(RedProperty, (byte)(internalColor.R * 255.0f));
                SetCurrentValue(GreenProperty, (byte)(internalColor.G * 255.0f));
                SetCurrentValue(BlueProperty, (byte)(internalColor.B * 255.0f));
                SetCurrentValue(AlphaProperty, (byte)(internalColor.A * 255.0f));

                var colorHSV = ColorHSV.FromColor(internalColor);
                SetCurrentValue(HueProperty, colorHSV.H);
                SetCurrentValue(SaturationProperty, colorHSV.S * 100.0f);
                SetCurrentValue(BrightnessProperty, colorHSV.V * 100.0f);
                interlock = false;
            }
            RenderColorPickerSurface();

            UpdateBinding(ColorProperty);
        }

        /// <summary>
        /// Raised when the <see cref="Red"/>, <see cref="Green"/>, <see cref="Blue"/> or <see cref="Alpha"/> properties are modified.
        /// </summary>
        /// <param name="e">The dependency property that has changed.</param>
        private void OnRGBAValueChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!interlock)
            {
                if (e.Property == RedProperty)
                    InternalColor = new Color4((byte)e.NewValue / 255.0f, InternalColor.G, InternalColor.B, InternalColor.A);
                else if (e.Property == GreenProperty)
                    InternalColor = new Color4(InternalColor.R, (byte)e.NewValue / 255.0f, InternalColor.B, InternalColor.A);
                else if (e.Property == BlueProperty)
                    InternalColor = new Color4(InternalColor.R, InternalColor.G, (byte)e.NewValue / 255.0f, InternalColor.A);
                else if (e.Property == AlphaProperty)
                    InternalColor = new Color4(InternalColor.R, InternalColor.G, InternalColor.B, (byte)e.NewValue / 255.0f);
                else
                    throw new ArgumentException("Property unsupported by method OnRGBAValueChanged.");

                interlock = true;
                var colorHSV = ColorHSV.FromColor(internalColor);
                SetCurrentValue(HueProperty, colorHSV.H);
                SetCurrentValue(SaturationProperty, colorHSV.S * 100.0f);
                SetCurrentValue(BrightnessProperty, colorHSV.V * 100.0f);
                interlock = false;
            }
            
            UpdateBinding(e.Property);
        }

        /// <summary>
        /// Raised when the <see cref="Hue"/>, <see cref="Saturation"/>, or <see cref="Brightness"/> properties are modified.
        /// </summary>
        /// <param name="e">The dependency property that has changed.</param>
        private void OnHSVValueChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!interlock)
            {
                ColorHSV colorHSV;

                if (e.Property == HueProperty)
                {
                    colorHSV = new ColorHSV((float)e.NewValue, Saturation / 100.0f, Brightness / 100.0f, Alpha / 255.0f);
                    RenderColorPickerSurface();
                }
                else if (e.Property == SaturationProperty)
                    colorHSV = new ColorHSV(Hue, (float)e.NewValue / 100.0f, Brightness / 100.0f, Alpha / 255.0f);
                else if (e.Property == BrightnessProperty)
                    colorHSV = new ColorHSV(Hue, Saturation / 100.0f, (float)e.NewValue / 100.0f, Alpha / 255.0f);
                else
                    throw new ArgumentException("Property unsupported by method OnHSVValueChanged.");

                InternalColor = colorHSV.ToColor();

                interlock = true;
                SetCurrentValue(RedProperty, (byte)(internalColor.R * 255.0f));
                SetCurrentValue(GreenProperty, (byte)(internalColor.G * 255.0f));
                SetCurrentValue(BlueProperty, (byte)(internalColor.B * 255.0f));
                SetCurrentValue(AlphaProperty, (byte)(internalColor.A * 255.0f));
                interlock = false;
            }

            UpdateBinding(e.Property);

            if (colorPickerSelector != null && colorPickerRenderSurface != null)
            {
                Canvas.SetLeft(colorPickerSelector, Saturation * colorPickerRenderSurface.Width / 100.0);
                Canvas.SetTop(colorPickerSelector, Brightness * colorPickerRenderSurface.Height / 100.0);
            }
            if (huePickerSelector != null && huePickerRenderSurface != null)
            {
                Canvas.SetLeft(huePickerSelector, Hue * huePickerRenderSurface.Width / 360.0);
            }
        }

        /// <summary>
        /// Updates the binding of the given dependency property, if binding updates are not currently suspended by user actions.
        /// </summary>
        /// <param name="dependencyProperty">The dependency property.</param>
        private void UpdateBinding(DependencyProperty dependencyProperty)
        {
            if (!suspendBindingUpdates)
            {
                BindingExpression expression = GetBindingExpression(dependencyProperty);
                if (expression != null)
                    expression.UpdateSource();
            }
        }

        /// <summary>
        /// Updates the bindings of all dependency properties, if binding updates are not currently suspended by user actions.
        /// </summary>
        private void UpdateAllBindings()
        {
            UpdateBinding(ColorProperty);
            UpdateBinding(RedProperty);
            UpdateBinding(GreenProperty);
            UpdateBinding(BlueProperty);
            UpdateBinding(AlphaProperty);
            UpdateBinding(HueProperty);
            UpdateBinding(SaturationProperty);
            UpdateBinding(BrightnessProperty);
        }

        /// <summary>
        /// Raised by <see cref="ColorProperty"/> when the <see cref="Color"/> dependency property is modified.
        /// </summary>
        /// <param name="sender">The dependency object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
        private static void OnColorPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var colorPicker = (ColorPicker)sender;
            colorPicker.OnColorChanged();
        }

        /// <summary>
        /// Raised by <see cref="HueProperty"/>, <see cref="SaturationProperty"/> or <see cref="BrightnessProperty"/> when respectively the
        /// <see cref="Hue"/>, <see cref="Saturation"/> or <see cref="Brightness"/> dependency property is modified.
        /// </summary>
        /// <param name="sender">The dependency object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
        private static void OnHSVPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var colorPicker = (ColorPicker)sender;
            colorPicker.OnHSVValueChanged(e);
        }

        /// <summary>
        /// Raised by <see cref="RedProperty"/>, <see cref="GreenProperty"/>, <see cref="BlueProperty"/> or <see cref="AlphaProperty"/> when respectively the
        /// <see cref="Red"/>, <see cref="Green"/>, <see cref="Blue"/> or <see cref="Alpha"/> dependency property is modified.
        /// </summary>
        /// <param name="sender">The dependency object where the event handler is attached.</param>
        /// <param name="e">The event data.</param>
        private static void OnRGBAPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var colorPicker = (ColorPicker)sender;
            colorPicker.OnRGBAValueChanged(e);
        }

        /// <summary>
        /// Coerce the value of the Color so its components are always equals to the float value of a byte divided by 255.
        /// </summary>
        private static object CoreceColorValue(DependencyObject sender, object baseValue)
        {
            return new Color(((Color4)baseValue).ToArray()).ToColor4();
        }

        /// <summary>
        /// Coerce the value of the Hue so it is always contained in the -360, +360 interval
        /// </summary>
        private static object CoerceHueValue(DependencyObject sender, object baseValue)
        {
            return ((float)baseValue + 360.0f) % 360.0f;
        }

        /// <summary>
        /// Coerce the saturation and brightness values so they are always contained between 0 and 100
        /// </summary>
        private static object CoercePercentageValue(DependencyObject sender, object baseValue)
        {
            return MathUtil.Clamp((float)baseValue, 0.0f, 100.0f);
        }
    }
}