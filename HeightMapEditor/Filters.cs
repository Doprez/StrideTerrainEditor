// Original classes and methods by Dewald Esterhuizen, modified by Idomeneas
using Stride.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Vector3 = System.Numerics.Vector3;
using Vector2 = System.Numerics.Vector2;
using Point = System.Drawing.Point;
using Stride.Core.Mathematics;
using Stride.Graphics;
using System.Runtime.InteropServices;
using Stride.Engine;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Rendering.Materials;
using Stride.Rendering;
using System.Reflection;
using System.Drawing;
using Image = System.Drawing.Image;
using Color = Stride.Core.Mathematics.Color;
using Stride.TextureConverter;

namespace HeightMapEditor
{

    public abstract class ConvolutionFilterBase
    {
        public abstract string FilterName
        {
            get;
        }
        public abstract double Factor
        {
            get;
        }
        public abstract double Bias
        {
            get;
        }
        public abstract double[,] FilterMatrix
        {
            get;
        }
    }

    //Image blurring is typically used to reduce image noise and detail.The filter’s matrix size affects the level of blurring. A larger matrix results in higher level of blurring, whereas a smaller matrix results in a lesser level of blurring.
    public class Blur3x3Filter : ConvolutionFilterBase
    {
        public override string FilterName
        {
            get { return "Blur3x3Filter"; }
        }
        private double factor = 1.0;
        public override double Factor
        {
            get { return factor; }
        }
        private double bias = 0.0;
        public override double Bias
        {
            get { return bias; }
        }
        private double[,] filterMatrix =
            new double[,] { { 0.0, 0.2, 0.0, },
                        { 0.2, 0.2, 0.2, },
                        { 0.0, 0.2, 0.2, }, };
        public override double[,] FilterMatrix
        {
            get { return filterMatrix; }
        }
    }

    public class Blur5x5Filter : ConvolutionFilterBase
    {
        public override string FilterName
        {
            get { return "Blur5x5Filter"; }
        }
        private double factor = 1.0 / 13.0;
        public override double Factor
        {
            get { return factor; }
        }
        private double bias = 0.0;
        public override double Bias
        {
            get { return bias; }
        }
        private double[,] filterMatrix =
            new double[,] { { 0, 0, 1, 0, 0, },
                        { 0, 1, 1, 1, 0, },
                        { 1, 1, 1, 1, 1, },
                        { 0, 1, 1, 1, 0, },
                        { 0, 0, 1, 0, 0, }, };
        public override double[,] FilterMatrix
        {
            get { return filterMatrix; }
        }
    }

    public class Gaussian3x3BlurFilter : ConvolutionFilterBase
    {
        public override string FilterName
        {
            get { return "Gaussian3x3BlurFilter"; }
        }
        private double factor = 1.0 / 16.0;
        public override double Factor
        {
            get { return factor; }
        }
        private double bias = 0.0;
        public override double Bias
        {
            get { return bias; }
        }
        private double[,] filterMatrix =
            new double[,] { { 1, 2, 1, },
                        { 2, 4, 2, },
                        { 1, 2, 1, }, };
        public override double[,] FilterMatrix
        {
            get { return filterMatrix; }
        }
    }

    public class Gaussian5x5BlurFilter : ConvolutionFilterBase
    {
        public override string FilterName
        {
            get { return "Gaussian5x5BlurFilter"; }
        }
        private double factor = 1.0 / 159.0;
        public override double Factor
        {
            get { return factor; }
        }
        private double bias = 0.0;
        public override double Bias
        {
            get { return bias; }
        }
        private double[,] filterMatrix =
            new double[,] { { 2, 04, 05, 04, 2, },
                        { 4, 09, 12, 09, 4, },
                        { 5, 12, 15, 12, 5, },
                        { 4, 09, 12, 09, 4, },
                        { 2, 04, 05, 04, 2, }, };
        public override double[,] FilterMatrix
        {
            get { return filterMatrix; }
        }
    }
    public class SoftenFilter : ConvolutionFilterBase
    {
        public override string FilterName
        {
            get { return "SoftenFilter"; }
        }
        private double factor = 1.0 / 8.0;
        public override double Factor
        {
            get { return factor; }
        }
        private double bias = 0.0;
        public override double Bias
        {
            get { return bias; }
        }
        private double[,] filterMatrix =
            new double[,] { { 1, 1, 1, },
                        { 1, 1, 1, },
                        { 1, 1, 1, }, };
        public override double[,] FilterMatrix
        {
            get { return filterMatrix; }
        }
    }
    public class MotionBlurFilter : ConvolutionFilterBase
    {
        public override string FilterName
        {
            get { return "MotionBlurFilter"; }
        }
        private double factor = 1.0 / 18.0;
        public override double Factor
        {
            get { return factor; }
        }
        private double bias = 0.0;
        public override double Bias
        {
            get { return bias; }
        }
        private double[,] filterMatrix =
            new double[,] { { 1, 0, 0, 0, 0, 0, 0, 0, 1, },
                        { 0, 1, 0, 0, 0, 0, 0, 1, 0, },
                        { 0, 0, 1, 0, 0, 0, 1, 0, 0, },
                        { 0, 0, 0, 1, 0, 1, 0, 0, 0, },
                        { 0, 0, 0, 0, 1, 0, 0, 0, 0, },
                        { 0, 0, 0, 1, 0, 1, 0, 0, 0, },
                        { 0, 0, 1, 0, 0, 0, 1, 0, 0, },
                        { 0, 1, 0, 0, 0, 0, 0, 1, 0, },
                        { 1, 0, 0, 0, 0, 0, 0, 0, 1, }, };
        public override double[,] FilterMatrix
        {
            get { return filterMatrix; }
        }
    }

    public class MotionBlurLeftToRightFilter : ConvolutionFilterBase
    {
        public override string FilterName
        {
            get { return "MotionBlurLeftToRightFilter"; }
        }
        private double factor = 1.0 / 9.0;
        public override double Factor
        {
            get { return factor; }
        }
        private double bias = 0.0;
        public override double Bias
        {
            get { return bias; }
        }
        private double[,] filterMatrix =
            new double[,] { { 1, 0, 0, 0, 0, 0, 0, 0, 0, },
                        { 0, 1, 0, 0, 0, 0, 0, 0, 0, },
                        { 0, 0, 1, 0, 0, 0, 0, 0, 0, },
                        { 0, 0, 0, 1, 0, 0, 0, 0, 0, },
                        { 0, 0, 0, 0, 1, 0, 0, 0, 0, },
                        { 0, 0, 0, 0, 0, 1, 0, 0, 0, },
                        { 0, 0, 0, 0, 0, 0, 1, 0, 0, },
                        { 0, 0, 0, 0, 0, 0, 0, 1, 0, },
                        { 0, 0, 0, 0, 0, 0, 0, 0, 1, }, };
        public override double[,] FilterMatrix
        {
            get { return filterMatrix; }
        }
    }

    public class MotionBlurRightToLeftFilter : ConvolutionFilterBase
    {
        public override string FilterName
        {
            get { return "MotionBlurRightToLeftFilter"; }
        }
        private double factor = 1.0 / 9.0;
        public override double Factor
        {
            get { return factor; }
        }
        private double bias = 0.0;
        public override double Bias
        {
            get { return bias; }
        }
        private double[,] filterMatrix =
            new double[,] { { 0, 0, 0, 0, 0, 0, 0, 0, 1, },
                        { 0, 0, 0, 0, 0, 0, 0, 1, 0, },
                        { 0, 0, 0, 0, 0, 0, 1, 0, 0, },
                        { 0, 0, 0, 0, 0, 1, 0, 0, 0, },
                        { 0, 0, 0, 0, 1, 0, 0, 0, 0, },
                        { 0, 0, 0, 1, 0, 0, 0, 0, 0, },
                        { 0, 0, 1, 0, 0, 0, 0, 0, 0, },
                        { 0, 1, 0, 0, 0, 0, 0, 0, 0, },
                        { 1, 0, 0, 0, 0, 0, 0, 0, 0, }, };
        public override double[,] FilterMatrix
        {
            get { return filterMatrix; }
        }
    }

    public class SharpenFilter : ConvolutionFilterBase
    {
        public override string FilterName
        {
            get { return "SharpenFilter"; }
        }
        private double factor = 1.0;
        public override double Factor
        {
            get { return factor; }
        }
        private double bias = 0.0;
        public override double Bias
        {
            get { return bias; }
        }
        private double[,] filterMatrix =
            new double[,] { { -1, -1, -1, },
                        { -1,  9, -1, },
                        { -1, -1, -1, }, };
        public override double[,] FilterMatrix
        {
            get { return filterMatrix; }
        }
    }
    public class IntenseSharpenFilter : ConvolutionFilterBase
    {
        public override string FilterName
        {
            get { return "IntenseSharpenFilter"; }
        }
        private double factor = 1.0;
        public override double Factor
        {
            get { return factor; }
        }
        private double bias = 0.0;
        public override double Bias
        {
            get { return bias; }
        }
        private double[,] filterMatrix =
            new double[,] { { 1,  1, 1, },
                        { 1, -7, 1, },
                        { 1,  1, 1, }, };
        public override double[,] FilterMatrix
        {
            get { return filterMatrix; }
        }
    }
    
    //Edge detection is the first step towards feature detection and feature extraction in digital image processing.Edges are generally perceived in images in areas exhibiting sudden differences in brightness.

    public class EdgeDetectionFilter : ConvolutionFilterBase
    {
        public override string FilterName
        {
            get { return "EdgeDetectionFilter"; }
        }
        private double factor = 1.0;
        public override double Factor
        {
            get { return factor; }
        }
        private double bias = 0.0;
        public override double Bias
        {
            get { return bias; }
        }
        private double[,] filterMatrix =
            new double[,] { { -1, -1, -1, },
                        { -1,  8, -1, },
                        { -1, -1, -1, }, };
        public override double[,] FilterMatrix
        {
            get { return filterMatrix; }
        }
    }

    public class EdgeDetection45DegreeFilter : ConvolutionFilterBase
    {
        public override string FilterName
        {
            get { return "EdgeDetection45DegreeFilter"; }
        }
        private double factor = 1.0;
        public override double Factor
        {
            get { return factor; }
        }
        private double bias = 0.0;
        public override double Bias
        {
            get { return bias; }
        }
        private double[,] filterMatrix =
            new double[,] { { -1,  0,  0,  0,  0, },
                        {  0, -2,  0,  0,  0, },
                        {  0,  0,  6,  0,  0, },
                        {  0,  0,  0, -2,  0, },
                        {  0,  0,  0,  0, -1, }, };
        public override double[,] FilterMatrix
        {
            get { return filterMatrix; }
        }
    }
    public class HorizontalEdgeDetectionFilter : ConvolutionFilterBase
    {
        public override string FilterName
        {
            get { return "HorizontalEdgeDetectionFilter"; }
        }
        private double factor = 1.0;
        public override double Factor
        {
            get { return factor; }
        }
        private double bias = 0.0;
        public override double Bias
        {
            get { return bias; }
        }
        private double[,] filterMatrix =
            new double[,] { {  0,  0,  0,  0,  0, },
                        {  0,  0,  0,  0,  0, },
                        { -1, -1,  2,  0,  0, },
                        {  0,  0,  0,  0,  0, },
                        {  0,  0,  0,  0,  0, }, };
        public override double[,] FilterMatrix
        {
            get { return filterMatrix; }
        }
    }
    
    public class VerticalEdgeDetectionFilter : ConvolutionFilterBase
    {
        public override string FilterName
        {
            get { return "VerticalEdgeDetectionFilter"; }
        }
        private double factor = 1.0;
        public override double Factor
        {
            get { return factor; }
        }
        private double bias = 0.0;
        public override double Bias
        {
            get { return bias; }
        }
        private double[,] filterMatrix =
            new double[,] { { 0,  0, -1,  0,  0, },
                        { 0,  0, -1,  0,  0, },
                        { 0,  0,  4,  0,  0, },
                        { 0,  0, -1,  0,  0, },
                        { 0,  0, -1,  0,  0, }, };
        public override double[,] FilterMatrix
        {
            get { return filterMatrix; }
        }
    }

    public class EdgeDetectionTopLeftBottomRightFilter : ConvolutionFilterBase
    {
        public override string FilterName
        {
            get { return "EdgeDetectionTopLeftBottomRightFilter"; }
        }
        private double factor = 1.0;
        public override double Factor
        {
            get { return factor; }
        }
        private double bias = 0.0;
        public override double Bias
        {
            get { return bias; }
        }
        private double[,] filterMatrix =
            new double[,] { { -5,  0,  0, },
                        {  0,  0,  0, },
                        {  0,  0,  5, }, };
        public override double[,] FilterMatrix
        {
            get { return filterMatrix; }
        }
    }

    //Emboss filters produce result images with an emphasis on depth, based on lines/edges expressed in an input/source image.

    public class EmbossFilter : ConvolutionFilterBase
    {
        public override string FilterName
        {
            get { return "EmbossFilter"; }
        }
        private double factor = 1.0;
        public override double Factor
        {
            get { return factor; }
        }
        private double bias = 128.0;
        public override double Bias
        {
            get { return bias; }
        }
        private double[,] filterMatrix =
            new double[,] { { 2,  0,  0, },
                        { 0, -1,  0, },
                        { 0,  0, -1, }, };
        public override double[,] FilterMatrix
        {
            get { return filterMatrix; }
        }
    }

    public class Emboss45DegreeFilter : ConvolutionFilterBase
    {
        public override string FilterName
        {
            get { return "Emboss45DegreeFilter"; }
        }
        private double factor = 1.0;
        public override double Factor
        {
            get { return factor; }
        }
        private double bias = 128.0;
        public override double Bias
        {
            get { return bias; }
        }
        private double[,] filterMatrix =
            new double[,] { { -1, -1,  0, },
                        { -1,  0,  1, },
                        {  0,  1,  1, }, };
        public override double[,] FilterMatrix
        {
            get { return filterMatrix; }
        }
    }

    public class EmbossTopLeftBottomRightFilter : ConvolutionFilterBase
    {
        public override string FilterName
        {
            get { return "EmbossTopLeftBottomRightFilter"; }
        }
        private double factor = 1.0;
        public override double Factor
        {
            get { return factor; }
        }
        private double bias = 128.0;
        public override double Bias
        {
            get { return bias; }
        }
        private double[,] filterMatrix =
            new double[,] { { -1, 0, 0, },
                        {  0, 0, 0, },
                        {  0, 0, 1, }, };
        public override double[,] FilterMatrix
        {
            get { return filterMatrix; }
        }
    }

    public class IntenseEmbossFilter : ConvolutionFilterBase
    {
        public override string FilterName
        {
            get { return "IntenseEmbossFilter"; }
        }
        private double factor = 1.0;
        public override double Factor
        {
            get { return factor; }
        }
        private double bias = 128.0;
        public override double Bias
        {
            get { return bias; }
        }
        private double[,] filterMatrix =
            new double[,] { { -1, -1, -1, -1,  0, },
                        { -1, -1, -1,  0,  1, },
                        { -1, -1,  0,  1,  1, },
                        { -1,  0,  1,  1,  1, },
                        {  0,  1,  1,  1,  1, }, };
        public override double[,] FilterMatrix
        {
            get { return filterMatrix; }
        }
    }

    // High pass filters produce result images where only high frequency components are retained.

    public class HighPass3x3Filter : ConvolutionFilterBase
    {
        public override string FilterName
        {
            get { return "HighPass3x3Filter"; }
        }
        private double factor = 1.0 / 16.0;
        public override double Factor
        {
            get { return factor; }
        }
        private double bias = 128.0;
        public override double Bias
        {
            get { return bias; }
        }
        private double[,] filterMatrix =
            new double[,] { { -1, -2, -1, },
                        { -2, 12, -2, },
                        { -1, -2, -1, }, };
        public override double[,] FilterMatrix
        {
            get { return filterMatrix; }
        }
    }

}
