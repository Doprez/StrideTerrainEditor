Stride Community Terrain Editor by Idomeneas
=====

Stride Community Engine Editor for terrain creation and manipulation, and Texture blending/manipulation. Includes my Perlin noise functions, extensions to the Heightmap, Texture and Color[] classes and much much more.

![Stride Engine Editor](Intro1.png?raw=true "Stride Engine Editor")

### Getting Started:
* Download the source and extract somewhere. 
* The Stride version used is 4.1. Make sure you open the project in the Stride Studio under that version. Unfortunately,
  version 4.2 seems to introduce some problems that I wasn't able to solve. So upgrade to 4.2 at your own risk.
* If you have trouble with the project compiling in Visual Studio (VS), clean the solution and rebuild.
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
![Terrain tab](Terraintab.png?raw=true "Terrain tab")

A lot of the magic happens here. Note the following:
* Depending on what you are saving (hitting a "Save" button), an appropriate Filename is chosen and the corresponding file is overwritten.
* All files involved with the current terrain are saved in the Resources/TerrainEditor/ folder. Click the SaveAs buttons (not shown in the picks here) to save them elsewhere.
* The "New" button creates a new terrain mesh with the parameters you see on this tab. Weight and Property textures are reset in this case. Max terrain size is 1024x1024 but you can change that to suit your needs if you want.
* The blended texture is important, and is utilized when rendering the terrain mesh object in game.

### Vegetation tab:
![Vegetation tab](Vegetation.png?raw=true "Vegetation tab")

Please note the following:
* Currently, trees are not instanced. An entity is created for each tree and the max is set to 10k. There is a hit on FPS depending on how many are in the frustum.
* The combined texture contains the following information: at each pixel location, the Red channel indicates a tree location, the Green channel indicates a road (aka like navigation), and the Blue channel indicates a collision. The latter is checked in game as the player moves about the terrain, to see on which vertices can they walk/jump to. The road is used like a navigation mess.
* Make sure you change the values of the selection ball radius and strength in the terrain tab for finer/detailed combined texture and placement or trees, roads, and collisions.
* The combined texture (terrain properties) is important, and is utilized when rendering the terrain mesh object trees in game.


### Credits:
* The Stride community for all their discussions on discord and their great project contributions. I studied a lot of the existing projects as I was working on the editor. Special thanks and mention to the following members of the community: Simba (@Doprez), alex.mkIII, Nicogo, Kanter, Manio143, Joreyk, Vaso, Tebjan, and Eideren for answering all my questions on discord. Apologies if I forgot anyone!
* Any .cs file gives credit at the top of the file.
* Some of these projects deserve special mention:
  * Eideren's ![StrideCommunity.ImGuiDebug](https://github.com/Eideren/StrideCommunity.ImGuiDebug) . It is fully included in the terrain editor and was extended by yours trully to handle images and a message log.
  * johang88's stride terrain projects. Learned a lot from them. In particular, ![TR.Stride](https://github.com/johang88/TR.Stride) and ![StrideTerrain](https://github.com/johang88/StrideTerrain) .
  * Jeske's ![StrideWireframeShader](https://github.com/jeske/StrideWireframeShader)  is utilized in the editor.
  * Tebjan's ![StrideTransformationInstancing](https://github.com/tebjan/StrideTransformationInstancing) although not used gave me many ideas on how to setup my model instancing as needed.
  * Profan's ![XenkoByteSized](https://github.com/profan/XenkoByteSized) , one of the best projects to start studying stride and its capabilities.
  * Joreyk (IXLLEGACYIXL) and Doprez ![NexStandard/Terrain1](https://github.com/NexStandard/Terrain1) which help a lot with showing how to change vertices on the GPU directly! Take care to get this kind of thing right or you can mess up your computer display!
