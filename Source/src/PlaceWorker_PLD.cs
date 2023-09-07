using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;

namespace RimPLD {
    public class PlaceWorker_PLD : PlaceWorker {
        public static readonly Color ColorActive = new Color(0f, 1f, 0f, 0.5f);
        public static readonly Color ColorInactive = new Color(0.5f, 0f, 0f, 0.5f);
        private void DrawDir(IntVec3 center, Direction8Way dir, Color color) {
            IntVec3 loc = RimPLD.displace(center, dir);
            GenDraw.DrawFieldEdges(new List<IntVec3>() {
                loc
            }, color, null);
        }
        public override void DrawGhost(
            ThingDef def,
            IntVec3 center,
            Rot4 rot,
            Color ghostCol,
            Thing thing = null
        ) {
            if(thing == null) {
                // Placing a new one
            } else {
                CompPLD pld = (thing as ThingWithComps).GetComp<CompPLD>();
                if(pld == null) return;
                Map currentMap = Find.CurrentMap;
                for(int i = 0; i < 3; ++i) {
                    if(pld.inputs[i].needsDirection()) {
                        DrawDir(center, pld.inputs[i].dir, GenTemperature.ColorSpotHot);
                    }
                    if(pld.outputs[i].needsDirection()) {
                        DrawDir(center, pld.outputs[i].dir, GenTemperature.ColorSpotCold);
                    }
                }
            }
        }
    }
}