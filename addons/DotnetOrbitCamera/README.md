# .NET Orbit Camera
An editor-style orbit camera for [Godot Engine 4](https://godotengine.org), written in C#. Useful for interfacing with other C# code in your project.

## Installation
You **must** have a .NET release of Godot. The standard release of the engine does not support C# scripts.

Additionally, you **must** build your project after adding this plugin, but before enabling the plugin in your project settings. Enabling it without building your project will not work.

1. Download [the latest release](https://github.com/wlsnmrk/godot-dotnet-orbit-camera/releases/latest) and follow the [instructions for moving the files into your Godot project](https://docs.godotengine.org/en/stable/tutorials/plugins/editor/installing_plugins.html), but _do not yet enable the plugin_.
1. Build your project.
1. Enable the plugin in your project settings.

## Usage
### Setup
Add a `DotnetOrbitCamera` node to your scene. A warning icon will appear to indicate the camera does not yet have a pivot object. Use the editor properties of the node to assign a pivot object, which the camera will orbit. (You can use an empty `Node3D` if you like.) Make sure the camera is not positioned straight up or down from the pivot object. The camera should automatically point at the pivot.

You can see an example scene using the camera in the `Examples/` directory.

## Editor Properties
* **Pivot**: The `Node3D` around which the camera orbits.
* **Move With Pivot**: If the pivot moves, move the camera exactly with it (keeping the same relative position). If false, the camera will attempt to stay within the limits defined by other properties, but will otherwise not move.
* **Minimum Elevation Angle**: Lower limit of angle from pivot to camera, above or below the pivot's Y-position, in degrees. Minimum value `-89`, maximum value **Maximum Elevation Angle**.
* **Maximum Elevation Angle**: Upper limit of angle from pivot to camera, above or below the pivot's Y-position, in degrees. Maximum value `89`, minimum value **Minimum Elevation Angle**.
* **Minimum Zoom Distance**: How close the camera can move to the pivot, in meters. Minimum `0.001`, maximum **Maximum Zoom Distance**.
* **Maximum Zoom Distance**: How far the camera can move from the pivot, in meters. Minimum **Minimum Zoom Distance**.
* **Pan Speed**: How quickly the camera pans. Minimum `0.001`.
* **Rotation Speed**: How quickly the camera orbits or spins. Minimum `0.001`.
* **Input Enabled**: Whether the camera responds to input.

## Input
When running your game, the camera will:
* Spin around the pivot object when the middle-mouse button is held and the mouse is moved.
* Pan (moving both camera and pivot object) when the `Shift` key is held along with the middle-mouse button and the mouse is moved.
    * Panning motion is constrained to the X-Z plane; the camera will not elevate during panning.
* Zoom in and out when the mouse wheel is turned (up and down, respectively).