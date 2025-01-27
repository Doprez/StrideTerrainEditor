//by Idomeneas
using ImGui;
using HeightMapEditor;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Extensions;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Xml;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace TerrainEditor
{
    public class ObjectInstance
    {
        //add specific variables here based on type, like water variables
        //List<Object> objects;
        public Entity ObjectEntity { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public Vector3 Scale { get; set; }
        public Vector4 Hue { get; set; }
        public ObjectInstance(Entity objectEntity, Vector3 position, Vector3 rotation,
            Vector3 scale, Vector4 hue)
        {
            ObjectEntity = objectEntity;
            position.Y -= 0.5f; //decrease height a little
            Position = position;
            Rotation = rotation;
            Scale = scale;
            Hue = hue;
        }
    }
    public interface IAreaObject : IComparable<IAreaObject>
    {
        /// <summary>
        /// name to display in game
        /// </summary>
        string ObjectName { get; set; }
        /// <summary>
        /// Entity to be loaded and added to scene
        /// </summary>
        int NumInstances => objectInstances.Count;
        public AreaObjectType ObjType { get; set;}

        List<ObjectInstance> objectInstances { get; set; }
        /// <summary>
        /// remove from Area scene
        /// </summary>
        void Remove();
        void SetPosition(Vector3 vec,int index);
        void SetRotation(Vector3 vec, int index);
        void SetScale(Vector3 vec, int index);
        void SetHue(Vector4 vec, int index);

    }

    public class AreaObject : IAreaObject, IComparable, IComparable<AreaObject>
    {
        #region interface
        public string ObjectName { get; set; }
        public int NumInstances => objectInstances.Count;
        public AreaObjectType ObjType { get; set; }
        public List<ObjectInstance> objectInstances { get; set; }

        /// <summary>
        /// remove from Area scene
        /// </summary>
        public void Remove()
        {
            AreaXML.Remove(ObjectName);
        }
        public void SetPosition(Vector3 vec, int index) 
        { objectInstances[index].Position = vec; }
        public void SetRotation(Vector3 vec, int index) 
        { objectInstances[index].Rotation = vec; }
        public void SetScale(Vector3 vec, int index) 
        { objectInstances[index].Scale =  vec; }
        public void SetHue(Vector4 vec, int index)
        { objectInstances[index].Hue = vec; }

        public int CompareTo(IAreaObject other)
        {
            if (other == null)
            {
                return 1;
            }

            return String.Compare(ObjectName, other.ObjectName, StringComparison.Ordinal);
        }
        public int CompareTo(AreaObject other)
        {
            if (other == null)
            {
                return 1;
            }

            return String.Compare(ObjectName, other.ObjectName, StringComparison.Ordinal);
        }
        public int CompareTo(object obj)
        {
            if (obj is AreaObject)
            {
                return CompareTo((AreaObject)obj);
            }

            throw new ArgumentException();
        }
        #endregion interface

        #region specific object variables
        public Buffer<Vector3> InstanceLocations { get; set; }
        public Buffer<Vector3> InstanceRotations { get; set; }
        public Buffer<Vector3> InstanceScales { get; set; }
        public Buffer<Vector4> InstanceColors { get; set; }

        #endregion specific object variables

        public AreaObject(string objectname, Entity ObjectEntity1,
            Vector3 Position1, Vector3 Rotation1, Vector3 Scale1, Vector4 Hue1,
            AreaObjectType Type1)
        {
            ObjectName = objectname;
            foreach(IAreaObject a in  AreaXML.GetAreaObjects())
            { 
                //object exists add instance
                if(a.ObjectName==ObjectName)
                {
                    if (a.objectInstances == null) a.objectInstances = new List<ObjectInstance>();
                    a.objectInstances.Add(new ObjectInstance(
                        ObjectEntity1, Position1, Rotation1, Scale1, Hue1));
                    AreaXML.Remove(ObjectName);
                    AreaXML.Add(this);
                    objectInstances=a.objectInstances;
                    ObjType = Type1;
                    return;
                }
            }
            //new object coming in
            if (objectInstances == null) objectInstances = new List<ObjectInstance>();
            objectInstances.Add(new ObjectInstance(
                ObjectEntity1, Position1, Rotation1, Scale1, Hue1));
            ObjType = Type1;
            AreaXML.Add(this);
        }

        public AreaObject(XmlElement node, TerrainComponent tcomp)
        {
            ObjectName = Utility.GetText(node["ObjectName"], null);
            ObjType = AreaXML.GetObjectType(
                Utility.GetText(node["ObjType"], "0"));
            //  ObjectEntity = Utility.GetText(node["ObjectEntity"], null);
            //  bool.TryParse(Utility.GetText(node["IsInstanced"], null),out bool IsInstanced1);
            XmlElement Instances = node["Instances"];
            int Count = int.Parse(Instances.GetAttribute("Count"));
            objectInstances=new List<ObjectInstance>();
          //  CameraComponent Camera = tcomp.Game.Services.GetService<SceneSystem>().GraphicsCompositor.Cameras[0].Camera;
          //  Entity CameraMultiType = tcomp.Game.Services.GetService<SceneSystem>().SceneInstance
           //     .FirstOrDefault(a => a.Name == "CameraMultiType");
           // CameraComponent Camera = CameraMultiType.Get<CameraComponent>();

            foreach (XmlElement Instance in Instances.GetElementsByTagName("Instance"))
            {
                string EntityName = Utility.GetText(Instance["EntityName"], null);
                BoundingBox box = new BoundingBox(
                    Stride.Core.Mathematics.Vector3.Zero,
                    Stride.Core.Mathematics.Vector3.Zero);
                if (ObjType == AreaObjectType.Water)
                {
                    System.Numerics.Vector2 dims = Utility.GetVector2(
                        Instance["PlaneDimensions"],new
                        System.Numerics.Vector2(0,0));
                    box.Maximum.X = dims.X;
                    box.Maximum.Z = dims.Y;
                }

                Vector3 Position = Utility.GetVector3(Instance["Position"], Vector3.Zero);
                Vector3 Rotation = Utility.GetVector3(Instance["Rotation"], Vector3.Zero);
                Vector3 Scale = Utility.GetVector3(Instance["Scale"], Vector3.Zero);
                Vector4 Hue = Utility.GetVector4(Instance["Hue"], Vector4.Zero);
                // AreaXML.AddModel(ObjectName,Game);
                Model model=new Model();
                if(ObjType != AreaObjectType.Water)
                {
                    model = tcomp.Content.Load<Model>(ObjectName);//Models/Tree1/Tree
                    if (model == null) throw new ArgumentException("Model does not exist!");
                    // Create a new entity to add to the scene
                    Entity entity = new Entity("Water, Entity Added by Script")
                        { new ModelComponent { Model = model } };
                    // Add a new entity to the scene
                    tcomp.SceneSystem.SceneInstance.RootScene.Entities.Add(entity);
                    entity.Transform.Position = Position.AsStrideVec3();
                    objectInstances.Add(new
                        ObjectInstance(entity, Position, Rotation, Scale, Hue));
                }
                else
                {                   
                    WaterScript wt = new WaterScript();//entity.Add(wt);
                    wt.WaterTransparency = TerrainEditorView.WaterTransparency;
                    wt.DisplacementSpeed = TerrainEditorView.DisplacementSpeed;
                    wt.DisplacementAmplitude = TerrainEditorView.DisplacementAmplitude;
                    wt.Setup(tcomp.Game.Services.GetService<SceneSystem>());
                    var meshDraw = //wt.BuildWaterSurface(GraphicsDevice, 1, 1,
                                   //  WaterWideX, WaterHighZ, true).ToMeshDraw();
                        GeometricPrimitive.Plane.New(tcomp.GraphicsDevice,
                 box.Maximum.X, box.Maximum.Z, normalDirection: NormalDirection.UpY,
                 generateBackFace: true).ToMeshDraw();
                    Mesh mesh =
                        new Mesh
                        {
                            Draw = meshDraw,
                            BoundingBox = box
                        };
                    //      entity.GetOrCreate<ModelComponent>().RenderGroup = RenderGroup.Group0;
                    model.Meshes.Add(mesh);
                    if (model.Materials != null)
                        model.Materials.Clear();
                    //load water material
                    Material material = tcomp.Content.Load<Material>(
                         //"Materials/Other/Sphere Material"
                         "Water/WaterMaterialShader"
                        //"Water/WaterMaterial"//loading this does not work at runtime, only if you place a plane in the editor and attach the material...
                        );
                    model.Materials.Add(material);
                    var normalPhase = wt.NormalTextureFlow.CurrentPhase;
                    var diffusePhase = wt.DiffuseTextureFlow.CurrentPhase;

                    material.Passes[0].Parameters.Set(TransformationKeys.WorldViewProjection, wt.World * wt.Camera.ViewProjectionMatrix);
                    material.Passes[0].Parameters.Set(TransformationKeys.World, wt.World);
                    material.Passes[0].Parameters.Set(GlobalKeys.Time, (float)tcomp.Game.UpdateTime.Elapsed.TotalSeconds);
                    material.Passes[0].Parameters.Set(GlobalKeys.TimeStep, (float)tcomp.Game.UpdateTime.Elapsed.TotalSeconds);
                    material.Passes[0].Parameters.Set(WaterShaderKeys.SkyTexture, TerrainEditorView.SkyTexture);
                    material.Passes[0].Parameters.Set(WaterShaderKeys.WaterFloorTexture, TerrainEditorView.WaterFloorTexture);
                    material.Passes[0].Parameters.Set(WaterShaderKeys.NoiseTexture, TerrainEditorView.NoiseMapTexture);
                    material.Passes[0].Parameters.Set(WaterShaderKeys.FlowMapTexture, TerrainEditorView.FlowMapTexture);
                    material.Passes[0].Parameters.Set(WaterShaderKeys.DiffuseTexture0, TerrainEditorView.DiffuseTexture1);
                    material.Passes[0].Parameters.Set(WaterShaderKeys.DiffuseTexture1, TerrainEditorView.DiffuseTexture2);
                    material.Passes[0].Parameters.Set(WaterShaderKeys.NormalTexture0, TerrainEditorView.NormalTexture1);
                    material.Passes[0].Parameters.Set(WaterShaderKeys.NormalTexture1, TerrainEditorView.NormalTexture2);
                    material.Passes[0].Parameters.Set(WaterShaderKeys.SunColor, wt.SunColor);
                    material.Passes[0].Parameters.Set(WaterShaderKeys.CameraPosition, wt.Camera.Entity.Transform.Position);
                    material.Passes[0].Parameters.Set(WaterShaderKeys.DirectionToLight, Stride.Core.Mathematics.Vector3.Normalize(new Stride.Core.Mathematics.Vector3(2f, 2f, 4f)));
                    material.Passes[0].Parameters.Set(WaterShaderKeys.WaterTransparency, wt.WaterTransparency);
                    material.Passes[0].Parameters.Set(WaterShaderKeys.SkyTextureOffset, wt.SkyTextureOffset);
                    material.Passes[0].Parameters.Set(WaterShaderKeys.NormalOffsets, wt.NormalTextureFlow.RandomOffsets);
                    material.Passes[0].Parameters.Set(WaterShaderKeys.DiffuseOffsets, wt.DiffuseTextureFlow.RandomOffsets);
                    material.Passes[0].Parameters.Set(WaterShaderKeys.NormalPhase, new Stride.Core.Mathematics.Vector2((normalPhase + 0.5f) % 1, normalPhase));
                    material.Passes[0].Parameters.Set(WaterShaderKeys.DiffusePhase, new Stride.Core.Mathematics.Vector2((normalPhase + 0.5f) % 1, normalPhase));
                    material.Passes[0].Parameters.Set(WaterShaderKeys.NormalPulseReduction, wt.NormalTextureFlow.PulseReduction);
                    material.Passes[0].Parameters.Set(WaterShaderKeys.DiffusePulseReduction, wt.DiffuseTextureFlow.PulseReduction);
                    material.Passes[0].Parameters.Set(WaterShaderKeys.TextureScale, wt.TextureScale);
                    material.Passes[0].Parameters.Set(WaterShaderKeys.DisplacementSpeed, wt.DisplacementSpeed);
                    material.Passes[0].Parameters.Set(WaterShaderKeys.DisplacementAmplitude, wt.DisplacementAmplitude);
                    material.Passes[0].Parameters.Set(WaterShaderKeys.UseCaustics, wt.UseCaustics ? 1 : 0);
                    Entity entity = new Entity("WaterPlane")
                         { new ModelComponent { Model = model } };
                    tcomp.SceneSystem.SceneInstance.RootScene.Entities.Add(entity);
                    entity.Transform.Position = Position.AsStrideVec3();
                    entity.Add(wt);
                    objectInstances.Add(new
                        ObjectInstance(entity, Position, Rotation, Scale, Hue));
                }
            }
            AreaXML.Add(this);
        }

        public void Save(XmlTextWriter xml)
        {
            xml.WriteStartElement("Object");

            xml.WriteStartElement("ObjectName");//URL
            xml.WriteString(ObjectName);
            xml.WriteEndElement();

            xml.WriteStartElement("ObjType");
            xml.WriteString(ObjType.ToString());
            xml.WriteEndElement();
            
            xml.WriteStartElement("Instances");
            xml.WriteAttributeString("Count", objectInstances.Count.ToString());
            {
                foreach (ObjectInstance obj in objectInstances)
                {
                    xml.WriteStartElement("Instance");

                    xml.WriteStartElement("EntityName");
                    xml.WriteString(obj.ObjectEntity.Name.ToString());
                    xml.WriteEndElement();

                    if(ObjType== AreaObjectType.Water)
                    {//save the water plane dimensions to have on load
                        xml.WriteStartElement("PlaneDimensions");
                        BoundingBox box = obj.ObjectEntity.Get<ModelComponent>().
                            Model.Meshes[0].BoundingBox;
                        System.Numerics.Vector2 dims = new System.Numerics.Vector2(box.Maximum.X,box.Maximum.Z);
                        string st1 = dims.X.ToString()+","+ dims.Y.ToString();
                        xml.WriteString(st1);
                        xml.WriteEndElement();

                    }

                    xml.WriteStartElement("Position");
                    string st = obj.Position.ToString().Replace("<", string.Empty).
                        Replace(">", string.Empty);
                    xml.WriteString(st);
                    xml.WriteEndElement();

                    xml.WriteStartElement("Rotation");
                    st = obj.Rotation.ToString().Replace("<", string.Empty).
                          Replace(">", string.Empty);
                    xml.WriteString(st);
                    xml.WriteEndElement();

                    xml.WriteStartElement("Scale");
                    st = obj.Scale.ToString().Replace("<", string.Empty).
                     Replace(">", string.Empty);
                    xml.WriteString(st);
                    xml.WriteEndElement();

                    xml.WriteStartElement("Hue");
                    st = obj.Hue.ToString().Replace("<", string.Empty).
                     Replace(">", string.Empty);
                    xml.WriteString(st);
                    xml.WriteEndElement();

                    xml.WriteEndElement();//end instance

                }
            }
            xml.WriteEndElement();//end all instances

            xml.WriteEndElement();
        }
    }

    /// <summary>
    /// The asset name must contain the type
    /// so if it contains "Tree" it is given a tree type
    /// </summary>
    public enum AreaObjectType
    {
        None,
        Mannequin,
        Mobile,//creatures and npcs
        Tree,
        Grass,
        Rock,
        Wall,
        Water,
        Door,
        Floor,
        Roof,
        Prefab,
        AreaEntry,
        AreaExit,
        AreaTrigger,
    }
    
    public class AreaXML
    {
        private static Dictionary<string, IAreaObject> m_Objects = new Dictionary<string, IAreaObject>();
        public static int Count => m_Objects.Count;
        public static Dictionary<string, IAreaObject> GetDictAreaObjects()
        {
            return m_Objects;
        }
        public static ICollection<IAreaObject> GetAreaObjects()
        {
            return m_Objects.Values;
        }

        public static IAreaObject GetAreaObject(string username)
        {
            IAreaObject a;

            m_Objects.TryGetValue(username, out a);

            return a;
        }

        public static void Add(IAreaObject a)
        {
            m_Objects.Add(a.ObjectName, a);
        }

        /// <summary>
        /// Removes the whole object and its instances
        /// </summary>
        /// <param name="name"></param>
        public static void Remove(string name)
        {
            m_Objects.Remove(name);
        }


        /// <summary>
        /// Adds the specific prefab as an object. 
        /// prefabs and models must be included in the build or an exemption is thrown
        /// </summary>
        public static void AddPrefab(string URL, TerrainComponent tcomp)
        {
            //Content.Load looks into the assets directory root always
            //Load a model 
            var Prefab = tcomp.Content.Load<Prefab>(URL);//Models/Tree1/Tree
            if (Prefab == null) 
                throw new ArgumentException("Prefab does not exist! Make sure it is included in build in stride studio (blue dot)!");
            // Instantiate a prefab
            var instance = Prefab.Instantiate();
            var Prefab0 = instance[0];
            // Change the X coordinate
           // Prefab0.Transform.Position.X = 20.0f;           
            Prefab0.Transform.Position = MultiTypeCameraController.ClickBallModelEntity.Transform.Position;
            tcomp.SceneSystem.SceneInstance.RootScene.Entities.Add(Prefab0);
            new AreaObject(URL, Prefab0,
                Prefab0.Transform.Position.AsNumericVec3(), Vector3.Zero, Vector3.One,
                Vector4.One,AreaObjectType.Prefab);
        }

        /// <summary>
        /// Adds the specific model as an object
        /// </summary>
        public static void AddModel(string URL, TerrainComponent tcomp)
        {
            //Content.Load looks into the assets directory root always
            //Load a model 
            var model = tcomp.Content.Load<Model>(URL);//Models/Tree1/Tree
            if (model == null) throw new ArgumentException("Model does not exist! Make sure it is included in build in stride studio (blue dot)!");
            // Create a new entity to add to the scene
            Entity entity = new Entity(GetObjectTypeString(GetObjectType(URL)) + ", Entity Added by Script")
               { new ModelComponent { Model = model } };
            // Add a new entity to the scene
            tcomp.SceneSystem.SceneInstance.RootScene.Entities.Add(entity);
            entity.Transform.Position = MultiTypeCameraController.ClickBallModelEntity.Transform.Position;
            
            new AreaObject(URL, entity, 
                entity.Transform.Position.AsNumericVec3(), Vector3.Zero, Vector3.One,
                Vector4.One,GetObjectType(URL));
        }

        public static void AddModel(Model model,Vector3 Pos,
            TerrainComponent tcomp)
        {
            if (model == null) throw new ArgumentException("Model does not exist! Make sure it is included in build in stride studio (blue dot)!");
            // Create a new entity to add to the scene
            tcomp.Content.TryGetAssetUrl(model, out string URL);
            Entity entity = new Entity(GetObjectTypeString(GetObjectType(URL)) +", Entity Added by Script")
               { new ModelComponent { Model = model } };
            // Add a new entity to the scene
            tcomp.SceneSystem.SceneInstance.RootScene.Entities.Add(entity);
            entity.Transform.Position = Pos.AsStrideVec3();
            new AreaObject(URL, entity, Pos, Vector3.Zero, Vector3.One,
                Vector4.One, GetObjectType(URL));
        }

        public static Vector3 GetRandomRotationY()
        {
            return new Vector3(0,Utility.Runif(0,2*3.14f),0);
        }
        public static Vector3 GetRandomScale()
        {
            return new Vector3(Utility.Runif(.7f, 1.5f),
                Utility.Runif(.7f, 2.1f), Utility.Runif(.7f, 1.5f));
        }
        public static void AddVegetationModel(Model model, 
            Vector3 Pos, TerrainComponent tcomp)
        {
            if (model == null) throw new ArgumentException("Model does not exist! Make sure it is included in build in stride studio (blue dot)!");
            Entity entity = new Entity("Vegetation, Entity Added by Script")
               { new ModelComponent { Model = model } };
            entity.Transform.Scale = GetRandomScale().AsStrideVec3();
            entity.Transform.RotationEulerXYZ = GetRandomRotationY().AsStrideVec3();
            tcomp.SceneSystem.SceneInstance.RootScene.Entities.Add(entity);
            entity.Transform.Position = Pos.AsStrideVec3();
            tcomp.Content.TryGetAssetUrl(model, out string URL);
            new AreaObject(URL, entity, Pos, Vector3.Zero, Vector3.One,
                Vector4.One, GetObjectType(URL));
        }

        public static AreaObjectType GetObjectType(string URL)
        {
            AreaObjectType type = AreaObjectType.None;
            if(URL.ToLower().Contains("tree"))
                return AreaObjectType.Tree;
            else
            if (URL.ToLower().Contains("mannequin"))
                return AreaObjectType.Mannequin;
            else 
            if (URL.ToLower().Contains("mobile"))
                return AreaObjectType.Mobile;
            else
            if (URL.ToLower().Contains("grass"))
                return AreaObjectType.Grass;
            else
            if (URL.ToLower().Contains("rock"))
                return AreaObjectType.Rock;
            else
            if (URL.ToLower().Contains("wall"))
                return AreaObjectType.Wall;
            else
            if (URL.ToLower().Contains("water"))
                return AreaObjectType.Water;
            else
            if (URL.ToLower().Contains("door"))
                return AreaObjectType.Door;
            else
            if (URL.ToLower().Contains("floor"))
                return AreaObjectType.Floor;
            else
            if (URL.ToLower().Contains("roof"))
                return AreaObjectType.Roof;
            else
            if (URL.ToLower().Contains("prefab"))
                return AreaObjectType.Prefab;
            else
            if (URL.ToLower().Contains("areaentry"))
                return AreaObjectType.AreaEntry;
            else
            if (URL.ToLower().Contains("areaexit"))
                return AreaObjectType.AreaExit;
            else
            if (URL.ToLower().Contains("areatrigger"))
                return AreaObjectType.AreaTrigger;
            return type;
        }

        public static string GetObjectTypeString(AreaObjectType ObjType)
        {
            string ret = "none";
            switch(ObjType)
            {
                case AreaObjectType.Mannequin:
                    ret = "Mannequin";
                    break;
                case AreaObjectType.Grass:
                    ret = "Grass";
                    break;
                case AreaObjectType.Door:
                    ret = "Door";
                    break;
                case AreaObjectType.Roof:
                    ret = "Roof";
                    break;
                case AreaObjectType.Mobile:
                    ret = "Mobile";
                    break;
                case AreaObjectType.Prefab:
                    ret = "Prefab";
                    break;
                case AreaObjectType.AreaEntry:
                    ret = "AreaEntry";
                    break;
                case AreaObjectType.AreaExit:
                    ret = "AreaExit";
                    break;
                case AreaObjectType.AreaTrigger:
                    ret = "AreaTrigger";
                    break;
                case AreaObjectType.Rock:
                    ret = "Rock";
                    break;
                case AreaObjectType.Floor:
                    ret = "Floor";
                    break;
                case AreaObjectType.Tree:
                    ret = "Tree";
                    break;
                case AreaObjectType.Wall:
                    ret = "Wall";
                    break;
                case AreaObjectType.Water:
                    ret = "Water";
                    break;
            }
            return ret;
        }
        public static void RemoveModel(string ObjectName,
            TerrainComponent tcomp,int index)
        {
            tcomp.SceneSystem.SceneInstance.RootScene.Entities.Remove(
                m_Objects[ObjectName].objectInstances[index].ObjectEntity);
            m_Objects[ObjectName].objectInstances.RemoveAt(index);
            if (m_Objects[ObjectName].objectInstances.Count==0)
                Remove(ObjectName);
        }

        public static void LoadArea(TerrainComponent tcomp)
        {
            string dir = Utility.Resources_TerrainEditorAreas_Directory +
                 AreaName;
            foreach (IAreaObject obj in AreaXML.GetAreaObjects())
            {
                for (int i = obj.NumInstances - 1; i >= 0; i--)
                {
                    AreaXML.RemoveModel(obj.ObjectName, tcomp, i);
                }
            }
            string filePath = dir + "\\" + AreaName + ".xml";
            AreaHandleModels.ResetPoints();
            m_Objects = new Dictionary<string, IAreaObject>(150, StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(filePath))
            {
                MessageBox.Show("filePath could not be found...", "ERROR Loading Area XML file");
                return;
            }
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(filePath);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "AREA LOAD ERROR");
                return;
            }
            XmlElement root = doc["AreaObjects"];
            AreaXML.AreaName = root.GetAttribute("AreaName");

            //read terrain variables
            XmlElement terrain = root["Terrain"];

            //this corresponds to the ingame heightmap asset
            //can change to load directly a texture from disk
            //run this after 2 seconds, let the tcomp fill in because the first time it throws an error
            filePath = dir + "\\";// + AreaName;
            LoadAllTextures(filePath, tcomp);

            //these variables arent really used, can use them when you 
            //have the assets in game studio and load from there
            TerrainHeightMap = Utility.GetText(terrain["TerrainHeightMap"], null);
            TerrainBlendedTexture = Utility.GetText(terrain["TerrainBlendedTexture"], null);

            tcomp.m_QuadSideWidthX=float.Parse(
                Utility.GetText(terrain["m_QuadSideWidthX"],"32"));
            TerrainEditorView.quadlenx = tcomp.m_QuadSideWidthX;
            tcomp.m_QuadSideWidthZ = float.Parse(Utility.GetText
                (terrain["m_QuadSideWidthZ"],"32"));
            TerrainEditorView.quadlenz = tcomp.m_QuadSideWidthZ;

            //load textures for heightmap and terrain mesh
            //    texture = tcomp.Content.Load<Texture>(TerrainBlendedTexture);
            //   TerrainEditorView.TerrainBlendedTexture = texture;
            //   TerrainEditorView.TerrainBlendedTextureIntPtr = ImGuiSystem.BindTexture(TerrainEditorView.TerrainBlendedTexture);

            //read all objects in the scene
            foreach (XmlElement obj in root.GetElementsByTagName("Object"))
            {
                try
                {
                    AreaObject aobj = new AreaObject(obj, tcomp);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message,"AREA LOAD ERROR");
                }
            }
            AreaXML.UpdateObjectLocations(tcomp);
            TerrainEditorView.CurrentTreeInstances = CountObjectType(AreaObjectType.Tree);
            TerrainEditorView.CurrentGrassInstances = CountObjectType(AreaObjectType.Grass);
        }

        public static int CountObjectType(AreaObjectType type)
        { 
            int count = 0;
            foreach (IAreaObject obj in AreaXML.GetAreaObjects())
            {
                if (obj.ObjType != type) continue;
                count+=obj.NumInstances;
            }
            return count; 
        }
        public static void UpdateObjectLocations(TerrainComponent tcomp,
            float offset=-0.5f)
        {
            foreach (IAreaObject obj in AreaXML.GetAreaObjects())
            {
                for (int i = 0; i < obj.NumInstances; i++)
                {
                    obj.objectInstances[i].Position =
                    new Vector3(obj.objectInstances[i].Position.X,
                    //obj.objectInstances[i].Position.Y,
                    tcomp.GetCPUHeightAt(
                        obj.objectInstances[i].Position.X,
                        obj.objectInstances[i].Position.Z)+ offset,
                    obj.objectInstances[i].Position.Z);
                    obj.objectInstances[i].ObjectEntity.Transform.Position =
                        obj.objectInstances[i].Position.AsStrideVec3();
                }
            }
        }

        public static string AreaName="Area0001";
        #region terrain variables
        public static string TerrainHeightMap;
        public static string TerrainBlendedTexture;
        #endregion terrain variables

        public static void LoadAllTextures(string Path, TerrainComponent tcomp)
        {
            string old_dir= System.IO.Directory.GetCurrentDirectory();
            System.IO.Directory.SetCurrentDirectory(Path);
            Texture texture = Utility.LoadTex(AreaName+"HeightMap.bmp", tcomp.GraphicsDevice, tcomp.Game.GraphicsContext,false);
            if (texture == null)
            {
                texture = PerlinNoise.MakeFlat(TerrainEditorView.m_Width,
                    TerrainEditorView.m_Height,
                    TerrainEditorView.TargetHeightValue.AsStrideColor()).ToTexture(
                     TerrainEditorView.m_Width, TerrainEditorView.m_Height, tcomp.Game.GraphicsDevice,
                     tcomp.Game.GraphicsContext.CommandList);
            }
            else//check if 
            {
                if (texture.CheckGrayScale(tcomp.Game.GraphicsContext))
                    PerlinNoise.IsGrayScaleHeightMap = true;
                else
                    PerlinNoise.IsGrayScaleHeightMap = false;
            }
            //           Texture texture = tcomp.Content.Load<Texture>(TerrainHeightMap);
            int Width = texture.Width, Height = texture.Height;
            ImGuiSystem.UpdateTexture(TerrainEditorView.TerrainHeightMapTextureIntPtr, texture);
            tcomp.Width = Width;
            tcomp.Height = Height;

            texture = Utility.LoadTex(AreaName + "Weights1.bmp", tcomp.GraphicsDevice, tcomp.Game.GraphicsContext, false);
            if (texture == null)
            {
                texture = Utility.FlatTex(Width, Height, new Color(255, 0, 0, 0),
                tcomp.GraphicsDevice, tcomp.Game.GraphicsContext);
            }
            TerrainEditorView.TerrainWeights1 = texture;
            ImGuiSystem.UpdateTexture(TerrainEditorView.TerrainWeights1IntPtr,texture);

            texture = Utility.LoadTex(AreaName + "Weights2.bmp", tcomp.GraphicsDevice, tcomp.Game.GraphicsContext, false);
            if (texture == null)
            {
                TerrainEditorView.TerrainWeights2 =
                    Utility.FlatTex(Width, Height, new Color(0, 0, 0, 0),
                tcomp.GraphicsDevice, tcomp.Game.GraphicsContext);
            }
            TerrainEditorView.TerrainWeights2 = texture;
            ImGuiSystem.UpdateTexture(TerrainEditorView.TerrainWeights2IntPtr, texture);

            for (int i = 1; i <= 8; i++)
            {
                texture = Utility.LoadTex(AreaName + "TerrainBlendTexture"+i+".bmp", tcomp.GraphicsDevice, tcomp.Game.GraphicsContext, false);
                if (texture == null)
                {
                    throw new Exception("TerrainBlendTexture"+i+": Bad texture file detected");
                }
                ImGuiSystem.UpdateTexture(TerrainEditorView.TerrainTexturesIntPtr[i-1], texture);
                texture = Utility.LoadTex(AreaName + "TerrainHeightTexture" + i + ".bmp", tcomp.GraphicsDevice, tcomp.Game.GraphicsContext, false);
                if (texture == null)
                {
                    throw new Exception("TerrainHeightTexture" + i + ": Bad texture file detected");
                }
                ImGuiSystem.UpdateTexture(TerrainEditorView.TerrainHeightBasedTexturesIntPtr[i - 1], texture);
            }
            System.IO.Directory.SetCurrentDirectory(old_dir);

        }

        public static void FlushAllTextures(string filePath, TerrainComponent tcomp)
        {
            TerrainEditorView.SaveTex(ImGuiSystem._loadedTextures[
                TerrainEditorView.TerrainHeightMapTextureIntPtr],
                 filePath + "HeightMap.bmp",
                tcomp.Game.GraphicsContext, false);
            TerrainEditorView.SaveTex(TerrainEditorView.TerrainWeights1,
                 filePath + "Weights1.bmp",
                tcomp.Game.GraphicsContext, false);
            TerrainEditorView.SaveTex(TerrainEditorView.TerrainWeights2,
                 filePath + "Weights2.bmp",
                tcomp.Game.GraphicsContext, false);
            TerrainEditorView.SaveTex(TerrainScript.GetTerrainTexture(1),
                 filePath + "TerrainBlendTexture1.bmp",
                tcomp.Game.GraphicsContext, false);
            TerrainEditorView.SaveTex(TerrainScript.GetTerrainTexture(2),
                 filePath + "TerrainBlendTexture2.bmp",
                tcomp.Game.GraphicsContext, false);
            TerrainEditorView.SaveTex(TerrainScript.GetTerrainTexture(3),
                 filePath + "TerrainBlendTexture3.bmp",
                tcomp.Game.GraphicsContext, false);
            TerrainEditorView.SaveTex(TerrainScript.GetTerrainTexture(4),
                 filePath + "TerrainBlendTexture4.bmp",
                tcomp.Game.GraphicsContext, false);
            TerrainEditorView.SaveTex(TerrainScript.GetTerrainTexture(5),
                 filePath + "TerrainBlendTexture5.bmp",
                tcomp.Game.GraphicsContext, false);
            TerrainEditorView.SaveTex(TerrainScript.GetTerrainTexture(6),
                 filePath + "TerrainBlendTexture6.bmp",
                tcomp.Game.GraphicsContext, false);
            TerrainEditorView.SaveTex(TerrainScript.GetTerrainTexture(7),
                 filePath + "TerrainBlendTexture7.bmp",
                tcomp.Game.GraphicsContext, false);
            TerrainEditorView.SaveTex(TerrainScript.GetTerrainTexture(8),
                 filePath + "TerrainBlendTexture8.bmp",
                tcomp.Game.GraphicsContext, false);
            TerrainEditorView.SaveTex(TerrainScript.GetHeightTerrainTexture(1),
                 filePath + "TerrainHeightTexture1.bmp",
                tcomp.Game.GraphicsContext, false);
            TerrainEditorView.SaveTex(TerrainScript.GetHeightTerrainTexture(2),
                 filePath + "TerrainHeightTexture2.bmp",
                tcomp.Game.GraphicsContext, false);
            TerrainEditorView.SaveTex(TerrainScript.GetHeightTerrainTexture(3),
                 filePath + "TerrainHeightTexture3.bmp",
                tcomp.Game.GraphicsContext, false);
            TerrainEditorView.SaveTex(TerrainScript.GetHeightTerrainTexture(4),
                 filePath + "TerrainHeightTexture4.bmp",
                tcomp.Game.GraphicsContext, false);
            TerrainEditorView.SaveTex(TerrainScript.GetHeightTerrainTexture(5),
                 filePath + "TerrainHeightTexture5.bmp",
                tcomp.Game.GraphicsContext, false);
            TerrainEditorView.SaveTex(TerrainScript.GetHeightTerrainTexture(6),
                 filePath + "TerrainHeightTexture6.bmp",
                tcomp.Game.GraphicsContext, false);
            TerrainEditorView.SaveTex(TerrainScript.GetHeightTerrainTexture(7),
                 filePath + "TerrainHeightTexture7.bmp",
                tcomp.Game.GraphicsContext, false);
            TerrainEditorView.SaveTex(TerrainScript.GetHeightTerrainTexture(8),
                 filePath + "TerrainHeightTexture8.bmp",
                tcomp.Game.GraphicsContext, false);

        }

        public static void SaveArea(TerrainComponent tcomp)//string filePath)
        {
            string dir = Utility.Resources_TerrainEditorAreas_Directory +
                "\\" + AreaName;
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            string filePath = dir+"\\"+AreaName +".xml";

            using (StreamWriter op = new StreamWriter(filePath))
            {
                XmlTextWriter xml = new XmlTextWriter(op);
                xml.Formatting = Formatting.Indented;
                xml.IndentChar = '\t';
                xml.Indentation = 1;
                xml.WriteStartDocument(true);
                xml.WriteStartElement("AreaObjects");
                xml.WriteAttributeString("Count", m_Objects.Count.ToString());
                xml.WriteAttributeString("AreaName",AreaXML.AreaName);
                
                //Terrain Variables
                xml.WriteStartElement("Terrain");

                xml.WriteStartElement("TerrainHeightMap");
                xml.WriteString(TerrainHeightMap);
                xml.WriteEndElement();
                //save the file to the disk
                filePath = dir + "\\" + AreaName;
                FlushAllTextures(filePath,tcomp);

                xml.WriteStartElement("TerrainBlendedTexture");
                xml.WriteString(TerrainBlendedTexture);
                xml.WriteEndElement();

                xml.WriteStartElement("m_QuadSideWidthX");
                string st = tcomp.m_QuadSideWidthX.ToString();
                xml.WriteString(st);
                xml.WriteEndElement();

                xml.WriteStartElement("m_QuadSideWidthZ");
                st = tcomp.m_QuadSideWidthZ.ToString();
                xml.WriteString(st);
                xml.WriteEndElement();
                
                xml.WriteEndElement();

                //save all objects
                foreach (AreaObject a in GetAreaObjects())
                    a.Save(xml);
                xml.WriteEndElement();
                xml.Close();
            }
        }
    }
}
