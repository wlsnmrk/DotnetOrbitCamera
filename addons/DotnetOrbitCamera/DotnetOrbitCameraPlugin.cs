#if TOOLS
using Godot;
using System;

namespace DotnetOrbitCamera
{
	[Tool]
	public partial class DotnetOrbitCameraPlugin : EditorPlugin
	{
		public override void _EnterTree()
		{
			var script = GD.Load<CSharpScript>("res://addons/DotnetOrbitCamera/DotnetOrbitCamera.cs");
			var icon = GD.Load<Texture2D>("res://addons/DotnetOrbitCamera/DotnetOrbitCamera.svg");
			AddCustomType("DotnetOrbitCamera", "Camera3D", script, icon);
		}

		public override void _ExitTree()
		{
			RemoveCustomType("DotnetOrbitCamera");
		}
	}
}
#endif
