Stride Community Terrain Editor by Idomeneas
=====

Stride Community Engine Editor for terrain creation and manipulation, and Texture blending/manipulation. Includes my Perlin noise functions, extensions to the Heightmap, Texture and Color[] classes and much much more.

![Stride Engine Editor](Intro1.png?raw=true "Stride Engine Editor")

### Getting Started:
* Download the source and extract somewhere. 
* The Stride version used is 4.1. Make sure you open the project in the Stride Studio under that version. Unfortunately,
version 4.2 seems to introduce some problems that I wasn't able to solve (or maybe it was just me...). So upgrading to 4.2 might cause issues, in particular with shader automatic compilation (the package Stride.Core.Assets.CompilerApp is not working for me in version 4.2).
* If you have trouble with the project compiling in Visual Studio (VS), clean the solution and rebuild (a few times).
* Make sure you open the project with the Stride editor and Stride version 4.1. If an item gives an error that it is not found, note the location and fix any broken references.
* If all goes well, open the editor either through Stride Game Studio or from VS.
* Note the following:
  * There are default settings for many parameters of the Editor GUI that can be changed in the file ![TerrainEditorView.cs](https://github.com/Idomeneas1970/Stride-Terrain-Editor/blob/main/StrideTerrainEditor/TerrainEditorView.cs) .
  * Default camera settings can be changed in the file ![BasicCameraController.cs](https://github.com/Idomeneas1970/Stride-Terrain-Editor/blob/main/StrideTerrainEditor/BasicCameraController.cs) .
  * Basic Keyboard Navigation help is displayed on the top-left. Can disable at any time from the "Main Menu".
    ![Key Navigation](Intro2.png?raw=true "Key Navigation")
  * Single clicking anywhere in the terrain mess will move the current terrain point selected (indicated by a red cube) to that point. Only if you have left-shift pressed can you affect the terrain; either its vertices or texture weights.
  * In many places of the GUI you can see a question mark (?). Hover over it to get more info about the GUI item.
  * Single clicking any of the 8 texture images will switch the mode Editor Mode to "Paint Textures".
  * Clicking any "Apply" button will switch automatically to the corresponding mode. For example, clicking the apply button under the Heightmap will switch mode to "Edit Locations".
  * Certain methods are not used, but I left them there so you can see what gives issues. I spent almost two years on and off trying to figure things out, maybe these will save you some time and trouble.
  
### Render Info tab:
![Render Info tab](RenderInfo.png?raw=true "Render Info tab")

Here you will find all sorts of basic options, from camera properties to basic light properties and change their values. Note the switches in the middle. Render Points uses my custom instancing render feature and shader to display a colored cube at all terrain mesh vertices with no hit on FPS.

### Randomization tab:
![Randomization tab](Randomization.png?raw=true "Randomization tab")

The values you see here affect all "Generate" type procedures (when you click any generate button).

### Messages tab:
![Messages tab](Messages.png?raw=true "Messages tab")

Any important messages after some terrain operations can be found here.

### Terrain tab:
![World tab](Worldtab.png?raw=true "World tab")

You can build a whole world here, with an automated process. First make sure you have generated some unique tiles, then load 
them. After that you can either generate the world map or load it. The tiles are loaded asynchronously as you move about the world. This part of the editor shows you how you can build a basic Civ type game and a lot of the issues involved.

### Terrain tab:
![Terrain tab](Terraintab.png?raw=true "Terrain tab")

A lot of the magic happens here. Note the following:
* Depending on what you are saving (hitting a "Save" button), an appropriate Filename is chosen and the corresponding file is overwritten. So use Save As button to store the file elsewhere (not in the resource folder).
* All files involved with the current terrain are saved in the Resources/TerrainEditor/ folder. Click the SaveAs buttons to save them elsewhere.
* The "New" button creates a new terrain mesh with the parameters you see on this tab. Weight and Property textures are reset in this case. Max terrain size is 1024x1024 but you can change that to suit your needs if you want.
* The blended texture is important, and is utilized when rendering the terrain mesh object in game.
* There are 3 types of shaders used to display terrain in editor, in game or in tiles mode, chosen via Display Mode: single texture, height based and multi blend.

### Area tab:
![Area tab](Areatab.png?raw=true "Area tab")

Please note the following:
* Currently, objects are not instanced. An entity is created for each object in the world. There is a hit on FPS depending on how many are in the frustum, so make sure you set the object visibility distance to what you need.
* Make sure you change the values of the selection ball radius and strength in the terrain tab for finer/detailed combined texture and placement or trees, roads, and collisions.
* You can load/save the created objects as an area!

### Image Manipulation tab:
![Image Manipulation tab](ImageManipulationtab.png?raw=true "Image Manipulation tab")

A great number of useful image utilities here, enjoy!

### Credits:
* The Stride community for all their discussions on discord and their great project contributions. I studied a lot of the existing projects as I was working on the editor. Special thanks and mention to the following members of the community: Simba (@Doprez), alex.mkIII, Nicogo, Kanter, Manio143, Joreyk, Vaso, Tebjan, and Eideren for answering all my questions on discord. Apologies if I forgot anyone!
* Any .cs file gives credit at the top of the file. If I forgot something it was not intentional, just let me know and will add you.
* Some of these projects deserve special mention:
  * Eideren's ![StrideCommunity.ImGuiDebug](https://github.com/Eideren/StrideCommunity.ImGuiDebug) . It is fully included in the terrain editor and was extended by yours trully to handle images and a message log.
  * johang88's stride terrain projects. Learned a lot from them. In particular, ![TR.Stride](https://github.com/johang88/TR.Stride) and ![StrideTerrain](https://github.com/johang88/StrideTerrain) .
  * Jeske's ![StrideWireframeShader](https://github.com/jeske/StrideWireframeShader)  is utilized in the editor.
  * Tebjan's ![StrideTransformationInstancing](https://github.com/tebjan/StrideTransformationInstancing) although not used gave me many ideas on how to setup my cube instancing as needed.
  * Profan's ![XenkoByteSized](https://github.com/profan/XenkoByteSized) , one of the best projects to start studying stride and its capabilities.
  * Joreyk (IXLLEGACYIXL) and Doprez ![NexStandard/Terrain1](https://github.com/NexStandard/Terrain1) which helped a lot with showing how to change vertices on the GPU directly! Take care to get this kind of thing right if you change the code I provided or you can mess up your computer display!
  * Tom Groner's shader from ![XenkoFlowingWater](https://github.com/TomGroner/XenkoFlowingWater) is used for water planes.
