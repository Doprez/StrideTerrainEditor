//by Idomeneas
using HeightMapEditor;
using ImGui;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Color = Stride.Core.Mathematics.Color;

namespace TerrainEditor
{
    //basic tile defs
    public enum TileType
    {
        TILE_TYPE_DEAD, TILE_TYPE_DESERT, TILE_TYPE_GRASSLAND, TILE_TYPE_PLAIN,
        TILE_TYPE_SNOW, TILE_TYPE_SWAMP, TILE_TYPE_TOWN, TILE_TYPE_MARSH,
        TILE_TYPE_ARCTIC, TILE_TYPE_TUNDRA, TILE_TYPE_GLACIER, TILE_TYPE_FOREST,
        TILE_TYPE_JUNGLE,
        //sea
        TILE_TYPE_BEACH, TILE_TYPE_WATER_TRENCH, TILE_TYPE_WATER_SHALLOW,
        TILE_TYPE_WATER_DEEP, TILE_TYPE_WATER_VOLCANO, TILE_TYPE_WATER_RIFT,
        //hills
        TILE_TYPE_HILL,//grassland by default
        TILE_TYPE_JUNGLE_HILL, TILE_TYPE_FOREST_HILL, TILE_TYPE_SNOW_HILL,
        TILE_TYPE_BROWN_HILL, TILE_TYPE_DESERT_HILL,
        //mountains
        TILE_TYPE_MOUNTAIN, TILE_TYPE_BROWN_MOUNTAIN, TILE_TYPE_SNOW_MOUNTAIN,
        TILE_TYPE_VOLCANO_MOUNTAIN, TILE_TYPE_GLACIER_MOUNTAIN,
    }
    public enum TileTypeGoods
    {       
        NONE = 0,
        //mines
        TILE_MINE_GOLD, TILE_MINE_SILVER, TILE_MINE_COPPER, TILE_MINE_PLATINUM,
        TILE_MINE_PALLADIUM, TILE_MINE_COAL, TILE_MINE_PLUTONIUM,
        //gems
        TILE_GEM_AMBER, TILE_GEM_AMETHYST, TILE_GEM_EMERALD, TILE_GEM_JADE,
        TILE_GEM_JASPER, TILE_GEM_RUBY, TILE_GEM_SAPPHIRE, TILE_GEM_TURQUOISE,
        TILE_GEM_DIAMOND,
        //sea
        TILE_SEA_FISH, TILE_SEA_DOLPHIN, TILE_SEA_WHALE, TILE_SEA_SQUID,
        //special
        TILE_SPECIAL_OIL,
        MaxValue
    }

    /// <summary>
    /// Mountain = 0, Snow = 1, Swamp = 2, Desert = 3, Grassland = 4, Dirt = 5, Beach = 6, Water = 7
    /// </summary>
    public enum TileTypePrecedence
    {
        Mountain = 0, Snow = 1, Swamp = 2, Desert = 3,
        Grassland = 4, Dirt = 5, Beach = 6, Water = 7,
    }

    public class UniqueWorldTile
    {
        //generate or loaded from disk, yields low, medium, and high quality heightmap versions
        public Texture unique_tile_heightmap;//from disk
        public TileTypePrecedence unique_tile_Type_Precedence;
        public List<VertexTypePosTexNormColor> VertexCPUBuffer = new List<VertexTypePosTexNormColor>();
    };

    public class WorldTile
    {
        public Entity TileEntity;
        public Int2 Index_inWorld;
        public Vector3 WorldPosition;
        public bool ModelBuilt;
        public int Width;
        public int Height;
        public List<VertexTypePosTexNormColor> VertexCPUBuffer = new List<VertexTypePosTexNormColor>();
        //material shader to use when drawing this tile
        public Material MaterialShader;
        //the unique world tile assigned to this world tile
        public UniqueWorldTile UniqueWorldTile;
        //the unique rotation angle of the unique tile assigned to this world tile
        public float Random_RotY;
        public float Random_Shiftx, Random_Shiftz; //random shift in loc of the original mesh
        public float Random_ScaleY; //random scale along y-axis of the original mesh
        /// <summary>
        /// 0=land,1=water,2=beach
        /// </summary>
        public int type;//0=land,1=water,2=beach
        //Mountain=0>Snow=1>Swamp=2>Desert=3>Grassland=4>Dirt=5>beach=6>Water=7
        public TileTypePrecedence Type_Precedence;
        //0=no extra pass,1=top-left,2=top-right,3=bot-left,4=bot-right
        public int removable_terrain_property;//0=forest,1=jungle,2=swamp forest,3=snow forest,4=oasis, 5=ruins
        public int permanent_terrain_property;//all mutual exclusive
                                              //	int tile_transitions[8];//0=top-left,1=top,2=top-right,3=left,4=right,5=left-bot,6=bot,7=right-bot
                                              //stores the index_into_unique_tile_textures for all neighbors
                                              //mines 0=gold mine,1=silver mine,2=copper mine,3=Platinum,
                                              //4=Palladium,5=coal,6=plutonium
                                              //gems8 Amber,Amethyst,Emerald,Jade,Jasper,Ruby,Sapphire,Turquoise
                                              //special: oil
                                              //sea: fish,dolphin,whale,squid 
        public bool is_hill;
        public bool has_snow;
        public bool has_dirt;
        public bool has_grass;
        public TileTypeGoods Goods;
        public bool has_river;

        public void SetVertexHeight(Int2 pos, float ht)
        {
            int index = (Width * pos.Y) + pos.X;
            var currentVertex = VertexCPUBuffer[index];
            currentVertex.Position.Y = ht;
            VertexCPUBuffer[index] = currentVertex;
            //            HeightMapColors[index] = ht.AsStrideColor();
            UpdateAtPos(pos, currentVertex);
        }
        public void UpdateAtPos(Int2 pos, IVertex vertex)
        {
            if (this is null)
            {
                throw new System.Exception("Terrain tile is null");
            }
            int index = (Width * pos.Y) + pos.X;
            VertexTypePosTexNormColor x = (VertexTypePosTexNormColor)vertex;
            VertexCPUBuffer[index] = x;
        }

        public float GetCPUHeightAt(int i, int j)
        {
            if (!IsValidCoordinate(i, j))
            {
                return 0;//no contribution for this point
            }
            return VertexCPUBuffer[j * Width + i].Position.Y;
        }
        public bool IsValidCoordinate(int x, int y)
             => x >= 0 && x < Width && y >= 0 && y < Height;

    };

    public class TerrainTiles
    {
        #region variables
        public static Int2 Index_inWorld;
        public static int Width, Height, MAX_NUM_OF_TILES = 1024, m_NumTilesWideX,
            m_NumTilesHighZ, total_tile_number, number_of_water_tiles,
            number_of_land_tiles;
        public static List<WorldTile> Tiles = new List<WorldTile>();
        public static List<UniqueWorldTile> UniqueWorldTiles = new List<UniqueWorldTile>();
        public static Int2 HeightRange { get; set; } = new Int2(-100, 100);//=> Heightmap.Size.Y;
        private static Color[] HeightMapColors;
        #endregion
        public static void ToggleWorldTiles(bool toggle, TerrainComponent tcomp)
        {
            //find entity at the middle of the world
            foreach (WorldTile wtile in Tiles)
            {
               // Entity ent = tcomp.SceneSystem.SceneInstance.FirstOrDefault(
               //     a => a.Name == "TerrainTile" +wtile.Index_inWorld.ToString().Replace(" ", ","));
                if (wtile.TileEntity!=null)//ent != null)
                    wtile.TileEntity.Enable<ActivableEntityComponent>(toggle);
            }
        }
       // public static System.Windows.Forms.Timer Blink;
        /// <summary>
        /// run any starting stuff for tiles here
        /// </summary>
        /// <param name="tcomp"></param>
        public static void InitializeWorldTiles(TerrainComponent tcomp)
        {
            Index_inWorld = new Int2(m_NumTilesWideX / 2 ,
                m_NumTilesHighZ / 2);
            WorldPositionActive = true;
            UpdateWorldPosition(tcomp);
            System.Windows.Forms.Timer Blink = new System.Windows.Forms.Timer();
            Blink.Interval = 500; // update every half second
            Blink.Tick += new EventHandler(timer_Tick);
            Blink.Start();

            void timer_Tick(object sender, EventArgs e)
            {
                //can make it blink every second
                //     int deltaTime = (int)tcomp.Game.UpdateTime.Elapsed.TotalSeconds;
                if (WorldPositionActive)//deltaTime % 10 == 0)
                {
                    WorldPositionActive = false;
                    TerrainTilesGumps.WorldMapTexture = TerrainEditorView.WorldTilesTexture;
                    ImGuiSystem.UpdateTexture(TerrainTilesGumps.WorldMapIntPtr, TerrainTilesGumps.WorldMapTexture);
                }
                else
                {
                    WorldPositionActive = true;
                    UpdateWorldPosition(tcomp);
                }
            }
            /*       //find entity at the middle of the world
                   WorldTile wtile = Tiles[m_NumTilesWideX / 2 +
                       m_NumTilesWideX * m_NumTilesHighZ / 2];
                   if (wtile == null) return;
                   if(!wtile.ModelBuilt)
                   {
                       AddTileModel(wtile, tcomp);
                   }
                   wtile = Tiles[m_NumTilesWideX / 2+1 +
                       m_NumTilesWideX * m_NumTilesHighZ / 2];
                   if (wtile == null) return;
                   if (!wtile.ModelBuilt)
                   {
                       AddTileModel(wtile, tcomp);
                   }
                   wtile = Tiles[m_NumTilesWideX / 2 - 1 +
                  m_NumTilesWideX * m_NumTilesHighZ / 2];
                   if (wtile == null) return;
                   if (!wtile.ModelBuilt)
                   {
                       AddTileModel(wtile, tcomp);
                   }*/
            //wtile.TileEntity.Enable<ActivableEntityComponent>(true);
        }

        public static Int2 GetWorldTilePos(TerrainComponent tcomp)
        {
            //camera pos
            Entity a = tcomp.Game.Services.GetService<SceneSystem>().SceneInstance.
                FirstOrDefault(a => a.Name == "CameraMultiType");
            Vector3 pos = a.Transform.Position;
            foreach (WorldTile wtile in Tiles)
            {
                if (wtile == null) continue;
                if (wtile.WorldPosition.X <= pos.X &&
                    pos.X <= wtile.WorldPosition.X + Width &&
                    wtile.WorldPosition.Z <= pos.Z &&
                    pos.Z <= wtile.WorldPosition.Z + Height)
                {
                    return wtile.Index_inWorld;
                }
            }                
            return new Int2(0,0);
        }

        public static bool WorldPositionActive=false;
        /// <summary>
        /// updates visible world tiles asynchronously, 
        /// creates an entities for them if they are not created yet
        /// </summary>
        /// <param name="tcomp"></param>
        public static void UpdateWorldTiles(TerrainComponent tcomp)
        {
            Entity a = tcomp.Game.Services.GetService<SceneSystem>().SceneInstance.
                FirstOrDefault(a => a.Name == "CameraMultiType");
            Vector3 pos = a.Transform.Position;
            //could check about current tile and update, not go through all tiles
             //         foreach (WorldTile wtile in Tiles) { 
            int i, j;
            int minx = (int)Math.Max(0, Index_inWorld.X - 5),
                minz = (int)Math.Max(0, Index_inWorld.Y - 5),
                maxx = (int)Math.Min(m_NumTilesWideX, Index_inWorld.X + 5),
                maxz = (int)Math.Min(m_NumTilesHighZ, Index_inWorld.Y + 5);
            Int2 oldindex= Index_inWorld;
            for (j = minz; j < maxz; j++)
            {
                for (i = minx; i < maxx; i++)
                {
                    WorldTile wtile = Tiles[i+j* m_NumTilesWideX];
                    if (wtile == null) continue;
                    if (wtile.WorldPosition.X <= pos.X &&
                        pos.X <= wtile.WorldPosition.X + Width &&
                        wtile.WorldPosition.Z <= pos.Z &&
                        pos.Z <= wtile.WorldPosition.Z + Height)
                    {
                        Index_inWorld = wtile.Index_inWorld;
                    }
                    //edges
                    if (pos.X < 0)
                    {
                        pos.X = (m_NumTilesWideX-1) * Width;
                        Index_inWorld.X = m_NumTilesWideX - 1;
                        a.Transform.Position = pos;
                    }
                    if (pos.X > (m_NumTilesWideX - 1) * Width)
                    {
                        pos.X = 0;
                        Index_inWorld.X = 0;
                        a.Transform.Position = pos;
                    }
                    if (pos.Z < 0)
                    {
                        pos.Z = (m_NumTilesHighZ-1) * Width;
                        Index_inWorld.Y = m_NumTilesHighZ - 1;
                        a.Transform.Position = pos;
                    }
                    if (pos.Z > (m_NumTilesHighZ - 1) * Width)
                    {
                        pos.Z = 0;
                        Index_inWorld.Y = 0;
                        a.Transform.Position = pos;
                    }
                    if (oldindex != Index_inWorld)
                    {
                        UpdateWorldPosition(tcomp);
                        WorldPositionActive = true;
                    }
                    if (wtile.TileEntity != null)
                        wtile.TileEntity.Enable<ActivableEntityComponent>(false);
                    if (Vector3.Distance(wtile.WorldPosition, pos)
                        < 2500)//this distance should depend on tile size...
                    {
                        if (!wtile.ModelBuilt)
                        {
                            //coolest thing...
                            Task t = Task.Run(() =>
                            {
                                AddTileModel(wtile, tcomp);
                            });
                            t.Wait();
                            if (t.IsCompleted)
                                wtile.TileEntity.Enable<ActivableEntityComponent>(true);
                        }
                        if (wtile.TileEntity != null)
                            wtile.TileEntity.Enable<ActivableEntityComponent>(true);
                    }
                }
            }
        }
        public static void UpdateWorldPosition(TerrainComponent tcomp)
        {
            Color[] ColorValues = TerrainEditorView.WorldTilesTexture.
                GetColorData(tcomp.Game.GraphicsContext);
            int i, j, index;
            int minx = (int)Math.Max(0, Index_inWorld.X-5),
                minz = (int)Math.Max(0, Index_inWorld.Y-5),
                maxx = (int)Math.Min(m_NumTilesWideX, Index_inWorld.X + 5),
                maxz = (int)Math.Min(m_NumTilesHighZ, Index_inWorld.Y + 5);
            for (j = minz; j < maxz; j++)
            {
                for (i = minx; i < maxx; i++)
                {
                    index = (m_NumTilesWideX * (m_NumTilesHighZ-1-j)) +
                        m_NumTilesWideX-1-i;
                    ColorValues[index]=(Utility.Runif()<0.5f)? Color.DarkGreen:Color.DarkOliveGreen;
                }
            }                    
            TerrainTilesGumps.WorldMapTexture=ColorValues.ToTexture(TerrainTilesGumps.WorldMapTexture.Width,
                TerrainTilesGumps.WorldMapTexture.Height,
                tcomp.Game.GraphicsDevice, tcomp.Game.GraphicsContext.CommandList);
            ImGuiSystem.UpdateTexture(TerrainTilesGumps.WorldMapIntPtr, TerrainTilesGumps.WorldMapTexture);
        }

        /// <summary>
        /// Builds premade unique tiles of certain types (their heightmap, property and 
        /// blended textures) that can be used at runtime
        /// once stitched together and stream the world tiles. The number is controlled by
        /// the TerrainEditorView.UniqueTileNum2Generate variable.
        /// Can always load the generated tile textures and edit it in the terrain editor,
        /// and then save it.
        /// Each Tile type is given a specific name and number, e.g., WorldTileHeightmapMountain1,
        /// WorldTileHeightmapMountain2,... or WorldTileHill1, WorldTileHill2,
        /// and so forth. All files are saved in folder UniqueWorldTiles of the WorldTile folder in the Resources folder.
        /// </summary>
        public static void GenerateUniqueTiles(TerrainComponent tcomp)
        {
            if (!Directory.Exists(Utility.Resources_WorldTile_Directory +
                "UniqueWorldTiles\\"))
            {
                Directory.CreateDirectory(Utility.Resources_WorldTile_Directory
                    + "UniqueWorldTiles\\");
            }
            string dir = Utility.Resources_WorldTile_Directory +
                "UniqueWorldTiles\\";
            int i, Width = TerrainEditorView.TileSizeX, Height = TerrainEditorView.TileSizeZ;
            string FilenameType = dir + "WorldTileHeightmapFlat.bmp";
            //generate flat surface
            GraphicsContext GraphicsContext = tcomp.Game.GraphicsContext;
            GraphicsDevice GraphicsDevice = tcomp.GraphicsDevice;
            CommandList CommandList = tcomp.Game.GraphicsContext.CommandList;
            short level = 0;
            Texture texture = PerlinNoise.MakeFlat(Width, Height,
                    TerrainEditorView.TargetHeightValue.AsStrideColor()).ToTexture(
                     Width, Height, GraphicsDevice, CommandList);
            TerrainEditorView.SaveTex(texture, FilenameType, GraphicsContext, false);
            FilenameType = dir + "WorldTileHeightmapSeabed.bmp";
            //generate flat surface
            level = -20;
            texture = Utility.FlatTex(TerrainEditorView.m_Width, TerrainEditorView.m_Height,
                level.AsStrideColor(), GraphicsDevice, GraphicsContext);
            TerrainEditorView.SaveTex(texture, FilenameType, GraphicsContext, false);
            for (i = 0; i < TerrainEditorView.UniqueTileNum2Generate; i++)
            {
                TerrainEditorView.ResetRandomizationValues();
                //generate mountains
                TerrainEditorView.ShiftX = TerrainEditorView.m_Width / 2.0f;
                TerrainEditorView.ShiftZ = TerrainEditorView.m_Height / 2.0f;
                TerrainEditorView.VarianceX = Width / 5;
                TerrainEditorView.VarianceZ = Height / 5;
                TerrainEditorView.Type = 4;
                TerrainEditorView.Type2 = 1;//PowValue = 0.5f;
                TerrainEditorView.FreqX = 2; TerrainEditorView.FreqZ = 2;
                TerrainEditorView.Persistance = 2; TerrainEditorView.Freq = 1.5f;
                TerrainEditorView.Error = 0.1f;
                texture = PerlinNoise.RandomizeElevationMountain(Width, Height,
                TerrainEditorView.Mincutoff, TerrainEditorView.Maxcutoff,
                TerrainEditorView.PowValue, TerrainEditorView.VarianceX,
                TerrainEditorView.VarianceZ,
                TerrainEditorView.NormalizationConst,
                TerrainEditorView.TargetHeightValue,
                TerrainEditorView.PixelCutoff,
                TerrainEditorView.FreqX, TerrainEditorView.FreqZ,
                TerrainEditorView.Persistance, TerrainEditorView.Freq,
                TerrainEditorView.Error, TerrainEditorView.Octave,
                TerrainEditorView.Type, TerrainEditorView.Type2)
                    .Smooth(Width, Height).EdgesToHeightLevel(Width, 
                    Height, 0).ToTexture(
                         TerrainEditorView.m_Width, TerrainEditorView.m_Height, GraphicsDevice,
                         GraphicsContext.CommandList);
                FilenameType = dir + "WorldTileHeightmapMountain" + i.ToString() + ".bmp";
                TerrainEditorView.SaveTex(texture, FilenameType, GraphicsContext, false);
                //Desert
                TerrainEditorView.ResetRandomizationValues();
                TerrainEditorView.PowValue = 1.1f;
                TerrainEditorView.Persistance = 2.5f;
                texture = PerlinNoise.RandomizeElevationMapPerlinBased(Width,
                    Height, TerrainEditorView.NormalizationConst,
                    TerrainEditorView.FreqX, TerrainEditorView.FreqZ,
                    TerrainEditorView.PixelCutoff, TerrainEditorView.Freq,
                    TerrainEditorView.Error, TerrainEditorView.PowValue,
                    TerrainEditorView.Persistance, TerrainEditorView.Octave,
                    TerrainEditorView.Mincutoff, TerrainEditorView.Maxcutoff,
                            1).Smooth(Width, Height).EdgesToHeightLevel(Width, 
                    Height, 0).ToTexture(Width, Height, GraphicsDevice,
                     CommandList);
                FilenameType = dir + "WorldTileDesert" + i.ToString() + ".bmp";
                TerrainEditorView.SaveTex(texture, FilenameType, GraphicsContext, false);

                //Hills
                TerrainEditorView.ResetRandomizationValues();
                TerrainEditorView.NormalizationConst = 2.5f;
                TerrainEditorView.VarianceX = Width / 5;
                TerrainEditorView.VarianceZ = Height / 5;
                texture = PerlinNoise.RandomHillsBivNormalMap(Width, Height,
                TerrainEditorView.Mincutoff, TerrainEditorView.Maxcutoff,
                TerrainEditorView.PowValue, TerrainEditorView.VarianceX,
                TerrainEditorView.VarianceZ,
                TerrainEditorView.NormalizationConst,
                TerrainEditorView.TargetHeightValue,
                TerrainEditorView.PixelCutoff).Smooth(Width, Height).EdgesToHeightLevel(Width,
                    Height, 0).ToTexture(Width, Height, GraphicsDevice, CommandList);
                FilenameType = dir + "WorldTileHill" + i.ToString() + ".bmp";
                TerrainEditorView.SaveTex(texture, FilenameType, GraphicsContext, false);
                //Rivers
                TerrainEditorView.ResetRandomizationValues();
                TerrainEditorView.Persistance = 1;
                TerrainEditorView.Octave = 1;
                TerrainEditorView.Mincutoff = 0.7f;
                TerrainEditorView.FreqX = 3; TerrainEditorView.FreqZ = 3;
                TerrainEditorView.PowValue = 1;
                texture = PerlinNoise.RandomizePerlinBand(TerrainEditorView.m_Width, TerrainEditorView.m_Height,
                TerrainEditorView.NormalizationConst, TerrainEditorView.FreqX, TerrainEditorView.FreqZ, TerrainEditorView.PixelCutoff, TerrainEditorView.Freq,
                 TerrainEditorView.Error, TerrainEditorView.PowValue, 1.0f, 1, 0.75f, 0.751f,
                 -1).ToTexture(TerrainEditorView.m_Width, TerrainEditorView.m_Height,
                 GraphicsDevice, GraphicsContext.CommandList);
                FilenameType = dir + "WorldTileRiver" + i.ToString() + ".bmp";
                TerrainEditorView.SaveTex(texture, FilenameType, GraphicsContext, false);
            }
            TerrainEditorView.MSGlog.Add2Log("Generated unique tiles!");
        }

        public static void LoadUniqueTiles(TerrainComponent tcomp)
        {
            if (!Directory.Exists(Utility.Resources_WorldTile_Directory +
                "UniqueWorldTiles\\"))
            {
                throw new Exception("The UniqueWorldTiles directory does not exist. Make sure you generate some unique world tiles first...");
            }
            if (UniqueWorldTiles != null)
            {
                UniqueWorldTiles.Clear();
            }
            UniqueWorldTiles = new List<UniqueWorldTile>();
            string old_dir = System.IO.Directory.GetCurrentDirectory();
            string dir = Utility.Resources_WorldTile_Directory +
                "UniqueWorldTiles\\";
            System.IO.Directory.SetCurrentDirectory(dir);
            int i, counttile = 0, Width = TerrainEditorView.TileSizeX, Height = TerrainEditorView.TileSizeZ;
            Texture texture = Utility.LoadTex("WorldTileHeightmapFlat.bmp", tcomp.GraphicsDevice, tcomp.Game.GraphicsContext, false);
            if (texture == null)
            {
                System.IO.Directory.SetCurrentDirectory(old_dir);
                throw new Exception("The WorldTileHeightmapFlat texture does not exist. Make sure you generate the unique world tiles first...");
            }
            counttile++;
            UniqueWorldTile uniquetile = new UniqueWorldTile();
            uniquetile.unique_tile_heightmap = texture;
            uniquetile.unique_tile_Type_Precedence = TileTypePrecedence.Snow |
              TileTypePrecedence.Desert | TileTypePrecedence.Dirt
              | TileTypePrecedence.Grassland;
            BuildCPUBuffer(uniquetile, tcomp);
            UniqueWorldTiles.Add(uniquetile);

            texture = Utility.LoadTex("WorldTileHeightmapSeabed.bmp", tcomp.GraphicsDevice, tcomp.Game.GraphicsContext, false);
            if (texture == null)
            {
                System.IO.Directory.SetCurrentDirectory(old_dir);
                throw new Exception("The WorldTileHeightmapSeabed texture does not exist. Make sure you generate the unique world tiles first...");
            }
            counttile++;
            uniquetile = new UniqueWorldTile();
            uniquetile.unique_tile_heightmap = texture;
            uniquetile.unique_tile_Type_Precedence = TileTypePrecedence.Water |
                TileTypePrecedence.Beach;
            BuildCPUBuffer(uniquetile, tcomp);
            UniqueWorldTiles.Add(uniquetile);
            i = 0;
            while (File.Exists("WorldTileHeightmapMountain" + i.ToString() + ".bmp"))
            {
                texture = Utility.LoadTex("WorldTileHeightmapMountain" + i.ToString() + ".bmp", tcomp.GraphicsDevice, tcomp.Game.GraphicsContext, false);
                if (texture == null)
                {
                    System.IO.Directory.SetCurrentDirectory(old_dir);
                    throw new Exception("The WorldTileHeightmapMountain texture does not exist. Make sure you generate the unique world tiles first...");
                }
                counttile++;
                uniquetile = new UniqueWorldTile();
                uniquetile.unique_tile_heightmap = texture;
                uniquetile.unique_tile_Type_Precedence =
                    TileTypePrecedence.Mountain | TileTypePrecedence.Snow
                    | TileTypePrecedence.Dirt;
                BuildCPUBuffer(uniquetile, tcomp);
                UniqueWorldTiles.Add(uniquetile);

                texture = Utility.LoadTex("WorldTileDesert" + i.ToString() + ".bmp", tcomp.GraphicsDevice, tcomp.Game.GraphicsContext, false);
                if (texture == null)
                {
                    System.IO.Directory.SetCurrentDirectory(old_dir);
                    throw new Exception("The WorldTileDesert texture does not exist. Make sure you generate the unique world tiles first...");
                }
                counttile++;
                uniquetile = new UniqueWorldTile();
                uniquetile.unique_tile_heightmap = texture;
                uniquetile.unique_tile_Type_Precedence = TileTypePrecedence.Desert;
                BuildCPUBuffer(uniquetile, tcomp);
                UniqueWorldTiles.Add(uniquetile);

                texture = Utility.LoadTex("WorldTileHill" + i.ToString() + ".bmp", tcomp.GraphicsDevice, tcomp.Game.GraphicsContext, false);
                if (texture == null)
                {
                    System.IO.Directory.SetCurrentDirectory(old_dir);
                    throw new Exception("The WorldTileHill texture does not exist. Make sure you generate the unique world tiles first...");
                }
                counttile++;
                uniquetile = new UniqueWorldTile();
                uniquetile.unique_tile_heightmap = texture;
                uniquetile.unique_tile_Type_Precedence =
                    TileTypePrecedence.Desert | TileTypePrecedence.Snow
                    | TileTypePrecedence.Dirt | TileTypePrecedence.Grassland;
                BuildCPUBuffer(uniquetile, tcomp);
                UniqueWorldTiles.Add(uniquetile);

                i++;
            }
            TerrainEditorView.MSGlog.Add2Log("Loaded " + counttile + " unique world tiles!");
            System.IO.Directory.SetCurrentDirectory(old_dir);
        }

        public static void CreateWorldTiles(TerrainComponent tcomp)
        {
            if (UniqueWorldTiles == null || UniqueWorldTiles.Count == 0)
            {
                TerrainEditorView.MSGlog.Add2Log("There are no unique world tiles! Make sure you load them first!");
                return;
            }
            if (Tiles == null || Tiles.Count == 0)
            {
                TerrainEditorView.MSGlog.Add2Log("There are no world tiles created! Make sure you Generate the World based on rules first!");
                return;
            }
            List<int> angles = new List<int> { 0, 90, 180, 270 };
            //assign a unique tile to each world tile based on its type
            int index, i, j, k;
            for (j = 0; j < m_NumTilesHighZ; j++)
            {
                for (i = 0; i < m_NumTilesWideX; i++)
                {
                    index = i + j * m_NumTilesWideX;
                    WorldTile wtile = Tiles[index];
                    wtile.Index_inWorld = new Int2(i, j);
                    List<UniqueWorldTile> utiles = UniqueWorldTiles.Where(
                        tile => tile.unique_tile_Type_Precedence.HasFlag(
                            wtile.Type_Precedence)).ToList();
                    if (utiles == null || utiles.Count == 0)
                    {
                        throw new Exception("There are no unique tiles with this world tile precedence...");
                    }
                    UniqueWorldTile tile = utiles[Utility.DUnif(0, utiles.Count - 1)];
                    wtile.UniqueWorldTile = tile;
                    wtile.Width = tile.unique_tile_heightmap.Width;
                    wtile.Height = tile.unique_tile_heightmap.Height;
                    wtile.WorldPosition = new Vector3(
                        wtile.Index_inWorld.X * Width, 0,
                        wtile.Index_inWorld.Y * Height);
                    wtile.ModelBuilt = false;
                    //all tiles are using height based shader from tcomp
                    //can change this depending on precedence or other tile type
                    wtile.MaterialShader = tcomp.MaterialBlendHeight;
                    //rotate in one of 4 angles
                    wtile.Random_RotY = Utility.RList(angles);
                    wtile.Random_Shiftx = 0;
                    wtile.Random_Shiftz = 0;
                    wtile.Random_ScaleY = 1;
                    //this operation is very slow also, read comments below                
                    // wtile.VertexCPUBuffer = BuildCPUBuffer(wtile, tcomp);
                    //Task.Run( () =>
                    // {
                    //  TerrainEditorView.MSGlog.Add2Log("Generated Tile (" +
                    //  wtile.Index_inWorld.X + "," + wtile.Index_inWorld.Y + ")!");
                    // Calculation running on a different thread
                    //   AddTileModel(wtile, tcomp);
                    // });
                }
            }
            TerrainEditorView.MSGlog.Add2Log("Created " +
                m_NumTilesHighZ * m_NumTilesWideX + " world tiles!");

            //the operations below take forever even for a 16x16 world
            //I left it here just in case someone finds parts useful
            //Fastest approach is to asynchronously build tiles
            //as we move about the terrain. Once the tile is built
            //we wont be redoing it anymore, but just enable/disable the entity
            //as we move about the terrain
            return;

            Task.Run(async () =>
            {
                await Task.Delay(1000);
                //fix seams, from bottom left to top right
                //always fix the top row and right column of pixels
                for (j = 0; j < m_NumTilesHighZ; j++)
                {
                    for (i = 0; i < m_NumTilesWideX; i++)
                    {
                        index = i + j * m_NumTilesWideX;
                        Tiles[index].Index_inWorld = new Int2(i, j);
                        //get the tile to the north
                        if (j + 1 < m_NumTilesHighZ)
                        {
                            //make the north tile bottom row have the same heights
                            //as the current tile top row
                            for (k = 0; k < Width; k++)
                            {
                                Tiles[i + (j + 1) * m_NumTilesWideX].
                                    VertexCPUBuffer[k] =
                                Tiles[index].VertexCPUBuffer[k];
                            }
                        }
                        //get the tile to the east
                        if (i + 1 < m_NumTilesWideX)
                        {
                            for (k = 0; k < Height; k++)
                            {
                                Tiles[i + 1 + j * m_NumTilesWideX].VertexCPUBuffer
                                    [k] = Tiles[index].VertexCPUBuffer[k];
                            }
                        }
                    }
                }

            }).Wait();

            //save all world tile heightmaps
            if (!Directory.Exists(Utility.Resources_WorldTile_Directory +
                "AllWorldTiles\\"))
            {
                Directory.CreateDirectory(Utility.Resources_WorldTile_Directory
                    + "AllWorldTiles\\");
            }
            string dir = Utility.Resources_WorldTile_Directory +
                "AllWorldTiles\\";

            Task.Run(() =>
            {
                int tileindex = 0;
                for (j = 0; j < m_NumTilesHighZ; j++)
                {
                    for (i = 0; i < m_NumTilesWideX; i++)
                    {
                        tileindex = i + j * m_NumTilesWideX;
                        Texture texture = CPUBufferAsTexture(Tiles[tileindex], tcomp);
                        string FilenameType = dir + "WorldTile" + "," +
                            Tiles[tileindex].Index_inWorld.X + "," +
                            Tiles[tileindex].Index_inWorld.Y + ".bmp";
                        TerrainEditorView.SaveTex(texture, FilenameType,
                            tcomp.Game.GraphicsContext, false);
                    }
                }
            }).Wait();
        }

        /// <summary>
        /// this one is tricky. We can do this at runtime but unless the unique tiles
        /// have been processed to have a certain height depending on type,
        /// the seams will look bad. Instead we make sure every tile goes down to 
        /// height zero near its edges. But if you consider making a civ type game
        /// an artist can work on the unique tiles and make them prettier and ready
        /// to be joined easier, having pretermined height edges
        /// </summary>
        /// <param name="wtile"></param>
        /// <param name="tcomp"></param>
        private static void FixSeams(WorldTile wtile, TerrainComponent tcomp)
        {
            int index, i = wtile.Index_inWorld.X, j = wtile.Index_inWorld.Y,
                k,l;
            //when a new tile coems in, we get all tiles around it, if they exist
            //and set the new tile edges to the those of the existing tiles 
            index = i + j * m_NumTilesWideX;
            //get the tile to the north
            Int2 pos;
            if (j + 1 < m_NumTilesHighZ &&
                Tiles[i + (j + 1) * m_NumTilesWideX].ModelBuilt)
            {
                for (k = 0; k < Width; k++)
                {
                    pos = new Int2(k, Height - 1);
                    wtile.SetVertexHeight(pos,Tiles[i + (j + 1) * m_NumTilesWideX].
                        GetCPUHeightAt(k, 0));
                }
            }
            //get the tile to the south
            if (j - 1 >= 0 &&
                Tiles[i + (j - 1) * m_NumTilesWideX].ModelBuilt)
            {
                for (k = 0; k < Width; k++)
                {
                    pos = new Int2(k, 0);
                    wtile.SetVertexHeight(pos, Tiles[i + (j - 1) * m_NumTilesWideX].GetCPUHeightAt(
                        k, Height - 1));
                }
            }
            //get the tile to the west
            if (i - 1 >=0 &&
                Tiles[i - 1 + j * m_NumTilesWideX].ModelBuilt)
            {
                for (k = 0; k < Height; k++)
                {
                    pos = new Int2(0, k);
                    wtile.SetVertexHeight(pos, Tiles[i-1 +j * m_NumTilesWideX].
                        GetCPUHeightAt(Width - 1, k));
                }
            }
            //get the tile to the east
            if (i + 1 < m_NumTilesWideX &&
                Tiles[i + 1 + j * m_NumTilesWideX].ModelBuilt)
            {
                for (k = 0; k < Height; k++)
                {
                    pos = new Int2(Width - 1, k);
                    wtile.SetVertexHeight(pos, Tiles[i + 1 + j * m_NumTilesWideX].
                        GetCPUHeightAt(0, k));
                }
            }
          //  return;
            //smooth all inner tile heights, not the edges
            //causes notable lag...not running it
     /*       index = i + j * m_NumTilesHighZ;
            for (k = 1; k < Width - 1; k++)
            {
                for (l = 1; l < Height - 1; l++)
                {
                    pos = new Int2(k, l);
                    float ht = 0;
                    if (IsValidCoordinate(k - 1, l - 1))
                        ht += wtile.VertexCPUBuffer[(Width * (l - 1)) + k - 1].Position.Y;
                    if (IsValidCoordinate(k - 1, l))
                        ht += wtile.VertexCPUBuffer[(Width * l) + k - 1].Position.Y;
                    if (IsValidCoordinate(k - 1, l + 1))
                        ht += wtile.VertexCPUBuffer[(Width * (l + 1)) + k - 1].Position.Y;
                    if (IsValidCoordinate(k + 1, l - 1))
                        ht += wtile.VertexCPUBuffer[(Width * (l - 1)) + k + 1].Position.Y;
                    if (IsValidCoordinate(k + 1, l))
                        ht += wtile.VertexCPUBuffer[(Width * l) + k + 1].Position.Y;
                    if (IsValidCoordinate(k + 1, l + 1))
                        ht += wtile.VertexCPUBuffer[(Width * (l + 1)) + k + 1].Position.Y;
                    if (IsValidCoordinate(k, l + 1))
                        ht += wtile.VertexCPUBuffer[(Width * (l + 1)) + k].Position.Y;
                    if (IsValidCoordinate(k, l - 1))
                        ht += wtile.VertexCPUBuffer[(Width * (l - 1)) + k].Position.Y;
                    ht /= GeneralExtensions.CountNeighbors(new
                        Int2(wtile.Width, wtile.Height), k, l);
                    if (ht < tcomp.HeightRange.X)
                        ht = tcomp.HeightRange.X;
                    if (ht > tcomp.HeightRange.Y)
                        ht = tcomp.HeightRange.Y;
                    wtile.SetVertexHeight(pos, ht);
                }
            }
      */ 
        }

        private static void RotateBuffer(WorldTile wtile, TerrainComponent tcomp)
        {
            Texture tex = wtile.UniqueWorldTile.unique_tile_heightmap.Rotate(
                wtile.Random_RotY, tcomp.GraphicsDevice, tcomp.Game.GraphicsContext);
//            wtile.VertexCPUBuffer = wtile.UniqueWorldTile.VertexCPUBuffer;
            HeightRange = tcomp.HeightRange;
            Color[]MapColors = tex.GetColorData(tcomp.Game.GraphicsContext);
            int TerrainLOD = 1;
            Width = wtile.Width;
            Height = wtile.Height;
            Vector3 minBounds = new Vector3(0);
            int m_num_quads_z = (Height - 1) / TerrainLOD,
                m_num_quads_x = (Width - 1) / TerrainLOD;
            Vector3 maxBounds = new Vector3(Width * tcomp.m_QuadSideWidthX, 0,
                Height * tcomp.m_QuadSideWidthZ);
            Vector3 center = 0.5f * (minBounds + maxBounds);
            int numVertsX = m_num_quads_x + 1;
            int numVertsZ = m_num_quads_z + 1;
            float stepX = TerrainLOD * (maxBounds.X - minBounds.X) / (Width - 1);// m_num_quads_x;
            float stepZ = TerrainLOD * (maxBounds.Z - minBounds.Z) / (Height - 1);// m_num_quads_z;
            int index = 0, x, z, m_vertexCount = numVertsX * numVertsZ;
            Vector3 pos = new Vector3(minBounds.X, 0, minBounds.Z);
            byte R = 149, G = 135, B = 118;
            VertexTypePosTexNormColor[] m_vertices = new VertexTypePosTexNormColor[m_vertexCount];
            for (z = 0; z < numVertsZ; z++)
            {
                pos.X = minBounds.X;
                for (x = 0; x < numVertsX; x++)
                {
                    index = z * numVertsX + x;
                    m_vertices[index].Position = new Vector3(
                        pos.X, GetHeightAt(x, z), pos.Z);
                    if (tcomp.TEXTURE_REPEAT > 0)//whole terrain has the texture repeatedly
                    {
                        m_vertices[index].TexCoord.X = tcomp.m_QuadSideWidthX * tcomp.TEXTURE_REPEAT * x / (float)numVertsX * TerrainLOD;
                        m_vertices[index].TexCoord.Y = tcomp.m_QuadSideWidthZ * tcomp.TEXTURE_REPEAT * (z * 1.0f) / (float)numVertsZ * TerrainLOD;
                    }
                    else //comp.TEXTURE_REPEAT == 0//make each quad have the texture
                    {
                        m_vertices[index].TexCoord.X = tcomp.m_QuadSideWidthX * x * TerrainLOD;
                        m_vertices[index].TexCoord.Y = tcomp.m_QuadSideWidthZ * z * TerrainLOD;
                    }
                    m_vertices[index].Normal = GetNormal(x, z);
                    m_vertices[index].Tangent = GetTangent(x, z);
                    m_vertices[index].Color = new Color(R / 255.0f, G / 255.0f, B / 255.0f, 1);// / 255.0f;
                    //weight textures here
                    m_vertices[index].Color1 = new Color(0);// / 255.0f;
                    m_vertices[index].Color2 = new Color(0);// / 255.0f;
                    pos.X += stepX;
                }
                pos.Z += stepZ;
            }
            if (wtile.VertexCPUBuffer != null)
                wtile.VertexCPUBuffer.Clear();
            wtile.VertexCPUBuffer = m_vertices.ToList();
        }

        /// <summary>
        /// this one needs to run as a separate task
        /// </summary>
        /// <param name="wtile"></param>
        /// <param name="tcomp"></param>
        private static void AddTileModel(WorldTile wtile, TerrainComponent tcomp)
        {
            HeightRange = tcomp.HeightRange;
            int TerrainLOD = 1;
            Width = wtile.Width;
            Height = wtile.Height;
            Vector3 minBounds = new Vector3(0);
            int m_num_quads_z = (Height-1) / TerrainLOD,
                m_num_quads_x = (Width-1) / TerrainLOD;
            Vector3 maxBounds = new Vector3(Width * tcomp.m_QuadSideWidthX, 0,
                Height * tcomp.m_QuadSideWidthZ);
            Vector3 center = 0.5f * (minBounds + maxBounds);
            int numVertsX = m_num_quads_x + 1;
            int numVertsZ = m_num_quads_z + 1;
            float stepX = TerrainLOD * (maxBounds.X - minBounds.X) / (Width - 1);// m_num_quads_x;
            float stepZ = TerrainLOD * (maxBounds.Z - minBounds.Z) / (Height - 1);// m_num_quads_z;
            int index = 0, x, z, m_vertexCount = numVertsX * numVertsZ;
            /*           VertexTypePosTexNormColor[] m_vertices = new VertexTypePosTexNormColor[Height * Width];
                       for (index = 0; index < Height * Width; index++)
                       {
                           m_vertices[index] = wtile.UniqueWorldTile.VertexCPUBuffer[index];
                           //   m_vertices[index].Position += wtile.WorldPosition;
                       }            
                       if (wtile.VertexCPUBuffer != null) wtile.VertexCPUBuffer.Clear();
                       wtile.VertexCPUBuffer = m_vertices.ToList();*/
            //pretty slow, not going to rotate, but could...
            //RotateBuffer(wtile, tcomp);
            //just use the original unique tile instead
            if (wtile.VertexCPUBuffer != null) wtile.VertexCPUBuffer.Clear();
            wtile.VertexCPUBuffer = wtile.UniqueWorldTile.VertexCPUBuffer;

            //fix seams here
            FixSeams(wtile, tcomp);

            int count = 0;
            int[] indices = new int[m_vertexCount * 6];
            for (z = 0; z < m_num_quads_z; z++)
            {
                for (x = 0; x < m_num_quads_x; x++)
                {
                    var vbase = numVertsX * z + x;
                    indices[count++] = (vbase + 1);
                    indices[count++] = (vbase + 1 + numVertsX);
                    indices[count++] = (vbase + numVertsX);
                    indices[count++] = (vbase + 1);
                    indices[count++] = (vbase + numVertsX);
                    indices[count++] = (vbase);
                }
            }
            var indexBuffer = Stride.Graphics.Buffer.Index.New(
                tcomp.GraphicsDevice, indices, GraphicsResourceUsage.Default);
            var vertexBuffer = Stride.Graphics.Buffer.New(
                tcomp.GraphicsDevice, wtile.VertexCPUBuffer.ToArray(),//m_vertices.ToArray(),
                BufferFlags.VertexBuffer, GraphicsResourceUsage.Default);
            var mesh = new Mesh
            {
                Draw = new MeshDraw
                {
                    PrimitiveType = PrimitiveType.TriangleList,
                    DrawCount = indices.Length,
                    IndexBuffer = new IndexBufferBinding(indexBuffer, true, indices.Length),
                    VertexBuffers = new[] { new VertexBufferBinding(vertexBuffer, VertexTypePosTexNormColor.Layout, vertexBuffer.ElementCount) },
                },
                MaterialIndex = 0,
            };
            if(wtile.TileEntity!=null)
                tcomp.SceneSystem.SceneInstance.RootScene.Entities.Remove(wtile.TileEntity);
            wtile.TileEntity = new Entity("TerrainTile"
             + wtile.Index_inWorld.ToString().Replace(" ", ","));
            tcomp.SceneSystem.SceneInstance.RootScene.Entities.Add(wtile.TileEntity);
            var comp = wtile.TileEntity.GetOrCreate<ModelComponent>();
            var model = new Model();
            comp.Model = model;

            model.Meshes.Add(mesh);
            if (model.Materials != null)  model.Materials.Clear();
            //change the material for this tile here, using height based for all
            model.Materials.Add(wtile.MaterialShader);
            //enable the new entity, will disable only if it is not close to the camera
           // wtile.TileEntity.Enable<ActivableEntityComponent>(true);
            wtile.TileEntity.Transform.Position = wtile.WorldPosition;
            wtile.ModelBuilt = true;
        }

        private static Texture CPUBufferAsTexture(WorldTile wtile, 
            TerrainComponent tcomp)
        {
            Texture tex = Texture.New2D(tcomp.GraphicsDevice, wtile.Width,
                wtile.Height, PixelFormat.R8G8B8A8_UNorm,
                TextureFlags.ShaderResource, 1, GraphicsResourceUsage.Dynamic);
            Color[] heightValues = new Color[wtile.Width * wtile.Height];
            int i, j, index;
            for (i = 0; i < wtile.Width; i++)
            {
                for (j = 0; j < wtile.Height; j++)
                {
                    index = (wtile.Width * j) + i;
                    heightValues[index] = wtile.GetCPUHeightAt(i,j).AsStrideColor();
                }
            }
            tex.SetData(tcomp.GraphicsCommandList, heightValues);
            return tex;
        }

        private static void BuildCPUBuffer(UniqueWorldTile utile, 
            TerrainComponent tcomp)
        {
            HeightRange = tcomp.HeightRange;
            HeightMapColors = utile.unique_tile_heightmap.
                GetColorData(tcomp.Game.GraphicsContext);
            int TerrainLOD = 1;
            Width = utile.unique_tile_heightmap.Width;
            Height= utile.unique_tile_heightmap.Height;
            Vector3 minBounds = new Vector3(0);
            int m_num_quads_z = (Height- 1) / TerrainLOD,
                m_num_quads_x = (Width -1)/ TerrainLOD;
            Vector3 maxBounds = new Vector3(Width * tcomp.m_QuadSideWidthX, 0,
                Height* tcomp.m_QuadSideWidthZ);
            Vector3 center = 0.5f * (minBounds + maxBounds);
            int numVertsX = m_num_quads_x + 1;
            int numVertsZ = m_num_quads_z + 1;
            float stepX = TerrainLOD * (maxBounds.X - minBounds.X) / (Width - 1);// m_num_quads_x;
            float stepZ = TerrainLOD * (maxBounds.Z - minBounds.Z) / (Height - 1);// m_num_quads_z;
            int index = 0, x, z, m_vertexCount = numVertsX * numVertsZ;
            Vector3 pos = new Vector3(minBounds.X, 0, minBounds.Z);
            byte R = 149, G = 135, B = 118;
            VertexTypePosTexNormColor[] m_vertices = new VertexTypePosTexNormColor[m_vertexCount];
            for (z = 0; z < numVertsZ; z++)
            {
                pos.X = minBounds.X;
                for (x = 0; x < numVertsX; x++)
                {
                    index = z * numVertsX + x;
                    m_vertices[index].Position = new Vector3(
                        pos.X, GetHeightAt(x, z), pos.Z);
                    if (tcomp.TEXTURE_REPEAT > 0)//whole terrain has the texture repeatedly
                    {
                        m_vertices[index].TexCoord.X = tcomp.m_QuadSideWidthX * tcomp.TEXTURE_REPEAT * x / (float)numVertsX * TerrainLOD;
                        m_vertices[index].TexCoord.Y = tcomp.m_QuadSideWidthZ * tcomp.TEXTURE_REPEAT * (z * 1.0f) / (float)numVertsZ * TerrainLOD;
                    }
                    else //comp.TEXTURE_REPEAT == 0//make each quad have the texture
                    {
                        m_vertices[index].TexCoord.X = tcomp.m_QuadSideWidthX * x * TerrainLOD;
                        m_vertices[index].TexCoord.Y = tcomp.m_QuadSideWidthZ * z * TerrainLOD;
                    }
                    m_vertices[index].Normal = GetNormal(x, z);
                    m_vertices[index].Tangent = GetTangent(x, z);
                    m_vertices[index].Color = new Color(R / 255.0f, G / 255.0f, B / 255.0f, 1);// / 255.0f;
                    //weight textures here
                    m_vertices[index].Color1 = new Color(0);// / 255.0f;
                    m_vertices[index].Color2 = new Color(0);// / 255.0f;
                    pos.X += stepX;
                }
                pos.Z += stepZ;
            }
            if(utile.VertexCPUBuffer!=null)
                    utile.VertexCPUBuffer.Clear();
            utile.VertexCPUBuffer=m_vertices.ToList();
            /*
            int count = 0;
            int[] indices = new int[m_vertexCount * 6];
            for (z = 0; z < m_num_quads_z; z++)
            {
                for (x = 0; x < m_num_quads_x; x++)
                {
                    var vbase = numVertsX * z + x;
                    indices[count++] = (vbase + 1);
                    indices[count++] = (vbase + 1 + numVertsX);
                    indices[count++] = (vbase + numVertsX);
                    indices[count++] = (vbase + 1);
                    indices[count++] = (vbase + numVertsX);
                    indices[count++] = (vbase);
                }
            }
            var indexBuffer = Stride.Graphics.Buffer.Index.New(
                tcomp.GraphicsDevice, indices, GraphicsResourceUsage.Default);
            var vertexBuffer = Stride.Graphics.Buffer.New(
                tcomp.GraphicsDevice, m_vertices.ToArray(),
                BufferFlags.VertexBuffer, GraphicsResourceUsage.Default);
            var mesh = new Mesh
            {
                Draw = new MeshDraw
                {
                    PrimitiveType = PrimitiveType.TriangleList,
                    DrawCount = indices.Length,
                    IndexBuffer = new IndexBufferBinding(indexBuffer, true, indices.Length),
                    VertexBuffers = new[] { new VertexBufferBinding(vertexBuffer, VertexTypePosTexNormColor.Layout, vertexBuffer.ElementCount) },
                },
                MaterialIndex = 0,
            };*/
            //  return m_vertices.ToList();// mesh;
        }

        public static bool IsValidCoordinate(int x, int y)
        => x >= 0 && x < Width && y >= 0 && y < Height;

        public static float GetHeightAt(int i, int j)
        {
            if (!IsValidCoordinate(i, j))
            {
                return HeightRange.X;
            }
            float ht = 0;
            if (PerlinNoise.IsGrayScaleHeightMap)
                ht = HeightMapColors[j * Width + i].R;
            else
                ht = HeightMapColors[j * Width + i].ToFloat();
            float height = HeightRange.X +
                  (HeightRange.Y - HeightRange.X)
                  * ht / PerlinNoise.HeightMultiplier;
            return height;
        }
        public static Vector3 GetTangent(int x, int z)
        {
            var flip = 1;
            var here = new Vector3(x, GetHeightAt(x, z), z);
            var left = new Vector3(x - 1, GetHeightAt(x - 1, z), z);
            if (left.X < 0.0f)
            {
                flip *= -1;
                left = new Vector3(x + 1, GetHeightAt(x + 1, z), z);
            }

            left -= here;

            var tangent = left * flip;
            tangent.Normalize();

            return tangent;
        }
        public static Vector3 GetNormal(int x, int y)
        {
            var heightL = GetHeightAt(x - 1, y);
            var heightR = GetHeightAt(x + 1, y);
            var heightD = GetHeightAt(x, y - 1);
            var heightU = GetHeightAt(x, y + 1);
            var normal = new Vector3(heightL - heightR, 2.0f, heightD - heightU);
            normal.Normalize();
            return normal;
        }

        public static Texture Generate_WorldMap_Texture(GraphicsDevice GraphicsDevice,
            GraphicsContext GraphicsContext)
        {
            Texture tex = new Texture();
            Color[] m_col = new Color[m_NumTilesWideX * m_NumTilesHighZ];
            int i, j, index;
            for (i = 0; i < m_NumTilesWideX; i++)//pixels wide
            {
                for (j = 0; j < m_NumTilesHighZ; j++)//pixels high
                {
                    index = j * m_NumTilesHighZ + i;
                    m_col[index].A = 255;
                    //Mountain=0>Snow=1>Swamp=2>Desert=3>Grassland=4>Dirt=5>beach=6>Water=7
                    switch (Tiles[index].Type_Precedence)
                    {
                        case TileTypePrecedence.Mountain://Mountain
                            {
                                m_col[index].R = (byte)(PerlinNoise.Rock.R);
                                m_col[index].G = (byte)(PerlinNoise.Rock.G);
                                m_col[index].B = (byte)(PerlinNoise.Rock.B);
                                break;
                            }
                        case TileTypePrecedence.Snow://snow
                            {
                                m_col[index].R = (byte)(PerlinNoise.Ice.R);
                                m_col[index].G = (byte)(PerlinNoise.Ice.G);
                                m_col[index].B = (byte)(PerlinNoise.Ice.B);
                                break;
                            }
                        case TileTypePrecedence.Swamp://swamp
                            {
                                m_col[index].R = (byte)(PerlinNoise.Swamp.R);
                                m_col[index].G = (byte)(PerlinNoise.Swamp.G);
                                m_col[index].B = (byte)(PerlinNoise.Swamp.B);
                                break;
                            }
                        case TileTypePrecedence.Desert://desert
                            {
                                m_col[index].R = (byte)(PerlinNoise.Desert.R);
                                m_col[index].G = (byte)(PerlinNoise.Desert.G);
                                m_col[index].B = (byte)(PerlinNoise.Desert.B);
                                break;
                            }
                        case TileTypePrecedence.Grassland://grass
                            {
                                m_col[index].R = (byte)(PerlinNoise.Grassland.R);
                                m_col[index].G = (byte)(PerlinNoise.Grassland.G);
                                m_col[index].B = (byte)(PerlinNoise.Grassland.B);
                                break;
                            }
                        case TileTypePrecedence.Dirt://dirt
                            {
                                m_col[index].R = (byte)(PerlinNoise.Dirt.R);
                                m_col[index].G = (byte)(PerlinNoise.Dirt.G);
                                m_col[index].B = (byte)(PerlinNoise.Dirt.B);
                                break;
                            }
                        case TileTypePrecedence.Beach://beach
                            {
                                m_col[index].R = (byte)(PerlinNoise.Sand.R);
                                m_col[index].G = (byte)(PerlinNoise.Sand.G);
                                m_col[index].B = (byte)(PerlinNoise.Sand.B);
                                break;
                            }
                        case TileTypePrecedence.Water://sea
                            {
                                m_col[index].R = (byte)(Color.DarkBlue.R);
                                m_col[index].G = (byte)(Color.DarkBlue.G);
                                m_col[index].B = (byte)(Color.DarkBlue.B);
                                break;
                            }
                    }
                }
            }
            tex = m_col.ToTexture(m_NumTilesWideX,
                m_NumTilesHighZ, GraphicsDevice, GraphicsContext.CommandList);
            Array.Clear(m_col, 0, m_col.Length);
            return tex;
        }

        /// <summary>
        /// Loads an existing biome world map and generates the rest of the 
        /// world tile attributes
        /// </summary>
        /// <param name="worldmap"></param>
        /// <param name="GraphicsDevice"></param>
        /// <param name="GraphicsContext"></param>
        public static void LoadTiles_WorldMap_Texture(
            Texture worldmap,
            GraphicsDevice GraphicsDevice,
            GraphicsContext GraphicsContext)
        {
            Color[] m_col = worldmap.GetColorData(GraphicsContext);
            m_NumTilesWideX=worldmap.Width;
            m_NumTilesHighZ=worldmap.Height;
            if (Tiles != null)
            {
                Tiles.Clear();
            }
            Tiles = new List<WorldTile>();
            int i, j, index;
            for (i = 0; i < m_NumTilesWideX; i++)//pixels wide
            {
                for (j = 0; j < m_NumTilesHighZ; j++)//pixels high
                {
                    index = j * m_NumTilesHighZ + i;
                    Color col= m_col[index];
                    //Mountain=0>Snow=1>Swamp=2>Desert=3>Grassland=4>Dirt=5>beach=6>Water=7
                    WorldTile wtile = new WorldTile();
                    if (col == PerlinNoise.Rock)
                    {
                        wtile.type = 0;
                    }
                    else if (col == PerlinNoise.Ice)
                    {
                        wtile.type = 0;
                    }
                    else if (col == PerlinNoise.Swamp)
                    {
                        wtile.type = 0;
                    }
                    else if (col == PerlinNoise.Desert)
                    {
                        wtile.type = 0;
                    }
                    else if (col == PerlinNoise.Grassland)
                    {
                        wtile.type = 0;
                    }
                    else if (col == PerlinNoise.Dirt)
                    {
                        wtile.type = 0;
                    }
                    else if (col == PerlinNoise.Sand)
                    {
                        wtile.type = 0;
                    }
                    else if (col == Color.DarkBlue)
                    {
                        wtile.type = 1;
                    }
                    Tiles.Add(wtile);
                }
            }

            number_of_land_tiles = Tiles.Where(t=> t.type==0).ToList().Count;
            number_of_water_tiles = Tiles.Where(t => t.type == 1).ToList().Count;
            total_tile_number = number_of_land_tiles + number_of_land_tiles;
            int z, x;
            for (z = 0; z < m_NumTilesHighZ; z++)
            {
                for (x = 0; x < m_NumTilesWideX; x++)
                {
                    if (Tiles[x + z * m_NumTilesWideX].type == 0)//land tile
                    {
                        Tiles[x + z * m_NumTilesWideX].has_dirt = false;
                        Tiles[x + z * m_NumTilesWideX].has_grass = false;
                        Tiles[x + z * m_NumTilesWideX].has_snow = false;
                        Tiles[x + z * m_NumTilesWideX].has_river = false;
                        if (Utility.Runif() < TerrainEditorView.map_plains_or_highlands)//make hill
                        {
                            Tiles[x + z * m_NumTilesWideX].is_hill = true;
                            Tiles[x + z * m_NumTilesWideX].Random_ScaleY = Utility.Runif(0.9f, 1.1f);
                            Tiles[x + z * m_NumTilesWideX].Random_Shiftx = Utility.Runif(-1.0f, 1.0f);
                            Tiles[x + z * m_NumTilesWideX].Random_Shiftz = Utility.Runif(-1.0f, 1.0f);
                            Tiles[x + z * m_NumTilesWideX].Random_RotY =//Utility.DUnif(0,3);//
                                (int)MathF.Floor(2 * 3.14f * Utility.DUnif(0, 179));
                            if (Utility.Runif() < TerrainEditorView.map_mountain_or_hill)//make mountain
                            {
                                // Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_textures = Mountain_indices[5//0
                                //Utility.DUnif(0,mountain_count-1)
                                //    ];//11
                                Tiles[x + z * m_NumTilesWideX].Type_Precedence = 0;
                                //Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_meshes = Utility.DUnif(0, MAX_NUM_OF_UNIQUE_MOUNTAIN_MODELS - 1);
                            }
                            else
                            {
                                //  Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_meshes = 0;// Utility.DUnif(0, MAX_NUM_OF_UNIQUE_HILL_MODELS - 1);
                                Generate_LandTile(x, z);
                            }
                        }
                        else//flat
                        {
                            Tiles[x + z * m_NumTilesWideX].is_hill = false;
                            //  Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_meshes = -1;
                            Generate_LandTile(x, z);
                        }
                    }
                    else//sea tile
                    {
                        Tiles[x + z * m_NumTilesWideX].is_hill = false;
                        int top = z + 1, bottom = z - 1, left = x - 1, right = x + 1;
                        if (x == 0) left = m_NumTilesWideX - 1;
                        if (z == 0) bottom = m_NumTilesHighZ - 1;
                        if (z == m_NumTilesHighZ - 1) top = 0;
                        if (x == m_NumTilesWideX - 1) right = 0;
                        //surrounded by water
                        if (Tiles[left + m_NumTilesWideX * top].type == 1 && Tiles[x + m_NumTilesWideX * top].type == 1 && Tiles[right + m_NumTilesWideX * top].type == 1 &&
                            Tiles[left + m_NumTilesWideX * z].type == 1 && Tiles[right + m_NumTilesWideX * z].type == 1 &&
                            Tiles[left + m_NumTilesWideX * bottom].type == 1 && Tiles[x + m_NumTilesWideX * bottom].type == 1 && Tiles[right + m_NumTilesWideX * bottom].type == 1)
                        {
                            //   Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_meshes = 0;//StartSea;
                            Tiles[x + z * m_NumTilesWideX].Type_Precedence = TileTypePrecedence.Water;//sea
                            ///     Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_textures = Water_indices[0];
                        }
                        else
                        {
                            //					Tiles[x+z* m_NumTilesWideX].type=2;
                            //  Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_textures = -100;//Beach_indices[0];
                        }
                    }
                }
            }

            for (z = 0; z < m_NumTilesHighZ; z++)
            {
                for (x = 0; x < m_NumTilesWideX; x++)
                {
                    //Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_textures_partial_version = 0;// Utility.DUnif(0, MAX_NUM_OF_TILE_VERSION - 1);
                    if (Tiles[x + z * m_NumTilesWideX].type == 0) Generate_UniformLandTile(x, z);
                    /*    if (Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_textures == -100)//Beach_indices[0])
                        {
                            Tiles[x+z* m_NumTilesWideX].Type_Precedence = TileTypePrecedence.Beach;//beach
                            Tiles[x+z* m_NumTilesWideX].type = 2;//beach
                            Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_textures = Seabed_indices[2];//Utility.DUnif(0,water_count-1)];
                        }*/
                }
            }
            //NEED TO DO THIS PASS HERE
            for (z = 0; z < m_NumTilesHighZ; z++)
            {
                for (x = 0; x < m_NumTilesWideX; x++)
                {
                    if (Tiles[x + z * m_NumTilesWideX].type == 2)//beach
                    {
                        //   Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_meshes = Process_BeachTile(x, z, true);
                        Tiles[x + z * m_NumTilesWideX].Random_ScaleY = Utility.Runif(0.9f, 1.1f);
                        Tiles[x + z * m_NumTilesWideX].Random_Shiftx = Utility.Runif(1.0f, 10.0f);
                        Tiles[x + z * m_NumTilesWideX].Random_Shiftz = Utility.Runif(1.0f, 10.0f);
                        Tiles[x + z * m_NumTilesWideX].Random_RotY = Utility.DUnif(0, 3);//Utility.DUnif(1,180)+Utility.DUnif(0,179);
                    }
                }
            }
        }

        /////////////tile generation
        /// <summary>
        /// Build a 0-1 (land-sea) matrix for the tiles. Uses a discrete random walk approach
        /// </summary>
        public static void Generate_LandSeaNeighborhoods()
        {
            //start with all sea tiles
            int x, z;
            int countland = 0, num_of_land_tiles_needed = (int)MathF.Floor(1.0f * m_NumTilesWideX * m_NumTilesHighZ * TerrainEditorView.map_land_or_sea);
            for (z = 0; z < m_NumTilesHighZ; z++)
                for (x = 0; x < m_NumTilesWideX; x++)
                {
                    WorldTile tile = new WorldTile();
                    tile.type = 1;//open sea
                    Tiles.Add(tile);
                }
            int centx, centz;
            float rx, rz;
            total_tile_number = m_NumTilesWideX * m_NumTilesHighZ;
            while (countland < num_of_land_tiles_needed)
            {
                centx = Utility.DUnif(0, m_NumTilesWideX - 1);
                centz = Utility.DUnif(0, m_NumTilesHighZ - 1);
                rx = Utility.Runif(m_NumTilesWideX * (1-TerrainEditorView.map_continent_or_island) / 10.0f,
                    m_NumTilesWideX * (1 - TerrainEditorView.map_continent_or_island) / 5.0f);
                rz = Utility.Runif(m_NumTilesHighZ * (1 - TerrainEditorView.map_continent_or_island) / 10.0f,
                    m_NumTilesHighZ * (1 - TerrainEditorView.map_continent_or_island) / 5.0f);
                //start filling the sea tiles using an ellipse brush
                x = centx; z = centz;
                while (x >= 0 && x < m_NumTilesWideX && z >= 0 && z < m_NumTilesHighZ &&
                    1.0f * (x - centx) * (x - centx) / (rx * rx) + 1.0f * (z - centz) * (z - centz) / (rz * rz) < 3.0f)//Utility.Runif(10,30))
                {
                    //make neighbors the same as current
                    if (Tiles[x + z * m_NumTilesWideX].type == 1)//sea tile
                    {
                        Tiles[x + z * m_NumTilesWideX].type = 0;
                        countland++;
                    }
                    if (countland >= num_of_land_tiles_needed)
                        break;
                    //random walk on x,z
                    x += Utility.DUnif(-TerrainEditorView.RandomWalkXDir, TerrainEditorView.RandomWalkXDir);
                    z += Utility.DUnif(-TerrainEditorView.RandomWalkZDir, TerrainEditorView.RandomWalkZDir);
                }
                if (countland >= num_of_land_tiles_needed)
                    break;
            }
            number_of_land_tiles = countland;
            number_of_water_tiles = total_tile_number - number_of_land_tiles;
        }

        /// <summary>
        /// Processes a land tile based on its neighbors and rules.
        /// Type Precedence is Mountain=0>Snow=1>Swamp=2>Desert=3>Grassland=4>beach=5>Water=6
        /// </summary>
        /// <param name="x"></param>
        /// <param name="z"></param>
        public static void Generate_LandTile(int x, int z)
        {
            //float TerrainEditorView.map_uniform_or_random,TerrainEditorView.map_land_or_sea,TerrainEditorView.map_warm_or_cold,TerrainEditorView.map_wet_or_dry,TerrainEditorView.map_rivers,
            //  TerrainEditorView.map_forestry,TerrainEditorView.map_highlands_or_plains,TerrainEditorView.map_mountain_or_hill,
            //  TerrainEditorView.map_goods_more_or_less,TerrainEditorView.map_continent_or_island,TerrainEditorView.map_human_friendly,tiles_side_length;
            //Mountain=0>Snow=1>Swamp=2>Desert=3>Grassland=4>beach=5>Water=6
            Tiles[x + z * m_NumTilesWideX].removable_terrain_property = -1;
            float coldbound = TerrainEditorView.map_warm_or_cold * 0.13f * m_NumTilesHighZ - 1;//from north-south poles
            float desertbound =//min((int)MathF.Floor(0.5f*numoftiles_side-coldbound),
                (1 - TerrainEditorView.map_wet_or_dry) * 0.13f * m_NumTilesHighZ - 1;//from equator
            int top = z + 1, bottom = z - 1, left = x - 1, right = x + 1;
            if (x == 0) left = m_NumTilesWideX - 1;
            if (z == 0) bottom = m_NumTilesHighZ - 1;
            if (z == m_NumTilesHighZ - 1) top = 0;
            if (x == m_NumTilesWideX - 1) right = 0;
            restart_tile_generation:
            //	Tiles[x+z* m_NumTilesWideX].is_hill=false;

            if (TerrainEditorView.use_earth_rules)
            {
                if (z < coldbound || z > m_NumTilesHighZ - coldbound)
                //near north-south pole, can only have mountain or snow or dirt
                {
                    if (Utility.Runif() < TerrainEditorView.map_wet_or_dry)//wet cold tile, snowed tile
                    {
                        //cant have snow around desert
                        if (Tiles[left + z * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Desert
                            || Tiles[left + top * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Desert
                            || Tiles[left + bottom * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Desert
                            || Tiles[x + top * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Desert
                            || Tiles[x + bottom * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Desert
                            || Tiles[right + z * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Desert
                            || Tiles[right + top * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Desert
                            || Tiles[right + bottom * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Desert)
                            goto restart_tile_generation;
                        //cant have snow around swamp
                        if (Tiles[left + z * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Swamp
                            || Tiles[left + top * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Swamp
                            || Tiles[left + bottom * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Swamp
                            || Tiles[x + top * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Swamp
                            || Tiles[x + bottom * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Swamp
                            || Tiles[right + z * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Swamp
                            || Tiles[right + top * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Swamp
                            || Tiles[right + bottom * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Swamp)
                            goto restart_tile_generation;
                        //cant have snow around grassland
                        if (Tiles[left + z * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Grassland
                            || Tiles[left + top * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Grassland
                            || Tiles[left + bottom * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Grassland
                            || Tiles[x + top * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Grassland
                            || Tiles[x + bottom * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Grassland
                            || Tiles[right + z * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Grassland
                            || Tiles[right + top * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Grassland
                            || Tiles[right + bottom * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Grassland)
                            goto restart_tile_generation;
                        Tiles[x + z * m_NumTilesWideX].Type_Precedence = TileTypePrecedence.Snow;
                        if (Tiles[x + z * m_NumTilesWideX].is_hill)
                            Tiles[x + z * m_NumTilesWideX].has_dirt = (Utility.Runif() < 0.9f);
                        else
                            Tiles[x + z * m_NumTilesWideX].has_dirt = (Utility.Runif() < 0.5f);
                        if (Utility.Runif() < TerrainEditorView.map_human_friendly)//wet tile->tundra has some food
                        {
                                                                                                              //				Tiles[x+z* m_NumTilesWideX].has_dirt=(Utility.Runif()<0.8f);
                            if (Utility.Runif() < TerrainEditorView.map_forestry)//add a forest tile
                                Tiles[x + z * m_NumTilesWideX].removable_terrain_property = 3;//snow forest
                        }
                        else
                        {
                         //   Tiles[x + z * m_NumTilesWideX].index_into_unique_tile_textures = Snow_indices[0];//MF->Utility.DUnif(0,snow_count-1)];
                        }
                    }
                    else//dirt tile with/out snow on it
                    {
                        if (Utility.Runif() < TerrainEditorView.map_human_friendly)//dirt with snow on
                        {
                            Tiles[x + z * m_NumTilesWideX].Type_Precedence = TileTypePrecedence.Dirt;
                          //  Tiles[x + z * m_NumTilesWideX].index_into_unique_tile_textures = Dirt_indices[0];//MF->Utility.DUnif(0,dirt_count-1)];
                                                                                                             //add forests/jungle
                            if (Utility.Runif() < TerrainEditorView.map_forestry)//add a forest tile
                                Tiles[x + z * m_NumTilesWideX].removable_terrain_property = 3;//snow forest
                            else
                                if (Tiles[x + z * m_NumTilesWideX].is_hill)
                                Tiles[x + z * m_NumTilesWideX].has_snow = (Utility.Runif() < 0.8f);
                            else
                                Tiles[x + z * m_NumTilesWideX].has_snow = (Utility.Runif() < 0.5f);
                        }
                        else
                        {
                            Tiles[x + z * m_NumTilesWideX].Type_Precedence = TileTypePrecedence.Snow;
                           // Tiles[x + z * m_NumTilesWideX].index_into_unique_tile_textures = Glacier_indices[0];//MF->Utility.DUnif(0,glacier_count-1)];
                            if (Tiles[x + z * m_NumTilesWideX].is_hill)
                                Tiles[x + z * m_NumTilesWideX].has_dirt = (Utility.Runif() < 0.9f);
                            else
                                Tiles[x + z * m_NumTilesWideX].has_dirt = (Utility.Runif() < 0.5f);
                        }
                    }
                }
                /*	else
                    if(z<coldbound+1 || z>numoftiles_side-coldbound-1)
                    //near north-south pole, can only have mountain or dirt
                    {
                        Tiles[x+z* m_NumTilesWideX].Type_Precedence=5;
                        Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_textures=Dirt_indices[MF->Utility.DUnif(0,dirt_count-1)];
                //add forests
                        if(Utility.Runif()<TerrainEditorView.map_forestry)//add a forest tile
                            Tiles[x+z* m_NumTilesWideX].removable_terrain_property=0;//forest
                    }*/
                else//between snow and equator, plains and grass
                    if (z < 0.5 * m_NumTilesHighZ - desertbound || z > 0.5 * m_NumTilesHighZ + desertbound)
                {
                    if (Utility.Runif() < TerrainEditorView.map_wet_or_dry)//wet tile, leads to wetlands or drylands
                    {
                        if (Utility.Runif() < TerrainEditorView.map_human_friendly)//grassland
                        {
                            //choose between dirt and grass
                            if (Utility.Runif() < 0.5f)
                            {
                                Tiles[x + z * m_NumTilesWideX].Type_Precedence = TileTypePrecedence.Grassland;//grass
                               // Tiles[x + z * m_NumTilesWideX].index_into_unique_tile_textures = Grass_indices[0];//MF->Utility.DUnif(0,grass_count-1)];
                                if (Tiles[x + z * m_NumTilesWideX].is_hill)
                                    Tiles[x + z * m_NumTilesWideX].has_dirt = (Utility.Runif() < 0.9f);
                                else
                                    Tiles[x + z * m_NumTilesWideX].has_dirt = (Utility.Runif() < 0.5f);
                            }
                            else
                            {
                                Tiles[x + z * m_NumTilesWideX].Type_Precedence = TileTypePrecedence.Dirt;//dirt
                               // Tiles[x + z * m_NumTilesWideX].index_into_unique_tile_textures = Dirt_indices[0];//MF->Utility.DUnif(0,dirt_count-1)];
                                if (Tiles[x + z * m_NumTilesWideX].is_hill)
                                    Tiles[x + z * m_NumTilesWideX].has_grass = (Utility.Runif() < 0.9f);
                                else
                                    Tiles[x + z * m_NumTilesWideX].has_grass = (Utility.Runif() < 0.5f);
                            }
                            //cant have grass around desert
                            if (Tiles[x + z * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Grassland)
                            {
                                //cant have grassland around snow
                                if (Tiles[left + z * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Snow
                                    || Tiles[left + top * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Snow
                                    || Tiles[left + bottom * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Snow
                                    || Tiles[x + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Snow
                                    || Tiles[x + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Snow
                                    || Tiles[right + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Snow
                                    || Tiles[right + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Snow
                                    || Tiles[right + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Snow)
                                    goto restart_tile_generation;
                                if (Tiles[left + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Desert
                                    || Tiles[left + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Desert
                                    || Tiles[left + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Desert
                                    || Tiles[x + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Desert
                                    || Tiles[x + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Desert
                                    || Tiles[right + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Desert
                                    || Tiles[right + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Desert
                                    || Tiles[right + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Desert)
                                    goto restart_tile_generation;//*
                            }
                            //add forests/jungle
                            if (Utility.Runif() < TerrainEditorView.map_forestry)//add a forest tile
                                Tiles[x + z * m_NumTilesWideX].removable_terrain_property = 2;//forest
                        }
                        else//swamp,marsh
                        {
                            if (Tiles[x + z * m_NumTilesWideX].is_hill) goto restart_tile_generation;
                            //cant have swamp around snow
                            if (Tiles[left + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[left + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[left + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[x + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[x + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[right + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[right + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[right + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Snow)
                                goto restart_tile_generation;
                            //cant have swamp around desert
                            if (Tiles[left + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[left + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[left + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[x + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[x + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[right + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[right + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[right + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Desert)
                                goto restart_tile_generation;
                            Tiles[x + z * m_NumTilesWideX].Type_Precedence = TileTypePrecedence.Swamp;
                           // Tiles[x + z * m_NumTilesWideX].index_into_unique_tile_textures = Marsh_indices[0];//MF->Utility.DUnif(0,marsh_count-1)];
                                                                                                              //add forests/jungle
                            if (Utility.Runif() < TerrainEditorView.map_forestry)//add a forest tile
                                Tiles[x + z * m_NumTilesWideX].removable_terrain_property = 2;//swamp forest
                        }
                    }
                    else//make jungle
                    {
                        //choose between dirt and grass
                        if (Utility.Runif() < 0.5f)
                        {
                            Tiles[x + z * m_NumTilesWideX].Type_Precedence = TileTypePrecedence.Grassland;//grass
                          //  Tiles[x + z * m_NumTilesWideX].index_into_unique_tile_textures = Grass_indices[0];//MF->Utility.DUnif(0,grass_count-1)];
                            if (Tiles[x + z * m_NumTilesWideX].is_hill)
                                Tiles[x + z * m_NumTilesWideX].has_dirt = (Utility.Runif() < 0.9f);
                            else
                                Tiles[x + z * m_NumTilesWideX].has_dirt = (Utility.Runif() < 0.5f);
                        }
                        else
                        {
                            Tiles[x + z * m_NumTilesWideX].Type_Precedence = TileTypePrecedence.Dirt;//dirt
                            //Tiles[x + z * m_NumTilesWideX].index_into_unique_tile_textures = Dirt_indices[0];//MF->Utility.DUnif(0,dirt_count-1)];
                            if (Tiles[x + z * m_NumTilesWideX].is_hill)
                                Tiles[x + z * m_NumTilesWideX].has_grass = (Utility.Runif() < 0.9f);
                            else
                                Tiles[x + z * m_NumTilesWideX].has_grass = (Utility.Runif() < 0.5f);
                        }
                        if (Tiles[x + z * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Grassland)
                        {
                            //cant have grassland around snow
                            if (Tiles[left + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[left + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[left + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[x + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[x + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[right + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[right + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[right + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Snow)
                                goto restart_tile_generation;
                            //cant have grass around desert
                            if (Tiles[left + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[left + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[left + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[x + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[x + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[right + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[right + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[right + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Desert)
                                goto restart_tile_generation;
                        }
                        //add forests/jungle
                        if (Utility.Runif() < TerrainEditorView.map_forestry)//add a forest tile
                            Tiles[x + z * m_NumTilesWideX].removable_terrain_property = 1;//jungle
                    }
                }
                else//near the equator desertbound
                {
                    if (Utility.Runif() < TerrainEditorView.map_wet_or_dry)//wet tile, leads to wetlands or drylands
                    {
                        if (Utility.Runif() < TerrainEditorView.map_human_friendly)//grassland
                        {
                            //choose between dirt and grass
                            if (Utility.Runif() < 0.5f)
                            {
                                Tiles[x + z * m_NumTilesWideX].Type_Precedence = TileTypePrecedence.Grassland;//grass
                              //  Tiles[x + z * m_NumTilesWideX].index_into_unique_tile_textures = Grass_indices[0];//MF->Utility.DUnif(0,grass_count-1)];
                                if (Tiles[x + z * m_NumTilesWideX].is_hill)
                                    Tiles[x + z * m_NumTilesWideX].has_dirt = (Utility.Runif() < 0.9f);
                                else
                                    Tiles[x + z * m_NumTilesWideX].has_dirt = (Utility.Runif() < 0.5f);
                            }
                            else
                            {
                                Tiles[x + z * m_NumTilesWideX].Type_Precedence = TileTypePrecedence.Dirt;//dirt
                               // Tiles[x + z * m_NumTilesWideX].index_into_unique_tile_textures = Dirt_indices[0];//MF->Utility.DUnif(0,dirt_count-1)];
                                if (Tiles[x + z * m_NumTilesWideX].is_hill)
                                    Tiles[x + z * m_NumTilesWideX].has_grass = (Utility.Runif() < 0.9f);
                                else
                                    Tiles[x + z * m_NumTilesWideX].has_grass = (Utility.Runif() < 0.5f);
                            }
                            if (Tiles[x + z * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Grassland)
                            {
                                //cant have grassland around snow
                                if (Tiles[left + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Snow
                                    || Tiles[left + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Snow
                                    || Tiles[left + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Snow
                                    || Tiles[x + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Snow
                                    || Tiles[x + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Snow
                                    || Tiles[right + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Snow
                                    || Tiles[right + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Snow
                                    || Tiles[right + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Snow)
                                    goto restart_tile_generation;
                                //cant have grass around desert
                                if (Tiles[left + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Desert
                                    || Tiles[left + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Desert
                                    || Tiles[left + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Desert
                                    || Tiles[x + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Desert
                                    || Tiles[x + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Desert
                                    || Tiles[right + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Desert
                                    || Tiles[right + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Desert
                                    || Tiles[right + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Desert)
                                    goto restart_tile_generation;
                            }
                            if (Utility.Runif() < TerrainEditorView.map_forestry)//add a forest tile
                                Tiles[x + z * m_NumTilesWideX].removable_terrain_property = 1;//jungle
                        }
                        else//swamp,marsh
                        {
                            if (Tiles[x + z * m_NumTilesWideX].is_hill) goto restart_tile_generation;
                            //cant have swamp around snow
                            if (Tiles[left + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[left + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[left + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[x + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[x + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[right + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[right + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[right + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Snow)
                                goto restart_tile_generation;
                            //cant have swamp around desert
                            if (Tiles[left + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[left + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[left + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[x + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[x + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[right + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[right + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[right + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Desert)
                                goto restart_tile_generation;
                            Tiles[x + z * m_NumTilesWideX].Type_Precedence = TileTypePrecedence.Swamp;
                           // Tiles[x + z * m_NumTilesWideX].index_into_unique_tile_textures = Marsh_indices[0];//MF->Utility.DUnif(0,marsh_count-1)];
                            Tiles[x + z * m_NumTilesWideX].has_dirt = (Utility.Runif() < 0.5f);
                        }
                        //Mountain=0>Snow=1>Swamp=2>Desert=3>Grassland=4>Dirt=5>beach=6>Water=7
                    }
                    else//dry, desert tiles, oasis
                    {
                        //			if(1.0*z<(0.5-desertbound)*numoftiles_side || 1.0*z>(0.5+desertbound)*numoftiles_side)goto restart_tile_generation;
                        //cant have desert around swamp
                        if (Tiles[left + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Swamp || Tiles[left + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Swamp
                            || Tiles[left + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Swamp || Tiles[x + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Swamp
                            || Tiles[x + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Swamp || Tiles[right + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Swamp
                            || Tiles[right + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Swamp || Tiles[right + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Swamp)
                            goto restart_tile_generation;
                        //cant have desert around snow
                        if (Tiles[left + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Snow || Tiles[left + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Snow
                            || Tiles[left + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Snow || Tiles[x + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Snow
                            || Tiles[x + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Snow || Tiles[right + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Snow
                            || Tiles[right + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Snow || Tiles[right + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Snow)
                            goto restart_tile_generation;
                        //cant have desert around grass
                        if (Tiles[left + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Grassland || Tiles[left + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Grassland
                            || Tiles[left + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Grassland || Tiles[x + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Grassland
                            || Tiles[x + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Grassland || Tiles[right + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Grassland
                            || Tiles[right + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Grassland || Tiles[right + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Grassland)
                            goto restart_tile_generation;
                        Tiles[x + z * m_NumTilesWideX].Type_Precedence = TileTypePrecedence.Desert;
                        if (Tiles[x + z * m_NumTilesWideX].is_hill)
                            Tiles[x + z * m_NumTilesWideX].has_dirt = (Utility.Runif() < 0.9f);
                        else
                            Tiles[x + z * m_NumTilesWideX].has_dirt = (Utility.Runif() < 0.5f);
                        if (Utility.Runif() < TerrainEditorView.map_human_friendly)//oasis
                        {
                            Tiles[x + z * m_NumTilesWideX].removable_terrain_property = 4;//oasis
                         //   Tiles[x + z * m_NumTilesWideX].index_into_unique_tile_textures = Desert_indices[0];//MF->Utility.DUnif(0,desert_count-1)];
                        }
                      //  else//desert
                           // Tiles[x + z * m_NumTilesWideX].index_into_unique_tile_textures = Desert_indices[0];//MF->Utility.DUnif(0,desert_count-1)];
                    }
                }
            }
            else//tiles are not bound by their position
            {
                if (Utility.Runif() < TerrainEditorView.map_warm_or_cold)
                //near north-south pole, can only have mountain or snow or dirt
                {
                    if (Utility.Runif() < TerrainEditorView.map_wet_or_dry)//wet cold tile, snowed tile
                    {
                        //cant have snow around desert
                        if (Tiles[left + z * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Desert
                            || Tiles[left + top * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Desert
                            || Tiles[left + bottom * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Desert
                            || Tiles[x + top * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Desert
                            || Tiles[x + bottom * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Desert
                            || Tiles[right + z * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Desert
                            || Tiles[right + top * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Desert
                            || Tiles[right + bottom * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Desert)
                            goto restart_tile_generation;
                        //cant have snow around swamp
                        if (Tiles[left + z * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Swamp
                            || Tiles[left + top * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Swamp
                            || Tiles[left + bottom * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Swamp
                            || Tiles[x + top * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Swamp
                            || Tiles[x + bottom * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Swamp
                            || Tiles[right + z * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Swamp
                            || Tiles[right + top * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Swamp
                            || Tiles[right + bottom * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Swamp)
                            goto restart_tile_generation;
                        //cant have snow around grassland
                        if (Tiles[left + z * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Grassland
                            || Tiles[left + top * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Grassland
                            || Tiles[left + bottom * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Grassland
                            || Tiles[x + top * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Grassland
                            || Tiles[x + bottom * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Grassland
                            || Tiles[right + z * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Grassland
                            || Tiles[right + top * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Grassland
                            || Tiles[right + bottom * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Grassland)
                            goto restart_tile_generation;
                        Tiles[x + z * m_NumTilesWideX].Type_Precedence = TileTypePrecedence.Snow;
                        if (Tiles[x + z * m_NumTilesWideX].is_hill)
                            Tiles[x + z * m_NumTilesWideX].has_dirt = (Utility.Runif() < 0.9f);
                        else
                            Tiles[x + z * m_NumTilesWideX].has_dirt = (Utility.Runif() < 0.5f);
                        if (Utility.Runif() < TerrainEditorView.map_human_friendly)//wet tile->tundra has some food
                        {
                         //   Tiles[x + z * m_NumTilesWideX].index_into_unique_tile_textures = Tundra_indices[0];//MF->Utility.DUnif(0,tundra_count-1)];
                                                                                                               //				Tiles[x+z* m_NumTilesWideX].has_dirt=(Utility.Runif()<0.8f);
                            if (Utility.Runif() < TerrainEditorView.map_forestry)//add a forest tile
                                Tiles[x + z * m_NumTilesWideX].removable_terrain_property = 3;//snow forest
                        }
                        else
                        {
                           // Tiles[x + z * m_NumTilesWideX].index_into_unique_tile_textures = Snow_indices[0];//MF->Utility.DUnif(0,snow_count-1)];
                        }
                    }
                    else//dirt tile with/out snow on it
                    {
                        if (Utility.Runif() < TerrainEditorView.map_human_friendly)//dirt with snow on
                        {
                            Tiles[x + z * m_NumTilesWideX].Type_Precedence = TileTypePrecedence.Dirt;
                        //    Tiles[x + z * m_NumTilesWideX].index_into_unique_tile_textures = Dirt_indices[0];//MF->Utility.DUnif(0,dirt_count-1)];
                                                                                                             //add forests/jungle
                            if (Utility.Runif() < TerrainEditorView.map_forestry)//add a forest tile
                                Tiles[x + z * m_NumTilesWideX].removable_terrain_property = 3;//snow forest
                            else
                                if (Tiles[x + z * m_NumTilesWideX].is_hill)
                                Tiles[x + z * m_NumTilesWideX].has_snow = (Utility.Runif() < 0.8f);
                            else
                                Tiles[x + z * m_NumTilesWideX].has_snow = (Utility.Runif() < 0.5f);
                        }
                        else
                        {
                            Tiles[x + z * m_NumTilesWideX].Type_Precedence = TileTypePrecedence.Snow;
                        //    Tiles[x + z * m_NumTilesWideX].index_into_unique_tile_textures = Glacier_indices[0];//MF->Utility.DUnif(0,glacier_count-1)];
                            if (Tiles[x + z * m_NumTilesWideX].is_hill)
                                Tiles[x + z * m_NumTilesWideX].has_dirt = (Utility.Runif() < 0.9f);
                            else
                                Tiles[x + z * m_NumTilesWideX].has_dirt = (Utility.Runif() < 0.5f);
                        }
                    }
                }
                else
                   if (Utility.Runif() < TerrainEditorView.map_wet_or_dry)
                {
                    if (Utility.Runif() < TerrainEditorView.map_wet_or_dry)//wet tile, leads to wetlands or drylands
                    {
                        if (Utility.Runif() < TerrainEditorView.map_human_friendly)//grassland
                        {
                            //choose between dirt and grass
                            if (Utility.Runif() < 0.5f)
                            {
                                Tiles[x + z * m_NumTilesWideX].Type_Precedence = TileTypePrecedence.Grassland;//grass
                             //   Tiles[x + z * m_NumTilesWideX].index_into_unique_tile_textures = Grass_indices[0];//MF->Utility.DUnif(0,grass_count-1)];
                                if (Tiles[x + z * m_NumTilesWideX].is_hill)
                                    Tiles[x + z * m_NumTilesWideX].has_dirt = (Utility.Runif() < 0.9f);
                                else
                                    Tiles[x + z * m_NumTilesWideX].has_dirt = (Utility.Runif() < 0.5f);
                            }
                            else
                            {
                                Tiles[x + z * m_NumTilesWideX].Type_Precedence = TileTypePrecedence.Dirt;//dirt
                              //  Tiles[x + z * m_NumTilesWideX].index_into_unique_tile_textures = Dirt_indices[0];//MF->Utility.DUnif(0,dirt_count-1)];
                                if (Tiles[x + z * m_NumTilesWideX].is_hill)
                                    Tiles[x + z * m_NumTilesWideX].has_grass = (Utility.Runif() < 0.9f);
                                else
                                    Tiles[x + z * m_NumTilesWideX].has_grass = (Utility.Runif() < 0.5f);
                            }
                            //cant have grass around desert
                            if (Tiles[x + z * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Grassland)
                            {
                                //cant have grassland around snow
                                if (Tiles[left + z * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Snow
                                    || Tiles[left + top * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Snow
                                    || Tiles[left + bottom * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Snow
                                    || Tiles[x + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Snow
                                    || Tiles[x + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Snow
                                    || Tiles[right + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Snow
                                    || Tiles[right + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Snow
                                    || Tiles[right + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Snow)
                                    goto restart_tile_generation;
                                if (Tiles[left + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Desert
                                    || Tiles[left + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Desert
                                    || Tiles[left + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Desert
                                    || Tiles[x + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Desert
                                    || Tiles[x + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Desert
                                    || Tiles[right + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Desert
                                    || Tiles[right + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Desert
                                    || Tiles[right + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Desert)
                                    goto restart_tile_generation;//*
                            }
                            //add forests/jungle
                            if (Utility.Runif() < TerrainEditorView.map_forestry)//add a forest tile
                                Tiles[x + z * m_NumTilesWideX].removable_terrain_property = 2;//forest
                        }
                        else//swamp,marsh
                        {
                            if (Tiles[x + z * m_NumTilesWideX].is_hill) goto restart_tile_generation;
                            //cant have swamp around snow
                            if (Tiles[left + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[left + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[left + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[x + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[x + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[right + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[right + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[right + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Snow)
                                goto restart_tile_generation;
                            //cant have swamp around desert
                            if (Tiles[left + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[left + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[left + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[x + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[x + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[right + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[right + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[right + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Desert)
                                goto restart_tile_generation;
                            Tiles[x + z * m_NumTilesWideX].Type_Precedence = TileTypePrecedence.Swamp;
                           // Tiles[x + z * m_NumTilesWideX].index_into_unique_tile_textures = Marsh_indices[0];//MF->Utility.DUnif(0,marsh_count-1)];
                                                                                                              //add forests/jungle
                            if (Utility.Runif() < TerrainEditorView.map_forestry)//add a forest tile
                                Tiles[x + z * m_NumTilesWideX].removable_terrain_property = 2;//swamp forest
                        }
                    }
                    else//make jungle
                    {
                        //choose between dirt and grass
                        if (Utility.Runif() < 0.5f)
                        {
                            Tiles[x + z * m_NumTilesWideX].Type_Precedence = TileTypePrecedence.Grassland;//grass
                         //   Tiles[x + z * m_NumTilesWideX].index_into_unique_tile_textures = Grass_indices[0];//MF->Utility.DUnif(0,grass_count-1)];
                            if (Tiles[x + z * m_NumTilesWideX].is_hill)
                                Tiles[x + z * m_NumTilesWideX].has_dirt = (Utility.Runif() < 0.9f);
                            else
                                Tiles[x + z * m_NumTilesWideX].has_dirt = (Utility.Runif() < 0.5f);
                        }
                        else
                        {
                            Tiles[x + z * m_NumTilesWideX].Type_Precedence = TileTypePrecedence.Dirt;//dirt
                          //  Tiles[x + z * m_NumTilesWideX].index_into_unique_tile_textures = Dirt_indices[0];//MF->Utility.DUnif(0,dirt_count-1)];
                            if (Tiles[x + z * m_NumTilesWideX].is_hill)
                                Tiles[x + z * m_NumTilesWideX].has_grass = (Utility.Runif() < 0.9f);
                            else
                                Tiles[x + z * m_NumTilesWideX].has_grass = (Utility.Runif() < 0.5f);
                        }
                        if (Tiles[x + z * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Grassland)
                        {
                            //cant have grassland around snow
                            if (Tiles[left + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[left + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[left + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[x + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[x + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[right + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[right + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[right + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Snow)
                                goto restart_tile_generation;
                            //cant have grass around desert
                            if (Tiles[left + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[left + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[left + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[x + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[x + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[right + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[right + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[right + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Desert)
                                goto restart_tile_generation;
                        }
                        //add forests/jungle
                        if (Utility.Runif() < TerrainEditorView.map_forestry)//add a forest tile
                            Tiles[x + z * m_NumTilesWideX].removable_terrain_property = 1;//jungle
                    }
                }
                else
                {
                    if (Utility.Runif() < TerrainEditorView.map_wet_or_dry)//wet tile, leads to wetlands or drylands
                    {
                        if (Utility.Runif() < TerrainEditorView.map_human_friendly)//grassland
                        {
                            //choose between dirt and grass
                            if (Utility.Runif() < 0.5f)
                            {
                                Tiles[x + z * m_NumTilesWideX].Type_Precedence = TileTypePrecedence.Grassland;//grass
                          //      Tiles[x + z * m_NumTilesWideX].index_into_unique_tile_textures = Grass_indices[0];//MF->Utility.DUnif(0,grass_count-1)];
                                if (Tiles[x + z * m_NumTilesWideX].is_hill)
                                    Tiles[x + z * m_NumTilesWideX].has_dirt = (Utility.Runif() < 0.9f);
                                else
                                    Tiles[x + z * m_NumTilesWideX].has_dirt = (Utility.Runif() < 0.5f);
                            }
                            else
                            {
                                Tiles[x + z * m_NumTilesWideX].Type_Precedence = TileTypePrecedence.Dirt;//dirt
                            //    Tiles[x + z * m_NumTilesWideX].index_into_unique_tile_textures = Dirt_indices[0];//MF->Utility.DUnif(0,dirt_count-1)];
                                if (Tiles[x + z * m_NumTilesWideX].is_hill)
                                    Tiles[x + z * m_NumTilesWideX].has_grass = (Utility.Runif() < 0.9f);
                                else
                                    Tiles[x + z * m_NumTilesWideX].has_grass = (Utility.Runif() < 0.5f);
                            }
                            if (Tiles[x + z * m_NumTilesWideX].Type_Precedence == TileTypePrecedence.Grassland)
                            {
                                //cant have grassland around snow
                                if (Tiles[left + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Snow
                                    || Tiles[left + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Snow
                                    || Tiles[left + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Snow
                                    || Tiles[x + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Snow
                                    || Tiles[x + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Snow
                                    || Tiles[right + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Snow
                                    || Tiles[right + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Snow
                                    || Tiles[right + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Snow)
                                    goto restart_tile_generation;
                                //cant have grass around desert
                                if (Tiles[left + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Desert
                                    || Tiles[left + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Desert
                                    || Tiles[left + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Desert
                                    || Tiles[x + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Desert
                                    || Tiles[x + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Desert
                                    || Tiles[right + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Desert
                                    || Tiles[right + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Desert
                                    || Tiles[right + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Desert)
                                    goto restart_tile_generation;
                            }
                            if (Utility.Runif() < TerrainEditorView.map_forestry)//add a forest tile
                                Tiles[x + z * m_NumTilesWideX].removable_terrain_property = 1;//jungle
                        }
                        else//swamp,marsh
                        {
                            if (Tiles[x + z * m_NumTilesWideX].is_hill) goto restart_tile_generation;
                            //cant have swamp around snow
                            if (Tiles[left + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[left + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[left + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[x + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[x + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[right + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[right + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Snow
                                || Tiles[right + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Snow)
                                goto restart_tile_generation;
                            //cant have swamp around desert
                            if (Tiles[left + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[left + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[left + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[x + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[x + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[right + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[right + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Desert
                                || Tiles[right + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Desert)
                                goto restart_tile_generation;
                            Tiles[x + z * m_NumTilesWideX].Type_Precedence = TileTypePrecedence.Swamp;
                           // Tiles[x + z * m_NumTilesWideX].index_into_unique_tile_textures = Marsh_indices[0];//MF->Utility.DUnif(0,marsh_count-1)];
                            Tiles[x + z * m_NumTilesWideX].has_dirt = (Utility.Runif() < 0.5f);
                        }
                        //Mountain=0>Snow=1>Swamp=2>Desert=3>Grassland=4>Dirt=5>beach=6>Water=7
                    }
                    else//dry, desert tiles, oasis
                    {
                        //			if(1.0*z<(0.5-desertbound)*numoftiles_side || 1.0*z>(0.5+desertbound)*numoftiles_side)goto restart_tile_generation;
                        //cant have desert around swamp
                        if (Tiles[left + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Swamp || Tiles[left + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Swamp
                            || Tiles[left + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Swamp || Tiles[x + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Swamp
                            || Tiles[x + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Swamp || Tiles[right + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Swamp
                            || Tiles[right + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Swamp || Tiles[right + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Swamp)
                            goto restart_tile_generation;
                        //cant have desert around snow
                        if (Tiles[left + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Snow || Tiles[left + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Snow
                            || Tiles[left + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Snow || Tiles[x + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Snow
                            || Tiles[x + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Snow || Tiles[right + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Snow
                            || Tiles[right + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Snow || Tiles[right + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Snow)
                            goto restart_tile_generation;
                        //cant have desert around grass
                        if (Tiles[left + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Grassland || Tiles[left + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Grassland
                            || Tiles[left + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Grassland || Tiles[x + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Grassland
                            || Tiles[x + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Grassland || Tiles[right + m_NumTilesWideX * z].Type_Precedence == TileTypePrecedence.Grassland
                            || Tiles[right + m_NumTilesWideX * top].Type_Precedence == TileTypePrecedence.Grassland || Tiles[right + m_NumTilesWideX * bottom].Type_Precedence == TileTypePrecedence.Grassland)
                            goto restart_tile_generation;
                        Tiles[x + z * m_NumTilesWideX].Type_Precedence = TileTypePrecedence.Desert;
                        if (Tiles[x + z * m_NumTilesWideX].is_hill)
                            Tiles[x + z * m_NumTilesWideX].has_dirt = (Utility.Runif() < 0.9f);
                        else
                            Tiles[x + z * m_NumTilesWideX].has_dirt = (Utility.Runif() < 0.5f);
                        if (Utility.Runif() < TerrainEditorView.map_human_friendly)//oasis
                        {
                            Tiles[x + z * m_NumTilesWideX].removable_terrain_property = 4;//oasis
                        //    Tiles[x + z * m_NumTilesWideX].index_into_unique_tile_textures = Desert_indices[0];//MF->Utility.DUnif(0,desert_count-1)];
                        }
                       // else//desert
                       //     Tiles[x + z * m_NumTilesWideX].index_into_unique_tile_textures = Desert_indices[0];//MF->Utility.DUnif(0,desert_count-1)];
                    }
                }
            }
            //Mountain=0>Snow=1>Swamp=2>Desert=3>Grassland=4>Dirt=5>beach=6>Water=7
            //add rivers
            if (Utility.Runif() < TerrainEditorView.map_rivers)//river tile
                Tiles[x + z * m_NumTilesWideX].has_river = true;
            else//no river
                Tiles[x + z * m_NumTilesWideX].has_river = false;
            //add goods
            if (Utility.Runif() < TerrainEditorView.map_goods_more_or_less)//goods tile
                Tiles[x + z * m_NumTilesWideX].Goods = (TileTypeGoods)Utility.DUnif(1, (int)TileTypeGoods.MaxValue - 1);
            else//no goods
                Tiles[x + z * m_NumTilesWideX].Goods = TileTypeGoods.NONE;
        }

        /// <summary>
        /// Builds Tile properties based on rules. Precedence is
        /// Jungle>Forest>Mountain>Hill>Swamp>Desert>Grassland>Water
        /// </summary>
        
        public static void Randomize_Terrain_BasedOnRules()
        {
            //Terrain Precedence
            //By precedence, I mean that when two different terrain types meet, 
            //one of them invariably "overlaps" the other. 
            //(listed highest to lowest): jungle, forest, mountain, hill, swamp, deserts, grassland, water 
            //(open water or river). Please note that this precedence does not reflect the relative 
            //elevations of the terrain but is instead based on which terrains looks best when 
            //overlapping other terrains.
            //Jungle>Forest>Mountain>Hill>Swamp>Desert>Grassland>Water
            //With this method drawing the map is now a 2-step process. 
            //For each map cell, the base terrain must be drawn, and then any transitions 
            //that overlay it in reverse order of precedence (lowest precedence drawn first). 
            //A quick example: To calculate the transitions needed for a hill terrain, you need only consider 
            //any adjacent jungles, forests, and mountains, since those are the only terrain types 
            //that have a higher precedence.
            if (Tiles != null)
            {
                Tiles.Clear();
            }
            Tiles = new List<WorldTile>();
            int x, z;
            Generate_LandSeaNeighborhoods();
            for (z = 0; z < m_NumTilesHighZ; z++)
            {
                for (x = 0; x < m_NumTilesWideX; x++)
                {
                    if (Tiles[x+z* m_NumTilesWideX].type == 0)//land tile
                    {
                        Tiles[x+z* m_NumTilesWideX].has_dirt = false;
                        Tiles[x+z* m_NumTilesWideX].has_grass = false;
                        Tiles[x+z* m_NumTilesWideX].has_snow = false;
                        Tiles[x+z* m_NumTilesWideX].has_river = false;
                        if (Utility.Runif() < TerrainEditorView.map_plains_or_highlands)//make hill
                        {
                            Tiles[x+z* m_NumTilesWideX].is_hill = true;
                            Tiles[x+z* m_NumTilesWideX].Random_ScaleY = Utility.Runif(0.9f, 1.1f);
                            Tiles[x+z* m_NumTilesWideX].Random_Shiftx = Utility.Runif(-1.0f, 1.0f);
                            Tiles[x+z* m_NumTilesWideX].Random_Shiftz = Utility.Runif(-1.0f, 1.0f);
                            Tiles[x+z* m_NumTilesWideX].Random_RotY =//Utility.DUnif(0,3);//
                                (int)MathF.Floor(2*3.14f* Utility.DUnif(0, 179));
                            if (Utility.Runif() < TerrainEditorView.map_mountain_or_hill)//make mountain
                            {
                               // Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_textures = Mountain_indices[5//0
                                                                                                //Utility.DUnif(0,mountain_count-1)
                            //    ];//11
                                Tiles[x+z* m_NumTilesWideX].Type_Precedence = 0;
                                //Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_meshes = Utility.DUnif(0, MAX_NUM_OF_UNIQUE_MOUNTAIN_MODELS - 1);
                            }
                            else
                            {
                              //  Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_meshes = 0;// Utility.DUnif(0, MAX_NUM_OF_UNIQUE_HILL_MODELS - 1);
                                Generate_LandTile(x, z);
                            }
                        }
                        else//flat
                        {
                            Tiles[x+z* m_NumTilesWideX].is_hill = false;
                          //  Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_meshes = -1;
                            Generate_LandTile(x, z);
                        }
                    }
                    else//sea tile
                    {
                        Tiles[x+z* m_NumTilesWideX].is_hill = false;
                        int top = z + 1, bottom = z - 1, left = x - 1, right = x + 1;
                        if (x == 0) left = m_NumTilesWideX - 1;
                        if (z == 0) bottom = m_NumTilesHighZ - 1;
                        if (z == m_NumTilesHighZ - 1) top = 0;
                        if (x == m_NumTilesWideX - 1) right = 0;
                        //surrounded by water
                        if (Tiles[left+ m_NumTilesWideX*top].type == 1 && Tiles[x + m_NumTilesWideX * top].type == 1 && Tiles[right + m_NumTilesWideX * top].type == 1 &&
                            Tiles[left + m_NumTilesWideX * z].type == 1 && Tiles[right + m_NumTilesWideX * z].type == 1 &&
                            Tiles[left + m_NumTilesWideX * bottom].type == 1 && Tiles[x + m_NumTilesWideX * bottom].type == 1 && Tiles[right + m_NumTilesWideX * bottom].type == 1)
                        {
                         //   Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_meshes = 0;//StartSea;
                            Tiles[x+z* m_NumTilesWideX].Type_Precedence = TileTypePrecedence.Water;//sea
                       ///     Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_textures = Water_indices[0];
                        }
                        else
                        {
                            //					Tiles[x+z* m_NumTilesWideX].type=2;
                          //  Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_textures = -100;//Beach_indices[0];
                        }
                    }
                }
            }

            for (z = 0; z < m_NumTilesHighZ; z++)
            {
                for (x = 0; x < m_NumTilesWideX; x++)
                {
                    //Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_textures_partial_version = 0;// Utility.DUnif(0, MAX_NUM_OF_TILE_VERSION - 1);
                    if (Tiles[x+z* m_NumTilesWideX].type == 0) Generate_UniformLandTile(x, z);
                /*    if (Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_textures == -100)//Beach_indices[0])
                    {
                        Tiles[x+z* m_NumTilesWideX].Type_Precedence = TileTypePrecedence.Beach;//beach
                        Tiles[x+z* m_NumTilesWideX].type = 2;//beach
                        Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_textures = Seabed_indices[2];//Utility.DUnif(0,water_count-1)];
                    }*/
                }
            }
            //NEED TO DO THIS PASS HERE
            for (z = 0; z < m_NumTilesHighZ; z++)
            {
                for (x = 0; x < m_NumTilesWideX; x++)
                {
                    if (Tiles[x+z* m_NumTilesWideX].type == 2)//beach
                    {
                     //   Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_meshes = Process_BeachTile(x, z, true);
                        Tiles[x+z* m_NumTilesWideX].Random_ScaleY = Utility.Runif(0.9f, 1.1f);
                        Tiles[x+z* m_NumTilesWideX].Random_Shiftx = Utility.Runif(1.0f, 10.0f);
                        Tiles[x+z* m_NumTilesWideX].Random_Shiftz = Utility.Runif(1.0f, 10.0f);
                        Tiles[x+z* m_NumTilesWideX].Random_RotY = Utility.DUnif(0, 3);//Utility.DUnif(1,180)+Utility.DUnif(0,179);
                    }
                }
            }

            //Generate_WorldTerrainEditorView.map_Texture();
        }

        public static int Process_BeachTile(int x, int z, bool corners)
        {
            //it comes in as "beach"
            int top = z + 1, bot = z - 1, left = x - 1, right = x + 1;
            if (x == 0) left = m_NumTilesWideX - 1;
            if (z == 0) bot = m_NumTilesHighZ - 1;
            if (z == m_NumTilesHighZ - 1) top = 0;
            if (x == m_NumTilesWideX - 1) right = 0;

            int[] small_boxes_active=new int[8];
            for (int i = 0; i < 8; i++) small_boxes_active[i] = 0;
            if (Tiles[left + m_NumTilesWideX * top].type == 0 || Tiles[left + m_NumTilesWideX * z].type == 0 || Tiles[x + m_NumTilesWideX * top].type == 0)
                small_boxes_active[0] = 1;
            if (Tiles[x + m_NumTilesWideX * top].type == 0)
                small_boxes_active[1] = 1;
            if (Tiles[x + m_NumTilesWideX * top].type == 0 || Tiles[right + m_NumTilesWideX * top].type == 0 || Tiles[right + m_NumTilesWideX * z].type == 0)
                small_boxes_active[2] = 1;
            if (Tiles[left + m_NumTilesWideX * z].type == 0)
                small_boxes_active[3] = 1;
            if (Tiles[right + m_NumTilesWideX * z].type == 0)
                small_boxes_active[4] = 1;
            if (Tiles[left + m_NumTilesWideX * z].type == 0 || Tiles[left + m_NumTilesWideX * bot].type == 0 || Tiles[x + m_NumTilesWideX * bot].type == 0)
                small_boxes_active[5] = 1;
            if (Tiles[x + m_NumTilesWideX * bot].type == 0)
                small_boxes_active[6] = 1;
            if (Tiles[x + m_NumTilesWideX * bot].type == 0 || Tiles[right + m_NumTilesWideX * bot].type == 0 || Tiles[right + m_NumTilesWideX * z].type == 0)
                small_boxes_active[7] = 1;
            int sum = 0;
            for (int i = 0; i < 8; i++)
                sum = sum + small_boxes_active[i] * (int)MathF.Pow(2, i);
            return sum;

        }

        public static void Generate_UniformLandTile(int x, int z)
        {
            if (Utility.Runif() < TerrainEditorView.map_uniform_or_random)//uniform
            {//find land around and set current
                int top = z + 1, bottom = z - 1, left = x - 1, right = x + 1;
                if (x == 0) left = m_NumTilesWideX - 1;
                if (z == 0) bottom = m_NumTilesHighZ - 1;
                if (z == m_NumTilesHighZ - 1) top = 0;
                if (x == m_NumTilesWideX - 1) right = 0;
                if (Tiles[x+z* m_NumTilesWideX].Type_Precedence == 0 || (int)Tiles[x+z* m_NumTilesWideX].Type_Precedence > 5
                    || Utility.Runif() > TerrainEditorView.map_uniform_or_random)
                {
                }
                else
                {
                    if (Utility.Runif() < 0.5f && (Tiles[left + m_NumTilesWideX * z].Type_Precedence > 0 && (int)Tiles[left + m_NumTilesWideX * z].Type_Precedence <= 5) && Utility.Runif() < TerrainEditorView.map_uniform_or_random)
                    {
                     //   Tiles[left + m_NumTilesWideX * z].index_into_unique_tile_textures = Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_textures;
                        Tiles[left + m_NumTilesWideX * z].is_hill = Tiles[x+z* m_NumTilesWideX].is_hill;
                        Tiles[left + m_NumTilesWideX * z].Type_Precedence = Tiles[x+z* m_NumTilesWideX].Type_Precedence;
                      //  Tiles[left + m_NumTilesWideX * z].index_into_unique_tile_textures_partial_version = Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_textures_partial_version;
                      //  Tiles[left + m_NumTilesWideX * z].index_into_unique_tile_meshes = Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_meshes;
                    }
                    if (Utility.Runif() < 0.5f && (Tiles[right + m_NumTilesWideX * z].Type_Precedence > 0 && (int)Tiles[right + m_NumTilesWideX * z].Type_Precedence <= 5) && Utility.Runif() < TerrainEditorView.map_uniform_or_random)
                    {
                     //   Tiles[right + m_NumTilesWideX * z].index_into_unique_tile_textures = Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_textures;
                        Tiles[right + m_NumTilesWideX * z].is_hill = Tiles[x+z* m_NumTilesWideX].is_hill;
                        Tiles[right + m_NumTilesWideX * z].Type_Precedence = Tiles[x+z* m_NumTilesWideX].Type_Precedence;
                       // Tiles[right + m_NumTilesWideX * z].index_into_unique_tile_meshes = Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_meshes;
                      //  Tiles[right + m_NumTilesWideX * z].index_into_unique_tile_textures_partial_version = Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_textures_partial_version;
                    }
                    if (Utility.Runif() < 0.5f && (Tiles[left + m_NumTilesWideX * top].Type_Precedence > 0 && (int)Tiles[left + m_NumTilesWideX * top].Type_Precedence <= 5) && Utility.Runif() < TerrainEditorView.map_uniform_or_random)
                    {
                       // Tiles[left + m_NumTilesWideX * top].index_into_unique_tile_textures = Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_textures;
                        Tiles[left + m_NumTilesWideX * top].is_hill = Tiles[x+z* m_NumTilesWideX].is_hill;
                        Tiles[left + m_NumTilesWideX * top].Type_Precedence = Tiles[x+z* m_NumTilesWideX].Type_Precedence;
                      //  Tiles[left + m_NumTilesWideX * top].index_into_unique_tile_meshes = Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_meshes;
                       // Tiles[left + m_NumTilesWideX * top].index_into_unique_tile_textures_partial_version =   Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_textures_partial_version;
                    }
                    if (Utility.Runif() < 0.5f && (Tiles[x + m_NumTilesWideX * top].Type_Precedence > 0 && (int)Tiles[x + m_NumTilesWideX * top].Type_Precedence <= 5) && Utility.Runif() < TerrainEditorView.map_uniform_or_random)
                    {
                       // Tiles[x + m_NumTilesWideX * top].index_into_unique_tile_textures = Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_textures;
                        Tiles[x + m_NumTilesWideX * top].is_hill = Tiles[x+z* m_NumTilesWideX].is_hill;
                        Tiles[x + m_NumTilesWideX * top].Type_Precedence = Tiles[x+z* m_NumTilesWideX].Type_Precedence;
                      //  Tiles[x + m_NumTilesWideX * top].index_into_unique_tile_meshes = Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_meshes;
                      //  Tiles[x + m_NumTilesWideX * top].index_into_unique_tile_textures_partial_version = Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_textures_partial_version;
                    }
                    if (Utility.Runif() < 0.5f && (Tiles[right + m_NumTilesWideX * top].Type_Precedence > 0 && (int)Tiles[right+ m_NumTilesWideX *top].Type_Precedence <= 5) && Utility.Runif() < TerrainEditorView.map_uniform_or_random)
                    {
                     //   Tiles[right + m_NumTilesWideX * top].index_into_unique_tile_textures = Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_textures;
                        Tiles[right + m_NumTilesWideX * top].is_hill = Tiles[x+z* m_NumTilesWideX].is_hill;
                        Tiles[right + m_NumTilesWideX * top].Type_Precedence = Tiles[x+z* m_NumTilesWideX].Type_Precedence;
                     //   Tiles[right+ m_NumTilesWideX *top].index_into_unique_tile_meshes = Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_meshes;
                     //   Tiles[right+ m_NumTilesWideX *top].index_into_unique_tile_textures_partial_version = Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_textures_partial_version;
                    }
                    if (Utility.Runif() < 0.5f && (Tiles[left+ m_NumTilesWideX *bottom].Type_Precedence > 0 && (int)Tiles[left+ m_NumTilesWideX *bottom].Type_Precedence <= 5) && Utility.Runif() < TerrainEditorView.map_uniform_or_random)
                    {
                       // Tiles[left+ m_NumTilesWideX *bottom].index_into_unique_tile_textures = Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_textures;
                        Tiles[left+ m_NumTilesWideX *bottom].is_hill = Tiles[x+z* m_NumTilesWideX].is_hill;
                        Tiles[left+ m_NumTilesWideX *bottom].Type_Precedence = Tiles[x+z* m_NumTilesWideX].Type_Precedence;
                       // Tiles[left+ m_NumTilesWideX *bottom].index_into_unique_tile_meshes = Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_meshes;
                      //  Tiles[left+ m_NumTilesWideX *bottom].index_into_unique_tile_textures_partial_version = Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_textures_partial_version;
                    }
                    if (Utility.Runif() < 0.5f && (Tiles[x+ m_NumTilesWideX *bottom].Type_Precedence > 0 && (int)Tiles[x+ m_NumTilesWideX *bottom].Type_Precedence <= 5) && Utility.Runif() < TerrainEditorView.map_uniform_or_random)
                    {
                     //   Tiles[x+ m_NumTilesWideX *bottom].index_into_unique_tile_textures = Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_textures;
                        Tiles[x+ m_NumTilesWideX *bottom].is_hill = Tiles[x+z* m_NumTilesWideX].is_hill;
                        Tiles[x+ m_NumTilesWideX *bottom].Type_Precedence = Tiles[x+z* m_NumTilesWideX].Type_Precedence;
                      //  Tiles[x+ m_NumTilesWideX *bottom].index_into_unique_tile_meshes = Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_meshes;
                      //  Tiles[x+ m_NumTilesWideX *bottom].index_into_unique_tile_textures_partial_version = Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_textures_partial_version;
                    }
                    if (Utility.Runif() < 0.5f && (Tiles[right+ m_NumTilesWideX *bottom].Type_Precedence > 0 && (int)Tiles[right+ m_NumTilesWideX *bottom].Type_Precedence <= 5) && Utility.Runif() < TerrainEditorView.map_uniform_or_random)
                    {
                      //  Tiles[right+ m_NumTilesWideX *bottom].index_into_unique_tile_textures = Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_textures;
                        Tiles[right+ m_NumTilesWideX *bottom].is_hill = Tiles[x+z* m_NumTilesWideX].is_hill;
                        Tiles[right+ m_NumTilesWideX *bottom].Type_Precedence = Tiles[x+z* m_NumTilesWideX].Type_Precedence;
                       // Tiles[right+ m_NumTilesWideX *bottom].index_into_unique_tile_meshes = Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_meshes;
                      //  Tiles[right+ m_NumTilesWideX *bottom].index_into_unique_tile_textures_partial_version = Tiles[x+z* m_NumTilesWideX].index_into_unique_tile_textures_partial_version;
                    }
                }
            }
        }
    };
}
