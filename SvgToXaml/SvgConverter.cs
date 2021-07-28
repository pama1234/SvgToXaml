﻿using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ShimSkiaSharp;

namespace SvgToXaml
{
    public static class SvgConverter
    {
        public static string ToString(double value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static string ToHexColor(SKColor skColor, string indent = "")
        {
            return $"{indent}#{skColor.Alpha:X2}{skColor.Red:X2}{skColor.Green:X2}{skColor.Blue:X2}";
        }

        public static string ToPoint(SKPoint skPoint)
        {
            return $"{ToString(skPoint.X)},{ToString(skPoint.Y)}";
        }

        public static string ToPoint(SkiaSharp.SKPoint skPoint)
        {
            return $"{ToString(skPoint.X)},{ToString(skPoint.Y)}";
        }

        public static string ToGradientSpreadMethod(SKShaderTileMode shaderTileMode)
        {
            switch (shaderTileMode)
            {
                default:
                case SKShaderTileMode.Clamp:
                    return "Pad";

                case SKShaderTileMode.Repeat:
                    return "Repeat";

                case SKShaderTileMode.Mirror:
                    return "Reflect";
            }
        }

        public static string ToBrush(SKShader skShader, SkiaSharp.SKRect skBounds, string indent = "")
        {
            if (skShader is ColorShader colorShader)
            {
                var brush = "";

                brush += $"{indent}<SolidColorBrush";
                brush += $" Color=\"{ToHexColor(colorShader.Color)}\"";
                brush += $"/>\r\n";

                return brush;
            }

            if (skShader is LinearGradientShader linearGradientShader)
            {
                var brush = "";

                var start = Svg.Skia.SkiaModelExtensions.ToSKPoint(linearGradientShader.Start);
                var end = Svg.Skia.SkiaModelExtensions.ToSKPoint(linearGradientShader.End);

                if (linearGradientShader.LocalMatrix is { })
                {
                    var localMatrix = Svg.Skia.SkiaModelExtensions.ToSKMatrix(linearGradientShader.LocalMatrix.Value);
                    start = localMatrix.MapPoint(start);
                    end = localMatrix.MapPoint(end);
                }

                brush += $"{indent}<LinearGradientBrush";
                brush += $" StartPoint=\"{ToPoint(start)}\"";
                brush += $" EndPoint=\"{ToPoint(end)}\"";
                brush += $" SpreadMethod=\"{ToGradientSpreadMethod(linearGradientShader.Mode)}\">\r\n";
                brush += $"{indent}  <LinearGradientBrush.GradientStops>\r\n";

                if (linearGradientShader.Colors is { } && linearGradientShader.ColorPos is { })
                {
                    for (var i = 0; i < linearGradientShader.Colors.Length; i++)
                    {
                        var color = ToHexColor(linearGradientShader.Colors[i]);
                        var offset = ToString(linearGradientShader.ColorPos[i]);
                        brush += $"{indent}    <GradientStop Offset=\"{offset}\" Color=\"{color}\"/>\r\n";
                    }
                }

                brush += $"{indent}  </LinearGradientBrush.GradientStops>\r\n";
                brush += $"{indent}</LinearGradientBrush>\r\n";

                return brush;
            }

            if (skShader is TwoPointConicalGradientShader twoPointConicalGradientShader)
            {
                var brush = "";

                // NOTE: twoPointConicalGradientShader.StartRadius is always 0.0
                var startRadius = twoPointConicalGradientShader.StartRadius;

                // TODO: Avalonia is passing 'radius' to 'SKShader.CreateTwoPointConicalGradient' as 'startRadius'
                // TODO: but we need to pass it as 'endRadius' to 'SKShader.CreateTwoPointConicalGradient'
                var endRadius = twoPointConicalGradientShader.EndRadius;

                var center = Svg.Skia.SkiaModelExtensions.ToSKPoint(twoPointConicalGradientShader.Start);
                var gradientOrigin = Svg.Skia.SkiaModelExtensions.ToSKPoint(twoPointConicalGradientShader.End);

                if (twoPointConicalGradientShader.LocalMatrix is { })
                {
                    var localMatrix = Svg.Skia.SkiaModelExtensions.ToSKMatrix(twoPointConicalGradientShader.LocalMatrix.Value);
                    center = localMatrix.MapPoint(center);
                    gradientOrigin = localMatrix.MapPoint(gradientOrigin);

                    var radius = localMatrix.MapVector(new SkiaSharp.SKPoint(endRadius, 0));
                    endRadius = radius.X;
                }

                endRadius = endRadius / skBounds.Width;

                brush += $"{indent}<RadialGradientBrush";
                brush += $" Center=\"{ToPoint(center)}\"";
                brush += $" GradientOrigin=\"{ToPoint(gradientOrigin)}\"";
                brush += $" Radius=\"{ToString(endRadius)}\"";
                brush += $" SpreadMethod=\"{ToGradientSpreadMethod(twoPointConicalGradientShader.Mode)}\">\r\n";
                brush += $"{indent}  <RadialGradientBrush.GradientStops>\r\n";

                if (twoPointConicalGradientShader.Colors is { } && twoPointConicalGradientShader.ColorPos is { })
                {
                    for (var i = 0; i < twoPointConicalGradientShader.Colors.Length; i++)
                    {
                        var color = ToHexColor(twoPointConicalGradientShader.Colors[i]);
                        var offset = ToString(twoPointConicalGradientShader.ColorPos[i]);
                        brush += $"{indent}    <GradientStop Offset=\"{offset}\" Color=\"{color}\"/>\r\n";
                    }
                }

                brush += $"{indent}  </RadialGradientBrush.GradientStops>\r\n";
                brush += $"{indent}</RadialGradientBrush>\r\n";

                return brush;
            }

            if (skShader is PictureShader pictureShader)
            {
                // TODO:
            }

            return "";
        }

        public static string ToPenLineCap(SKStrokeCap strokeCap)
        {
            switch (strokeCap)
            {
                default:
                case SKStrokeCap.Butt:
                    return "Flat";

                case SKStrokeCap.Round:
                    return "Round";

                case SKStrokeCap.Square:
                    return "Square";
            }
        }

        public static string ToPenLineJoin(SKStrokeJoin strokeJoin)
        {
            switch (strokeJoin)
            {
                default:
                case SKStrokeJoin.Miter:
                    return "Miter";

                case SKStrokeJoin.Round:
                    return "Round";

                case SKStrokeJoin.Bevel:
                    return "Bevel";
            }
        }

        private static string ToPen(SKPaint skPaint, SkiaSharp.SKRect skBounds, string indent = "")
        {
            if (skPaint.Shader is { })
            {
                var pen = "";

                pen += $"{indent}<Pen";

                if (skPaint.Shader is ColorShader colorShader)
                {
                    pen += $" Brush=\"{ToHexColor(colorShader.Color)}\"";
                }

                if (skPaint.StrokeWidth != 1.0)
                {
                    pen += $" Thickness=\"{ToString(skPaint.StrokeWidth)}\"";
                }

                if (skPaint.StrokeCap != SKStrokeCap.Butt)
                {
                    pen += $" LineCap=\"{ToPenLineCap(skPaint.StrokeCap)}\"";
                }

                if (skPaint.StrokeJoin != SKStrokeJoin.Bevel)
                {
                    pen += $" LineJoin=\"{ToPenLineJoin(skPaint.StrokeJoin)}\"";
                }

                if (skPaint.StrokeMiter != 10.0)
                {
                    pen += $" MiterLimit=\"{ToString(skPaint.StrokeMiter)}\"";
                }

                if (skPaint.Shader is not ColorShader || (skPaint.PathEffect is DashPathEffect { Intervals: { } }))
                {
                    pen += $">\r\n";
                }
                else
                {
                    pen += $"/>\r\n";
                }

                if (skPaint.PathEffect is DashPathEffect dashPathEffect && dashPathEffect.Intervals is { })
                {
                    var dashes = new List<double>();

                    foreach (var interval in dashPathEffect.Intervals)
                    {
                        dashes.Add(interval / skPaint.StrokeWidth);
                    }

                    var offset = dashPathEffect.Phase / skPaint.StrokeWidth;

                    pen += $"{indent}  <Pen.DashStyle>\r\n";
                    pen += $"{indent}    <DashStyle Dashes=\"{string.Join(",", dashes.Select(ToString))}\" Offset=\"{ToString(offset)}\"/>\r\n";
                    pen += $"{indent}  </Pen.DashStyle>\r\n";
                }

                if (skPaint.Shader is not ColorShader)
                {
                    pen += $"{indent}  <Pen.Brush>\r\n";
                    pen += ToBrush(skPaint.Shader, skBounds, indent + "    ");
                    pen += $"{indent}  </Pen.Brush>\r\n";
                }

                if (skPaint.Shader is not ColorShader || (skPaint.PathEffect is DashPathEffect { Intervals: { } }))
                {
                    pen += $"{indent}</Pen>\r\n";
                }

                return pen;
            }

            return "";
        }

        private static string ToSvgPathData(SkiaSharp.SKPath path)
        {
            var data = path.ToSvgPathData();

            if (path.FillType == SkiaSharp.SKPathFillType.EvenOdd)
            {
                // EvenOdd
                data = $"F0 {data}";
            }
            else
            {
                // Nonzero 
                data = $"F1 {data}";
            }

            return data;
        }

        public static string ToXaml(SKPicture? skPicture)
        {
            var sb = new StringBuilder();

            sb.Append($"<Image>\r\n");
            sb.Append($"  <DrawingImage>\r\n");
            sb.Append($"    <DrawingGroup>\r\n");

            var indent = "      ";

            if (skPicture?.Commands is { })
            {
                var totalMatrixStack = new Stack<SkiaSharp.SKMatrix>();
                var totalMatrix = SkiaSharp.SKMatrix.Identity;

                var totalClipPaths = new List<(SkiaSharp.SKPath Path, SkiaSharp.SKClipOperation Operation, bool Antialias)>();
                var totalClipPathsStack = new Stack<List<(SkiaSharp.SKPath Path, SkiaSharp.SKClipOperation Operation, bool Antialias)>>();

#if false
                if (!totalMatrix.IsIdentity)
                {
                    sb.Append($"{indent}  <DrawingGroup.Transform>\r\n");
                    sb.Append($"{indent}    <MatrixTransform Matrix=\"" +
                              $"{ToString(totalMatrix.ScaleX)}," +
                              $"{ToString(totalMatrix.SkewY)}," +
                              $"{ToString(totalMatrix.SkewX)}," +
                              $"{ToString(totalMatrix.ScaleY)}," +
                              $"{ToString(totalMatrix.TransX)}," +
                              $"{ToString(totalMatrix.TransY)}" +
                              $"\" />\r\n");
                    sb.Append($"{indent}  </DrawingGroup.Transform>\r\n");
                }
#endif

                foreach (var canvasCommand in skPicture.Commands)
                {
                    switch (canvasCommand)
                    {
                        case ClipPathCanvasCommand(var clipPath, var skClipOperation, var antialias):
                        {
                            var path = Svg.Skia.SkiaModelExtensions.ToSKPath(clipPath);
                            var operation = Svg.Skia.SkiaModelExtensions.ToSKClipOperation(skClipOperation);

                            if (path is { })
                            {
                                // TODO:
                                path.Transform(totalMatrix);

                                totalClipPaths.Add((path, operation, antialias));

                                // TODO:
                            }

                            break;
                        }
                        case ClipRectCanvasCommand(var skRect, var skClipOperation, var antialias):
                        {
                            var rect = Svg.Skia.SkiaModelExtensions.ToSKRect(skRect);
                            var operation = Svg.Skia.SkiaModelExtensions.ToSKClipOperation(skClipOperation);

                            var path = new SkiaSharp.SKPath();
                            path.AddRect(rect);
                            // TODO:
                            path.Transform(totalMatrix);

                            // TODO:
                            totalClipPaths.Add((path, operation, antialias));

                            break;
                        }
                        case SaveCanvasCommand:
                        {
                            totalMatrixStack.Push(totalMatrix);

                            totalClipPathsStack.Push(totalClipPaths.ToList());

                            // TODO:

                            break;
                        }
                        case RestoreCanvasCommand:
                        {
                            // TODO:
                            if (totalMatrixStack.Count > 0)
                            {
                                totalMatrix = totalMatrixStack.Pop();
                            }

                            // TODO:
                            if (totalClipPathsStack.Count > 0)
                            {
                                totalClipPaths = totalClipPathsStack.Pop();
                            }

                            // TODO:

                            break;
                        }
                        case SetMatrixCanvasCommand(var skMatrix):
                        {
                            totalMatrix = Svg.Skia.SkiaModelExtensions.ToSKMatrix(skMatrix);

                            // TODO:

                            break;
                        }
                        case SaveLayerCanvasCommand(var count, var skPaint):
                        {
                            // TODO:

                            break;
                        }
                        case DrawImageCanvasCommand(var skImage, var skRect, var dest, var skPaint):
                        {
                            // TODO:

                            break;
                        }
                        case DrawPathCanvasCommand(var skPath, var skPaint):
                        {
                            sb.Append($"{indent}<GeometryDrawing");

                            if ((skPaint.Style == SKPaintStyle.Fill || skPaint.Style == SKPaintStyle.StrokeAndFill) && skPaint.Shader is ColorShader colorShader)
                            {
                                sb.Append($" Brush=\"{ToHexColor(colorShader.Color)}\"");
                            }

                            var path = Svg.Skia.SkiaModelExtensions.ToSKPath(skPath);

                            var clipPath = default(SkiaSharp.SKPath);

                            if (totalClipPaths.Count > 0)
                            {
                                for (var index = 0; index < totalClipPaths.Count; index++)
                                {
                                    if (clipPath is null)
                                    {
                                        clipPath = totalClipPaths[index].Path;
                                    }
                                    else
                                    {
                                        clipPath = clipPath.Op(totalClipPaths[index].Path, SkiaSharp.SKPathOp.Intersect);
                                    }
                                }
                            }

                            path.Transform(totalMatrix);

                            if (clipPath is { })
                            {
                                // TODO:
                                // path = clipPath.Op(path, SkiaSharp.SKPathOp.Intersect);
                            }

                            var data = ToSvgPathData(path);

                            sb.Append($" Geometry=\"{data}\"");

                            var brush = default(string);
                            var pen = default(string);

                            if ((skPaint.Style == SKPaintStyle.Fill || skPaint.Style == SKPaintStyle.StrokeAndFill) && skPaint.Shader is not ColorShader)
                            {
                                if (skPaint.Shader is { })
                                {
                                    brush = ToBrush(skPaint.Shader, path.Bounds, $"{indent}    ");
                                }
                            }

                            if (skPaint.Style == SKPaintStyle.Stroke || skPaint.Style == SKPaintStyle.StrokeAndFill)
                            {
                                if (skPaint.Shader is { })
                                {
                                    pen = ToPen(skPaint, path.Bounds, $"{indent}    ");
                                }
                            }

                            if (brush is not null || pen is not null)
                            {
                                sb.Append($">\r\n");
                            }
                            else
                            {
                                sb.Append($"/>\r\n");
                            }

                            if (brush is { })
                            {
                                sb.Append($"{indent}  <GeometryDrawing.Brush>\r\n");
                                sb.Append($"{brush}");
                                sb.Append($"{indent}  </GeometryDrawing.Brush>\r\n");
                            }

                            if (pen is { })
                            {
                                sb.Append($"{indent}  <GeometryDrawing.Pen>\r\n");
                                sb.Append($"{pen}");
                                sb.Append($"{indent}  </GeometryDrawing.Pen>\r\n");
                            }

                            if (brush is not null || pen is not null)
                            {
                                sb.Append($"{indent}</GeometryDrawing>\r\n");
                            }

                            break;
                        }
                        case DrawTextBlobCanvasCommand(var skTextBlob, var f, var y, var skPaint):
                        {
                            // TODO:

                            break;
                        }
                        case DrawTextCanvasCommand(var text, var f, var y, var skPaint):
                        {
                            // TODO:

                            break;
                        }
                        case DrawTextOnPathCanvasCommand(var text, var skPath, var hOffset, var vOffset, var skPaint):
                        {
                            // TODO:

                            break;
                        }
                    }
                }
            }

            sb.Append($"    </DrawingGroup>\r\n");
            sb.Append($"  </DrawingImage>\r\n");
            sb.Append($"</Image>");

            return sb.ToString();
        }
    }
}
