using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

using static OCDheim.PlayerHelpers;
using static OCDheim.PrecisionMode;

namespace OCDheim
{
    [HarmonyPatch]
    public class KeyBinder : MonoBehaviour
    {
        private static readonly ButtonConfig SnapModeKey = new ButtonConfig { Name = "SnapModeKey", Key = KeyCode.LeftShift };
        private static readonly ButtonConfig SnapModeJoy = new ButtonConfig { Name = "SnapModeJoy", GamepadButton = InputManager.GamepadButton.RightShoulder };
        private static readonly ButtonConfig GridModeKey = new ButtonConfig { Name = "GridModeKey", Key = KeyCode.LeftAlt };
        private static readonly ButtonConfig GridModeJoy = new ButtonConfig { Name = "GridModeJoy", GamepadButton = InputManager.GamepadButton.RightStickButton };
        private static readonly ButtonConfig PrecisionModeKey = new ButtonConfig { Name = "PrecisionModeKey", Key = KeyCode.Z };
        private static readonly ButtonConfig PrecisionModeJoy = new ButtonConfig { Name = "PrecisionModeJoy", GamepadButton = InputManager.GamepadButton.ButtonWest };
        
        private const string JoyScrollUnlock = "JoyLTrigger";
        private const string JoyScrollDown = "JoyDPadDown";
        private const string JoyScrollUp = "JoyDPadUp";

        private const float ScrollPrecision = 0.01f;
        
        // One physical keypress still arrived as two rising edges: ZInput.GetButton does not hold steady across
        // frames for these buttons, so the held-state flickers and a single ALT toggled Grid Mode twice. Ignore
        // a second key-driven toggle that lands within a fraction of a second of the last one - no human
        // double-taps a mode key that fast, and the auto-disable path is deliberately left undebounced.
        private const float ToggleDebounceSeconds = 0.25f;

        private static bool gridKeyHeldLastFrame;
        private static bool precisionKeyHeldLastFrame;
        private static float lastGridToggleTime = float.NegativeInfinity;
        private static float lastPrecisionToggleTime = float.NegativeInfinity;

        private static bool _gridModeEnabled;
        private static bool _gridModeFreshlyEnabled;
        private static bool _gridModeFreshlyDisabled;

        public static bool snapModeDisabled => ZInput.GetButton(SnapModeKey.Name) || ZInput.GetButton(SnapModeJoy.Name);
        public static bool snapModeEnabled => !snapModeDisabled;

        public static bool gridModeEnabled
        {
            get => _gridModeEnabled;
            private set {
                _gridModeEnabled = value;
                _gridModeFreshlyEnabled = value;
                _gridModeFreshlyDisabled = !value;
            }
        }
        public static bool gridModeDisabled => !gridModeEnabled;
        public static bool gridModFreshlyEnabled { get { var temp = _gridModeFreshlyEnabled; _gridModeFreshlyEnabled = false; return temp; } }
        public static bool gridModFreshlyDisabled { get { var temp = _gridModeFreshlyDisabled; _gridModeFreshlyDisabled = false; return temp; } }

        public static PrecisionMode precisionMode { get; private set; } = ORDINARY;

        private void Awake()
        {
            InputManager.Instance.AddButton(OCDheim.GUID, SnapModeJoy);
            InputManager.Instance.AddButton(OCDheim.GUID, SnapModeKey);
            InputManager.Instance.AddButton(OCDheim.GUID, GridModeKey);
            InputManager.Instance.AddButton(OCDheim.GUID, GridModeJoy);
            InputManager.Instance.AddButton(OCDheim.GUID, PrecisionModeKey);
            InputManager.Instance.AddButton(OCDheim.GUID, PrecisionModeJoy);
        }

        private void Update()
        {
            // ZInput.GetButtonDown reports the same press on more than one frame under Valheim's new Input
            // System, so a single ALT toggled Grid Mode twice and left it exactly where it started - which
            // made every Grid Mode feature downstream look broken. Track the held state and act on the
            // rising edge ourselves.
            var gridHeld = ZInput.GetButton(GridModeKey.Name)
                           || (snapModeEnabled && ZInput.GetButton(GridModeJoy.Name));
            var gridModeButton = gridHeld && !gridKeyHeldLastFrame
                                 && Time.unscaledTime - lastGridToggleTime > ToggleDebounceSeconds;
            gridKeyHeldLastFrame = gridHeld;
            if (gridModeButton) { lastGridToggleTime = Time.unscaledTime; }

            var toggleGridMode = (gridModeButton && (gridModeEnabled || player.HasConstructionToolEquipped()))
                                 || (gridModeEnabled && !player.HasConstructionToolEquipped());

            var precisionHeld = ZInput.GetButton(PrecisionModeKey.Name)
                                || (snapModeEnabled && ZInput.GetButton(PrecisionModeJoy.Name));
            var precisionModeButton = precisionHeld && !precisionKeyHeldLastFrame
                                      && Time.unscaledTime - lastPrecisionToggleTime > ToggleDebounceSeconds;
            precisionKeyHeldLastFrame = precisionHeld;
            if (precisionModeButton) { lastPrecisionToggleTime = Time.unscaledTime; }
            var togglePrecisionMode = (precisionModeButton && (precisionMode == SUPERIOR || player.HasBuildPieceEquipped()))
                                      || (precisionMode == SUPERIOR && !player.HasBuildPieceEquipped());

            if (toggleGridMode)
            {
                gridModeEnabled = !gridModeEnabled;
                Logger.Info(() => $"[{(gridModeEnabled ? "ENABLED" : "DISABLED")}] GRID MODE");
            }
            if (togglePrecisionMode)
            {
                precisionMode = precisionMode == ORDINARY ? SUPERIOR : ORDINARY;
                Logger.Info(() => $"[{(precisionMode == SUPERIOR ? "ENABLED" : "DISABLED")}] PRECISION MODE");
            }
        }

        public static float ScrollΔ()
        {
            var scrollΔ = BlockCameraZoom.ReadScrollWheel();
            if (scrollΔ != 0)
            {
                return scrollΔ > 0 ? ScrollPrecision : - ScrollPrecision;
            }
            
            if (ZInput.GetButton(JoyScrollUnlock) && ZInput.GetButtonDown(JoyScrollDown))
            {
                return - ScrollPrecision;
            }

            if (ZInput.GetButton(JoyScrollUnlock) && ZInput.GetButtonDown(JoyScrollUp))
            {
                return ScrollPrecision;
            }

            return scrollΔ;
        }
    }
}
