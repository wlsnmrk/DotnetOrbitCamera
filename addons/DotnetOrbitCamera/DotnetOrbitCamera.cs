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
        // Minima for various exported properties
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

        private float _xRotationLimitRads = Mathf.DegToRad(80f);
        [Export(PropertyHint.Range, "0,89")]
        public float VerticalRotationLimit 
        { 
            get { return Mathf.RadToDeg(_xRotationLimitRads); }
            set
            {
                while (value > 360)
                {
                    value -= 360;
                }
                while (value < 0)
                {
                    value += 360;
                }
                _xRotationLimitRads = Mathf.DegToRad(value);
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
                LimitCameraZoom();
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
                LimitCameraZoom();
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
            if (Pivot != null && (Pivot.GlobalPosition !=  _lastPivotPosition || GlobalPosition != _lastPosition))
            {
                LookAtPivot();
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
                        ZoomCamera(true);
                    }
                    if (mouseButtonEvent.ButtonIndex == MouseButton.WheelDown)
                    {
                        ZoomCamera(false);
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

        public Vector3 GetPositionRelativeToPivot()
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
            return new Vector2(Mathf.DegToRad(-mouseMotion.Y * RotationSpeed), Mathf.DegToRad(-mouseMotion.X * RotationSpeed));
        }

        public void RotateCamera(float xAngleRads, float yAngleRads)
        {
            if (Pivot != null && IsInsideTree() && Pivot.IsInsideTree())
            {
                // Rotate about local X
                var cameraQuat = Basis.GetRotationQuaternion();
                var cameraX = cameraQuat * Vector3.Right;
                var relPos = GetPositionRelativeToPivot();
                var newRelativeCameraPos = relPos.Rotated(cameraX, xAngleRads);
                var newRelativeCameraPosNorm = newRelativeCameraPos.Normalized();
                var cameraZ = new Vector3(relPos.X, 0, relPos.Z).Normalized();
                var angleX = Mathf.Atan2(cameraZ.Cross(newRelativeCameraPosNorm).Dot(cameraX), cameraZ.Dot(newRelativeCameraPosNorm));
                if (angleX > _xRotationLimitRads)
                {
                    newRelativeCameraPos = cameraZ.Rotated(cameraX, _xRotationLimitRads) * newRelativeCameraPos.Length();
                }
                if (angleX < -_xRotationLimitRads)
                {
                    newRelativeCameraPos = cameraZ.Rotated(cameraX, -_xRotationLimitRads) * newRelativeCameraPos.Length();
                }

                // Rotate about global Y
                newRelativeCameraPos = newRelativeCameraPos.Rotated(Vector3.Up, yAngleRads);

                GlobalPosition = Pivot.GlobalPosition + newRelativeCameraPos;

                LookAtPivot();
            }
        }

        Vector3 PanVectorFromMouseMotion(Vector2 mouseMotion)
        {
            Vector3 panVector = Vector3.Zero;
            if (Pivot != null && IsInsideTree() && Pivot.IsInsideTree())
            {
                var relPos = GetPositionRelativeToPivot();
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

        public void ZoomCamera(bool isDirectionIn)
        {
            if (Pivot != null && IsInsideTree() && Pivot.IsInsideTree())
            {
                var relPos = GetPositionRelativeToPivot();
                var newCameraPos = relPos * (isDirectionIn ? 0.8f : 1.2f);
                Position = Pivot.Position + newCameraPos;
                LimitCameraZoom();
            }
        }

        public void LimitCameraZoom()
        {
            if (Pivot != null && IsInsideTree() && Pivot.IsInsideTree())
            {
                var relPos = GetPositionRelativeToPivot();
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
