using UnityEngine;

using static OCDheim.GroundLevelSpinner;

namespace OCDheim
{
    // Minimal classes describing specific VFXs of specific "ToolOps" in a DSL-like form.
    public class LevelGroundOverlayVisualizer : SecondaryEnabledOnGridModePrimaryDisabledOnGridMode
    {
        protected override void InitializeOverlay()
        {
            base.InitializeOverlay();
            SpeedUp(secondary);
            VisualizeTerraformingBounds(secondary);
        }
    }

    public class RaiseGroundOverlayVisualizer : ModifyGroundLevelOverlayVisualizer
    {
        public RaiseGroundOverlayVisualizer() : base(RaiseGroundSpinner) {}
    }
    
    public class LowerGroundOverlayVisualizer : ModifyGroundLevelOverlayVisualizer
    {
        public LowerGroundOverlayVisualizer() : base(LowerGroundSpinner) {}
    }

    public abstract class ModifyGroundLevelOverlayVisualizer : SecondaryEnabledOnGridModePrimaryEnabledAlways
    {
        private const float VanillaValheimOverlayBump = 0.05f;

        private GroundLevelSpinner spinner { get; }
        
        protected ModifyGroundLevelOverlayVisualizer(GroundLevelSpinner spinner)
        {
            this.spinner = spinner;
        }

        protected override void InitializeOverlay()
        {
            base.InitializeOverlay();
            Freeze(secondary);
            Freeze(tertiary);
            VisualizeTerraformingBounds(secondary);
            VisualizeTerraformingBounds(tertiary);
        }

        private float groundHeight => transform.position.y;

        protected override void OnRefresh()
        {
            base.OnRefresh();
            if (KeyBinder.gridModeEnabled)
            {
                if (KeyBinder.copyHeightPressed)
                {
                    HeightClipboard.Save(groundHeight);
                }

                var previewingPaste = HeightClipboard.hasSavedHeight && KeyBinder.pasteModifierHeld;
                if (!previewingPaste) { spinner.Refresh(); }

                var offset = previewingPaste ? HeightClipboard.MissingTo(groundHeight) : spinner.value;
                secondary.localPosition = new Vector3(0f, offset, 0f);

                hoverInfo.text = BuildHoverText(offset);
            }
            tertiary.enabled = KeyBinder.gridModeEnabled;
        }

        private string BuildHoverText(float offset)
        {
            var target = secondary.worldPosition.y + VanillaValheimOverlayBump;
            var pending = offset != 0f ? $" ({offset:+0.00;-0.00})" : string.Empty;
            var text = $"x: {secondary.worldPosition.x:0}, y: {secondary.worldPosition.z:0}\nh: {target:0.00}{pending}";

            if (!HeightClipboard.hasSavedHeight) { return text; }

            return text + $"\nsaved: {HeightClipboard.savedHeight + VanillaValheimOverlayBump:0.00}";
        }
    }

    public class PaveRoadOverlayVisualizer : SecondaryEnabledOnGridModePrimaryDisabledOnGridMode
    {
        protected override void InitializeOverlay()
        {
            base.InitializeOverlay();
            SpeedUp(secondary);
            VisualizeRecoloringBounds(secondary);
        }
    }

    public class CultivateOverlayVisualizer : PaveRoadOverlayVisualizer
    {
        protected override void OnRefresh()
        {
            base.OnRefresh();
            hoverInfo.color = secondary.color;
        }
    }

    public class SeedGrassOverlayVisualizer : SecondaryEnabledOnGridModePrimaryEnabledAlways
    {
        protected override void InitializeOverlay()
        {
            base.InitializeOverlay();
            Freeze(secondary);
            VisualizeRecoloringBounds(secondary);
            primary.localPosition = new Vector3(0.0f, 2.0f, 0.0f);
            secondary.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
        }

        protected override void OnRefresh()
        {
            base.OnRefresh();
            primary.startSize = KeyBinder.gridModeEnabled ? 4.0f : 5.5f;
        }

        protected override void OnEnableGrid()
        {
            base.OnEnableGrid();
            primary.enabled = false;
            primary.startSize = 4.0f;
            primary.enabled = true;
        }

        protected override void OnDisableGrid()
        {
            primary.enabled = false;
            primary.startSize = 5.5f;
            primary.enabled = true;
            base.OnDisableGrid();
        }
    }

    public class RemoveModificationsOverlayVisualizer : SecondaryAndPrimaryEnabledAlways
    {
        protected override void InitializeOverlay()
        {
            Freeze(primary);
            SpeedUp(secondary);
            VisualizeTerraformingBounds(primary);
            VisualizeIconInsideTerraformingBounds(secondary, cross);
        }
        
        protected override void OnRefresh()
        {
            base.OnRefresh();
            ScaleVertically(primary);
            ScaleVertically(secondary);
        }
    }

    public abstract class UndoRedoModificationsOverlayVisualizer : SecondaryAndPrimaryEnabledAlways
    {
        protected override void InitializeOverlay()
        {
            Freeze(primary);
            Freeze(secondary);
            VisualizeRecoloringBounds(primary);
            VisualizeIconInsideRecoloringBounds(secondary, Icon());
        }

        protected abstract Texture2D Icon();
    }

    public class UndoModificationsOverlayVisualizer : UndoRedoModificationsOverlayVisualizer
    {
        protected override Texture2D Icon()
        {
            return undo;
        }
    }

    public class RedoModificationsOverlayVisualizer : UndoRedoModificationsOverlayVisualizer
    {
        protected override Texture2D Icon()
        {
            return redo;
        }
    }
}
