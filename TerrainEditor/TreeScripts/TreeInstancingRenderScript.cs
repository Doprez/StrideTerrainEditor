//by Idomeneas
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Collections;
using Stride.Engine;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering;
using Stride.Extensions;
using System.Runtime.InteropServices;
using HeightMapEditor;
using System.Linq;
using System.Collections.Generic;

namespace TerrainEditor
{

    public class TreeInstancingRenderScript : StartupScript
    {
        #region data
        [Display(Browsable = true)]
        [DataMember(10)]
        public bool Enabled = false;

        [Display(Browsable = false)]
        [DataMember(20)]
        public int InstanceCount { get; set; } = 0;

        [Display(Browsable = false)]
        [DataMember(30)]
        public Buffer<Vector3> InstanceLocations;

        [Display(Browsable = false)]
        [DataMember(40)]
        public Buffer<Vector4> InstanceColors;

        [Display(Browsable = false)]
        [DataMember(41)]
        public Buffer<int> InstanceType;

        [Display(Browsable = false)]
        [DataMember(42)]
        public Buffer<Vector3> InstanceScale;

        [Display(Browsable = false)]
        [DataMember(44)]
        public Buffer<Vector3> InstanceRotation;

        [DataMember(50)]
        public Model VegetationModel1 { get; set; }

        [DataMember(51)]
        public Model VegetationModel2 { get; set; }

        [DataMember(52)]
        public Model VegetationModel3 { get; set; }

        /// <summary>
        /// Maximum number of vegetation models loaded 
        /// </summary>
        [DataMember(60)]
        public int MaxTreeInstances = 30000;

        /// <summary>
        /// Maximum number of vegetation models loaded 
        /// </summary>
        [DataMember(61)]
        public int MaxTreeTypes = 3;

        /*       [DataMember(70)]
               public List<VegetationInstance>? VegetationInstances { get; set; } = new List<VegetationInstance>();

               [DataContract]
               public struct VegetationInstance
               {
                   /// <summary>
                   /// Index that points to the vegetation model we display for this instance.
                   /// Value in 1-MaxTreeTypes
                   /// </summary>
                   public int VegModelType { get; set; }
                   public Vector3 VegPosition { get; set; }
                   public Vector3 VegScale { get; set; }
                   public Vector3 VegRotation { get; set; }

                   public VegetationInstance(int VegModelType1, Vector3 VegPosition1,
                       Vector3 VegScale1, Vector3 VegRotation1)
                   {
                       VegModelType = VegModelType1;
                       VegPosition = VegPosition1;
                       VegScale = VegScale1;
                       VegRotation = VegRotation1;
                   }
               }
        */
        #endregion data
        TerrainComponent tcomp;

        public override void Start()
        {
            if (!Enabled) return;
            Entity Terrain_Entity =
                       Services.GetService<SceneSystem>().SceneInstance.FirstOrDefault(e => e.Name == "TerrainComponent");
            tcomp = Terrain_Entity.Get<TerrainComponent>();
            InitializeInstanceBuffersPosCol(GraphicsDevice);

            Entity.GetOrCreate<ModelComponent>().Model = VegetationModel2;
            //Entity.GetOrCreate<ModelComponent>().Enabled = false;
        }

        public bool InitializeInstanceBuffersPosCol(GraphicsDevice GraphicsDevice)
        {
            try
            {
                // Set the number of instances in the array.
                List<Stride.Core.Mathematics.Int2> locs = Utility.GetTreeLocations(
                    TerrainEditorView.TerrainPropertiesTreeLocs.Width,
                    TerrainEditorView.TerrainPropertiesTreeLocs.Height,
                    100, TerrainEditorView.Repulsion_Distance);
                InstanceCount = locs.Count;
                TerrainEditorView.CurrentTreeInstances= InstanceCount;// tcomp.Heightmap.Size.X * tcomp.Heightmap.Size.Y;// 1024 *1024;
                if (InstanceCount <= 0) return false;
                Vector3[] instancespos = tcomp.GetPositions();
                Vector4[] instancescol = new Vector4[InstanceCount];
                int[] instancesType = new int[InstanceCount];
                Vector3[] instancesScale = new Vector3[InstanceCount];
                Vector3[] instancesRotation = new Vector3[InstanceCount];
                for (int i = 0; i < InstanceCount; i++)
                {
                    instancespos[i] = new Vector3(Utility.Runif(-50, 50),
                       Utility.Runif(-50, 50), Utility.Runif(-50, 50));
                    instancescol[i] = new Vector4(1, 1, 1, 1);//
                       //   Vector4( Utility.Runif(), Utility.Runif(),Utility.Runif(), Utility.Runif());
                }
                InstanceLocations = Stride.Graphics.Buffer.Structured.New<Vector3>(
                     GraphicsDevice, instancespos);
                InstanceColors = Stride.Graphics.Buffer.Structured.New<Vector4>(
                     GraphicsDevice, instancescol);
             /*   InstanceType = Stride.Graphics.Buffer.Structured.New<int>(
                     GraphicsDevice, instancesType);
                InstanceScale = Stride.Graphics.Buffer.Structured.New<Vector3>(
                     GraphicsDevice, instancesScale);
                InstanceRotation = Stride.Graphics.Buffer.Structured.New<Vector3>(
                     GraphicsDevice, instancesRotation);*/
                return true;
            }
            catch
            {
                return false;
            }

        }
    }

}
