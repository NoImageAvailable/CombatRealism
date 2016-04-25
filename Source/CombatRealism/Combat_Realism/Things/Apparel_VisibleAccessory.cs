using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace Combat_Realism
{
    public abstract class Apparel_VisibleAccessory : Apparel
    {
        public override void DrawWornExtras()
        {
            Vector3 drawVec = this.wearer.Drawer.DrawPos;
            drawVec.y = Altitudes.AltitudeFor(AltitudeLayer.Pawn);
            Vector3 s = new Vector3(1.5f, 1.5f, 1.5f);
            
            // Get the graphic path
            string path = def.graphicData.texPath + "_" + wearer?.story.BodyType.ToString();
            Graphic graphic = GraphicDatabase.Get<Graphic_Multi>(path, ShaderDatabase.Cutout, def.graphicData.drawSize, DrawColor);
            ApparelGraphicRecord apparelGraphic = new ApparelGraphicRecord(graphic, this);

            // Downed check
            Rot4 rotation = wearer.Rotation;
            float angle = 0;
            if(wearer.GetPosture() != PawnPosture.Standing)
            {
                float? newAngle = wearer.Drawer?.renderer?.wiggler?.downedAngle;
                if (newAngle != null)
                    angle = (float)newAngle;
                rotation = LayingFacing();
            }
            drawVec.y += GetAltitudeOffset(rotation);
            Material mat = apparelGraphic.graphic.MatAt(rotation);

            mat.shader = ShaderDatabase.Cutout;
            mat.color = DrawColor;
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(drawVec, Quaternion.AngleAxis(angle, Vector3.up), s);
            Graphics.DrawMesh(rotation == Rot4.West ? MeshPool.plane10Flip : MeshPool.plane10, matrix, mat, 0);
        }

        protected abstract float GetAltitudeOffset(Rot4 rotation);

        // Copied from PawnRenderer
        private Rot4 LayingFacing()
        {
            if (wearer == null)
            {
                return Rot4.Random;
            }
            if (wearer.GetPosture() == PawnPosture.LayingFaceUp)
            {
                return Rot4.South;
            }
            if (wearer.RaceProps.Humanlike)
            {
                switch (wearer.thingIDNumber % 4)
                {
                    case 0:
                        return Rot4.South;
                    case 1:
                        return Rot4.South;
                    case 2:
                        return Rot4.East;
                    case 3:
                        return Rot4.West;
                }
            }
            else
            {
                switch (wearer.thingIDNumber % 4)
                {
                    case 0:
                        return Rot4.South;
                    case 1:
                        return Rot4.East;
                    case 2:
                        return Rot4.West;
                    case 3:
                        return Rot4.West;
                }
            }
            return Rot4.Random;
        }
    }
}
