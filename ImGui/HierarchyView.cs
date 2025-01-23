//original file by Eideren, https://github.com/Eideren/StrideCommunity.ImGuiDebug
//Idomeneas, Minor changes
namespace ImGui
{
    using System.Numerics;
    using System.Collections.Generic;
    using Guid = System.Guid;
    
    using Stride.Core;
    using Stride.Engine;
    using Stride.Games;

    using ImGuiNET;
    using static ImGuiNET.ImGui;
    using static ImGuiExtension;
    public class HierarchyView : BaseWindow
    {
        /// <summary>
        /// Based on hashcodes, it doesn't have to be exact, we just don't want to keep references from being collected
        /// </summary>
        HashSet<Guid> _recursingThrough = new HashSet<Guid>();
        List<IIdentifiable> _searchResult = new List<IIdentifiable>();
        string _searchTerm = "";
        public static bool show_HeirarchyGUI;
        const float DUMMY_WIDTH = 19;
        const float INDENTATION2 = DUMMY_WIDTH+8;
        protected override ImGuiWindowFlags WindowFlags => ImGuiWindowFlags.NoTitleBar;
        public HierarchyView( IServiceRegistry service ) : base( service ) { 
            show_HeirarchyGUI = false;
        }

   //     protected override Vector2? WindowPos => new Vector2(Game.GraphicsContext.CommandList.Viewport.Width / 2.0f, 55);
  //      protected override Vector2? WindowSize => _windowSize;
  //      Vector2? _windowSize = new Vector2(320f, 340f);

        public override void Update(GameTime gameTime)
        {
            if (!show_HeirarchyGUI) return;
            base.Update( gameTime );
        }

        protected override void OnDraw( bool collapsed )
        {
            if (!show_HeirarchyGUI) return;
            if( collapsed )
                return;
            
            if( InputText( "Search", ref _searchTerm, 64 ) )
            {
                _searchResult.Clear();
                if( System.String.IsNullOrWhiteSpace( _searchTerm ) == false )
                    RecursiveSearch( _searchResult, _searchTerm.ToLower(), Game.SceneSystem.SceneInstance.RootScene );
            }

            using( Child() )
            {
                foreach( IIdentifiable identifiable in _searchResult )
                {
                    RecursiveDrawing( identifiable );
                }

                if( _searchResult.Count > 0 )
                {
                    Spacing();
                    Spacing();
                }
                
                foreach( var child in EnumerateChildren( Game.SceneSystem.SceneInstance.RootScene ) )
                    RecursiveDrawing( child );
            }
        }
        
        void RecursiveSearch( List<IIdentifiable> result, string term, IIdentifiable source )
        {
            if( source == null )
                return;
            
            foreach( var child in EnumerateChildren( source ) )
            {
                RecursiveSearch( result, term, child );
            }
            
            string strLwr;
            if( source is Entity entity )
                strLwr = entity.Name.ToLower();
            else if( source is Scene scene )
                strLwr = scene.Name.ToLower();
            else 
                return;

            if( term.Contains( strLwr ) || strLwr.Contains( term ) )
                result.Add( source );
        }

        protected override void OnDestroy(){}
        
        void RecursiveDrawing( IIdentifiable source )
        {
            if( source == null )
                return;
            
            string label;
            bool canRecurse;
            {
                if( source is Entity entity )
                {
                    label = entity.Name;
                    canRecurse = entity.Transform.Children.Count > 0;
                }
                else if( source is Scene scene )
                {
                    label = scene.Name;
                    canRecurse = scene.Children.Count > 0 || scene.Entities.Count > 0;
                }
                else return;
            }

            using( ID( source.Id.GetHashCode() ) )
            {
                bool recursingThrough = _recursingThrough.Contains( source.Id );
                bool recurse = canRecurse && recursingThrough;
                if( canRecurse )
                {
                    if( ArrowButton( "", recurse ? ImGuiDir.Down : ImGuiDir.Right ) )
                    {
                        if( recurse )
                            _recursingThrough.Remove( source.Id );
                        else
                            _recursingThrough.Add( source.Id );
                    }
                }
                else
                    Dummy( new Vector2( DUMMY_WIDTH, 1 ) );
                SameLine();
                    
                if( Button( label ) )
                    Inspector.FindFreeInspector( Services ).Target = source;
                    
                using( UIndent( INDENTATION2 ) )
                {
                    if( recurse )
                    {
                        foreach( var child in EnumerateChildren( source ) )
                        {
                            RecursiveDrawing( child );
                        }
                    }
                }
            }
        }
        
        static IEnumerable<IIdentifiable> EnumerateChildren( IIdentifiable source )
        {
            if( source is Entity entity )
            {
                foreach( var child in entity.Transform.Children )
                    yield return child.Entity;
            }
            else if( source is Scene scene )
            {
                foreach( var childEntity in scene.Entities )
                    yield return childEntity;
                foreach( var childScene in scene.Children )
                    yield return childScene;
            }
        }
    }
}