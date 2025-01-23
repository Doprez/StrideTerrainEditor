//by Idomeneas
using HeightMapEditor;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using System.Linq;

namespace TerrainEditor
{
    public struct InstanceTypePosCol
    {
        public Vector3 position;
        public Vector4 color;
        public int index;
    };

    public class CubeInstancingRenderScript : SyncScript
    {
        [Display(Browsable = true)]
        [DataMember(10)]
        public bool Enabled = false;

        [Display(Browsable = false)]
        [DataMember(20)]
        public int InstanceCount { get; set; }

        [Display(Browsable = false)]
        [DataMember(30)]
        public Buffer<Vector3> InstanceLocations;

        [Display(Browsable = false)]
        [DataMember(40)]
        public Buffer<Vector4> InstanceColors;

        public static int UpdatingMode =-1;
        public override void Start()
        {
            if (!Enabled) return;

            Entity Terrain_Entity =
                SceneSystem.SceneInstance.FirstOrDefault(e => e.Name == "TerrainComponent");
            TerrainComponent tcomp = Terrain_Entity.Get<TerrainComponent>();
            InitializeInstanceBuffersPosCol(tcomp,GraphicsDevice);
        }

        public override void Update()
        {
            if (!Enabled) return;
            if (UpdatingMode == -1) return;
            Entity Terrain_Entity =
                SceneSystem.SceneInstance.FirstOrDefault(e => e.Name == "TerrainComponent");
            TerrainComponent tcomp = Terrain_Entity.Get<TerrainComponent>();
            UpdateCubePointInstanceBuffers(tcomp, GraphicsDevice);

        }

        public bool InitializeInstanceBuffersPosCol(
            TerrainComponent tcomp,GraphicsDevice GraphicsDevice)
        {
            // Set the number of instances in the array.
            InstanceCount = tcomp.Width * tcomp.Height;// 1024 *1024;
            Vector3[] instancespos = tcomp.GetPositions();//.Heightmap.ToWorldPoints(tcomp.m_QuadSideWidthX, tcomp.m_QuadSideWidthZ);// new Vector3[InstanceCount];
            Vector4[] instancescol = new Vector4[InstanceCount];
            for (int i = 0; i < InstanceCount; i++)
            {
                //        instancespos[i] = new Vector3(Utility.Runif(-120, 120),
                //             Utility.Runif(-120, 120), Utility.Runif(-120, 120));
                instancescol[i] = new Vector4(1, 0, 0, 1);// Utility.Runif(), Utility.Runif(),Utility.Runif(), Utility.Runif());
            }
            InstanceLocations = Stride.Graphics.Buffer.Structured.New<Vector3>(
                 GraphicsDevice, instancespos);
            InstanceColors = Stride.Graphics.Buffer.Structured.New<Vector4>(
                 GraphicsDevice, instancescol);
            return true;
        }

        public bool UpdateCubePointInstanceBuffers(
            TerrainComponent tcomp, GraphicsDevice GraphicsDevice)
        {
            // Set the number of instances in the array.
            int InstanceCount = tcomp.Width * tcomp.Height;// 1024 *1024;

            if (UpdatingMode == 0 || UpdatingMode == 2)
            {
                Vector3[] instancespos = tcomp.GetPositions();
                //.Heightmap.ToWorldPoints(tcomp.m_QuadSideWidthX, tcomp.m_QuadSideWidthZ);// new Vector3[InstanceCount];
                InstanceLocations = Stride.Graphics.Buffer.Structured.New<Vector3>(GraphicsDevice, instancespos);
            }
            if (UpdatingMode == 1 || UpdatingMode == 2)
            {
                Vector4[] instancescol = new Vector4[InstanceCount];
                for (int i = 0; i < InstanceCount; i++)
                {
                    //        instancespos[i] = new Vector3(Utility.Runif(-120, 120),
                    //             Utility.Runif(-120, 120), Utility.Runif(-120, 120));
                    instancescol[i] = new Vector4(1, 0, 0, 1);
                    if (TerrainScript.SelectedPoints[i])
                        instancescol[i] = new Vector4(0, 1, 0, 1);// Utility.Runif(), Utility.Runif(),Utility.Runif(), Utility.Runif());
                }
                InstanceColors = Stride.Graphics.Buffer.Structured.New<Vector4>(GraphicsDevice, instancescol);
            }
            UpdatingMode = -1;
            return true;
        }

    }

}
