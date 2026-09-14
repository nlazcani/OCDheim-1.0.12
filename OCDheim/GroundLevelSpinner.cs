using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;

using static OCDheim.PlayerHelpers;

namespace OCDheim
{
    public class GroundLevelSpinner
    {
        public static readonly GroundLevelSpinner RaiseGroundSpinner = new GroundLevelSpinner( 1.0f,  0.0f, 1.0f);
        public static readonly GroundLevelSpinner LowerGroundSpinner = new GroundLevelSpinner(-0.5f, -1.0f, 0.0f);
        
        public float value { get; private set; }
        private float maxValue { get; }
        private float minValue { get; }

        private GroundLevelSpinner(float value , float minValue, float maxValue)
        {
            this.value = value;
            this.minValue = minValue;
            this.maxValue = maxValue;
        }

        public void Refresh()
        {
            var scrollΔ = KeyBinder.ScrollΔ();
            if (scrollΔ == 0) { return; }

            if (scrollΔ > 0) {
                Up(scrollΔ);
            }
            if (scrollΔ < 0)
            {
                Down(scrollΔ);
            }
        }

        private void Up(float scrollΔ)
        {
            if (value + scrollΔ > maxValue)
            {
                value = maxValue;
            }
            else
            {
                value = Mathf.Round((value + scrollΔ) * 100) / 100;
            }
        }

        private void Down(float scrollΔ)
        {
            if (value + scrollΔ < minValue)
            {
                value = minValue;
            }
            else
            {
                value = Mathf.Round((value + scrollΔ) * 100) / 100;
            }
        }
    }

    [HarmonyPatch]
    public static class BlockCameraZoom
    {
        private static bool readingOurOwnScroll;

        // The spinner used to read the wheel straight off UnityEngine.Input.GetAxis("Mouse ScrollWheel").
        // Valheim now drives ZInput through Unity's new Input System, and the legacy Input Manager is no longer
        // fed - GetAxis just answers a silent 0, so scrolling did nothing and threw nothing. Read the wheel
        // through ZInput like the game itself does, stepping around our own camera-zoom block on the way in.
        public static float ReadScrollWheel()
        {
            readingOurOwnScroll = true;
            try
            {
                var fromZInput = ZInput.GetMouseScrollWheel();
                if (fromZInput != 0f) { return fromZInput; }

                // ZInput derives an "input source" from the mouse-delta action and returns a flat 0 whenever
                // ShouldAcceptInputFromSource turns it down, so the wheel can read as motionless while it is
                // plainly turning. The mouse device itself is not gated - ask it directly.
                var mouse = Mouse.current;

                return mouse == null ? 0f : mouse.scroll.ReadValue().y;
            }
            finally
            {
                readingOurOwnScroll = false;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ZInput))]
        [HarmonyPatch(nameof(ZInput.GetMouseScrollWheel))]
        public static bool Prefix(ref float __result)
        {
            if (!readingOurOwnScroll && player.HasRaiseGroundTerraformToolEquipped())
            {
                __result = 0f;
                return false;
            }

            return true;
        }
    }
}
