//By Idomeneas
using Stride.Core.Mathematics;
using Stride.Graphics;
using System;
using Stride.Core.Annotations;
using Stride.Shaders;
using Stride.Rendering.Materials;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Stride.Rendering.Materials.ComputeColors;
using System.Text;
using Stride.Core;


namespace TerrainEditor
{
    /// <summary>
    /// Custom vertex type so that we can generate tangents for supporting normal maps
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    [DataContract]
    [DataStyle(DataStyle.Compact)]
    public struct VertexTypePosTexNormColor : IEquatable<VertexTypePosTexNormColor>, IVertex
    {
        public VertexTypePosTexNormColor(Vector3 position, 
            Vector3 normal, Vector3 tangent, 
            Vector2 TexCoord1, Color Color1
            //, Vector4 Weights11, Vector4 Weights21
            ) : this()
        {
            Position = position;
            Normal = normal;
            Tangent = tangent;
            TexCoord = TexCoord1;
            Color = Color1;
 //           FirstTexWeights = Weights11;
 //           SecondTexWeights = Weights21;
        }

        public Vector3 Position;
        public Vector3 Normal;
        public Vector3 Tangent;
        public Vector2 TexCoord;
        public Color Color;
        public Color Color1;// FirstTexWeights;
        public Color Color2;// SecondTexWeights;

        public static readonly int Size = 36;// 60;//44;

        public static readonly VertexDeclaration Layout = new VertexDeclaration(
           VertexElement.Position<Vector3>(),//12=4*3
           VertexElement.Normal<Vector3>(),//24
           VertexElement.Tangent<Vector3>(),//36
           VertexElement.TextureCoordinate<Vector2>(),//44
           VertexElement.Color<Color>(0),//,//60
           VertexElement.Color<Color>(1),
           VertexElement.Color<Color>(2));//92

        public bool Equals(VertexTypePosTexNormColor other)
            => //FirstTexWeights.Equals(other.FirstTexWeights) && SecondTexWeights.Equals(other.SecondTexWeights) &&
            Position.Equals(other.Position) && Normal.Equals(other.Normal) && Tangent.Equals(other.Tangent)
            && Color.Equals(other.Color) && TexCoord.Equals(other.TexCoord);

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is VertexTypePosTexNormColor && Equals((VertexTypePosTexNormColor)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Position.GetHashCode();
                hashCode = (hashCode * 397) ^ Normal.GetHashCode();
                hashCode = (hashCode * 397) ^ TexCoord.GetHashCode();
                hashCode = (hashCode * 397) ^ Color.GetHashCode();
 //               hashCode = (hashCode * 397) ^ FirstTexWeights.GetHashCode();
   //             hashCode = (hashCode * 397) ^ SecondTexWeights.GetHashCode();
                return hashCode;
            }
        }

        public VertexDeclaration GetLayout()
            => Layout;

        public void FlipWinding()
            => TexCoord.X = (1.0f - TexCoord.X);

        public static bool operator ==(VertexTypePosTexNormColor left, VertexTypePosTexNormColor right)
            => left.Equals(right);

        public static bool operator !=(VertexTypePosTexNormColor left, VertexTypePosTexNormColor right)
            => !left.Equals(right);

        public override string ToString()
            => string.Format("Position: {0}, Normal: {1}, Tangent {2}, Texcoord: {3}, Color: {4}",//, Texture Weights1: {5}, Texture Weights12: {6}",
                Position, Normal, Tangent, TexCoord, Color
                //, FirstTexWeights, SecondTexWeights
                );

    }

    [DataContract("TerrainMaskComputeColor")]
    [Display("Terrain Mask")]
    public class TerrainMaskComputeColor : ComputeNode, IComputeScalar
    {
        [DataMember(0), DataMemberRange(0, 8)] // Could be increaed to larger than 8 if more colors are added to the vertex stream
        public int TerrainLayerIndex { get; set; }

        private (string, string) GetSemanticNameAndChannel()
        {
            // Calculate the name of the correct semantic from COLOR0 to COLOR(n) and channel
            // 4 channels per color.
            var semanticIndex = TerrainLayerIndex / 4;
            var channel = (TerrainLayerIndex % 4) switch
            {
                0 => "r",
                1 => "g",
                2 => "b",
                _ => "a"
            };

            return ($"COLOR{semanticIndex}", channel);
        }

        public override ShaderSource GenerateShaderSource(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            var (semanticName, channel) = GetSemanticNameAndChannel();
            return new ShaderClassSource("ComputeColorFromStream", semanticName, channel);
        }
    }

    /// <summary>
    /// Describes a custom vertex format structure that contains position and color information. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct VertexPositionColor : IEquatable<VertexPositionColor>, IVertex
    {
        /// <summary>
        /// Initializes a new <see cref="VertexPositionColor"/> instance.
        /// </summary>
        /// <param name="position">The position of this vertex.</param>
        /// <param name="color">the color</param>
        public VertexPositionColor(Vector3 position, Vector4 color)
            : this()
        {
            Position = position;
            Color = color;
        }

        /// <summary>
        /// XYZ position.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// The color.
        /// </summary>
        public Vector4 Color;

        /// <summary>
        /// Defines structure byte size.
        /// </summary>
        public static readonly int Size = 28;

        /// <summary>
        /// The vertex layout of this structure.
        /// </summary>
        public static readonly VertexDeclaration Layout = new VertexDeclaration(
            VertexElement.Position<Vector3>(),
            VertexElement.Color<Vector4>());

        public bool Equals(VertexPositionColor other)
        {
            return Position.Equals(other.Position) && Color.Equals(other.Color);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is VertexPositionColor && Equals((VertexPositionColor)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Position.GetHashCode();
                hashCode = (hashCode * 397) ^ Color.GetHashCode();
                return hashCode;
            }
        }

        public VertexDeclaration GetLayout()
        {
            return Layout;
        }

        public void FlipWinding()
        {
        }

        public static bool operator ==(VertexPositionColor left, VertexPositionColor right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(VertexPositionColor left, VertexPositionColor right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return string.Format("Position: {0}, Color: {1}", Position, Color);
        }
    }

}
