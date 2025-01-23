//by Idomeneas
using HeightMapEditor;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using System;
using System.Collections.Generic;

namespace TerrainEditor
{
    public class AreaHandleModels
    {
        private static List<Vector3> Points = new List<Vector3>();

        public static void ResetPoints()
        {
            Points = new List<Vector3>();
        }
        public static void ToggleAreaModels(Vector3 pos)//CameraComponent camera)
        {
           // if (camera == null) return;
            foreach (IAreaObject obj in AreaXML.GetAreaObjects())
            {
                for (int i = 0; i < obj.NumInstances; i++)
                {
                    obj.objectInstances[i].ObjectEntity.Enable
                        <ActivableEntityComponent>(true);
                    //always show water planes by default
                    if (obj.ObjType == AreaObjectType.Water)
                    {
                        WaterScript wt=obj.objectInstances[i].ObjectEntity.Get<WaterScript>();
                        wt.Update();
                        continue;
                    }
                    if (Vector3.Distance(obj.objectInstances[i].Position.AsStrideVec3(),
                        pos//camera.Entity.Transform.Position
                        ) > TerrainEditorView.ObjectVisibility)
                    obj.objectInstances[i].ObjectEntity.
                        Enable<ActivableEntityComponent>(false);                     
                }
            }
        }

        public static void ToggleAreaModels(AreaObjectType type,bool val)
        {
            foreach (IAreaObject obj in AreaXML.GetAreaObjects())
            {
                if (obj.ObjType != type) continue;
                for (int i = 0; i < obj.NumInstances; i++)
                {
                    obj.objectInstances[i].ObjectEntity.
                        Enable<ActivableEntityComponent>(val);
                }
            }
        }
        public static void ToggleAreaModels(bool val)
        {
            foreach (IAreaObject obj in AreaXML.GetAreaObjects())
            {
                for (int i = 0; i < obj.NumInstances; i++)
                {
                    obj.objectInstances[i].ObjectEntity.
                        Enable<ActivableEntityComponent>(val);
                }
            }
        }

        public static void ProcessWater(TerrainComponent tcomp, MouseButton button,
            ClickResult clickResult)
        {
        }
        

        public static void ProcessTrees(TerrainComponent tcomp, 
            MouseButton button,ClickResult clickResult)
        {
           /* if (!TerrainEditorView.ShowTrees)
            {
                TerrainEditorView.ShowTrees = true;
                ToggleAreaModels(AreaObjectType.Tree, true);
            }*/
            if (button == MouseButton.Left)//increase trees
            {
                if (TerrainEditorView.CurrentTreeInstances >=
                    TerrainEditorView.MaxTreeInstances)
                {
                    TerrainEditorView.MSGlog.Add2Log("Maximum number of vegetation objects reached...");
                    return;
                }
                float radius = TerrainEditorView.Radius;
                int index,i, j,trees2add = TerrainEditorView.MaxTreeInstances -
                    TerrainEditorView.CurrentTreeInstances;
                int minx = (int)Math.Max(0, clickResult.WorldPosition.X - radius),
                    minz = (int)Math.Max(0, clickResult.WorldPosition.Z - radius),
                    maxx = (int)Math.Min(tcomp.Width, clickResult.WorldPosition.X + radius),
                    maxz = (int)Math.Min(tcomp.Height, clickResult.WorldPosition.Z + radius);
                for (i = minx; i < maxx; i++)
                {
                    for (j = minz; j < maxz; j++)
                    {
                        index = (tcomp.Width * j) + i;
                        float height = tcomp.GetCPUHeightAt(i, j);/// 3.1f;
                        Vector3 Point = new Vector3(tcomp.m_QuadSideWidthX * i, height,
                            tcomp.m_QuadSideWidthZ * j);
                        if (Vector3.Distance(clickResult.WorldPosition, Point
                            ) <= radius
                            && Utility.Runif() < TerrainEditorView.BallSelectionStrength)
                        {
                            float randx = Utility.Runif(clickResult.WorldPosition.X -
                                0.5f, clickResult.WorldPosition.X + 0.5f);
                            float randz = Utility.Runif(clickResult.WorldPosition.Z -
                                0.5f, clickResult.WorldPosition.Z + 0.5f);
                          //  Vector3 Point = clickResult.WorldPosition;// new Vector3(randx, height, randz);
                            if (TerrainEditorView.UseRepulsion)
                            {
                                bool valid = true;
                                for (int l = 0; l < Points.Count; l++)
                                {
                                    if (Vector3.Distance(Point, Points[l]) <=
                                        TerrainEditorView.Repulsion_Distance)
                                    {
                                        valid = false;
                                        break;
                                    }
                                }
                                if (valid) Points.Add(Point); else return;
                            }
                            int type = 0;
                            if (TerrainEditorView.TreeEditorPlaceRandom)
                                type = Utility.DUnif(0, TerrainEditorView.MaxTreeTypes - 1);
                            else
                                type = TerrainEditorView.TerrainTreeModeSelected;
                            TerrainEditorView.CurrentTreeInstances++;
                            AreaXML.AddVegetationModel(tcomp.AllTreeModels[type], Point.AsNumericVec3(), tcomp);
                            trees2add--;
                        }
                    }
                }
            }
            if (button == MouseButton.Right)//decrease trees
            {
                if (TerrainEditorView.CurrentTreeInstances == 0)
                {
                    TerrainEditorView.MSGlog.Add2Log("All trees removed...");
                    return;
                }
                float radius = TerrainEditorView.Radius;
                //find the closest tree object within radius and remove it
                foreach (IAreaObject obj in AreaXML.GetAreaObjects())
                {
                    for (int i = 0; i < obj.NumInstances; i++)
                    {
                        if (obj.ObjType != AreaObjectType.Tree) continue;
                        if (Vector3.Distance(
                            obj.objectInstances[i].Position.AsStrideVec3(),
                            clickResult.WorldPosition) <= radius)
                        {
                            for(int j=0;j<Points.Count;j++)
                            {
                                if (Vector3.Distance(
                                    obj.objectInstances[i].Position.AsStrideVec3(),
                                    Points[j]) <= 0.0001f)
                                {
                                    Points.RemoveAt(j);
                                    break;
                                }
                            }
                            AreaXML.RemoveModel(obj.ObjectName, tcomp, i);
                            TerrainEditorView.CurrentTreeInstances--;
                            return;
                        }
                    }
                }
            }
        }

        public static void ProcessGrass(TerrainComponent tcomp, MouseButton button,
    ClickResult clickResult)
        {
           /* if (!TerrainEditorView.ShowGrass)
            {
                TerrainEditorView.ShowGrass = true;
                ToggleAreaModels(AreaObjectType.Grass, true);
            }*/
            if (button == MouseButton.Left)//increase grass
            {
                if (TerrainEditorView.CurrentGrassInstances >=
                    TerrainEditorView.MaxGrassInstances)
                {
                    TerrainEditorView.MSGlog.Add2Log("Maximum number of grass objects reached...");
                    return;
                }
                float radius = TerrainEditorView.Radius;
                int index, i, j, grass2add = TerrainEditorView.MaxGrassInstances -
                    TerrainEditorView.CurrentGrassInstances;
                int minx = (int)Math.Max(0, clickResult.WorldPosition.X - radius),
                    minz = (int)Math.Max(0, clickResult.WorldPosition.Z - radius),
                    maxx = (int)Math.Min(tcomp.Width, clickResult.WorldPosition.X + radius),
                    maxz = (int)Math.Min(tcomp.Height, clickResult.WorldPosition.Z + radius);
                for (i = minx; i < maxx; i++)
                {
                    for (j = minz; j < maxz; j++)
                    {
                        index = (tcomp.Width * j) + i;
                        float height = tcomp.GetCPUHeightAt(i, j);/// 3.1f;
                        Vector3 Point = new Vector3(tcomp.m_QuadSideWidthX * i, height,
                            tcomp.m_QuadSideWidthZ * j);
                        if (Vector3.Distance(clickResult.WorldPosition, Point
                            ) <= radius
                            && Utility.Runif() < TerrainEditorView.BallSelectionStrength)
                        {
                            float randx = Utility.Runif(clickResult.WorldPosition.X -
                                0.5f, clickResult.WorldPosition.X + 0.5f);
                            float randz = Utility.Runif(clickResult.WorldPosition.Z -
                                0.5f, clickResult.WorldPosition.Z + 0.5f);
                            //  Vector3 Point = clickResult.WorldPosition;// new Vector3(randx, height, randz);
                            if (TerrainEditorView.UseRepulsion)
                            {
                                bool valid = true;
                                for (int l = 0; l < Points.Count; l++)
                                {
                                    if (Vector3.Distance(Point, Points[l]) <=
                                        TerrainEditorView.Repulsion_Distance)
                                    {
                                        valid = false;
                                        break;
                                    }
                                }
                                if (valid) Points.Add(Point); else return;
                            }
                            int type = 0;
                            if (TerrainEditorView.GrassEditorPlaceRandom)
                                type = Utility.DUnif(0, TerrainEditorView.MaxGrassTypes - 1);
                            else
                                type = TerrainEditorView.TerrainGrassModeSelected;
                            TerrainEditorView.CurrentGrassInstances++;
                            AreaXML.AddVegetationModel(tcomp.AllGrassModels[type], 
                                Point.AsNumericVec3(), tcomp);
                            grass2add--;
                        }
                    }
                }
            }
            if (button == MouseButton.Right)//decrease grass
            {
                if (TerrainEditorView.CurrentGrassInstances == 0)
                {
                    TerrainEditorView.MSGlog.Add2Log("All grass removed...");
                    return;
                }
                float radius = TerrainEditorView.Radius;
                //find the closest grass object within radius and remove it
                foreach (IAreaObject obj in AreaXML.GetAreaObjects())
                {
                    for (int i = 0; i < obj.NumInstances; i++)
                    {
                        if(obj.ObjType!=AreaObjectType.Grass) continue;
                        if (Vector3.Distance(
                            obj.objectInstances[i].Position.AsStrideVec3(),
                            clickResult.WorldPosition) <= radius)
                        {
                            for (int j = 0; j < Points.Count; j++)
                            {
                                if (Vector3.Distance(
                                    obj.objectInstances[i].Position.AsStrideVec3(),
                                    Points[j]) <= 0.0001f)
                                {
                                    Points.RemoveAt(j);
                                    break;
                                }
                            }
                            AreaXML.RemoveModel(obj.ObjectName, tcomp, i);
                            TerrainEditorView.CurrentGrassInstances--;
                            return;
                        }
                    }
                }
            }
        }

    }
}
