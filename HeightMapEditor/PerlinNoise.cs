//by Idomeneas
using System.Drawing;
using System.Runtime.CompilerServices;
using Color = Stride.Core.Mathematics.Color;
using Point = System.Drawing.Point;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

namespace HeightMapEditor
{
    public class PerlinNoise
    {
        /// <summary>
        /// Used to distinguish between Grey scale heightmaps HeightMultiplier=255.0f (yields a byte 0-255)
        /// or float heightmaps HeightMultiplier=1000000.0f (based on a short -32,768 to 32,767,
        /// sum yields 65,535 levels for much smoother maps).
        /// The values returned from the Perlin generated heights are always in [0,1]
        /// </summary>
        public static float HeightMultiplier = 255.0f;
        public static float HeightMin = -1000.0f;
        public static float HeightMax = 1000.0f;
        private static bool IsGrayScaleHeightMapval = true;
        public static bool IsGrayScaleHeightMap
        {
            get { return IsGrayScaleHeightMapval; }
            set
            {
                IsGrayScaleHeightMapval = value;
                if (IsGrayScaleHeightMapval)
                {
                    HeightMultiplier = 255.0f;
                }
                else
                {
                    HeightMultiplier = 10000.0f;//float.MaxValue;
                }
            }
        }

        public enum BiomeType
        {
            Desert,
            Savanna,
            TropicalRainforest,
            Grassland,
            Woodland,
            SeasonalForest,
            TemperateRainforest,
            BorealForest,
            Tundra,
            Ice
        }

        BiomeType[,] BiomeTable = new BiomeType[6, 6] {   
	//COLDEST        //COLDER          //COLD                  //HOT                          //HOTTER                       //HOTTEST
	{ BiomeType.Ice, BiomeType.Tundra, BiomeType.Grassland,    BiomeType.Desert,              BiomeType.Desert,              BiomeType.Desert },              //DRYEST
	{ BiomeType.Ice, BiomeType.Tundra, BiomeType.Grassland,    BiomeType.Desert,              BiomeType.Desert,              BiomeType.Desert },              //DRYER
	{ BiomeType.Ice, BiomeType.Tundra, BiomeType.Woodland,     BiomeType.Woodland,            BiomeType.Savanna,             BiomeType.Savanna },             //DRY
	{ BiomeType.Ice, BiomeType.Tundra, BiomeType.BorealForest, BiomeType.Woodland,            BiomeType.Savanna,             BiomeType.Savanna },             //WET
	{ BiomeType.Ice, BiomeType.Tundra, BiomeType.BorealForest, BiomeType.SeasonalForest,      BiomeType.TropicalRainforest,  BiomeType.TropicalRainforest },  //WETTER
	{ BiomeType.Ice, BiomeType.Tundra, BiomeType.BorealForest, BiomeType.TemperateRainforest, BiomeType.TropicalRainforest,  BiomeType.TropicalRainforest }   //WETTEST
};

        //biome map
        public static Color Ice = Color.White;
        public static Color Desert = new Color(238 / 255f, 218 / 255f, 130 / 255f, 1);
        public static Color Savanna = new Color(177 / 255f, 209 / 255f, 110 / 255f, 1);
        public static Color TropicalRainforest = new Color(66 / 255f, 123 / 255f, 25 / 255f, 1);
        public static Color Tundra = new Color(96 / 255f, 131 / 255f, 112 / 255f, 1);
        public static Color TemperateRainforest = new Color(29 / 255f, 73 / 255f, 40 / 255f, 1);
        public static Color Grassland = new Color(164 / 255f, 225 / 255f, 99 / 255f, 1);
        public static Color SeasonalForest = new Color(73 / 255f, 100 / 255f, 35 / 255f, 1);
        public static Color BorealForest = new Color(95 / 255f, 115 / 255f, 62 / 255f, 1);
        public static Color Woodland = new Color(139 / 255f, 175 / 255f, 90 / 255f, 1);
        public static Color Rock = new Color(109,114,118,255);
        public static Color Dirt = new Color(159, 111, 39, 255);
        public static Color Sand = new Color(250, 189, 75, 255);
        public static Color Swamp = new Color(134, 152, 188, 255);

        public static Color GetBiomeColor(float level)
        {
            Color col = Color.DarkBlue;
            if (level < 0.05f)//deep water
            {
                col = Color.DarkBlue;
            }
            else if (level < 0.1f)//water
            {
                col = Color.Blue;
            }
            else if (level < 0.15f)//shallow water
            {
                col = Color.CadetBlue;
            }
            else if (level < 0.2f)//sand/beach
            {
                col = Sand;
            }
            else if (level < 0.25f)//dirt
            {
                col = Dirt;
            }
            else if (level < 0.3f)//grassland
            {
                col = Grassland;
            }
            else if (level < 0.4f)//desert
            {
                col = Desert;
            }
            else if (level < 0.45f)//swamp
            {
                col = Color.DarkGreen;
            }
            else if (level < 0.55f)//jungle
            {
                col = TropicalRainforest;
            }
            else if (level < 0.65f)//Savanna
            {
                col = Savanna;
            }
            else if (level < 0.75f)//TemperateRainforest
            {
                col = TemperateRainforest;
            }
            else if (level < 0.85f)//tundra
            {
                col = Tundra;
            }
            else if (level < 0.95f)//Rock
            {
                col = Rock;
            }
            else //if (level < 0.99f)//snow
            {
                col = Ice;
            }
            return col;
        }

        public static Color[] GenerateBiome(int m_Width, int m_Height,
    float NormalizationConst, float xfreq, float yfreq, float pixelcutoff, float freq,
    float error, float powval, float Persistance, int Octave, float mincutoff, float maxcutoff,
    int type)
        {
            int i, j, index;
            float[] Persistances = new float[10];
            for (i = 0; i < 10; i++)
                Persistances[i] = MathF.Pow(Persistance, -1.0f * i);

            float[] PerlinNoiseMat = new float[m_Width * m_Height];
            Color[] HeightColor = new Color[m_Width * m_Height];
            if (type == 0)
                PerlinNoiseMat = GetPerlinMatPersistancePerOctave(m_Width, m_Height,
                    NormalizationConst, xfreq, yfreq, pixelcutoff, freq, error
                , powval, Persistances, Octave, mincutoff, maxcutoff);
            else
            {
                PerlinNoiseMat = GetPerlinRidgeMat(m_Width, m_Height,
                  NormalizationConst, xfreq, yfreq, pixelcutoff, freq, error,
                  powval, Persistances, Octave, mincutoff, maxcutoff);
            }

            for (i = 0; i < m_Width; i++)
            {
                for (j = 0; j < m_Height; j++)
                {
                    index = (m_Width * j) + i;
                    if (PerlinNoise.IsGrayScaleHeightMap)
                    {                        
                        byte color = GetBiomeColor(PerlinNoiseMat[index]).R;
                        HeightColor[index].R = color;
                        HeightColor[index].G = color;
                        HeightColor[index].B = color;
                        HeightColor[index].A = 255;
                    }
                    else
                    {
                        HeightColor[index] = GetBiomeColor(PerlinNoiseMat[index]);
                       // HeightColor[index].G = GetBiomeColor(PerlinNoiseMat[index]).G;
                       // HeightColor[index].B = GetBiomeColor(PerlinNoiseMat[index]).B;
                       // HeightColor[index].A = GetBiomeColor(PerlinNoiseMat[index]).A;
                    }
                }
            }
            Array.Clear(PerlinNoiseMat);
            return HeightColor;
        }


        public static Color[] RandomizeElevationMountain(int m_Width, int m_Height,
            float mincutoff, float maxcutoff, float powval, float VarianceX,
            float VarianceY, float NormalizationConst, float TargetHeightValue,
            float pixelcutoff, float xfreq, float yfreq,float Persistance,
            float freq, float error,int Octave,int Type, int Type2)
        {
            int i, j, index;
            Color[] HeightColor = new Color[m_Width * m_Height];
            int NumComp = 100;
            float centx = 0.5f * m_Width, centy = 0.5f * m_Height;
            float[] meansx = new float[NumComp], meansy = new float[NumComp],
             stdsx = new float[NumComp], stdsy = new float[NumComp];
            float mdist = Math.Max(m_Width, m_Height);
            float[] probs = new float[NumComp];
            //grid of 10x10, 100 mixture components
            Vector2 minBounds = new Vector2(.1f * m_Width, .1f * m_Height);
            Vector2 maxBounds = new Vector2(.9f * m_Width, .9f * m_Height);
            float Stepx = (maxBounds.X- minBounds.X)/10, 
                Stepy = (maxBounds.Y - minBounds.Y) / 10, 
                posx = minBounds.X, posy = minBounds.Y;
            for (i = 0; i < 10; i++)
            {
                probs[i] = 1 / NumComp;
                posx = minBounds.X;
                for (j = 0; j < 10; j++)
                {
                    index = i * 10 + j;
                    meansx[index] = posx;
                    meansy[index] = posy;
                    stdsx[index] = Utility.Runif(0.15f * VarianceX, 0.3f * VarianceX);
                    stdsy[index] = Utility.Runif(0.15f * VarianceY, 0.3f * VarianceY);
                    posx += Stepx;
                }
                posy += Stepy;
            }
            float[] Persistances = new float[10];
            for (i = 0; i < 10; i++)
                Persistances[i] = MathF.Pow(Persistance, -1.0f * i);
            float[] Heights = /*RandomizeFromCurve(m_Width, m_Height,
             NormalizationConst, xfreq, yfreq, pixelcutoff, freq, error,
             powval, Persistance, Octave, mincutoff, maxcutoff,
              ColorStart, ColorEnd, LocationStart, LocationEnd,
             Type, Type2, TargetHeightValue, VarianceX, VarianceY);*/
             GetPerlinRidgeMat(m_Width, m_Height,                  
             NormalizationConst, xfreq, yfreq, pixelcutoff, freq, error, 
             powval, Persistances, Octave, mincutoff, maxcutoff);
            //generate heights
            float minf = 10000000.0f, maxf = -1000000.0f,
               dropoffx = VarianceX + Utility.Runif() * m_Width / 10.0f,//runif(0.05f * centx, 0.2f * centx),// runif(0.15f * centx, 0.22f * centz),
                dropoffz = VarianceY + Utility.Runif() * m_Height / 10.0f,//runif(0.05f * centz, 0.2f * centz),// runif(0.15f * centx, 0.22f * centz),
                curvepow = Utility.Runif(0.3f, 0.7f);
            for (j = 0; j < m_Height; j++)
            {
                for (i = 0; i < m_Width; i++)
                {
                    index = (m_Width * j) + i;
                    Heights[index] += TargetHeightValue +
                        (powval * 30.0f*Utility.GetYFromCurve(Type, Type2, 1.0f * i, 1.0f * j,
                        centx, centy,xfreq * centx,yfreq * centy, mincutoff,
                        maxcutoff,-2.0f * MathF.PI * centx, 
                        2.0f * MathF.PI * centy, Utility.Runif(0.3f, 0.7f),
                                dropoffx,
                                dropoffz,//variances
                                curvepow)
                            +
                        powval * 70.0f*Utility.GetNormalMixture(1.0f * i,
                            1.0f * j, meansx, meansy, stdsx, stdsy,
                            probs, mincutoff, maxcutoff))/100.0f;
                    if (Heights[index] < minf) minf = Heights[index];
                    if (Heights[index] > maxf) maxf = Heights[index];
                }
            }

            for (j = 0; j < m_Height; j++)
            {
                for (i = 0; i < m_Width; i++)
                {
                    index = (m_Width * j) + i;
                    Heights[index] = (Heights[index] - minf) / (maxf - minf);
                    Heights[index] /= NormalizationConst;
                    if (Heights[index] < pixelcutoff) Heights[index] = 0;
                    if (Heights[index] < mincutoff) Heights[index] = mincutoff;
                    if (Heights[index] > maxcutoff) Heights[index] = maxcutoff;
                    if (PerlinNoise.IsGrayScaleHeightMap)
                    {
                        byte color = (byte)Utility.BoundValue0255(
                            MathF.Round(PerlinNoise.HeightMultiplier
                        * Heights[index]));
                        HeightColor[index] = new Color(color);
                    }
                    else
                        HeightColor[index] = (PerlinNoise.HeightMultiplier
                        * Heights[index]).AsStrideColor();
                }
            }
            Array.Clear(Heights, 0, Heights.Length);
            return HeightColor;
        }

        public static Color[] RandomizeSingleBivNormalMap(int m_Width, int m_Height, 
            float NormalizationConst,
        float shiftx, float shiftz, float pixelcutoff, float freq, float error
        , float powval, float Persistance, int Octave, float mincutoff, float maxcutoff,
         float varx, float vary)
        {
            int i, j, index;
            float centx = 0.5f * m_Width + shiftx, centz = 0.5f * m_Height + shiftz,
                dropoffx = varx + Utility.Runif() * m_Width / 10.0f,//runif(0.05f * centx, 0.2f * centx),// runif(0.15f * centx, 0.22f * centz),
                dropoffz = vary + Utility.Runif() * m_Height / 10.0f;//runif(0.05f * centz, 0.2f * centz),// runif(0.15f * centx, 0.22f * centz),
            Color[] HeightColor = new Color[m_Width * m_Height];

            float[] Persistances = new float[10];
            for (i = 0; i < 10; i++)
                Persistances[i] = MathF.Pow(Persistance, -1.0f * i);
            float[] Heights = new float[m_Width * m_Height];
            Bitmap newBitmap = new Bitmap(m_Width, m_Height);
            //generate heights
            float minf = 1000000.0f, maxf = -100000.0f;
            //generate heights
            for (j = 0; j < m_Height; j++)
            {
                for (i = 0; i < m_Width; i++)
                {
                    index = (m_Width * j) + i;
                    Heights[index] = (powval * Utility.GetYBellShape(1.0f * i, 
                        1.0f * j, centx, centz,
                        dropoffx, dropoffz, mincutoff, maxcutoff)) / NormalizationConst; 
                    if (Heights[index] < minf) minf = Heights[index];
                    if (Heights[index] > maxf) maxf = Heights[index];
                }
            }
            for (j = 0; j < m_Height; j++)
            {
                for (i = 0; i < m_Width; i++)
                {
                    index = (m_Width * j) + i;
                    Heights[index] = (Heights[index] - minf) / (maxf - minf);
                    if (Heights[index] < pixelcutoff) Heights[index] = 0;
                    if (Heights[index] < mincutoff) Heights[index] = mincutoff;
                    if (Heights[index] > maxcutoff) Heights[index] = maxcutoff;
                    if (PerlinNoise.IsGrayScaleHeightMap)
                    {
                        byte color = (byte)Utility.BoundValue0255(
                            MathF.Round(PerlinNoise.HeightMultiplier
                        * Heights[index]));
                        HeightColor[index] = new Color(color);
                    }
                    else
                        HeightColor[index] = (PerlinNoise.HeightMultiplier
                        * Heights[index]).AsStrideColor();
                }
            }
            Array.Clear(Heights, 0, Heights.Length);
            return HeightColor;

        }

        public static Color[] RandomizeFromCurve(int m_Width, int m_Height,
            float NormalizationConst, float xfreq, float yfreq, float pixelcutoff, float freq, 
            float error, float powval, float Persistance, int Octave, float mincutoff, float maxcutoff,
             Vector3 mincol, Vector3 maxcol, Vector2 startpos, Vector2 endpos,
            int type, int type2, float TargetHeightValue, float varx, float varz)
        {
            int i, j, index;
            float[] Persistances = new float[10];
            for (i = 0; i < 10; i++)
                Persistances[i] = MathF.Pow(Persistance, -1.0f * i);
            Color[] HeightColor = new Color[m_Width * m_Height];

            float prob = Utility.Runif(0.3f, 0.7f), centx = 0.5f * m_Width, 
                centz = 0.5f * m_Height,
                dropoffx = varx+ Utility.Runif()* m_Width/10.0f,//runif(0.05f * centx, 0.2f * centx),// runif(0.15f * centx, 0.22f * centz),
                dropoffz = varz + Utility.Runif() * m_Height / 10.0f,//runif(0.05f * centz, 0.2f * centz),// runif(0.15f * centx, 0.22f * centz),
                curvepow = Utility.Runif(0.3f, 0.7f),
                shiftx = centx,// runif(-0.8f * centx, 0.8f * centx),
                shiftz = centz,// runif(-0.8f * centz, 0.8f * centz),
                               //mux = runif(0.1f * m_Width, 0.5f * m_Width), muz = runif(0.1f *m_Height, 0.5f * m_Height),
                               //mux1 = runif(0.5f * m_Width, 1.0f * m_Width), muz1 = runif(0.5f * m_Height, 1.0f * m_Height),
                stdx = Utility.Runif(0.15f * centx, 0.32f * centx), stdz = Utility.Runif(0.12f * centz, 0.225f * centz), 
                rho = Utility.Runif(-0.9f, 0.9f),
                stdx1 = Utility.Runif(0.15f * centx, 0.325f * centx), stdz1 = Utility.Runif(0.25f * centz, 0.435f * centz),
                prob1 = Utility.Runif(0.01f, 0.25f), prob2 = Utility.Runif(0.01f, 0.25f), prob3 = Utility.Runif(0.01f, 0.25f),
                prob4 = Utility.Runif(0.01f, 0.25f), prob5 = 1 - prob1 - prob2 - prob3 - prob4,
                muxminus = -0.5f * centx, muxplus = 0.5f * centx,
                muzminus = -0.5f * centz, muzplus = 0.5f * centz;

            //RandomizeElevationMapPerlinBased(NormalizationConst,xfreq, yfreq, pixelcutoff, freq,error, powval, Persistance, Octave, mincutoff, maxcutoff,startpos, endpos, mincol, maxcol,-1, 0.5f*TargetHeightValue);

            float[] Heights = new float[m_Width * m_Height];
            Bitmap newBitmap = new Bitmap(m_Width, m_Height);
            //generate heights
            float minf = 1000000.0f, maxf = -100000.0f;
            //generate heights
            for (j = 0; j < m_Height; j++)
            {
                for (i = 0; i < m_Width; i++)
                {
                    index = (m_Width * j) + i;
                    //float perlin = 0;// RGBAToFloat(PictPixels[index].R, PictPixels[index].G, PictPixels[index].B, PictPixels[index].A);
                    Heights[index] = TargetHeightValue+Utility.Runif(-error,error);// +perlin;
                    if (type == 0)//circle
                        Heights[index] += powval * (//1.5f * perlin +
                            Utility.GetYFromCurve(0, type2, 1.0f * i, 1.0f * j, shiftx, shiftz,
                                xfreq * centx,
                                yfreq * centz//controls spreadness on y-axis
                                , mincutoff, maxcutoff,
                                -2.0f * MathF.PI * centx, 2.0f * MathF.PI * centz, Utility.Runif(0.3f, 0.7f),
                                dropoffx,
                                dropoffz,//variances
                                curvepow)
                            );
                    else if (type == 1)//cos
                        Heights[index] += powval * (//1.5f * perlin +
                            Utility.GetYFromCurve(1, type2, 1.0f * i, 1.0f * j, shiftx, shiftz,
                                xfreq * centx,
                                yfreq * centz//controls spreadness on y-axis
                                , mincutoff, maxcutoff,
                                -2.0f * MathF.PI * centx, 2.0f * MathF.PI * centz, Utility.Runif(0.3f, 0.7f),
                                dropoffx,
                                dropoffz,//variances
                                curvepow)
                            );
                    else if (type == 2)//sine
                        Heights[index] += powval * (//1.5f * perlin +
                            Utility.GetYFromCurve(2, type2, 1.0f * i, 1.0f * j, shiftx, shiftz,
                                xfreq * centx,
                                yfreq * centz//controls spreadness on y-axis
                                , mincutoff, maxcutoff,
                                -2.0f * MathF.PI * centx, 2.0f * MathF.PI * centz, Utility.Runif(0.3f, 0.7f),
                                dropoffx,
                                dropoffz,//variances
                                 curvepow)
                            );
                    else if (type == 3)//astroid curve
                        Heights[index] += powval * (//1.5f * perlin +
                            Utility.GetYFromCurve(3, type2, 1.0f * i, 1.0f * j, shiftx, shiftz,
                                xfreq * centx, yfreq * centz, mincutoff, maxcutoff,
                                -10.0f, 10.0f, Utility.Runif(0.3f, 0.7f), dropoffx, dropoffz, curvepow)
                            );
                    else if (type == 4)//Folium of Descartes
                        Heights[index] += powval * (//1.5f * perlin +
                            Utility.GetYFromCurve(4, type2, 1.0f * i, 1.0f * j, shiftx, shiftz,
                                xfreq * centx,
                                yfreq * centz//controls spreadness on y-axis
                                , mincutoff, maxcutoff,
                                -10.0f, 10.0f, Utility.Runif(0.3f, 0.7f), dropoffx, dropoffz, curvepow)
                            );
                    else if (type == 5)//Involute of a Circle
                        Heights[index] += powval * (//1.5f * perlin +
                            Utility.GetYFromCurve(5, type2, 1.0f * i, 1.0f * j, shiftx, shiftz,
                                0.15f * xfreq * centx,
                                0.15f * yfreq * centz//controls spreadness on y-axis
                                , mincutoff, maxcutoff, -10.0f, 10.0f, Utility.Runif(0.3f, 0.7f), dropoffx, dropoffz,
                                curvepow)
                            );
                    else if (type == 6)//Nephroid Curves
                        Heights[index] += powval * (//1.5f * perlin +
                            Utility.GetYFromCurve(6, type2, 1.0f * i, 1.0f * j, shiftx, shiftz,
                                0.15f * xfreq * centx,
                                0.15f * yfreq * centz//controls spreadness on y-axis
                                , mincutoff, maxcutoff, -10.0f, 10.0f, Utility.Runif(0.3f, 0.7f), dropoffx, dropoffz,
                                curvepow)
                            );
                    else if (type == 7)//Witch of Agnesi Curves
                        Heights[index] += powval * (//1.5f * perlin +
                            Utility.GetYFromCurve(7, type2, 1.0f * i, 1.0f * j, shiftx, shiftz,
                                0.015f * xfreq * centx,
                                0.015f * yfreq * centz//controls spreadness on y-axis
                                , mincutoff, maxcutoff, -20.0f, 20.0f, Utility.Runif(0.3f, 0.7f), dropoffx, dropoffz,
                                curvepow)
                            );
                    else //if (type == 8)//snake curve
                        Heights[index] += powval * (//1.5f * perlin +
                            Utility.GetYFromCurve(8, type2, 1.0f * i, 1.0f * j, shiftx, shiftz,
                                xfreq * centx,
                                yfreq * centz//controls spreadness on y-axis
                                , mincutoff, maxcutoff, -20.0f, 20.0f, Utility.Runif(0.3f, 0.7f), dropoffx, dropoffz,
                                curvepow)
                            );
 //                   htmid = htmid / NormalizationConst;
                    if (Heights[index] < minf) minf = Heights[index];
                    if (Heights[index] > maxf) maxf = Heights[index];
                }
            }
            for (j = 0; j < m_Height; j++)
            {
                for (i = 0; i < m_Width; i++)
                {
                    index = (m_Width * j) + i;
                    Heights[index] = (Heights[index] - minf) / (maxf - minf);
                    Heights[index] /= NormalizationConst;
                    if (Heights[index] < pixelcutoff) Heights[index] = 0;
                    if (Heights[index] < mincutoff) Heights[index] = mincutoff;
                    if (Heights[index] > maxcutoff) Heights[index] = maxcutoff;
                    if (PerlinNoise.IsGrayScaleHeightMap)
                    {
                        byte color = (byte)Utility.BoundValue0255(
                            MathF.Round(PerlinNoise.HeightMultiplier
                        * Heights[index]));
                        HeightColor[index] = new Color(color);
                    }
                    else
                        HeightColor[index] = (PerlinNoise.HeightMultiplier
                        * Heights[index]).AsStrideColor();
                }
            }
            Array.Clear(Heights, 0, Heights.Length);
            return HeightColor;
        }

        public static Color[] RandomizeBivNormalMap(int m_Width, int m_Height,
            float mincutoff, float maxcutoff, float powval,float VarianceX, 
            float VarianceY, float NormalizationConst,float TargetHeightValue,
            float pixelcutoff,int NumComp)
        {
            int i, j, index;
            Color[] HeightColor = new Color[m_Width * m_Height];

            float centx = 0.5f * m_Width, centy = 0.5f * m_Height;
            float[]weights=new float[NumComp];//NumComp mixture components
            float[] meansx = new float[NumComp], meansy = new float[NumComp],
             stdsx = new float[NumComp], stdsy = new float[NumComp];
            float theta,r,mdist=Math.Max(m_Width,m_Height);
            for (i = 0; i < NumComp; i++)
            {
                r = Utility.Runif(0.05f * mdist, 0.95f * mdist);
                theta = Utility.Runif(0, 2 * 3.14f);
                meansx[i] = centx + MathF.Cos(theta) *r;
                   // Utility.Runif(-m_Width, m_Width);
                meansy[i] = centy + MathF.Sin(theta) * r;
                // Utility.Runif(-m_Height, m_Height);
                stdsx[i] =Utility.Runif(0.15f * VarianceX, 0.2f * VarianceX);
                stdsy[i] = Utility.Runif(0.15f * VarianceY, 0.2f * VarianceY);
                weights[i] = Utility.DUnif(10, 100);
            }
            float[] probs = Utility.RDirichlet(weights);
            float[] Heights = new float[m_Width * m_Height];
            //generate heights
            float minf = 10000000.0f, maxf = -1000000.0f;
            for (j = 0; j < m_Height; j++)
            {
                for (i = 0; i < m_Width; i++)
                {
                    index = (m_Width * j) + i;
                    //get weights
                    Heights[index] = TargetHeightValue+
                            powval * Utility.GetNormalMixture(1.0f * i, 
                            1.0f * j, meansx, meansy, stdsx, stdsy, 
                            probs, mincutoff, maxcutoff);                    
                    if (Heights[index] < minf) minf = Heights[index];
                    if (Heights[index] > maxf) maxf = Heights[index];
                }
            }
          //  MainMenuMessageBar.Text = "minf="+ minf.ToString("0.0")+ ", maxf=" + maxf.ToString("0.0");

            for (j = 0; j < m_Height; j++)
            {
                for (i = 0; i < m_Width; i++)
                {
                    index = (m_Width * j) + i;
                    Heights[index] = (Heights[index] - minf) / (maxf - minf);
                    Heights[index] /= NormalizationConst;
                    if (Heights[index] < pixelcutoff) Heights[index] = 0;
                    if (Heights[index] < mincutoff) Heights[index] = mincutoff;
                    if (Heights[index] > maxcutoff) Heights[index] = maxcutoff;
                    if (PerlinNoise.IsGrayScaleHeightMap)
                    {
                        byte color = (byte)Utility.BoundValue0255(
                            MathF.Round(PerlinNoise.HeightMultiplier
                        * Heights[index]));
                        HeightColor[index] = new Color(color);
                    }
                    else
                        HeightColor[index] = (PerlinNoise.HeightMultiplier
                        * Heights[index]).AsStrideColor();
                }
            }
            Array.Clear(Heights, 0, Heights.Length);
            return HeightColor;
        }

        public static Color[] RandomHillsBivNormalMap(int m_Width, int m_Height,
            float mincutoff, float maxcutoff, float powval, float VarianceX,
            float VarianceY, float NormalizationConst, float TargetHeightValue,
            float pixelcutoff)
        {
            int i, j, index;
            Color[] HeightColor = new Color[m_Width * m_Height];
            int NumComp = 100;
            float centx = 0.5f * m_Width, centy = 0.5f * m_Height;
            float[] meansx = new float[NumComp], meansy = new float[NumComp],
             stdsx = new float[NumComp], stdsy = new float[NumComp];
            float mdist = Math.Max(m_Width, m_Height);
            float[] probs = new float[NumComp];
            //grid of 10x10, 100 mixture components
            float Stepx=m_Width/10, Stepy=m_Height/10,posx=0,posy=0;
            for(i=0;i<10;i++)
            {
                probs[i]=1/NumComp;
                posx = 0;
                for (j = 0; j < 10; j++)
                {
                    index=i*10+j;
                    meansx[index] = posx;
                    meansy[index] = posy;
                    stdsx[index] = Utility.Runif(0.15f * VarianceX, 0.3f * VarianceX);
                    stdsy[index] = Utility.Runif(0.15f * VarianceY, 0.3f * VarianceY);
                    posx += Stepx;
                }
                posy += Stepy;
            }
            float[] Heights = new float[m_Width * m_Height];
            //generate heights
            float minf = 10000000.0f, maxf = -1000000.0f;
            for (j = 0; j < m_Height; j++)
            {
                for (i = 0; i < m_Width; i++)
                {
                    index = (m_Width * j) + i;
                    //get weights
                    Heights[index] = TargetHeightValue +
                            powval * Utility.GetNormalMixture(1.0f * i,
                            1.0f * j, meansx, meansy, stdsx, stdsy,
                            probs, mincutoff, maxcutoff);
                    if (Heights[index] < minf) minf = Heights[index];
                    if (Heights[index] > maxf) maxf = Heights[index];
                }
            }
            //  MainMenuMessageBar.Text = "minf="+ minf.ToString("0.0")+ ", maxf=" + maxf.ToString("0.0");

            for (j = 0; j < m_Height; j++)
            {
                for (i = 0; i < m_Width; i++)
                {
                    index = (m_Width * j) + i;
                    Heights[index] = (Heights[index] - minf) / (maxf - minf);
                    Heights[index] /= NormalizationConst;
                    if (Heights[index] < pixelcutoff) Heights[index] = 0;
                    if (Heights[index] < mincutoff) Heights[index] = mincutoff;
                    if (Heights[index] > maxcutoff) Heights[index] = maxcutoff;
                    if (PerlinNoise.IsGrayScaleHeightMap)
                    {
                        byte color = (byte)Utility.BoundValue0255(
                            MathF.Round(PerlinNoise.HeightMultiplier
                        * Heights[index]));
                        HeightColor[index] = new Color(color);
                    }
                    else
                        HeightColor[index] = (PerlinNoise.HeightMultiplier
                        * Heights[index]).AsStrideColor();
                }
            }
            Array.Clear(Heights, 0, Heights.Length);
            return HeightColor;
        }

        public static Color[] RandomizeElevationMapPerlinBased(int m_Width, int m_Height,
            float NormalizationConst, float xfreq, float yfreq, float pixelcutoff, float freq,
            float error, float powval, float Persistance, int Octave, float mincutoff, float maxcutoff,
            int type)
        {
            int i, j, index;
            float[] Persistances = new float[10];
            for (i = 0; i < 10; i++)
                Persistances[i] = MathF.Pow(Persistance, -1.0f * i);

            float[] PerlinNoiseMat = new float[m_Width * m_Height];
            Color[] HeightColor = new Color[m_Width * m_Height];
            if (type == 0)
                PerlinNoiseMat = GetPerlinMatPersistancePerOctave(m_Width, m_Height,
                    NormalizationConst, xfreq, yfreq, pixelcutoff, freq, error
                , powval, Persistances, Octave, mincutoff, maxcutoff); 
            else 
            {
                PerlinNoiseMat = GetPerlinRidgeMat(m_Width, m_Height,
                  NormalizationConst, xfreq, yfreq, pixelcutoff, freq, error,
                  powval, Persistances, Octave, mincutoff, maxcutoff);
            }

            for (i = 0; i < m_Width; i++)
            {
                for (j = 0; j < m_Height; j++)
                {
                    index = (m_Width * j) + i;
                    if (PerlinNoise.IsGrayScaleHeightMap)
                    {
                        byte color = (byte)Utility.BoundValue0255(
                            MathF.Round(PerlinNoise.HeightMultiplier
                        * PerlinNoiseMat[index]));//(int)(Utility.BoundValue01(PerlinNoiseMat[index], mincutoff, maxcutoff) * 255.0f);//Convert to 0-256 values.
                        HeightColor[index] = new Color(color);// PerlinNoiseMat[index].AsStrideColor();
                    }
                    else
                        HeightColor[index] = (PerlinNoise.HeightMultiplier
                        * PerlinNoiseMat[index]).AsStrideColor();
                }
            }
            Array.Clear(PerlinNoiseMat, 0, PerlinNoiseMat.Length);
            return HeightColor;
        }

        public static float[] GetPerlinRidgeMat(int m_Width, int m_Height, float NormalizationConst,
                float xfreq, float yfreq, float pixelcutoff, float freq, float error
                , float powval, float[] Persistance, int Octave, float mincutoff, float maxcutoff)
        {
            int i, j, index;
            float[] PerlinNoiseMat = new float[m_Width * m_Height];
            float[] Gradient = new float[m_Width * m_Height * 2];
            for (i = 0; i < m_Width; i++)
            {
                for (j = 0; j < m_Height; j++)
                {
                    index = (m_Width * j) + i;
                    float theta = (float)(Utility.RandomFloat(0.0f, 2.0f) * Math.PI);
                    Gradient[index] = (float)Math.Cos(theta);
                    Gradient[index + 1] = (float)Math.Sin(theta);
                }
            }

            float []octaves = { 1, 2, 4, 8, 16, 32, 64, 128, 256, 512 };
            //for (i = 0; i< 20; i++)octaves[i] = powf(2.0, 1.0f * i);

            float minf = 1000000.0f, maxf = -100000.0f, amplitude = 1.0f;
            for (i = 0; i < m_Width; i++)
            {
                for (j = 0; j < m_Height; j++)
                {
                    index = (m_Width * j) + i;
                    float partsum, perlin;
                    float[] vals=new float[10];
                    int oct, oct1;
                    float nx = freq * xfreq * i / m_Width,//-0.5f,
                        ny = freq * yfreq * j / m_Height;// -0.5f;
                    amplitude = 1.0f;
                    //the points cannot be the grid points
                    for (oct = 0; oct < Octave; oct++)
                    {
                        perlin = PerlinNoiseGen(new Vector2(octaves[oct] * nx //+ oct * xfreq
                            , octaves[oct] * ny //+ oct * yfreq
                        ),Gradient, m_Width, m_Height);
                        if (perlin > 100.0f || perlin < -100.0f) perlin = 0.0f;
                        partsum = 0;
                        for (oct1 = 0; oct1 < oct; oct1++) partsum += vals[oct1];
                        if (oct > 0)
                            vals[oct] = (partsum * (2.0f -MathF.Abs(perlin) * 2.0f) * 2.0f) * amplitude;
                        else
                            vals[oct] = (2.0f - MathF.Abs(perlin) * 2.0f) * 2.0f * amplitude;
                        amplitude *= Persistance[oct];
                        //				0.5f * (1.0f + perlin) * amplitude
                    }
                    float val = 0.0f;
                    for (oct = 0; oct < Octave; oct++) val += vals[oct];
                    PerlinNoiseMat[index] = val;
                    if (PerlinNoiseMat[index] < minf) minf = PerlinNoiseMat[index];
                    if (PerlinNoiseMat[index] > maxf) maxf = PerlinNoiseMat[index];
                }
            }

            float totalAmplitude = 0.0f;
            amplitude = 1.0f;
            for (int oct = 0; oct < Octave; oct++)//This loops trough the octaves.
            {
                amplitude *= Persistance[oct];// powf(Persistance, error);
                totalAmplitude += amplitude;
            }
            //normalization into [0,1]
            for (i = 0; i < m_Width; i++)
            {
                for (j = 0; j < m_Height; j++)
                {
                    index = (m_Width * j) + i;
                    //PerlinNoiseMat[index] =powf(PerlinNoiseMat[index] , powval);
                    //if (PerlinNoiseMat[index] - minf < 0.1f * (maxf - minf))
                    //	PerlinNoiseMat[index] = minf+ 0.1f * (maxf - minf);// 0.5f * (maxf - minf);// += runif(0.01f, 0.1f) * (maxf - minf);
                    //else
                    if (PerlinNoiseMat[index] - minf < pixelcutoff * (maxf - minf))
                        PerlinNoiseMat[index] = pixelcutoff * (maxf - minf) + minf;
                    PerlinNoiseMat[index] = MathF.Pow(
                        PerlinNoiseMat[index] / totalAmplitude, powval) / NormalizationConst;
                    //powf((PerlinNoiseMat[index] - minf) / (maxf - minf), powval);// / NormalizationConst;
                    //		int color= (int)(PerlinNoiseMat[index]*255.0f);//Convert to 0-256 values.
                    //	Pixels[index].R =color;
                }
            }

            //normalization
            for (i = 0; i < m_Width; i++)
            {
                for (j = 0; j < m_Height; j++)
                {
                    index = (m_Width * j) + i;
                    if (PerlinNoiseMat[index] - minf < pixelcutoff * (maxf - minf))
                        PerlinNoiseMat[index] = pixelcutoff * (maxf - minf) + minf;
                }
            }

            for (i = 0; i < m_Width; i++)
            {
                for (j = 0; j < m_Height; j++)
                {
                    index = (m_Width * j) + i;
                    PerlinNoiseMat[index] += Utility.NormalError(0,error);
                    PerlinNoiseMat[index] = (PerlinNoiseMat[index] - minf) / (maxf - minf);
                    PerlinNoiseMat[index] /= NormalizationConst;
                    if (PerlinNoiseMat[index] < pixelcutoff) PerlinNoiseMat[index] = 0;
                    if (PerlinNoiseMat[index] < mincutoff) PerlinNoiseMat[index] = mincutoff;
                    if (PerlinNoiseMat[index] > maxcutoff) PerlinNoiseMat[index] = maxcutoff;
                }
            }
            Array.Clear(Gradient, 0, Gradient.Length);
            return PerlinNoiseMat;

        }
        
        public static Color[] RandomizePerlinBand(int m_Width, int m_Height,
            float NormalizationConst, float xfreq, float yfreq, float pixelcutoff, float freq,
            float error, float powval, float Persistance, int Octave, float mincutoff, float maxcutoff,
            int channel)
        {
            int i, j, index;
            float[] Persistances = new float[10];
            Color[] HeightColor = new Color[m_Width * m_Height];
            Persistances[0] = Persistance;
            float[] PerlinNoiseMat = GetPerlinRidgeMat(m_Width, m_Height,
                    NormalizationConst, xfreq, yfreq, pixelcutoff, freq, error
                , powval, Persistances, Octave, mincutoff, maxcutoff);
            
            for (i = 0; i < m_Width; i++)
            {
                for (j = 0; j < m_Height; j++)
                {
                    index = (m_Width * j) + i;
                    byte R, G, B, A;
                    Utility.FloatToRGBA(PerlinNoiseMat[index], out R, out G, out B, out A);
                    if (channel == -1)
                    {
                        int color = (byte)Utility.BoundValue0255(
                            MathF.Round(255.0f * PerlinNoiseMat[index]));
                        byte col=0; 
                        if (color > 255.0f *mincutoff) col = 255;
                        HeightColor[index] = new Color(col, col, col); 
                    }
                    else if (channel == 0)
                    {
                        HeightColor[index] = new Color(R, 0, 0);
                    }
                    else if (channel == 1)
                    {
                        HeightColor[index] = new Color(0, G, 0);
                    }
                    else
                    {
                        HeightColor[index] = new Color(0, 0, B);
                    }
                }
            }
            Array.Clear(PerlinNoiseMat, 0, PerlinNoiseMat.Length);
            return HeightColor;
        }

        /// <summary>
        /// Random Voronoi tesselation,with the boundary set to the same color; 
        /// use for texture blending when set to checkhull=true. When checkhull=false
        /// the boundaries are filled in as well.
        /// </summary>
        /// <param name="m_Width"></param>
        /// <param name="m_Height"></param>
        /// <param name="NormalizationConst"></param>
        /// <param name="freq"></param>
        /// <param name="powval"></param>
        /// <param name="mincutoff"></param>
        /// <param name="maxcutoff"></param>
        /// <param name="mincol"></param>
        /// <param name="maxcol"></param>
        /// <param name="checkhull"></param>
        /// <returns></returns>
        public static Color[] RandomVoronoiForTextures(int m_Width, int m_Height,
            float NormalizationConst, float freq, float powval, float mincutoff, float maxcutoff,
            Vector3 mincol, Vector3 maxcol,
            bool checkhull)
        {
            int i, j, k, index;
            Color[] HeightColor = new Color[m_Width * m_Height];

            int size = (int)MathF.Floor(Utility.Runif(1.0f, MathF.Min(m_Width, m_Height) * freq / NormalizationConst + 1.0f));
            if (size > m_Width* m_Height) size = m_Width * m_Height;
            if (size < 10) size = 10;
            Utility.VoronoiVertex[] Verts = new Utility.VoronoiVertex[size];
            List<Point> AllPoints = new List<Point>();
            for (k = 0; k < size; k++)
            {
                Vector3 col = Utility.GetColourFromVec1toVec2(Utility.Runif(0, 1), 
                    mincutoff, maxcutoff, mincol, maxcol);
                Verts[k].x = (int)Utility.Runif(0, m_Width - 1);
                Verts[k].y = (int)Utility.Runif(0, m_Height - 1);
                if (PerlinNoise.IsGrayScaleHeightMap)
                {
                    col.Y = col.X; col.Z = col.X;
                }
                Verts[k].R = (byte)(MathF.Floor(255.0f * col.X));
                Verts[k].G = (byte)(MathF.Floor(255.0f * col.Y));
                Verts[k].B = (byte)(MathF.Floor(255.0f * col.Z));
                Verts[k].A = 255;
                Point Pt = new Point(Verts[k].x, Verts[k].y);
                AllPoints.Add(Pt);
            }

           // Vector3 commoncol = Utility.GetColourFromVec1toVec2(Utility.Runif(0, 1), mincutoff, maxcutoff,
           //             mincol, maxcol);
            float wtstart = Utility.Runif(0, 1);
            Vector3 commoncol = wtstart * mincol + (1.0f- wtstart) * maxcol;
            List<Point> Hull = Utility.ConvexHull.GetConvexHull(AllPoints);
            AllPoints.Clear();

            int indexk;
            for (j = 0; j < m_Height; j++)
            {
                for (i = 0; i < m_Width; i++)
                {
                    index = (m_Width * j) + i;
                    //if(i<0.1f*m_Width || i>0.9f*m_Width || j<0.1f*m_Height || j>0.9f*m_Height){PictPixels[index].B =0;PictPixels[index].G =0;PictPixels[index].R =0;PictPixels[index].A =255;continue;}
                    //D3DXVECTOR3 col=GetColourBluetoWhite(PerlinNoiseMat[index],vmin,vmax,
                    //	mincol,maxcol);
                    //if outside the convex hull, make the common color
                    Point pt= new Point(i,j);
                    if (checkhull && !Utility.ConvexHull.IsInHull(pt, Hull))
                    {
                        byte colorR = (byte)Utility.BoundValue0255(
                            MathF.Round(255.0f * commoncol.X));
                        byte colorG = (byte)Utility.BoundValue0255(
                            MathF.Round(255.0f * commoncol.Y));
                        byte colorB = (byte)Utility.BoundValue0255(
                            MathF.Round(255.0f * commoncol.Z));
                        if (PerlinNoise.IsGrayScaleHeightMap)
                        {
                            HeightColor[index] = new Color(colorR, colorR, colorR,255);
                        }
                        else
                        HeightColor[index] = new Color(colorR, colorG, colorB,255);
                        continue;
                    }
                    //find the vertex that is nearest
                    float mindist = 10000000.0f;
                    indexk = -1;
                    for (k = 0; k < size; k++)
                    {
                        float distij = Utility.Dist2d(new Vector2(Verts[k].x, Verts[k].y),
                            new Vector2(i, j));
                        if (distij <= mindist)
                        {
                            indexk = k;
                            mindist = distij;
                        }
                    }
                    HeightColor[index] = new Color(Verts[indexk].R, Verts[indexk].G, Verts[indexk].B,255);
                }
            }
            Hull.Clear();
            return HeightColor;
        }

        public static Color[] RandomVoronoiBiome(int m_Width, int m_Height,
            float NormalizationConst, float xfreq, float yfreq, float pixelcutoff, float freq,
            float error, float powval, float Persistance, int Octave, 
            float mincutoff, float maxcutoff,int voronoi_num_vertices)
        {
            int i, j, k, index;
            float[] Persistances = new float[10];
            for (i = 0; i < 10; i++)
                Persistances[i] = MathF.Pow(Persistance, -1.0f * i);
            Color[] HeightColor = new Color[m_Width * m_Height];
            int size = voronoi_num_vertices;// (int)MathF.Floor(MathF.Min(m_Width, m_Height) * freq / NormalizationConst/100.0f + 1.0f);
            if (size > m_Width * m_Height) size = m_Width * m_Height;
            if (size < 2) size = 2;
            Utility.VoronoiVertex[] Verts = new Utility.VoronoiVertex[size];
            List<Point> AllPoints = new List<Point>();
            float[] PerlinNoiseMat = GetPerlinMatPersistancePerOctave(m_Width, m_Height,
                    NormalizationConst, xfreq, yfreq, pixelcutoff, freq, error
                , powval, Persistances, Octave, mincutoff, maxcutoff);
            for (k = 0; k < size; k++)
            {
                Verts[k].x = (int)Utility.Runif(0, m_Width - 1);
                Verts[k].y = (int)Utility.Runif(0, m_Height - 1);
                if (PerlinNoise.IsGrayScaleHeightMap)
                {
                    byte color = GetBiomeColor(PerlinNoiseMat[Verts[k].x + Verts[k].y * m_Width]).R;
                    Verts[k].R = color;
                    Verts[k].G = color;
                    Verts[k].B = color;
                    Verts[k].A = 255;
                }
                else
                {
                    Color col = GetBiomeColor(PerlinNoiseMat[Verts[k].x + Verts[k].y * m_Width]);
                    Verts[k].R = col.R;
                    Verts[k].G = col.G;
                    Verts[k].B = col.B;
                    Verts[k].A = 255;
                }
 
                Point Pt = new Point(Verts[k].x, Verts[k].y);
                AllPoints.Add(Pt);
            }
            Array.Clear(PerlinNoiseMat);
            AllPoints.Clear();

            int indexk;
            for (j = 0; j < m_Height; j++)
            {
                for (i = 0; i < m_Width; i++)
                {
                    index = (m_Width * j) + i;
                    //if(i<0.1f*m_Width || i>0.9f*m_Width || j<0.1f*m_Height || j>0.9f*m_Height){PictPixels[index].B =0;PictPixels[index].G =0;PictPixels[index].R =0;PictPixels[index].A =255;continue;}
                    //D3DXVECTOR3 col=GetColourBluetoWhite(PerlinNoiseMat[index],vmin,vmax,
                    //	mincol,maxcol);
                    //if outside the convex hull, make the common color
                    Point pt = new Point(i, j);
                    //find the vertex that is nearest
                    float mindist = 10000000.0f;
                    indexk = -1;
                    for (k = 0; k < size; k++)
                    {
                        float distij = Utility.Dist2d(new Vector2(Verts[k].x, Verts[k].y),
                            new Vector2(i, j));
                        if (distij <= mindist)
                        {
                            indexk = k;
                            mindist = distij;
                        }
                    }
                    HeightColor[index] = new Color(Verts[indexk].R, Verts[indexk].G, Verts[indexk].B,255);
                }
            }
            return HeightColor;
        }
        
        public static Color[] RandomizeSmoothTransitions(int m_Width, int m_Height,
            float powval, float mincutoff, float maxcutoff,
            Vector3 mincol, Vector3 maxcol, Vector2 startpos,
            Vector2 endpos)
        {
            int i, j, index;
            float dist_from_start, dist_from_end, wtstart, wtend;
            Color[] HeightColor = new Color[m_Width * m_Height];

            for (i = 0; i < m_Width; i++)
            {
                for (j = 0; j < m_Height; j++)
                {
                    index = (m_Width * j) + i;
                    //get weights
                    dist_from_start = 1.0f / MathF.Pow(Utility.Dist2d(
                        new Vector2(startpos.X, 1.0f - startpos.Y),
                        new Vector2((float)i / (1.0f * m_Width) + 0.0001f,
                            (float)j / (1.0f * m_Height) + 0.0001f)), powval);
                    dist_from_end = 1.0f / MathF.Pow(Utility.Dist2d(
                        new Vector2(endpos.X, 1.0f - endpos.Y),
                        new Vector2((float)i / (1.0f * m_Width) + 0.0001f,
                            (float)j / (1.0f * m_Height) + 0.0001f)), powval);
                    wtstart = dist_from_start / (dist_from_start + dist_from_end);
                    wtend = 1 - wtstart;// dist_from_end / (dist_from_start + dist_from_end);
                    Vector3 veccol = wtstart * mincol + wtend * maxcol;
                    if (PerlinNoise.IsGrayScaleHeightMap)
                    {
                        byte color = (byte)Utility.BoundValue0255(
                            MathF.Round(255.0f * veccol.X));
                        HeightColor[index] = new Color(color);
                    }
                    else
                    {
                        byte colorR = (byte)Utility.BoundValue0255(
                    MathF.Round(255.0f * veccol.X));
                        byte colorG = (byte)Utility.BoundValue0255(
                    MathF.Round(255.0f * veccol.Y));
                        byte colorB = (byte)Utility.BoundValue0255(
                    MathF.Round(255.0f * veccol.Z));
                        HeightColor[index] = new Color(colorR, colorG, colorB);
                    }
                }
            }
            return HeightColor;
        }

        public static Color[] RandomizePerlinOneChannelOnly(int m_Width, int m_Height,
            float NormalizationConst, float xfreq, float yfreq, float pixelcutoff, float freq,
            float error, float powval, float Persistance, int Octave, float mincutoff, float maxcutoff,
            int channel, float TargetHeightValue)
        {
            int i, j, index;
            float[] Persistances = new float[10];
            for (i = 0; i < 10; i++)
                Persistances[i] = MathF.Pow(Persistance, -1.0f * i);

            //FloatMat256x256 
            float[] PerlinNoiseMat = GetPerlinMatPersistancePerOctave(m_Width, m_Height,
                    NormalizationConst, xfreq, yfreq, pixelcutoff, freq, error
                , powval, Persistances, Octave, mincutoff, maxcutoff);

            Color[] HeightColor = new Color[m_Width * m_Height];

            for (i = 0; i < m_Width; i++)
            {
                for (j = 0; j < m_Height; j++)
                {
                    index = (m_Width * j) + i;
                    byte color = (byte)Utility.BoundValue0255(
                        MathF.Round(PerlinNoise.HeightMultiplier
                        * PerlinNoiseMat[index]));
                    if (channel == -1)
                    {
                        HeightColor[index] = new Color(color, color, color);
                    }
                    else if (channel == 0)
                    {
                        HeightColor[index] = new Color(color, 0, 0);
                    }
                    else if (channel == 1)
                    {
                        HeightColor[index] = new Color(0,color, 0);
                    }
                    else
                    {
                        HeightColor[index] = new Color(0, 0,color);
                    }
                }
            }
            Array.Clear(PerlinNoiseMat, 0, PerlinNoiseMat.Length);
            return HeightColor;
        }

        //random Perlin cloud, blue-white
        public static Color[] RandomCloud(int m_Width, int m_Height,
            float NormalizationConst, float xfreq, float yfreq, float pixelcutoff, float freq,
            float error, float powval, float Persistance, int Octave, float mincutoff, float maxcutoff,
            Vector3 mincol, Vector3 maxcol)
        {
            int i, j, index;
            float[] Persistances = new float[10];
            for (i = 0; i < 10; i++)
                Persistances[i] = MathF.Pow(Persistance, -1.0f * i);

            float[] PerlinNoiseMat = GetPerlinMatPersistancePerOctave(m_Width, m_Height,
                    NormalizationConst, xfreq, yfreq, pixelcutoff, freq, error, powval, Persistances,
                    Octave, mincutoff, maxcutoff);
            Color[] HeightColor = new Color[m_Width * m_Height];
            for (i = 0; i < m_Width; i++)
            {
                for (j = 0; j < m_Height; j++)
                {
                    index = (m_Width * j) + i;
                    Vector3 col = Utility.GetColourFromVec1toVec2(PerlinNoiseMat[index],
                     mincutoff, maxcutoff, mincol, maxcol);
                    float height = //PerlinNoiseMat[index];//
                               col.AsStrideColor().ToFloat(); 
                    if (PerlinNoise.IsGrayScaleHeightMap)
                    {
                        byte color = (byte)Utility.BoundValue0255(
                            MathF.Round(255.0f * height));
                        HeightColor[index] = new Color(color);
                    }
                    else
                        HeightColor[index] = ( height).AsStrideColor();
                }
            }
            Array.Clear(PerlinNoiseMat, 0, PerlinNoiseMat.Length);
            return HeightColor;
        }

        public static Color[] RandomizeBMP(int m_Width, int m_Height,
            float error, float mincutoff, float maxcutoff, string greyscale)
        {
            int i, j, index;
            Color[] HeightColor = new Color[m_Width * m_Height];
            for (i = 0; i < m_Width; i++)
            {
                for (j = 0; j < m_Height; j++)
                {
                    index = (m_Width * j) + i;
                    if (greyscale == "No")
                    {
                        float color = Utility.Runif(-error, error);
                        HeightColor[index] = color.AsStrideColor();
                    }
                    else
                    {
                        byte color = (byte)Utility.BoundValue0255(MathF.Round(PerlinNoise.HeightMultiplier
                            * Utility.Runif(-error, error)));
                        HeightColor[index] = new Color(color);
                    }
                }
            }
            return HeightColor;
        }
        public static Color[] RandomizeAdd2Existing(int m_Width, int m_Height,
             float error, TerrainComponent tcomp)
        {
            int i, j, index;
            Color[] HeightColor = new Color[m_Width * m_Height];
            for (i = 0; i < m_Width; i++)
            {
                for (j = 0; j < m_Height; j++)
                {
                    index = (m_Width * j) + i;
                    if (PerlinNoise.IsGrayScaleHeightMap)
                    {
                        float fl = tcomp.GetCPUHeightAt(i,j) + Utility.Runif(-error, error);
                        byte color = (byte)Utility.BoundValue0255(MathF.Round(fl));
                        HeightColor[index] = new Color(color);
                    }
                    else
                    {
                        float color = tcomp.GetCPUHeightAt(i, j) + Utility.Runif(-error, error);
                        HeightColor[index] = color.AsStrideColor();
                    }
                }
            }
            return HeightColor;
        }

        public static Color[] PerlinSmooth(TerrainComponent tcomp,
            float normalizationcost=1.0f)
        {
            int i, j, index;
            int m_Width = tcomp.Width, m_Height = tcomp.Height;
            float[] ImageHeights = tcomp.GetAllHeights();// Heightmap.ToFloats();//.new float[m_Width * m_Height];

            #region smoothers
            for (i = 1; i < m_Width - 1; i++)
            {
                for (j = 1; j < m_Height - 1; j++)
                {
                    ImageHeights[(m_Width * j) + i] = (10.0f*(
                        ImageHeights[(m_Width * (j - 1)) + i - 1]//[x-1][z-1]
                        + ImageHeights[(m_Width * (j + 1)) + i - 1]//[x-1][z+1]
                        + ImageHeights[(m_Width * (j - 1)) + i + 1]//[x+1][z-1]
                        + ImageHeights[(m_Width * (j + 1)) + i + 1]//[x+1][z+1]
                        + ImageHeights[(m_Width * (j + 1)) + i]//[x][z+1]
                        + ImageHeights[(m_Width * (j - 1)) + i]//[x][z-1]
                        + ImageHeights[(m_Width * j) + i + 1]//[x+1][z]
                        + ImageHeights[(m_Width * j) + i - 1]//[x-1][z]
                        ) / 80.0f);
                }
            }
            i = 0; j = 0;
            ImageHeights[(m_Width * j) + i] = (10.0f * (
                        ImageHeights[(m_Width * (j + 1)) + i + 1]//[x+1][z+1]
                        + ImageHeights[(m_Width * (j + 1)) + i]//[x][z+1]
                        + ImageHeights[(m_Width * j) + i + 1]//[x+1][z]
                        ) / 30.0f);
            i = 0; j = m_Height - 1;
            ImageHeights[(m_Width * j) + i] = (10.0f * (
                        ImageHeights[(m_Width * (j - 1)) + i + 1]//[x+1][z-1]
                        + ImageHeights[(m_Width * (j - 1)) + i]//[x][z-1]
                        + ImageHeights[(m_Width * j) + i + 1]//[x+1][z]
                        ) / 30.0f);
            i = m_Width - 1; j = 0;
            ImageHeights[(m_Width * j) + i] = (10.0f * (
                        ImageHeights[(m_Width * (j + 1)) + i - 1]//[x-1][z+1]
                        + ImageHeights[(m_Width * (j + 1)) + i]//[x][z+1]
                        + ImageHeights[(m_Width * j) + i - 1]//[x-1][z]
                        ) / 30.0f);
            i = m_Width - 1; j = m_Height - 1;
            ImageHeights[(m_Width * j) + i] =(10.0f * (
                        ImageHeights[(m_Width * (j - 1)) + i - 1]//[x-1][z-1]
                        + ImageHeights[(m_Width * (j - 1)) + i]//[x][z-1]
                        + ImageHeights[(m_Width * j) + i - 1]//[x-1][z]
                        ) / 30.0f);
            for (j = 1; j < m_Height - 1; j++)
            {
                i = 0;
                ImageHeights[(m_Width * j) + i] = (10.0f * (
                            +ImageHeights[(m_Width * (j - 1)) + i + 1]//[x+1][z-1]
                        + ImageHeights[(m_Width * (j + 1)) + i + 1]//[x+1][z+1]
                        + ImageHeights[(m_Width * (j + 1)) + i]//[x][z+1]
                        + ImageHeights[(m_Width * (j - 1)) + i]//[x][z-1]
                        + ImageHeights[(m_Width * j) + i + 1]//[x+1][z]
                        ) / 50.0f);
                i = m_Width - 1;
                ImageHeights[(m_Width * j) + i] =(10.0f * (
                        ImageHeights[(m_Width * (j - 1)) + i - 1]//[x-1][z-1]
                        + ImageHeights[(m_Width * (j + 1)) + i - 1]//[x-1][z+1]
                        + ImageHeights[(m_Width * (j + 1)) + i]//[x][z+1]
                        + ImageHeights[(m_Width * (j - 1)) + i]//[x][z-1]
                        + ImageHeights[(m_Width * j) + i - 1]//[x-1][z]
                        ) / 50.0f);
            }

            for (i = 1; i < m_Width - 1; i++)
            {
                j = 0;
                ImageHeights[(m_Width * j) + i] = (10.0f * (
                    ImageHeights[(m_Width * (j + 1)) + i - 1]//[x-1][z+1]
                        + ImageHeights[(m_Width * (j + 1)) + i + 1]//[x+1][z+1]
                        + ImageHeights[(m_Width * (j + 1)) + i]//[x][z+1]
                        + ImageHeights[(m_Width * j) + i + 1]//[x+1][z]
                        + ImageHeights[(m_Width * j) + i - 1]//[x-1][z]
                        ) / 50.0f);

                j = m_Height - 1;
                ImageHeights[(m_Width * j) + i] = (10.0f * (
                        ImageHeights[(m_Width * (j - 1)) + i - 1]//[x-1][z-1]
                        + ImageHeights[(m_Width * (j - 1)) + i + 1]//[x+1][z-1]
                        + ImageHeights[(m_Width * (j - 1)) + i]//[x][z-1]
                        + ImageHeights[(m_Width * j) + i + 1]//[x+1][z]
                        + ImageHeights[(m_Width * j) + i - 1]//[x-1][z]
                        ) / 50.0f);
            }
            #endregion smoothers

            for (i = 0; i < m_Width; i++)
            {
                for (j = 0; j < m_Height; j++)
                {
                    index = (m_Width * j) + i;
                  /*  if (PerlinNoise.IsGrayScaleHeightMap)
                    {
                        byte color = (byte)Utility.BoundValue0255(MathF.Round(
                            ImageHeights[index]/ normalizationcost));
                        HeightColor[index] = new Color(color);
                    }
                    else
                        HeightColor[index] = (ImageHeights[index]).AsStrideColor();
                    */
                    float ht = ImageHeights[index] / normalizationcost;
                    float height = (ht - tcomp.HeightRange.X) *
                        PerlinNoise.HeightMultiplier /
                        (tcomp.HeightRange.Y - tcomp.HeightRange.X);
                    if (PerlinNoise.IsGrayScaleHeightMap)
                    {
                        byte b = height.ToByte();
                        tcomp.HeightMapColors[j * m_Width + i] =
                            new Color(b, b, b, 255);
                    }
                    else
                        tcomp.HeightMapColors[j * m_Width + i] =
                            height.AsStrideColor();
                }
            }
            Array.Clear(ImageHeights, 0, ImageHeights.Length);
            return tcomp.HeightMapColors;
        }
        
        public static Color[] SmoothVertexWeights(TerrainComponent tcomp,
            int which=1,float normalizationcost = 1.0f)
        {
            int i, j;
            int m_Width = tcomp.Width, m_Height = tcomp.Height;
            Color[] Weights = tcomp.GetAllWeights(which);
            Stride.Core.Mathematics.Int2 size = new
                Stride.Core.Mathematics.Int2(tcomp.Width, tcomp.Height);

            #region smoothers
            for (i = 0; i < m_Width; i++)
            {
                for (j = 0; j < m_Height; j++)
                {
                    Color col = Color.Zero;
                    if (IsValidCoordinate(i - 1, j - 1, m_Width, m_Height))
                        col += Weights[(m_Width * (j - 1)) + i - 1];
                    if (IsValidCoordinate(i - 1, j , m_Width, m_Height))
                        col += Weights[(m_Width * j) + i - 1];
                    if (IsValidCoordinate(i - 1, j + 1, m_Width, m_Height))
                        col += Weights[(m_Width * (j + 1)) + i - 1];
                    if (IsValidCoordinate(i + 1, j - 1, m_Width, m_Height))
                        col += Weights[(m_Width * (j - 1)) + i + 1];
                    if (IsValidCoordinate(i + 1, j , m_Width, m_Height))
                        col += Weights[(m_Width * j) + i + 1];
                    if (IsValidCoordinate(i + 1, j + 1, m_Width, m_Height))
                        col += Weights[(m_Width * (j + 1)) + i + 1];
                    if (IsValidCoordinate(i, j + 1, m_Width, m_Height))
                        col += Weights[(m_Width * (j + 1)) + i ];
                    if (IsValidCoordinate(i, j - 1, m_Width, m_Height))
                        col += Weights[(m_Width * (j - 1)) + i ];
                   // col = col.ToColor4().ToVector4().Fix01().AsNumericVec4().ToStrideColor();
                    Weights[(m_Width * j) + i] = col *(1/ HeightMapEditor.GeneralExtensions.CountNeighbors(
                            size, i, j)/ normalizationcost);
                }
            }

            #endregion smoothers

            return Weights;
        }
        
        /// <summary>
        /// works the best and with the cpu values stored. weight textures are updated after this call
        /// </summary>
        /// <param name="tcomp"></param>
        /// <param name="normalizationcost"></param>
        public static void SmoothAllVertexWeights(TerrainComponent tcomp,
           float normalizationcost = 1.0f)
        {
            int i, j;
            int m_Width = tcomp.Width, m_Height = tcomp.Height;
            Stride.Core.Mathematics.Int2 size = new
                Stride.Core.Mathematics.Int2(tcomp.Width, tcomp.Height);

            #region smoothers
            for (i = 0; i < m_Width; i++)
            {
                for (j = 0; j < m_Height; j++)
                {
                    Stride.Core.Mathematics.Int2 pos = new Stride.Core.Mathematics.Int2(i, j);
                    float num = GeneralExtensions.CountNeighbors(new
                        Stride.Core.Mathematics.Int2(tcomp.Width, tcomp.Height), i, j);
                    Stride.Core.Mathematics.Vector4 col = tcomp.GetCPUColorAt(i, j).ToVector4();
                    Stride.Core.Mathematics.Vector4 wt1 = tcomp.GetCPUWeight1At(i, j).ToVector4();
                    Stride.Core.Mathematics.Vector4 wt2 = tcomp.GetCPUWeight2At(i, j).ToVector4();
                    col = (tcomp.GetCPUColorAt(i - 1, j - 1).ToVector4() +
                        tcomp.GetCPUColorAt(i - 1, j).ToVector4() +
                        tcomp.GetCPUColorAt(i - 1, j + 1).ToVector4() +
                        tcomp.GetCPUColorAt(i + 1, j - 1).ToVector4() +
                        tcomp.GetCPUColorAt(i + 1, j).ToVector4() +
                        tcomp.GetCPUColorAt(i + 1, j + 1).ToVector4() +
                        tcomp.GetCPUColorAt(i, j - 1).ToVector4() +
                        tcomp.GetCPUColorAt(i, j + 1).ToVector4())
                        / num;
                    wt1 = (tcomp.GetCPUWeight1At(i - 1, j - 1).ToVector4() +
                   tcomp.GetCPUWeight1At(i - 1, j).ToVector4() +
                   tcomp.GetCPUWeight1At(i - 1, j + 1).ToVector4() +
                   tcomp.GetCPUWeight1At(i + 1, j - 1).ToVector4() +
                   tcomp.GetCPUWeight1At(i + 1, j).ToVector4() +
                   tcomp.GetCPUWeight1At(i + 1, j + 1).ToVector4() +
                   tcomp.GetCPUWeight1At(i, j - 1).ToVector4() +
                   tcomp.GetCPUWeight1At(i, j + 1).ToVector4())
                   / num;
                    wt2 = (tcomp.GetCPUWeight2At(i - 1, j - 1).ToVector4() +
                   tcomp.GetCPUWeight2At(i - 1, j).ToVector4() +
                   tcomp.GetCPUWeight2At(i - 1, j + 1).ToVector4() +
                   tcomp.GetCPUWeight2At(i + 1, j - 1).ToVector4() +
                   tcomp.GetCPUWeight2At(i + 1, j).ToVector4() +
                   tcomp.GetCPUWeight2At(i + 1, j + 1).ToVector4() +
                   tcomp.GetCPUWeight2At(i, j - 1).ToVector4() +
                   tcomp.GetCPUWeight2At(i, j + 1).ToVector4())
                   / num;
                    col = col.Fix01();
                    wt1 = wt1.Fix01();
                    wt2 = wt2.Fix01();
                    tcomp.SetVertexColor(pos, col, wt1, wt2);
                }
            }
            #endregion smoothers
        }

        public static bool IsValidCoordinate(int x, int y, int Width, int Height)
            => x >= 0 && x < Width && y >= 0 && y < Height;

        public static Color[] MakeFlat(int m_Width, int m_Height,
            Color col)
        {
            int i, j, index;
            Color[] HeightColor = new Color[m_Width * m_Height];
            if(col==Color.Zero) return HeightColor;
            for (i = 0; i < m_Width; i++)
            {
                for (j = 0; j < m_Height; j++)
                {
                    index = (m_Width * j) + i;
                    HeightColor[index]= col;
                }
            }
           return HeightColor;
        }
       
        public static Color[] RandomizeBMPPerlin2(
            int m_Width, int m_Height, float NormalizationConst,
               float xfreq, float yfreq, float pixelcutoff, float freq, float error
               , float powval, float Persistance, int Octave, float mincutoff, float maxcutoff,
               string greyscale,bool saveit=false)
        {
            int i, j, index;
            float[] Persistances = new float[10];
            for (i = 0; i < 10; i++)
                Persistances[i] = MathF.Pow(Persistance, -1.0f * i);
            Color[] HeightColor = new Color[m_Width * m_Height];
            if (greyscale == "Yes")
            {               
                float[] PerlinNoiseMat = GetPerlinMatPersistancePerOctave(m_Width, m_Height,
                    NormalizationConst, xfreq, yfreq, pixelcutoff, freq, error
                , powval, Persistances, Octave, mincutoff, maxcutoff);
               // Bitmap newBitmap = new Bitmap(m_Width, m_Height);
                for (i = 0; i < m_Width; i++)
                {
                    for (j = 0; j < m_Height; j++)
                    {
                        index = (m_Width * j) + i;
                        if (PerlinNoise.IsGrayScaleHeightMap)
                        {
                            byte color = (byte)Utility.BoundValue0255(
                                MathF.Round(PerlinNoise.HeightMultiplier
                            * PerlinNoiseMat[index]));//(int)(Utility.BoundValue01(PerlinNoiseMat[index], mincutoff, maxcutoff) * 255.0f);//Convert to 0-256 values.
                            HeightColor[index] = new Color(color);// PerlinNoiseMat[index].AsStrideColor();
                        }
                        else
                            HeightColor[index] = (PerlinNoise.HeightMultiplier
                            * PerlinNoiseMat[index]).AsStrideColor();
                    }
                }
               /* if (System.IO.File.Exists(filename))
                    System.IO.File.Delete(filename);
                newBitmap.Save(filename, System.Drawing.Imaging.ImageFormat.Bmp);
                // Dispose of the image files.
                newBitmap.Dispose();*/
                Array.Clear(PerlinNoiseMat, 0, PerlinNoiseMat.Length);
            }
            else
            {
                float[] PerlinNoiseMatRed = GetPerlinMatPersistancePerOctave(m_Width, m_Height,
                    NormalizationConst, xfreq, yfreq, pixelcutoff, freq, error
                , powval, Persistances, Octave, mincutoff, maxcutoff);
                float[] PerlinNoiseMatGreen = GetPerlinMatPersistancePerOctave(m_Width, m_Height,
                    NormalizationConst, xfreq, yfreq, pixelcutoff, freq, error
                , powval, Persistances, Octave, mincutoff, maxcutoff);
                float[] PerlinNoiseMatBlue = GetPerlinMatPersistancePerOctave(m_Width, m_Height,
                    NormalizationConst, xfreq, yfreq, pixelcutoff, freq, error
                , powval, Persistances, Octave, mincutoff, maxcutoff);
                for (i = 0; i < m_Width; i++)
                {
                    for (j = 0; j < m_Height; j++)
                    {
                        index = (m_Width * j) + i;
                        byte red = (byte)Utility.BoundValue0255(
                            MathF.Round(255.0f * PerlinNoiseMatRed[index]));
                        byte green = (byte)Utility.BoundValue0255(MathF.Round(255.0f * PerlinNoiseMatGreen[index]));
                        byte blue = (byte)Utility.BoundValue0255(MathF.Round(255.0f * PerlinNoiseMatBlue[index]));
                        HeightColor[index] = new Color(red,green,blue);
                    }
                }
                Array.Clear(PerlinNoiseMatRed, 0, PerlinNoiseMatRed.Length);
                Array.Clear(PerlinNoiseMatGreen, 0, PerlinNoiseMatGreen.Length);
                Array.Clear(PerlinNoiseMatBlue, 0, PerlinNoiseMatBlue.Length);
            }
            if (saveit) 
            { 
            }
            return HeightColor;
        }
        public static Vector2 grad(Vector2 p, float[] Gradient, int m_Width, int m_Height)
        {
            int index = (m_Width * (int)p.Y) + (int)p.X;
            Vector2 pV = 2.0f * (new Vector2(Gradient[index], Gradient[index + 1])) - new Vector2(1.0f, 1.0f), pOut;
            pOut = Vector2.Normalize(pV);
            return pOut;
        }

        public static float PerlinNoiseGen(Vector2 p, float[] Gradient, int m_Width, int m_Height)
        {
            //int x,y;
            //if (floorf(p.x) < 0 || floorf(p.x) > (m_Width - 1))
            //	x = (int)floorf(p.x) & (m_Width - 1);		else
            //	int sample_j0 = (j / samplePeriod) * samplePeriod;
            //	int sample_j1 = (sample_j0 + samplePeriod) % m_Height; //wrap around
            //	float vertical_blend = (j - sample_j0) * sampleFrequency;

            int x = (int)Math.Floor(p.X) & (m_Width - 1);
            int y = (int)Math.Floor(p.Y) & (m_Height - 1);
            //    Y = Math.floor(y) & 255;
            // Find relative x,y of point in square.
            // x -= Math.floor(x);
            // y -= Math.floor(y);
            /* Calculate lattice points. */
            Vector2 p0 = new Vector2(x, y);
            Vector2 p1 = p0 + new Vector2(1.0f, 0.0f);
            Vector2 p2 = p0 + new Vector2(0.0f, 1.0f);
            Vector2 p3 = p0 + new Vector2(1.0f, 1.0f);

            /* Look up gradients at lattice points. */
            Vector2 g0 = grad(p0, Gradient, m_Width, m_Height);
            Vector2 g1 = grad(p1, Gradient, m_Width, m_Height);
            Vector2 g2 = grad(p2, Gradient, m_Width, m_Height);
            Vector2 g3 = grad(p3, Gradient, m_Width, m_Height);

            float t0 = p.X - p0.X;
            float fade_t0 = fade(t0); /* Used for interpolation in horizontal direction */

            float t1 = p.Y - p0.Y;
            float fade_t1 = fade(t1); /* Used for interpolation in vertical direction. */

            /* Calculate dot products and interpolate.*/
            Vector2 v0 = p - p0, v1 = p - p1, v2 = p - p2, v3 = p - p3;

            float p0p1 = (1.0f - fade_t0) * Vector2.Dot(g0, v0)
                + fade_t0 * Vector2.Dot(g1, v1); /* between upper two lattice points */
            float p2p3 = (1.0f - fade_t0) * Vector2.Dot(g2, v2)
                + fade_t0 * Vector2.Dot(g3, v3); /* between lower two lattice points */

            /* Calculate final result */
            return (1.0f - fade_t1) * p0p1 + fade_t1 * p2p3;
        }
        public static float fade(float t)
        {
            return t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f);
        }

        public static float[] GetPerlinMatPersistancePerOctave(int m_Width, int m_Height, float NormalizationConst,
                float xfreq, float yfreq, float pixelcutoff, float freq, float error
                , float powval, float[] Persistance, int Octave, float mincutoff, float maxcutoff)
        {
            int i, j, index;
            float[] PerlinNoiseMat = new float[m_Width * m_Height];
            float[] Gradient = new float[m_Width * m_Height * 2];
            for (i = 0; i < m_Width; i++)
            {
                for (j = 0; j < m_Height; j++)
                {
                    index = (m_Width * j) + i;
                    float theta = (float)(Utility.RandomFloat(0.0f, 2.0f) * Math.PI);
                    Gradient[index] = (float)Math.Cos(theta);
                    Gradient[index + 1] = (float)Math.Sin(theta);
                }
            }

            /*    //have to loop it
                for (i = 0; i < m_Width; i++)
                {
                    for (j = 0; j < m_Height; j++)
                    {
                        index = (m_Width * j) + i;
                        PerlinNoiseMat[index] = 0;
                    }
                }
    */

            float minf = 1000000.0f, maxf = -100000.0f;
            float amplitude = 1.0f;
            float totalAmplitude = 0.0f;
            float perlin;
            float[] octaves = { 1, 2, 4, 8, 16, 32, 64, 128, 256, 512 };
            //for (i = 0; i < 20; i++)octaves[i] = powf(2.0, 1.0f * i);

            for (int oct = 0; oct < Octave; oct++)//This loops trough the octaves.
            {
                for (i = 0; i < m_Width; i++)
                {
                    for (j = 0; j < m_Height; j++)
                    {
                        index = (m_Width * j) + i;
                        float nx = 0.5f * i / m_Width,//-0.5f,
                            ny = 0.5f * j / m_Height;// -0.5f;
                                                     //the points cannot be the grid points
                        perlin = PerlinNoiseGen(new Vector2((float)(octaves[oct] * nx + oct * xfreq),
                            (float)(octaves[oct] * ny + oct * yfreq)), Gradient, m_Width, m_Height);
                        if (perlin > 100.0f || perlin < -100.0f) perlin = 0.0f;
                        PerlinNoiseMat[index] += 0.5f * (1.0f + perlin) * amplitude;
                        //				newfreq *= 2.0f;
                    }
                }
                //		PerlinNoiseMat[index] /= totalAmplitude;
                //PerlinNoiseMat[index] = powf(PerlinNoiseMat[index] / totalAmplitude , powval);
                amplitude *= Persistance[oct];// *runif(0.1f, 0.5f);// 			powf(Persistance, -error * oct);
                totalAmplitude += amplitude;
            }

            for (i = 0; i < m_Width; i++)
            {
                for (j = 0; j < m_Height; j++)
                {
                    index = (m_Width * j) + i;
                    PerlinNoiseMat[index] /= totalAmplitude;
                    PerlinNoiseMat[index] += Utility.NormalError(0,error);
                    if (PerlinNoiseMat[index] < minf) minf = PerlinNoiseMat[index];
                    if (PerlinNoiseMat[index] > maxf) maxf = PerlinNoiseMat[index];
                }
            }

            //normalization and cutoffs
            for (i = 0; i < m_Width; i++)
            {
                for (j = 0; j < m_Height; j++)
                {
                    index = (m_Width * j) + i;
                    if (PerlinNoiseMat[index] - minf < pixelcutoff * (maxf - minf))
                        PerlinNoiseMat[index] = pixelcutoff * (maxf - minf) + minf;
                }
            }

            for (i = 0; i < m_Width; i++)
            {
                for (j = 0; j < m_Height; j++)
                {
                    index = (m_Width * j) + i;
                    PerlinNoiseMat[index] = (PerlinNoiseMat[index] - minf) / (maxf - minf);
                    PerlinNoiseMat[index] /= NormalizationConst;
                    if (PerlinNoiseMat[index] < mincutoff) PerlinNoiseMat[index] = mincutoff;
                    if (PerlinNoiseMat[index] > maxcutoff) PerlinNoiseMat[index] = maxcutoff;
                }
            }
            Array.Clear(Gradient, 0, Gradient.Length);
            return PerlinNoiseMat;
        }
        public static float[] GetPerlinMat(int m_Width, int m_Height, float NormalizationConst,
                float xfreq, float yfreq, float pixelcutoff, float freq, float error
                , float powval, float Persistance, int Octave, float mincutoff, float maxcutoff)
        {
            int i, j, index;
            float[] PerlinNoiseMat = new float[m_Width * m_Height];
            float[] Gradient = new float[m_Width * m_Height * 2];
            for (i = 0; i < m_Width; i++)
            {
                for (j = 0; j < m_Height; j++)
                {
                    index = (m_Width * j) + i;
                    float theta = (float)(Utility.RandomFloat(0.0f, 2.0f) * Math.PI);
                    Gradient[index] = (float)Math.Cos(theta);
                    Gradient[index + 1] = (float)Math.Sin(theta);
                }
            }

            float minf = 10000000000.0f, maxf = -10000000000.0f;
            //           float totalAmplitude = 0.0f;
            float[] octaves = { 1, 2, 4, 8, 16, 32, 64, 128, 256, 512 };
            //for (i = 0; i < 20; i++)octaves[i] = powf(2.0, 1.0f * i);
            float newfreq = freq, newfreqx = xfreq, newfreqy = yfreq;
            for (i = 0; i < m_Width; i++)
            {
                for (j = 0; j < m_Height; j++)
                {
                    index = (m_Width * j) + i;
                    float partsum, perlin;
                    float[] vals = new float[10];
                    int oct, oct1;
                    float nx = freq * xfreq * i / m_Width,//-0.5f,
                        ny = freq * yfreq * j / m_Height;// -0.5f;
                    float amplitude = 1.0f;
                    //the points cannot be the grid points
                    for (oct = 0; oct < Octave; oct++)
                    {
                        perlin = PerlinNoiseGen(new Vector2(octaves[oct] * nx //+ oct * xfreq
                            , octaves[oct] * ny //+ oct * yfreq
                        ), Gradient, m_Width, m_Height);
                        if (perlin > 1000.0f || perlin < -1000.0f) perlin = 0.0f;
                        partsum = 0;
                        for (oct1 = 0; oct1 < oct; oct1++) partsum += vals[oct1];
                        if (oct > 0)
                            vals[oct] = (partsum * (2.0f - MathF.Abs(perlin) * 2.0f) * 2.0f) * amplitude;
                        else
                            vals[oct] = (2.0f - MathF.Abs(perlin) * 2.0f) * 2.0f * amplitude;
                        amplitude *= Persistance;// [oct];
                        //				0.5f * (1.0f + perlin) * amplitude
                    }
                    float val = 0.0f;
                    for (oct = 0; oct < Octave; oct++) val += vals[oct];
                    PerlinNoiseMat[index] = val;
                    if (PerlinNoiseMat[index] < minf) minf = PerlinNoiseMat[index];
                    if (PerlinNoiseMat[index] > maxf) maxf = PerlinNoiseMat[index];
                }
            }

            /*  for (int oct = Octave; oct >= 0; oct--)//This loops trough the octaves.
              {
                  amplitude *= MathF.Pow(Persistance, error);
                  totalAmplitude += amplitude;
                  newfreqx = newfreqx + Utility.Runif(0, 2);
                  newfreqy = newfreqy + Utility.Runif(0, 2);
                  for (i = 0; i < m_Width; i++)
                  {
                      for (j = 0; j < m_Height; j++)
                      {
                          index = (m_Width * j) + i;
                          //the points cannot be the grid points
                          PerlinNoiseMat[index] +=
                              MathF.Pow(PerlinNoiseGen(new Vector2(newfreq * newfreqx * i / (m_Width*1.0f),
                              newfreq * newfreqy * j / (m_Height * 1.0f)), Gradient, m_Width, m_Height) * amplitude,
                              powval);
                          if (PerlinNoiseMat[index] < minf) minf = PerlinNoiseMat[index];
                          if (PerlinNoiseMat[index] > maxf) maxf = PerlinNoiseMat[index];
                      }
                  }
              }
  */
            //normalization
            for (i = 0; i < m_Width; i++)
            {
                for (j = 0; j < m_Height; j++)
                {
                    index = (m_Width * j) + i;
                    PerlinNoiseMat[index] += Utility.NormalError(0, error);
                    PerlinNoiseMat[index] = (PerlinNoiseMat[index] - minf) / (maxf - minf);
                    PerlinNoiseMat[index] /= NormalizationConst;
                    if (PerlinNoiseMat[index] < pixelcutoff) PerlinNoiseMat[index] = 0;
                    if (PerlinNoiseMat[index] < mincutoff) PerlinNoiseMat[index] = mincutoff;
                    if (PerlinNoiseMat[index] > maxcutoff) PerlinNoiseMat[index] = maxcutoff;
                }
            }

            Array.Clear(Gradient, 0, Gradient.Length);
            return PerlinNoiseMat;
        }

    }

}