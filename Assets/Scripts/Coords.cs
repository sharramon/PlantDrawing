// Copyright 2020 The Tilt Brush Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace TiltBrush
{

    /// Properties for various coordinate transforms,
    /// and helper functions for converting between them.
    ///
    /// Names follow the conventions in go/vr-coords.
    ///
    /// There are 3 main coordinate systems that we are interested in:
    /// - Room Space        Real-world tracking space; room floor should be at Y=0
    /// - Scene Space       Virtual environment; origin is the center of the env
    /// - Canvas Space      Brush strokes only; origin is at the floor.
    ///
    /// The global coordinate system is also used as a convenient
    /// space to work in before projecting into some other space.
    /// Currently, Global === Room, although it's best to distinguish
    /// between the two in your code.
    ///
    public static class Coords
    {
        // Unless otherwise noted, transforms are in global coordinates

        /// Simplified for Quest 3 integration
        public static event CanvasScript.PoseChangedEventHandler CanvasPoseChanged
        {
            add { /* Simplified: no canvas pose change events */ }
            remove { /* Simplified: no canvas pose change events */ }
        }

        /// Simplified for Quest 3 integration
        public static TrTransform CanvasPose
        {
            get { return SimpleApp.Instance?.ActiveCanvas?.Pose ?? TrTransform.identity; }
            set { if (SimpleApp.Instance?.ActiveCanvas != null) SimpleApp.Instance.ActiveCanvas.Pose = value; }
        }

        /// Simplified for Quest 3 integration
        public static TrTransform CanvasLocalPose
        {
            get { return SimpleApp.Instance?.ActiveCanvas?.LocalPose ?? TrTransform.identity; }
            set { if (SimpleApp.Instance?.ActiveCanvas != null) SimpleApp.Instance.ActiveCanvas.LocalPose = value; }
        }

        /// Helpers for getting and setting transforms on Transform components.
        /// Transform natively allows you to access parent-relative ("local")
        /// and root-relative ("global") views of position, rotation, and scale.
        ///
        /// These helpers:
        ///
        /// - access data as a TrTransform
        /// - provide scene-relative, canvas-relative and room-relative views
        ///
        /// The syntax is a slight abuse of C#:
        ///
        ///   TrTranform xf = Coords.AsRoom[gameobj.transform];
        ///   Coords.AsRoom[gameobj.transform] = xf;
        ///
        public static TransformExtensions.GlobalAccessor AsRoom;
        public static TransformExtensions.GlobalAccessor AsGlobal
            = new TransformExtensions.GlobalAccessor();
        public static TransformExtensions.LocalAccessor AsLocal
            = new TransformExtensions.LocalAccessor();

        /// Deprecated. Replace with canvasInstance.AsCanvas[]
        public static TransformExtensions.RelativeAccessor AsCanvas;

        // Internal

        public static void Init(SimpleApp app)
        {
            // Simplified: use null for canvas accessor since we don't need it
            AsCanvas = new TransformExtensions.RelativeAccessor(null);
            // Room coordinate system === Unity global coordinate system
            AsRoom = new TransformExtensions.GlobalAccessor();
        }
    }

} // namespace TiltBrush
