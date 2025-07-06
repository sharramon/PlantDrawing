using UnityEngine;

namespace TiltBrush
{
    /// <summary>
    /// Configuration and helper methods for Meta Quest input setup.
    /// This helps bridge between Unity's Input system and Meta's controller inputs.
    /// </summary>
    public static class SimpleInputConfig
    {
        // Input axis names - these should match your Input Manager settings
        public static class Axes
        {
            // Trigger inputs
            public const string LeftTrigger = "LeftTrigger";
            public const string RightTrigger = "RightTrigger";
            
            // Grip inputs
            public const string LeftGrip = "LeftGrip";
            public const string RightGrip = "RightGrip";
            
            // Thumbstick inputs
            public const string LeftThumbstickX = "LeftThumbstickX";
            public const string LeftThumbstickY = "LeftThumbstickY";
            public const string RightThumbstickX = "RightThumbstickX";
            public const string RightThumbstickY = "RightThumbstickY";
            
            // Button inputs
            public const string LeftPrimaryButton = "LeftPrimaryButton";
            public const string LeftSecondaryButton = "LeftSecondaryButton";
            public const string RightPrimaryButton = "RightPrimaryButton";
            public const string RightSecondaryButton = "RightSecondaryButton";
        }
        
        // Input button names
        public static class Buttons
        {
            public const string LeftTrigger = "LeftTrigger";
            public const string RightTrigger = "RightTrigger";
            public const string LeftGrip = "LeftGrip";
            public const string RightGrip = "RightGrip";
            public const string LeftPrimary = "LeftPrimary";
            public const string LeftSecondary = "LeftSecondary";
            public const string RightPrimary = "RightPrimary";
            public const string RightSecondary = "RightSecondary";
        }
        
        /// <summary>
        /// Gets the trigger value for the specified hand (0-1 range)
        /// </summary>
        public static float GetTriggerValue(bool isLeftHand)
        {
            string axisName = isLeftHand ? Axes.LeftTrigger : Axes.RightTrigger;
            return Input.GetAxis(axisName);
        }
        
        /// <summary>
        /// Gets the grip value for the specified hand (0-1 range)
        /// </summary>
        public static float GetGripValue(bool isLeftHand)
        {
            string axisName = isLeftHand ? Axes.LeftGrip : Axes.RightGrip;
            return Input.GetAxis(axisName);
        }
        
        /// <summary>
        /// Gets the thumbstick position for the specified hand
        /// </summary>
        public static Vector2 GetThumbstickPosition(bool isLeftHand)
        {
            if (isLeftHand)
            {
                return new Vector2(Input.GetAxis(Axes.LeftThumbstickX), Input.GetAxis(Axes.LeftThumbstickY));
            }
            else
            {
                return new Vector2(Input.GetAxis(Axes.RightThumbstickX), Input.GetAxis(Axes.RightThumbstickY));
            }
        }
        
        /// <summary>
        /// Checks if the primary button is pressed for the specified hand
        /// </summary>
        public static bool GetPrimaryButton(bool isLeftHand)
        {
            string buttonName = isLeftHand ? Buttons.LeftPrimary : Buttons.RightPrimary;
            return Input.GetButton(buttonName);
        }
        
        /// <summary>
        /// Checks if the secondary button is pressed for the specified hand
        /// </summary>
        public static bool GetSecondaryButton(bool isLeftHand)
        {
            string buttonName = isLeftHand ? Buttons.LeftSecondary : Buttons.RightSecondary;
            return Input.GetButton(buttonName);
        }
        
        /// <summary>
        /// Checks if the trigger is pressed for the specified hand
        /// </summary>
        public static bool GetTriggerPressed(bool isLeftHand, float threshold = 0.5f)
        {
            return GetTriggerValue(isLeftHand) > threshold;
        }
        
        /// <summary>
        /// Checks if the grip is pressed for the specified hand
        /// </summary>
        public static bool GetGripPressed(bool isLeftHand, float threshold = 0.5f)
        {
            return GetGripValue(isLeftHand) > threshold;
        }
        
        /// <summary>
        /// Gets the controller position and rotation for the specified hand
        /// </summary>
        public static bool GetControllerTransform(bool isLeftHand, out Vector3 position, out Quaternion rotation)
        {
            // This would need to be implemented based on your XR setup
            // For now, return false to indicate no controller found
            position = Vector3.zero;
            rotation = Quaternion.identity;
            return false;
        }
        
        /// <summary>
        /// Gets the hand position and rotation for the specified hand
        /// </summary>
        public static bool GetHandTransform(bool isLeftHand, out Vector3 position, out Quaternion rotation)
        {
            // This would need to be implemented based on your hand tracking setup
            // For now, return false to indicate no hand found
            position = Vector3.zero;
            rotation = Quaternion.identity;
            return false;
        }
    }
    
    /// <summary>
    /// Extension methods for easier input handling
    /// </summary>
    public static class InputExtensions
    {
        /// <summary>
        /// Gets trigger value for left hand
        /// </summary>
        public static float LeftTrigger => SimpleInputConfig.GetTriggerValue(true);
        
        /// <summary>
        /// Gets trigger value for right hand
        /// </summary>
        public static float RightTrigger => SimpleInputConfig.GetTriggerValue(false);
        
        /// <summary>
        /// Gets grip value for left hand
        /// </summary>
        public static float LeftGrip => SimpleInputConfig.GetGripValue(true);
        
        /// <summary>
        /// Gets grip value for right hand
        /// </summary>
        public static float RightGrip => SimpleInputConfig.GetGripValue(false);
        
        /// <summary>
        /// Gets left thumbstick position
        /// </summary>
        public static Vector2 LeftThumbstick => SimpleInputConfig.GetThumbstickPosition(true);
        
        /// <summary>
        /// Gets right thumbstick position
        /// </summary>
        public static Vector2 RightThumbstick => SimpleInputConfig.GetThumbstickPosition(false);
    }
} 