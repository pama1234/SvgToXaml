﻿//#define USE_BRUSH_TRANSFORM
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace SvgToXamlConverter
{
    public static class SvgConverter
    {
        private const byte OpaqueAlpha = 255;

        private static readonly ShimSkiaSharp.SKColor s_empty = new ShimSkiaSharp.SKColor(0, 0, 0, 0);

        private static readonly ShimSkiaSharp.SKColor s_transparentBlack = new ShimSkiaSharp.SKColor(0, 0, 0, 255);

        public static string NewLine = "\r\n";

        public static string ToGradientSpreadMethod(ShimSkiaSharp.SKShaderTileMode shaderTileMode)
        {
            switch (shaderTileMode)
            {
                default:
                case ShimSkiaSharp.SKShaderTileMode.Clamp:
                    return "Pad";

                case ShimSkiaSharp.SKShaderTileMode.Repeat:
                    return "Repeat";

                case ShimSkiaSharp.SKShaderTileMode.Mirror:
                    return "Reflect";
            }
        }

        public static string ToTileMode(ShimSkiaSharp.SKShaderTileMode shaderTileMode)
        {
            switch (shaderTileMode)
            {
                default:
                case ShimSkiaSharp.SKShaderTileMode.Clamp:
                    return "None";

                case ShimSkiaSharp.SKShaderTileMode.Repeat:
                    return "Tile";

                case ShimSkiaSharp.SKShaderTileMode.Mirror:
                    return "FlipXY";
            };
        }

        public static string ToPenLineCap(ShimSkiaSharp.SKStrokeCap strokeCap)
        {
            switch (strokeCap)
            {
                default:
                case ShimSkiaSharp.SKStrokeCap.Butt:
                    return "Flat";

                case ShimSkiaSharp.SKStrokeCap.Round:
                    return "Round";

                case ShimSkiaSharp.SKStrokeCap.Square:
                    return "Square";
            }
        }

        public static string ToPenLineJoin(ShimSkiaSharp.SKStrokeJoin strokeJoin)
        {
            switch (strokeJoin)
            {
                default:
                case ShimSkiaSharp.SKStrokeJoin.Miter:
                    return "Miter";

                case ShimSkiaSharp.SKStrokeJoin.Round:
                    return "Round";

                case ShimSkiaSharp.SKStrokeJoin.Bevel:
                    return "Bevel";
            }
        }

        public static string ToString(double value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string ToString(float value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string ToHexColor(ShimSkiaSharp.SKColor skColor)
        {
            var sb = new StringBuilder();
            sb.Append('#');
            sb.AppendFormat("{0:X2}", skColor.Alpha);
            sb.AppendFormat("{0:X2}", skColor.Red);
            sb.AppendFormat("{0:X2}", skColor.Green);
            sb.AppendFormat("{0:X2}", skColor.Blue);
            return sb.ToString();
        }

        public static string ToPoint(ShimSkiaSharp.SKPoint skPoint)
        {
            var sb = new StringBuilder();
            sb.Append(ToString(skPoint.X));
            sb.Append(',');
            sb.Append(ToString(skPoint.Y));
            return sb.ToString();
        }

        public static string ToRect(ShimSkiaSharp.SKRect sKRect)
        {
            var sb = new StringBuilder();
            sb.Append(ToString(sKRect.Left));
            sb.Append(',');
            sb.Append(ToString(sKRect.Top));
            sb.Append(',');
            sb.Append(ToString(sKRect.Width));
            sb.Append(',');
            sb.Append(ToString(sKRect.Height));
            return sb.ToString();
        }

        public static string ToPoint(SkiaSharp.SKPoint skPoint)
        {
            var sb = new StringBuilder();
            sb.Append(ToString(skPoint.X));
            sb.Append(',');
            sb.Append(ToString(skPoint.Y));
            return sb.ToString();
        }

        public static string ToMatrix(SkiaSharp.SKMatrix skMatrix)
        {
            var sb = new StringBuilder();
            sb.Append(ToString(skMatrix.ScaleX));
            sb.Append(',');
            sb.Append(ToString(skMatrix.SkewY));
            sb.Append(',');
            sb.Append(ToString(skMatrix.SkewX));
            sb.Append(',');
            sb.Append(ToString(skMatrix.ScaleY));
            sb.Append(',');
            sb.Append(ToString(skMatrix.TransX));
            sb.Append(',');
            sb.Append(ToString(skMatrix.TransY));
            return sb.ToString();
        }

        public static string ToSvgPathData(SkiaSharp.SKPath path)
        {
            if (path.FillType == SkiaSharp.SKPathFillType.EvenOdd)
            {
                // EvenOdd
                var sb = new StringBuilder();
                sb.Append("F0 ");
                sb.Append(path.ToSvgPathData());
                return sb.ToString();
            }
            else
            {
                // Nonzero 
                var sb = new StringBuilder();
                sb.Append("F1 ");
                sb.Append(path.ToSvgPathData());
                return sb.ToString();
            }
        }

        private static SkiaSharp.SKMatrix AdjustMatrixLocation(SkiaSharp.SKMatrix matrix, float x, float y)
        {
            return new SkiaSharp.SKMatrix(
                matrix.ScaleX, 
                matrix.SkewX, 
                matrix.TransX - x, 
                matrix.SkewY, 
                matrix.ScaleY, 
                matrix.TransY - y, 
                matrix.Persp0, 
                matrix.Persp1, 
                matrix.Persp2);
        }

        public static string ToBrush(ShimSkiaSharp.ColorShader colorShader, SkiaSharp.SKRect skBounds)
        {
            var sb = new StringBuilder();

            sb.Append($"<SolidColorBrush");
            sb.Append($" Color=\"{ToHexColor(colorShader.Color)}\"");
            sb.Append($"/>{NewLine}");

            return sb.ToString();
        }

        public static string ToBrush(ShimSkiaSharp.LinearGradientShader linearGradientShader, SkiaSharp.SKRect skBounds)
        {
            var sb = new StringBuilder();

            var start = Svg.Skia.SkiaModelExtensions.ToSKPoint(linearGradientShader.Start);
            var end = Svg.Skia.SkiaModelExtensions.ToSKPoint(linearGradientShader.End);

#if !USE_BRUSH_TRANSFORM
            if (linearGradientShader.LocalMatrix is { })
            {
                var localMatrix = Svg.Skia.SkiaModelExtensions.ToSKMatrix(linearGradientShader.LocalMatrix.Value);
                localMatrix.TransX = Math.Max(0f, localMatrix.TransX - skBounds.Location.X);
                localMatrix.TransY = Math.Max(0f, localMatrix.TransY - skBounds.Location.Y);

                start = localMatrix.MapPoint(start);
                end = localMatrix.MapPoint(end);
            }
            else
            {
                start.X = Math.Max(0f, start.X - skBounds.Location.X);
                start.Y = Math.Max(0f, start.Y - skBounds.Location.Y);
                end.X = Math.Max(0f, end.X - skBounds.Location.X);
                end.Y = Math.Max(0f, end.Y - skBounds.Location.Y);
            }
#else
            if (linearGradientShader.LocalMatrix is null)
            {
                start.X = Math.Max(0f, start.X - skBounds.Location.X);
                start.Y = Math.Max(0f, start.Y - skBounds.Location.Y);
                end.X = Math.Max(0f, end.X - skBounds.Location.X);
                end.Y = Math.Max(0f, end.Y - skBounds.Location.Y);
            }
#endif

            sb.Append($"<LinearGradientBrush");
            sb.Append($" StartPoint=\"{ToPoint(start)}\"");
            sb.Append($" EndPoint=\"{ToPoint(end)}\"");

            if (linearGradientShader.Mode != ShimSkiaSharp.SKShaderTileMode.Clamp)
            {
                sb.Append($" SpreadMethod=\"{ToGradientSpreadMethod(linearGradientShader.Mode)}\"");
            }

            sb.Append($">{NewLine}");

#if USE_BRUSH_TRANSFORM
            if (linearGradientShader.LocalMatrix is { })
            {
                // TODO: Missing Transform property on LinearGradientBrush
                var localMatrix = Svg.Skia.SkiaModelExtensions.ToSKMatrix(linearGradientShader.LocalMatrix.Value);
                localMatrix = AdjustMatrixLocation(localMatrix, skBounds.Location.X, skBounds.Location.Y);
                sb.Append($"  <LinearGradientBrush.Transform>{NewLine}");
                sb.Append($"    <MatrixTransform Matrix=\"{ToMatrix(localMatrix)}\"/>{NewLine}");
                sb.Append($"  </LinearGradientBrush.Transform>{NewLine}");
            }
#endif

            sb.Append($"  <LinearGradientBrush.GradientStops>{NewLine}");

            if (linearGradientShader.Colors is { } && linearGradientShader.ColorPos is { })
            {
                for (var i = 0; i < linearGradientShader.Colors.Length; i++)
                {
                    var color = ToHexColor(linearGradientShader.Colors[i]);
                    var offset = ToString(linearGradientShader.ColorPos[i]);
                    sb.Append($"    <GradientStop Offset=\"{offset}\" Color=\"{color}\"/>{NewLine}");
                }
            }

            sb.Append($"  </LinearGradientBrush.GradientStops>{NewLine}");
            sb.Append($"</LinearGradientBrush>{NewLine}");

            return sb.ToString();
        }

        public static string ToBrush(ShimSkiaSharp.TwoPointConicalGradientShader twoPointConicalGradientShader, SkiaSharp.SKRect skBounds)
        {
            var sb = new StringBuilder();

            // NOTE: twoPointConicalGradientShader.StartRadius is always 0.0
            var startRadius = twoPointConicalGradientShader.StartRadius;

            // TODO: Avalonia is passing 'radius' to 'SKShader.CreateTwoPointConicalGradient' as 'startRadius'
            // TODO: but we need to pass it as 'endRadius' to 'SKShader.CreateTwoPointConicalGradient'
            var endRadius = twoPointConicalGradientShader.EndRadius;

            var center = Svg.Skia.SkiaModelExtensions.ToSKPoint(twoPointConicalGradientShader.Start);
            var gradientOrigin = Svg.Skia.SkiaModelExtensions.ToSKPoint(twoPointConicalGradientShader.End);

#if !USE_BRUSH_TRANSFORM
            if (twoPointConicalGradientShader.LocalMatrix is { })
            {
                var localMatrix = Svg.Skia.SkiaModelExtensions.ToSKMatrix(twoPointConicalGradientShader.LocalMatrix.Value);

                localMatrix.TransX = Math.Max(0f, localMatrix.TransX - skBounds.Location.X);
                localMatrix.TransY = Math.Max(0f, localMatrix.TransY - skBounds.Location.Y);

                center = localMatrix.MapPoint(center);
                gradientOrigin = localMatrix.MapPoint(gradientOrigin);

                var radius = localMatrix.MapVector(new SkiaSharp.SKPoint(endRadius, 0));
                endRadius = radius.X;
            }
            else
            {
                center.X = Math.Max(0f, center.X - skBounds.Location.X);
                center.Y = Math.Max(0f, center.Y - skBounds.Location.Y);
                gradientOrigin.X = Math.Max(0f, gradientOrigin.X - skBounds.Location.X);
                gradientOrigin.Y = Math.Max(0f, gradientOrigin.Y - skBounds.Location.Y);
            }
#else
            if (twoPointConicalGradientShader.LocalMatrix is null)
            {
                center.X = Math.Max(0f, center.X - skBounds.Location.X);
                center.Y = Math.Max(0f, center.Y - skBounds.Location.Y);
                gradientOrigin.X = Math.Max(0f, gradientOrigin.X - skBounds.Location.X);
                gradientOrigin.Y = Math.Max(0f, gradientOrigin.Y - skBounds.Location.Y);
            }
#endif

            endRadius = endRadius / skBounds.Width;

            sb.Append($"<RadialGradientBrush");
            sb.Append($" Center=\"{ToPoint(center)}\"");
            sb.Append($" GradientOrigin=\"{ToPoint(gradientOrigin)}\"");
            sb.Append($" Radius=\"{ToString(endRadius)}\"");

            if (twoPointConicalGradientShader.Mode != ShimSkiaSharp.SKShaderTileMode.Clamp)
            {
                sb.Append($" SpreadMethod=\"{ToGradientSpreadMethod(twoPointConicalGradientShader.Mode)}\"");
            }

            sb.Append($">{NewLine}");

#if USE_BRUSH_TRANSFORM
            if (twoPointConicalGradientShader.LocalMatrix is { })
            {
                // TODO: Missing Transform property on RadialGradientBrush
                var localMatrix = Svg.Skia.SkiaModelExtensions.ToSKMatrix(twoPointConicalGradientShader.LocalMatrix.Value);
                localMatrix = AdjustMatrixLocation(localMatrix, skBounds.Location.X, skBounds.Location.Y);
                sb.Append($"  <RadialGradientBrush.Transform>{NewLine}");
                sb.Append($"    <MatrixTransform Matrix=\"{ToMatrix(localMatrix)}\"/>{NewLine}");
                sb.Append($"  </RadialGradientBrush.Transform>{NewLine}");
            }
#endif

            sb.Append($"  <RadialGradientBrush.GradientStops>{NewLine}");

            if (twoPointConicalGradientShader.Colors is { } && twoPointConicalGradientShader.ColorPos is { })
            {
                for (var i = 0; i < twoPointConicalGradientShader.Colors.Length; i++)
                {
                    var color = ToHexColor(twoPointConicalGradientShader.Colors[i]);
                    var offset = ToString(twoPointConicalGradientShader.ColorPos[i]);
                    sb.Append($"    <GradientStop Offset=\"{offset}\" Color=\"{color}\"/>{NewLine}");
                }
            }

            sb.Append($"  </RadialGradientBrush.GradientStops>{NewLine}");
            sb.Append($"</RadialGradientBrush>{NewLine}");

            return sb.ToString();
        }

        public static string ToBrush(ShimSkiaSharp.PictureShader pictureShader, SkiaSharp.SKRect skBounds)
        {
            if (pictureShader?.Src is null)
            {
                return "";
            }

            var sb = new StringBuilder();

#if !USE_BRUSH_TRANSFORM
            if (pictureShader.LocalMatrix is { })
            {
                var localMatrix = Svg.Skia.SkiaModelExtensions.ToSKMatrix(pictureShader.LocalMatrix);

                if (!localMatrix.IsIdentity)
                {
#if DEBUG
                    sb.Append($"<!-- TODO: Transform: {ToMatrix(localMatrix)} -->{NewLine}");
#endif
                }
            }
            else
            {
                // TODO: Adjust using skBounds.Location ?
            }
#endif

            var sourceRect = pictureShader.Src.CullRect;
            var destinationRect = pictureShader.Tile;

            // TODO: Use different than Image ?
            sb.Append($"<VisualBrush");

            if (pictureShader.TmX != ShimSkiaSharp.SKShaderTileMode.Clamp)
            {
                sb.Append($" TileMode=\"{ToTileMode(pictureShader.TmX)}\"");
            }

            sb.Append($" SourceRect=\"{ToRect(sourceRect)}\"");
            sb.Append($" DestinationRect=\"{ToRect(destinationRect)}\"");

            sb.Append($">{NewLine}");

#if USE_BRUSH_TRANSFORM
            if (pictureShader?.LocalMatrix is { })
            {
                // TODO: Missing Transform property on VisualBrush
                var localMatrix = Svg.Skia.SkiaModelExtensions.ToSKMatrix(pictureShader.LocalMatrix);
                localMatrix = AdjustMatrixLocation(localMatrix, skBounds.Location.X, skBounds.Location.Y);
                sb.Append($"  <VisualBrush.Transform>{NewLine}");
                sb.Append($"    <MatrixTransform Matrix=\"{ToMatrix(localMatrix)}\"/>{NewLine}");
                sb.Append($"  </VisualBrush.Transform>{NewLine}");
            }
#endif

            sb.Append($"  <VisualBrush.Visual>{NewLine}");
            sb.Append(ToXamlImage(pictureShader.Src, key: null));
            sb.Append($"{NewLine}");
            sb.Append($"  </VisualBrush.Visual>{NewLine}");
            sb.Append($"</VisualBrush>{NewLine}");

            return sb.ToString();
        }

        public static string ToBrush(ShimSkiaSharp.SKShader skShader, SkiaSharp.SKRect skBounds)
        {
            return skShader switch
            {
                ShimSkiaSharp.ColorShader colorShader => ToBrush(colorShader, skBounds),
                ShimSkiaSharp.LinearGradientShader linearGradientShader => ToBrush(linearGradientShader, skBounds),
                ShimSkiaSharp.TwoPointConicalGradientShader twoPointConicalGradientShader => ToBrush(twoPointConicalGradientShader, skBounds),
                ShimSkiaSharp.PictureShader pictureShader => ToBrush(pictureShader, skBounds),
                _ => ""
            };
        }

        public static string ToPen(ShimSkiaSharp.SKPaint skPaint, SkiaSharp.SKRect skBounds)
        {
            if (skPaint.Shader is null)
            {
                return "";
            }

            var sb = new StringBuilder();

            sb.Append($"<Pen");

            if (skPaint.Shader is ShimSkiaSharp.ColorShader colorShader)
            {
                sb.Append($" Brush=\"{ToHexColor(colorShader.Color)}\"");
            }

            if (skPaint.StrokeWidth != 1.0)
            {
                sb.Append($" Thickness=\"{ToString(skPaint.StrokeWidth)}\"");
            }

            if (skPaint.StrokeCap != ShimSkiaSharp.SKStrokeCap.Butt)
            {
                sb.Append($" LineCap=\"{ToPenLineCap(skPaint.StrokeCap)}\"");
            }

            if (skPaint.StrokeJoin != ShimSkiaSharp.SKStrokeJoin.Bevel)
            {
                sb.Append($" LineJoin=\"{ToPenLineJoin(skPaint.StrokeJoin)}\"");
            }

            if (skPaint.StrokeMiter != 10.0)
            {
                sb.Append($" MiterLimit=\"{ToString(skPaint.StrokeMiter)}\"");
            }

            if (skPaint.Shader is not ShimSkiaSharp.ColorShader || (skPaint.PathEffect is ShimSkiaSharp.DashPathEffect { Intervals: { } }))
            {
                sb.Append($">{NewLine}");
            }
            else
            {
                sb.Append($"/>{NewLine}");
            }

            if (skPaint.PathEffect is ShimSkiaSharp.DashPathEffect dashPathEffect && dashPathEffect.Intervals is { })
            {
                var dashes = new List<double>();

                foreach (var interval in dashPathEffect.Intervals)
                {
                    dashes.Add(interval / skPaint.StrokeWidth);
                }

                var offset = dashPathEffect.Phase / skPaint.StrokeWidth;

                sb.Append($"  <Pen.DashStyle>{NewLine}");
                sb.Append($"    <DashStyle Dashes=\"{string.Join(",", dashes.Select(ToString))}\" Offset=\"{ToString(offset)}\"/>{NewLine}");
                sb.Append($"  </Pen.DashStyle>{NewLine}");
            }

            if (skPaint.Shader is not ShimSkiaSharp.ColorShader)
            {
                sb.Append($"  <Pen.Brush>{NewLine}");
                sb.Append(ToBrush(skPaint.Shader, skBounds));
                sb.Append($"  </Pen.Brush>{NewLine}");
            }

            if (skPaint.Shader is not ShimSkiaSharp.ColorShader || (skPaint.PathEffect is ShimSkiaSharp.DashPathEffect { Intervals: { } }))
            {
                sb.Append($"</Pen>{NewLine}");
            }

            return sb.ToString();

        }

        public static void ToXamlGeometryDrawing(SkiaSharp.SKPath path, ShimSkiaSharp.SKPaint skPaint, StringBuilder sb)
        {
            sb.Append($"<GeometryDrawing");

            if ((skPaint.Style == ShimSkiaSharp.SKPaintStyle.Fill || skPaint.Style == ShimSkiaSharp.SKPaintStyle.StrokeAndFill) && skPaint.Shader is ShimSkiaSharp.ColorShader colorShader)
            {
                sb.Append($" Brush=\"{ToHexColor(colorShader.Color)}\"");
            }

            sb.Append($" Geometry=\"{ToSvgPathData(path)}\"");

            var brush = default(string);
            var pen = default(string);

            if ((skPaint.Style == ShimSkiaSharp.SKPaintStyle.Fill || skPaint.Style == ShimSkiaSharp.SKPaintStyle.StrokeAndFill) && skPaint.Shader is not ShimSkiaSharp.ColorShader)
            {
                if (skPaint.Shader is { })
                {
                    brush = ToBrush(skPaint.Shader, path.Bounds);
                }
            }

            if (skPaint.Style == ShimSkiaSharp.SKPaintStyle.Stroke || skPaint.Style == ShimSkiaSharp.SKPaintStyle.StrokeAndFill)
            {
                if (skPaint.Shader is { })
                {
                    pen = ToPen(skPaint, path.Bounds);
                }
            }

            if (brush is not null || pen is not null)
            {
                sb.Append($">{NewLine}");
            }
            else
            {
                sb.Append($"/>{NewLine}");
            }

            if (brush is { })
            {
                sb.Append($"  <GeometryDrawing.Brush>{NewLine}");
                sb.Append($"{brush}");
                sb.Append($"  </GeometryDrawing.Brush>{NewLine}");
            }

            if (pen is { })
            {
                sb.Append($"  <GeometryDrawing.Pen>{NewLine}");
                sb.Append($"{pen}");
                sb.Append($"  </GeometryDrawing.Pen>{NewLine}");
            }

            if (brush is not null || pen is not null)
            {
                sb.Append($"</GeometryDrawing>{NewLine}");
            }
        }
        
        private enum LayerType
        {
            UnknownPaint,
            MatrixGroup,
            ClipPathGroup,
            MaskGroup,
            MaskBrush,
            OpacityGroup,
            FilterGroup
        }

        public static string ToXamlDrawingGroup(ShimSkiaSharp.SKPicture? skPicture, string? key = null)
        {
            if (skPicture?.Commands is null)
            {
                return "";
            }

            var sb = new StringBuilder();

            Write($"<DrawingGroup{(key is null ? "" : ($" x:Key=\"{key}\""))}>{NewLine}");

            var totalMatrixStack = new Stack<List<SkiaSharp.SKMatrix>>();
            var currentTotalMatrixList = new List<SkiaSharp.SKMatrix>();

            var clipPathStack = new Stack<List<SkiaSharp.SKPath>>();
            var currentClipPathList = new List<SkiaSharp.SKPath>();

            var layersStack = new Stack<(StringBuilder Builder, LayerType Type, object? Value)?>();

            foreach (var canvasCommand in skPicture.Commands)
            {
                switch (canvasCommand)
                {
                    case ShimSkiaSharp.ClipPathCanvasCommand(var clipPath, _, _):
                    {
                        var path = Svg.Skia.SkiaModelExtensions.ToSKPath(clipPath);
                        if (path is null)
                        {
                            break;
                        }

                        var clipGeometry = ToSvgPathData(path);

                        Write($"<DrawingGroup>{NewLine}");
                        Write($"  <DrawingGroup.ClipGeometry>{NewLine}");
                        Write($"    <StreamGeometry>{clipGeometry}</StreamGeometry>{NewLine}");
                        Write($"  </DrawingGroup.ClipGeometry>{NewLine}");

                        currentClipPathList.Add(path);

                        break;
                    }
                    case ShimSkiaSharp.ClipRectCanvasCommand(var skRect, _, _):
                    {
                        var rect = Svg.Skia.SkiaModelExtensions.ToSKRect(skRect);
                        var path = new SkiaSharp.SKPath();
                        path.AddRect(rect);

                        var clipGeometry = ToSvgPathData(path);

                        Write($"<DrawingGroup>{NewLine}");
                        Write($"  <DrawingGroup.ClipGeometry>{NewLine}");
                        Write($"    <StreamGeometry>{clipGeometry}</StreamGeometry>{NewLine}");
                        Write($"  </DrawingGroup.ClipGeometry>{NewLine}");

                        currentClipPathList.Add(path);

                        break;
                    }
                    case ShimSkiaSharp.SetMatrixCanvasCommand(var skMatrix):
                    {
                        var matrix = Svg.Skia.SkiaModelExtensions.ToSKMatrix(skMatrix);
                        if (matrix.IsIdentity)
                        {
                            break;
                        }

                        var previousMatrixList = new List<SkiaSharp.SKMatrix>();

                        foreach (var totalMatrixList in totalMatrixStack)
                        {
                            if (totalMatrixList.Count > 0)
                            {
                                var totalMatrix = totalMatrixList.FirstOrDefault();
                                previousMatrixList.Add(totalMatrix);
                            }
                        }

                        previousMatrixList.Reverse();

                        foreach (var previousMatrix in previousMatrixList)
                        {
                            var inverted = previousMatrix.Invert();
                            matrix = inverted.PreConcat(matrix);
                        }

                        Write($"<DrawingGroup>{NewLine}");
                        Write($"  <DrawingGroup.Transform>{NewLine}");
                        Write($"    <MatrixTransform Matrix=\"{ToMatrix(matrix)}\"/>{NewLine}");
                        Write($"  </DrawingGroup.Transform>{NewLine}");

                        currentTotalMatrixList.Add(matrix);

                        break;
                    }
                    case ShimSkiaSharp.SaveLayerCanvasCommand(_, var skPaint):
                    {
                        // Mask

                        if (skPaint is { } && skPaint.Shader is null && skPaint.ColorFilter is { } && skPaint.ImageFilter is null && skPaint.Color is { } skMaskEndColor && skMaskEndColor.Equals(s_transparentBlack))
                        {
                            SaveLayer(LayerType.MaskBrush, skPaint);

                            break;
                        }

                        if (skPaint is { } && skPaint.Shader is null && skPaint.ColorFilter is null && skPaint.ImageFilter is null && skPaint.Color is { } skMaskStartColor && skMaskStartColor.Equals(s_transparentBlack))
                        {
                            SaveLayer(LayerType.MaskGroup, skPaint);

                            break;
                        }

                        // Opacity

                        if (skPaint is { } && skPaint.Shader is null && skPaint.ColorFilter is null && skPaint.ImageFilter is null && skPaint.Color is { } skOpacityColor && skOpacityColor.Alpha < OpaqueAlpha)
                        {
                            SaveLayer(LayerType.OpacityGroup, skPaint);

                            break;
                        }

                        // Filter

                        if (skPaint is { } && skPaint.Shader is null && skPaint.ColorFilter is null && skPaint.ImageFilter is { } && skPaint.Color is { } skFilterColor && skFilterColor.Equals(s_transparentBlack))
                        {
                            SaveLayer(LayerType.FilterGroup, skPaint);

                            break;
                        }

                        SaveLayer(LayerType.UnknownPaint, skPaint);

                        break;
                    }
                    case ShimSkiaSharp.SaveCanvasCommand:
                    {
                        layersStack.Push(null);

                        Save();

                        break;
                    }
                    case ShimSkiaSharp.RestoreCanvasCommand:
                    {
                        Restore();

                        break;
                    }
                    case ShimSkiaSharp.DrawPathCanvasCommand(var skPath, var skPaint):
                    {
                        var path = Svg.Skia.SkiaModelExtensions.ToSKPath(skPath);
                        if (!path.IsEmpty)
                        {
                            ToXamlGeometryDrawing(path, skPaint, sb);
                        }

                        break;
                    }
                    case ShimSkiaSharp.DrawTextCanvasCommand(var text, var x, var y, var skPaint):
                    {
                        var paint = Svg.Skia.SkiaModelExtensions.ToSKPaint(skPaint);
                        var path = paint.GetTextPath(text, x, y);
                        if (!path.IsEmpty)
                        {
                            Debug($"{text}");

                            ToXamlGeometryDrawing(path, skPaint, sb);
                        }
      
                        break;
                    }
                    case ShimSkiaSharp.DrawTextOnPathCanvasCommand(var text, var skPath, var hOffset, var vOffset, var skPaint):
                    {
                        // TODO:

                        Debug($"TODO: TextOnPath");

                        break;
                    }
                    case ShimSkiaSharp.DrawTextBlobCanvasCommand(var skTextBlob, var x, var y, var skPaint):
                    {
                        // TODO:
 
                        Debug($"TODO: TextBlob");

                        break;
                    }
                    case ShimSkiaSharp.DrawImageCanvasCommand(var skImage, var skRect, var dest, var skPaint):
                    {
                        // TODO:

                        Debug($"TODO: Image");

                        break;
                    }
                }
            }

            void Debug(string message)
            {
#if DEBUG
                sb.Append($"<!-- {message} -->{NewLine}");
#endif
            }

            void Write(string value)
            {
                sb.Append(value);
            }

            void SaveLayer(LayerType type, object? value)
            {
                Debug($"SaveLayer({type})");

                layersStack.Push((sb, type, value));
                sb = new StringBuilder();

                Save();
            }

            void Save()
            {
                Debug($"Save()");

                // matrix

                totalMatrixStack.Push(currentTotalMatrixList);
                currentTotalMatrixList = new List<SkiaSharp.SKMatrix>();

                // clip-path

                clipPathStack.Push(currentClipPathList);
                currentClipPathList = new List<SkiaSharp.SKPath>();
            }

            void Restore()
            {
                Debug($"Restore()");

                // layers

                var layer = layersStack.Count > 0 ? layersStack.Pop() : null;
                if (layer is { })
                {
                    var (builder, type, value) = layer.Value;
                    var content = sb.ToString();

                    sb = builder;

                    Debug($"StartLayer({type})");

                    switch (type)
                    {
                        case LayerType.UnknownPaint:
                        {
                            if (value is not ShimSkiaSharp.SKPaint)
                            {
                                break;
                            }

                            Write(content);

                            break;
                        }
                        case LayerType.MaskGroup:
                        {
                            if (value is not ShimSkiaSharp.SKPaint)
                            {
                                break;
                            }

                            Write($"<DrawingGroup>{NewLine}");
                            Write(content);
                            Write($"</DrawingGroup>{NewLine}");

                            break;
                        }
                        case LayerType.MaskBrush:
                        {
                            if (value is not ShimSkiaSharp.SKPaint)
                            {
                                break;
                            }

                            Write($"<DrawingGroup.OpacityMask>{NewLine}");
                            Write($"  <VisualBrush");
                            Write($" TileMode=\"None\"");
                            //Write($" SourceRect=\"{ToRect(sourceRect)}\"");
                            //Write($" DestinationRect=\"{ToRect(destinationRect)}\"");
                            Write($">{NewLine}");
                            Write($"    <VisualBrush.Visual>{NewLine}");
                            Write($"      <Image>{NewLine}");
                            Write($"        <DrawingImage>{NewLine}");
                            Write($"          <DrawingGroup>{NewLine}");
                            Write(content);
                            Write($"          </DrawingGroup>{NewLine}");
                            Write($"        </DrawingImage>{NewLine}");
                            Write($"      </Image>{NewLine}");
                            Write($"    </VisualBrush.Visual>{NewLine}");
                            Write($"  </VisualBrush>{NewLine}");
                            Write($"</DrawingGroup.OpacityMask>{NewLine}");

                            break;
                        }
                        case LayerType.OpacityGroup:
                        {
                            if (value is not ShimSkiaSharp.SKPaint paint)
                            {
                                break;
                            }

                            if (paint.Color is { } skColor)
                            {
                                Write($"<DrawingGroup Opacity=\"{ToString(skColor.Alpha / 255.0)}\">{NewLine}");
                                Write(content);
                                Write($"</DrawingGroup>{NewLine}");
                            }

                            break;
                        }
                        case LayerType.FilterGroup:
                        {
                            if (value is not ShimSkiaSharp.SKPaint)
                            {
                                break;
                            }

                            Write(content);

                            break;
                        }
                    }

                    Debug($"EndLayer({type})");
                }

                // clip-path
                        
                foreach (var _ in currentClipPathList)
                {
                    Write($"</DrawingGroup>{NewLine}");
                }

                currentClipPathList.Clear();

                if (clipPathStack.Count > 0)
                {
                    currentClipPathList = clipPathStack.Pop();
                }

                // matrix
      
                foreach (var _ in currentTotalMatrixList)
                {
                    Write($"</DrawingGroup>{NewLine}");
                }

                currentTotalMatrixList.Clear();

                if (totalMatrixStack.Count > 0)
                {
                    currentTotalMatrixList = totalMatrixStack.Pop();
                }
            }

            Restore();

            Write($"</DrawingGroup>");

            return sb.ToString();
        }

        public static string ToXamlImage(ShimSkiaSharp.SKPicture? skPicture, string? key = null)
        {
            var sb = new StringBuilder();

            sb.Append($"<Image{(key is null ? "" : ($" x:Key=\"{key}\""))}>{NewLine}");
            sb.Append($"  <DrawingImage>{NewLine}");
            sb.Append(ToXamlDrawingGroup(skPicture, key: null));
            sb.Append($"  </DrawingImage>{NewLine}");
            sb.Append($"</Image>");

            return sb.ToString();
        }
        
        public static string ToXamlStyles(List<string> paths, bool generateImage = false, bool generatePreview = true)
        {
            var results = new List<(string Path, string Key, string Xaml)>();

            foreach (var path in paths)
            {
                try
                {
                    var svg = new Svg.Skia.SKSvg();
                    svg.Load(path);
                    if (svg.Model is null)
                    {
                        continue;
                    }

                    var key = $"_{CreateKey(path)}";
                    if (generateImage)
                    {
                        var xaml = ToXamlImage(svg.Model, key);
                        results.Add((path, key, xaml));
                    }
                    else
                    {
                        var xaml = ToXamlDrawingGroup(svg.Model, key);
                        results.Add((path, key, xaml));
                    }
                }
                catch
                {
                    // ignored
                }
            }
 
            var sb = new StringBuilder();

            sb.Append($"<Styles xmlns=\"https://github.com/avaloniaui\"{NewLine}");
            sb.Append($"        xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">{NewLine}");

            if (generatePreview)
            {
                sb.Append($"  <Design.PreviewWith>");
                sb.Append($"    <ScrollViewer HorizontalScrollBarVisibility=\"Auto\" VerticalScrollBarVisibility=\"Auto\">");
                sb.Append($"      <WrapPanel ItemWidth=\"50\" ItemHeight=\"50\" MaxWidth=\"400\">");

                foreach (var result in results)
                {
                    if (generateImage)
                    {
                        sb.Append($"        <ContentControl Content=\"{{DynamicResource {result.Key}}}\"/>");
                    }
                    else
                    {
                        sb.Append($"        <Image>");
                        sb.Append($"            <Image.Source>");
                        sb.Append($"                <DrawingImage Drawing=\"{{DynamicResource {result.Key}}}\"/>");
                        sb.Append($"            </Image.Source>");
                        sb.Append($"        </Image>"); 
                    }
                }

                sb.Append($"      </WrapPanel>");
                sb.Append($"    </ScrollViewer>");
                sb.Append($"  </Design.PreviewWith>");
            }

            sb.Append($"  <Style>{NewLine}");
            sb.Append($"    <Style.Resources>{NewLine}");

            foreach (var result in results)
            {
                sb.Append($"<!-- {Path.GetFileName(result.Path)} -->{NewLine}");
                sb.Append(result.Xaml);
                sb.Append(NewLine);
            }

            sb.Append($"    </Style.Resources>{NewLine}");
            sb.Append($"  </Style>{NewLine}");
            sb.Append($"</Styles>");

            return sb.ToString();
        }

        public static string CreateKey(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            string key = name.Replace("-", "_");
            return $"_{key}";
        }

        public static string Format(string xml)
        {
            try
            {
                using var ms = new MemoryStream();
                using var writer = new XmlTextWriter(ms, Encoding.UTF8);
                var document = new XmlDocument();
                document.LoadXml(xml);
                writer.Formatting = Formatting.Indented;
                writer.Indentation = 2;
                writer.IndentChar = ' ';
                document.WriteContentTo(writer);
                writer.Flush();
                ms.Flush();
                ms.Position = 0;
                using var sReader = new StreamReader(ms);
                return sReader.ReadToEnd();
            }
            catch
            {
                // ignored
            }

            return "";
        }
    }
}
