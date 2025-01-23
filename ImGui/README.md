StrideCommunity.ImGuiDebug
=====

Bare-bone implementation of ImGui and a couple of debug tools for Stride

![](https://user-images.githubusercontent.com/5742236/55237373-563a1400-5232-11e9-8c24-beeaf127c0ac.png)

### How to:
* Add this repo as a submodule of your game's repo.
* Add a project reference pointing to this project inside your game's .csproj.
* Reference ImGui.NET's nuget package in your game's project, see below.
	* Optional: ``SetTargetFramework`` will specify the target framework when building, setting it to Core 2.0+ or Framework 4.8+ will allow you to monitor heap allocations within your samples, if you don't need that you can safely remove that line.
```xml
<ProjectReference Include="..\StrideCommunity.ImGuiDebug\StrideCommunity.ImGuiDebug.csproj" >
	<SetTargetFramework>TargetFramework=--yourDotNetFrameworkHere--</SetTargetFramework>
</ProjectReference>
```
* Start ImGui within your game's BeginRun():
```cs
using StrideCommunity.ImGuiDebug;
protected override void BeginRun()
{
    base.BeginRun();
    new ImGuiSystem( Services, GraphicsDeviceManager );
}
```

Builtin Debug interfaces:
```cs
new HierarchyView( Services );
new PerfMonitor( Services );
Inspector.FindFreeInspector( Services ).Target = objectToInspect;
```

Example interface implementation:
```cs
using System.Numerics;
using static ImGuiNET.ImGui;
using static StrideCommunity.ImGuiDebug.ImGuiExtension;

public class YourInterface : StrideCommunity.ImGuiDebug.BaseWindow
{
    bool my_tool_active;
    Vector4 my_color;
    float[] my_values = { 0.2f, 0.1f, 1.0f, 0.5f, 0.9f, 2.2f };

    public YourInterface( Stride.Core.IServiceRegistry services ) : base( services )
    {
    }

    protected override void OnDestroy()
    {
    }

    protected override void OnDraw( bool collapsed )
    {
        if( collapsed )
            return;

        if( BeginMenuBar() )
        {
            if( BeginMenu( "File" ) )
            {
                if( MenuItem( "Open..", "Ctrl+O" ) ) { /* Do stuff */ }
                if( MenuItem( "Save", "Ctrl+S" ) ) { /* Do stuff */ }
                if( MenuItem( "Close", "Ctrl+W" ) ) { my_tool_active = false; }
                EndMenu();
            }
            EndMenuBar();
        }

        // Edit a color (stored as ~4 floats)
        ColorEdit4( "Color", ref my_color );

        // Plot some values
        PlotLines( "Frame Times", ref my_values[ 0 ], my_values.Length );

        // Display contents in a scrolling region
        TextColored( new Vector4( 1, 1, 0, 1 ), "Important Stuff" );
        using( Child() )
        {
            for( int n = 0; n < 50; n++ )
                Text( $"{n}: Some text" );
        }
    }
}

```

Credits
-------
[Profan's contribution](https://github.com/profan/dear-xenko)
[jazzay's contribution](https://github.com/jazzay/Xenko.Extensions#xenkoimgui)
[ImGui.NET](https://github.com/mellinoe/ImGui.NET)
[Dear ImGui](https://github.com/ocornut/imgui)
