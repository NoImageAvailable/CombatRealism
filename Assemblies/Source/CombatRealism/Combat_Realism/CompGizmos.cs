using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism
{
    public class GizmoAmmoStatus : Command
    {
        //Link
        public CompReloader compAmmo;

        private static readonly Texture2D FullTex =
            SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.2f, 0.24f));
        private static readonly Texture2D EmptyTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);

        public override float Width
        {
            get
            {
                return 120;
            }
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft)
        {
            var overRect = new Rect(topLeft.x, topLeft.y, Width, Height);
            Widgets.DrawBox(overRect);
            GUI.DrawTexture(overRect, BGTex);

            var inRect = overRect.ContractedBy(6);

            //Item label
            var textRect = inRect;
            textRect.height = overRect.height / 2;
            Text.Font = GameFont.Tiny;
            Widgets.Label(textRect, compAmmo.parent.def.LabelCap);

            //Bar
            var barRect = inRect;
            barRect.yMin = overRect.y + overRect.height / 2f;
            var ePct = (float)compAmmo.count / compAmmo.reloaderProp.roundPerMag;
            Widgets.FillableBar(barRect, ePct, FullTex, EmptyTex, false);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(barRect, compAmmo.count + " / " + compAmmo.reloaderProp.roundPerMag);
            Text.Anchor = TextAnchor.UpperLeft;

            return new GizmoResult(GizmoState.Clear);
        }
    }

    // God comp to handle generating gizmos for all the different comps, need this since CCL does not display gizmos from more than one comp
    public class CompGizmos : CommunityCoreLibrary.CompRangedGizmoGiver
    {
        public override IEnumerable<Command> CompGetGizmosExtra()
        {
            // Reloader gizmos
            CompReloader compReloader = this.parent.TryGetComp<CompReloader>();
            if (compReloader != null)
            {
                var ammoStatusGizmo = new GizmoAmmoStatus { compAmmo = compReloader };
                yield return ammoStatusGizmo;

                if (compReloader.wielder != null)
                {
                    var reloadCommandGizmo = new Command_Action
                    {
                        action = compReloader.StartReload,
                        defaultLabel = "CR_ReloadLabel".Translate(),
                        defaultDesc = "CR_ReloadDesc".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Buttons/Reload", true)
                    };
                    yield return reloadCommandGizmo;
                }
            }

            // Fire mode gizmos
            CompFireModes compFireModes = this.parent.TryGetComp<CompFireModes>();
            if (compFireModes != null && compFireModes.casterPawn != null && compFireModes.casterPawn.Faction.Equals(Faction.OfColony))
            {
                var toggleFireModeGizmo = new Command_Action
                {
                    action = compFireModes.ToggleFireMode,
                    defaultLabel = ("CR_" + compFireModes.currentFireMode.ToString() + "Label").Translate(),
                    defaultDesc = "CR_ToggleFireModeDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get(("UI/Buttons/" + compFireModes.currentFireMode.ToString()), true)
                };
                yield return toggleFireModeGizmo;

                var toggleAimModeGizmo = new Command_Action
                {
                    action = compFireModes.ToggleAimMode,
                    defaultLabel = ("CR_" + compFireModes.currentAimMode.ToString() + "Label").Translate(),
                    defaultDesc = "CR_ToggleAimModeDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get(("UI/Buttons/" + compFireModes.currentAimMode.ToString()), true)
                };
                yield return toggleAimModeGizmo;
            }
        }
    }
}
