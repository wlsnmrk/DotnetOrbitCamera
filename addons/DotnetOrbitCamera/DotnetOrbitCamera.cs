using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DotnetOrbitCamera
{
    [Tool]
    public partial class DotnetOrbitCamera : Camera3D
    {
        // Have to slow the mouse way down
        private const float _mousePanSpeedFactor = 0.01f;
        private const float _maxElevationRotationPerFrameDegrees = 180;
        private const float _zoomFactorIn = 0.8f;
        private const float _zoomFactorOut = 1.2f;
        // Limits for exported properties
        private const float _minElevationAngleMinimumDegrees = -89;
        private const float _maxElevationAngleMaximumDegrees = 89;
        private const float _minZoomMinimum = 0.001f;
        private const float _panSpeedMinimum = 0.001f;
        private const float _rotationSpeedMinimum = 0.001f;

        private Vector3 _lastPosition;
        private Vector3 _lastPivotPosition;

        private Node3D? _pivot;
        [Export]
        public Node3D? Pivot 
        { 
            get {  return _pivot; }
            set
            {
                _pivot = value;
                LookAtPivot();
                UpdateConfigurationWarnings();
            }
        }

        [Export]
        public bool MoveWithPivot { get; set; } = true;

        private float _minElevationAngleRads = Mathf.DegToRad(-89f);
        [Export]
        public float MinimumElevationAngle
        { 
            get { return Mathf.RadToDeg(_minElevationAngleRads); }
            set
            {
                value = Mathf.Wrap(value, -180, 180);
                if (value < _minElevationAngleMinimumDegrees)
                {
                    value = _minElevationAngleMinimumDegrees;
                }
                if (value > MaximumElevationAngle)
                {
                    value = MaximumElevationAngle;
                }
                _minElevationAngleRads = Mathf.DegToRad(value);
                if (MaximumElevationAngle < value)
                {
                    MaximumElevationAngle = value;
                }
                // Apply rotation limits
                RotateCamera(0, 0);
            }
        }

        private float _maxElevationAngleRads = Mathf.DegToRad(89f);
        [Export]
        public float MaximumElevationAngle
        {
            get { return Mathf.RadToDeg(_maxElevationAngleRads); }
            set
            {
                value = Mathf.Wrap(value, -180, 180);
                if (value > _maxElevationAngleMaximumDegrees)
                {
                    value = _maxElevationAngleMaximumDegrees;
                }
                if (value < MinimumElevationAngle)
                {
                    value = MinimumElevationAngle;
                }
                _maxElevationAngleRads = Mathf.DegToRad(value);
                if (MinimumElevationAngle > value)
                {
                    MinimumElevationAngle = value;
                }
                // Apply rotation limits
                RotateCamera(0, 0);
            }
        }

        private float _minZoomDistance = 0.1f;
        [Export]
        public float MinimumZoomDistance 
        { 
            get { return _minZoomDistance; }
            set
            {
                if (value < _minZoomMinimum)
                {
                    value = _minZoomMinimum;
                }
                _minZoomDistance = value;
                if (MaximumZoomDistance < _minZoomDistance)
                {
                    MaximumZoomDistance = _minZoomDistance;
                }
                // Enforce zoom bounds but otherwise leave camera where it is
                ZoomCamera(1.0f);
            }
        }

        private float _maxZoomDistance = 1000f;
        [Export]
        public float MaximumZoomDistance 
        { 
            get { return _maxZoomDistance; }
            set
            {
                if (value < MinimumZoomDistance)
                {
                    value = MinimumZoomDistance;
                }
                _maxZoomDistance = value;
                // Enforce zoom bounds but otherwise leave camera where it is
                ZoomCamera(1.0f);
            }
        }

        private float _panSpeed = 1f;
        [Export]
        public float PanSpeed
        {
            get { return _panSpeed; }
            set
            {
                if (value < _panSpeedMinimum)
                {
                    value = _panSpeedMinimum;
                }
                _panSpeed = value;
            }
        }

        private float _rotationSpeed = 1f;
        [Export]
        public float RotationSpeed
        {
            get { return _rotationSpeed; }
            set
            {
                if (value < _rotationSpeedMinimum)
                {
                    value = _rotationSpeedMinimum;
                }
                _rotationSpeed = value;
            }
        }

        [Export]
        public bool InputEnabled { get; set; } = true;

        public override void _Ready()
        {
            LookAtPivot();
            UpdateConfigurationWarnings();
        }

        public override void _Process(double delta)
        {
            if (Pivot == null && !Engine.IsEditorHint())
            {
                throw new InvalidOperationException("No pivot node for orbit camera (did you forget to set one?)");
            }
            if (Pivot != null)
            {
                var needsUpdate = false;
                if (MoveWithPivot && Pivot.GlobalPosition != _lastPivotPosition)
                {
                    var oldRelPos = GetLastPivotRelativePosition();
                    GlobalPosition = Pivot.GlobalPosition + oldRelPos;
                    needsUpdate = true;
                }
                else if (Pivot.GlobalPosition != _lastPivotPosition || GlobalPosition != _lastPosition)
                {
                    needsUpdate = true;
                }

                if (needsUpdate)
                {
                    RotateCamera(0, 0);
                    ZoomCamera(1.0f);
                    LookAtPivot();
                }
            }
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (!Engine.IsEditorHint() && InputEnabled)
            {
                if (@event is InputEventMouseMotion mouseMotionEvent)
                {
                    if (Input.IsMouseButtonPressed(MouseButton.Middle))
                    {
                        if (Input.IsKeyPressed(Key.Shift))
                        {
                            var panVector = PanVectorFromMouseMotion(mouseMotionEvent.Relative);
                            PanCamera(panVector);
                        }
                        else
                        {
                            var rotationAngles = RotationAnglesFromMouseMotion(mouseMotionEvent.Relative);
                            RotateCamera(rotationAngles.X, rotationAngles.Y);
                        }
                        GetViewport().SetInputAsHandled();
                    }
                }
                else if (@event is InputEventMouseButton mouseButtonEvent && mouseButtonEvent.IsPressed())
                {
                    if (mouseButtonEvent.ButtonIndex == MouseButton.WheelUp)
                    {
                        ZoomCamera(_zoomFactorIn);
                    }
                    if (mouseButtonEvent.ButtonIndex == MouseButton.WheelDown)
                    {
                        ZoomCamera(_zoomFactorOut);
                    }
                    GetViewport().SetInputAsHandled();
                }
            }
        }

        public override string[] _GetConfigurationWarnings()
        {
            var warnings = new List<string>();
            if (Pivot == null)
            {
                warnings.Add("Pivot node must be set");
            }
            return warnings.ToArray();
        }

        public Vector3 GetPivotRelativePosition()
        {
            if (Pivot == null)
            {
                throw new InvalidOperationException("No pivot node for orbit camera (did you forget to set one?)");
            }
            if (!IsInsideTree() || !Pivot.IsInsideTree())
            {
                return Vector3.Zero;
            }
            return GlobalPosition - Pivot.GlobalPosition;
        }

        public Vector3 GetLastPivotRelativePosition()
        {
            return _lastPosition - _lastPivotPosition;
        }

        public void LookAtPivot()
        {
            if (Pivot != null && IsInsideTree() && Pivot.IsInsideTree())
            {
                LookAtIfSafe(Pivot.GlobalPosition);
                _lastPivotPosition = Pivot.GlobalPosition;
                _lastPosition = GlobalPosition;
            }
        }

        public void LookAtIfSafe(Vector3 lookAt)
        {
            if (IsInsideTree()) 
            {
                var v = (lookAt - GlobalPosition).Normalized();
                if (lookAt != GlobalPosition && v != Vector3.Up && v != Vector3.Down)
                {
                    LookAt(lookAt);
                }
            }
        }

        public Vector2 RotationAnglesFromMouseMotion(Vector2 mouseMotion)
        {
            var xDegrees = -mouseMotion.Y * RotationSpeed;
            // Don't let input flip the camera so far over the vertical axis it counts as a valid rotation
            xDegrees = Mathf.Clamp(xDegrees, -_maxElevationRotationPerFrameDegrees, _maxElevationRotationPerFrameDegrees);
            var yDegrees = -mouseMotion.X * RotationSpeed;
            return new Vector2(Mathf.DegToRad(xDegrees), Mathf.DegToRad(yDegrees));
        }

        public void RotateCamera(float xAngleRads, float yAngleRads)
        {
            if (Pivot != null && IsInsideTree() && Pivot.IsInsideTree())
            {
                xAngleRads = Mathf.Wrap(xAngleRads, -Mathf.Pi, Mathf.Pi);
                // Rotate about local X
                var cameraQuat = Basis.GetRotationQuaternion();
                var cameraX = cameraQuat * Vector3.Right;
                var relPos = GetPivotRelativePosition();
                var newRelativeCameraPos = Mathf.Abs(xAngleRads) > 1e-3 ? relPos.Rotated(cameraX, xAngleRads) : relPos;
                var cameraZ = new Vector3(relPos.X, 0, relPos.Z).Normalized();
                var horizProjection = newRelativeCameraPos.Dot(cameraZ);
                // If we've crossed the vertical axis, discard the rotation; else clamp the rotation
                if (horizProjection <= 0)
                {
                    newRelativeCameraPos = relPos;
                }
                else
                {
                    var vertProjection = newRelativeCameraPos.Dot(Vector3.Up);
                    var angleX = Mathf.Atan2(vertProjection, horizProjection);
                    if (angleX > _maxElevationAngleRads)
                    {
                        // Rotate about negative-X axis because x axis is from camera, but cameraZ is *to* camera
                        newRelativeCameraPos = cameraZ.Rotated(-cameraX, _maxElevationAngleRads) * newRelativeCameraPos.Length();
                    }
                    if (angleX < _minElevationAngleRads)
                    {
                        newRelativeCameraPos = cameraZ.Rotated(-cameraX, _minElevationAngleRads) * newRelativeCameraPos.Length();
                    }
                }

                // Rotate about global Y
                yAngleRads = Mathf.Wrap(yAngleRads, -Mathf.Pi, Mathf.Pi);
                if (Mathf.Abs(yAngleRads) > 1e-3)
                {
                    newRelativeCameraPos = newRelativeCameraPos.Rotated(Vector3.Up, yAngleRads);
                }

                GlobalPosition = Pivot.GlobalPosition + newRelativeCameraPos;

                LookAtPivot();
            }
        }

        Vector3 PanVectorFromMouseMotion(Vector2 mouseMotion)
        {
            Vector3 panVector = Vector3.Zero;
            if (Pivot != null && IsInsideTree() && Pivot.IsInsideTree())
            {
                var relPos = GetPivotRelativePosition();
                var cameraDist = relPos.Length();
                var cameraX = Basis.GetRotationQuaternion() * Vector3.Right;
                var cameraZ = new Vector3(relPos.X, 0, relPos.Z).Normalized();
                panVector = ((cameraX * -mouseMotion.X) + (cameraZ * -mouseMotion.Y)) * (_mousePanSpeedFactor * cameraDist * PanSpeed);
            }
            return panVector;
        }

        public void PanCamera(Vector3 v)
        {
            if (Pivot != null && IsInsideTree() && Pivot.IsInsideTree())
            {
                GlobalPosition += v;
                Pivot.GlobalPosition += v;
                LookAtPivot();
            }
        }

        public void ZoomCamera(float zoomFactor)
        {
            if (Pivot != null && IsInsideTree() && Pivot.IsInsideTree())
            {
                var relPos = GetPivotRelativePosition();
                if (Mathf.Abs(zoomFactor - 1.0) > 1e-3)
                {
                    relPos *= zoomFactor;
                }
                var distSq = relPos.LengthSquared();
                if (distSq > MaximumZoomDistance * MaximumZoomDistance)
                {
                    relPos = relPos.Normalized() * MaximumZoomDistance;
                }
                else if (distSq < MinimumZoomDistance * MinimumZoomDistance)
                {
                    relPos = relPos.Normalized() * MinimumZoomDistance;
                }
                GlobalPosition = Pivot.Position + relPos;
            }
        }
    }
}
