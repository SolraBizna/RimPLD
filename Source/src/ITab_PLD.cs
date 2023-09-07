/* Meine Götter this is some of the ugliest code I've ever written */

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.AccessControl;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimPLD {
    public class ITab_PLD : ITab {
        const float IOWidth = 200f;
        const float InputLeftEdge = Margin;
        const float OutputLeftEdge = WinWidth-Margin-IOWidth;
        const float WinWidth = Output1KinkX + BoxStride + IOWidth + Margin*2;
        const float WinHeight = InterTopEdge + BoxStride*3 + Margin;
        private static readonly Vector2 WinSize = new Vector2(WinWidth, WinHeight);
        const float Margin = 10f;
        const float ArrayLeftEdge = Margin + IOWidth + BoxStride;
        const float ArrayTopEdge = Margin + BoxSize;
        const float InterTopEdge = ArrayTopEdge + BoxStride * 9;
        const float LatchLeft = ArrayLeftEdge + BoxSize*6.5f;
        const float LatchTop = ArrayTopEdge + BoxSize*6;
        const float LatchWidth = BoxSize*2f;
        const float LatchHeight = BoxSize*2f;
        const float BoxSize = 32f;
        const float BoxStride = BoxSize;
        const float KinkStride = 16f;
        const float Input1CenterY = ArrayTopEdge + BoxStride * 0.5f;
        const float Output1CenterY = ArrayTopEdge + BoxStride * 4.5f + SwitchOffset;
        const float Output1KinkX = LatchLeft + LatchWidth * 0.75f + BoxStride;
        const float SwitchOffset = 8f;
        const float SwitchSize = 32f;
        const float HatSegmentSize = 8f;
        const float HatWidth = 24f;
        const float HatHeight = 24f;
        CompPLD pld;
        public ITab_PLD() {
            size = WinSize;
            labelKey = "TabPLD";
        }
        protected override void FillTab() {
            pld = (SelObject as Thing).TryGetComp<CompPLD>();
            if(pld == null) return; // ???
            bool dirty = false;
            bool powerWasDead = !pld.powerWasOn;
            for(int y = 0; y < 8; ++y) {
                bool inputActive = (pld.lastInputs & (1 << y)) != 0;
                Texture2D lineTexture = RimPLD.get_texture(inputActive?"On":"Off");
                if(y != 4 && y != 5) {
                    float rightmost = 0;
                    for(int x = 0; x < 6; ++x) {
                        if((pld.p1 & (1L << (x * 8 + y))) != 0) rightmost = x;
                    }
                    if(y % 2 == 1) {
                        GUI.DrawTexture(new Rect(Margin+IOWidth+16-1, ArrayTopEdge + y * BoxStride + 15, ArrayLeftEdge + BoxStride * rightmost + 4 - (Margin+IOWidth+16-1), 2), lineTexture);
                        GUI.DrawTexture(new Rect(Margin+IOWidth+16-1, ArrayTopEdge + y * BoxStride + 11, 2, 6), lineTexture);
                    }
                    else {
                        GUI.DrawTexture(new Rect(ArrayLeftEdge - BoxStride*2, ArrayTopEdge + y * BoxStride + 15, BoxStride * (2 + rightmost) + 4, 2), lineTexture);
                        GUI.DrawTexture(new Rect(Margin+IOWidth+16-1, ArrayTopEdge + y * BoxStride + 15, 2, 6), lineTexture);
                    }
                }
                else {
                    float leftmost = 6.25f;
                    for(int x = 5; x >= 0; --x) {
                        if((pld.p1 & (1L << (x * 8 + y))) != 0) leftmost = x;
                    }
                    float rightmost;
                    if(y == 4) {
                        rightmost = Output1KinkX;
                        GUI.DrawTexture(new Rect(LatchLeft + LatchWidth * 0.75f - 1, ArrayTopEdge+y*BoxStride+15,2,LatchTop-(ArrayTopEdge+y*BoxStride+15)), lineTexture);
                        Texture2D nodeTexture = RimPLD.get_texture(inputActive?"NodeOn":"NodeOff");
                        GUI.DrawTexture(new Rect(LatchLeft + LatchWidth * 0.75f - 3, Output1CenterY-3-SwitchOffset, 6, 6), nodeTexture);
                    }
                    else {
                        rightmost = LatchLeft + LatchWidth * 0.25f + 1;
                        GUI.DrawTexture(new Rect(rightmost-1, ArrayTopEdge+y*BoxStride+15,2,LatchTop-(ArrayTopEdge+y*BoxStride+15)), lineTexture);
                    }
                    GUI.DrawTexture(new Rect(ArrayLeftEdge + 2 + leftmost * BoxStride, ArrayTopEdge + y * BoxStride + 15, rightmost - (ArrayLeftEdge + 2 + leftmost * BoxStride), 2), lineTexture);
                }
            }
            for(int y = 0; y < 3; ++y) {
                bool outerActive = (pld.lastOuters & (1 << y)) != 0;
                Texture2D lineTexture = RimPLD.get_texture(outerActive?"On":"Off");
                Texture2D pullTexture = RimPLD.get_texture(outerActive?"GndPullOn":"GndPullOff");
                GUI.DrawTexture(new Rect(ArrayLeftEdge + 8, InterTopEdge + y * BoxStride + 15, Output1KinkX + KinkStride * y - (ArrayLeftEdge + 8), 2), lineTexture);
                GUI.DrawTexture(new Rect(ArrayLeftEdge - 24, InterTopEdge + y * BoxStride, BoxSize, BoxSize), pullTexture);
                float top = Output1CenterY + BoxStride * 2 * y;
                if(y == 0) {
                    top += SwitchOffset;
                }
                GUI.DrawTexture(new Rect(Output1KinkX + KinkStride * y - 1, top - 1, 2,InterTopEdge + y * BoxStride + 16 - top + 2), lineTexture);
                if(y == 0) {
                    Texture2D nodeTexture = RimPLD.get_texture(outerActive?"NodeOn":"NodeOff");
                    GUI.DrawTexture(new Rect(LatchLeft+LatchWidth*0.25f-1, LatchTop+LatchHeight, 2, (InterTopEdge + y * BoxStride + 13) - (LatchTop+LatchHeight)), lineTexture);
                    GUI.DrawTexture(new Rect(LatchLeft+LatchWidth*0.25f-3, InterTopEdge + y * BoxStride + 13, 6, 6), nodeTexture);
                } else if(y == 1) {
                    Texture2D nodeTexture = RimPLD.get_texture(outerActive?"NodeOn":"NodeOff");
                    GUI.DrawTexture(new Rect(LatchLeft+LatchWidth*0.75f-1, LatchTop+LatchHeight, 2, (InterTopEdge + y * BoxStride + 13) - (LatchTop+LatchHeight)), lineTexture);
                    GUI.DrawTexture(new Rect(LatchLeft+LatchWidth*0.75f-3, InterTopEdge + y * BoxStride + 13, 6, 6), nodeTexture);
                }
            }
            for(int y = 0; y < 3; ++y) {
                bool outputActive = (pld.lastOutputs & (1 << y)) != 0;
                Texture2D lineTexture = RimPLD.get_texture(outputActive?"On":"Off");
                float left;
                if(y == 0) {
                    left = Output1KinkX - 6 + SwitchSize;
                } else {
                    left = Output1KinkX + y * KinkStride;
                }
                float top = Output1CenterY + y * 2 * BoxStride;
                GUI.DrawTexture(new Rect(left - 1, top - 1, Output1KinkX + BoxStride*2 - left + 1, 2), lineTexture);
            }
            for(int x = 0; x < 6; ++x) {
                bool interActive = (pld.lastInters & (1 << x)) != 0;
                Texture2D pullTexture = RimPLD.get_texture(powerWasDead?"VccPullDead":interActive?"VccPullOn":"VccPullOff");
                Texture2D outTexture = RimPLD.get_texture(interActive?"VccPullOn":"VccPullOff");
                Texture2D lineTexture = RimPLD.get_texture(interActive?"On":"Off");
                float botmost = -0.5f;
                for(int y = 0; y < 3; ++y) {
                    if((pld.p2 & (1 << (y * 6 + x))) != 0) botmost = y;
                }
                GUI.DrawTexture(new Rect(ArrayLeftEdge + x * BoxStride + 15 + 8, ArrayTopEdge, 2, BoxStride * (9 + botmost) - 4), lineTexture);
                GUI.DrawTexture(new Rect(ArrayLeftEdge + x * BoxStride + 8, ArrayTopEdge - BoxStride, BoxSize, BoxSize), pullTexture);
                for(int y = 0; y < 8; ++y) {
                    bool inputActive = (pld.lastInputs & (1 << y)) != 0;
                    string made = "AndInput" + (inputActive ? "On" : "Off") + (interActive ? "On" : "Off") + "Make";
                    bool wasOn = (pld.p1 & (1L << (x * 8 + y))) != 0;
                    bool isOn = wasOn;
                    Widgets.Checkbox(
                        ArrayLeftEdge + x * BoxStride,
                        ArrayTopEdge + y * BoxStride + 8,
                        ref isOn,
                        BoxSize,
                        false,
                        true,
                        RimPLD.get_texture(made),
                        RimPLD.get_texture("AndInputBreak")
                    );
                    if(wasOn != isOn) {
                        // toggle that program bit
                        pld.p1 ^= (1L << (x * 8 + y));
                        if(isOn) {
                            // disallow impossible combinations
                            pld.p1 &= ~(1L << ((x * 8 + y) ^ 1));
                        }
                        dirty = true;
                    }
                }
                for(int y = 0; y < 3; ++y) {
                    bool outerActive = (pld.lastOuters & (1 << y)) != 0;
                    string made = "OrInput" + (interActive ? "On" : "Off") + (outerActive ? "On" : "Off") + "Make";
                    bool wasOn = (pld.p2 & (1 << (y * 6 + x))) != 0;
                    bool isOn = wasOn;
                    Widgets.Checkbox(
                        ArrayLeftEdge + x * BoxStride + 16,
                        InterTopEdge + y * BoxStride - 8,
                        ref isOn,
                        BoxSize,
                        false,
                        true,
                        RimPLD.get_texture(made),
                        RimPLD.get_texture("OrInputBreak")
                    );
                    if(wasOn != isOn) {
                        // toggle that program bit
                        pld.p2 ^= (1 << (y * 6 + x));
                        dirty = true;
                    }
                }
            }
            GUI.DrawTexture(new Rect(LatchLeft, LatchTop, LatchWidth, LatchHeight), RimPLD.get_texture("Latch"));
            bool switchState = pld.outA_is_latch;
            bool switchOn = (pld.lastOutputs & 1) != 0;
            Texture2D topTexture = RimPLD.get_texture("Switch"+(switchOn?"On":"Off")+"Top");
            Texture2D botTexture = RimPLD.get_texture("Switch"+(switchOn?"On":"Off")+"Bot");
            bool newSwitchState = switchState;
            Widgets.Checkbox(Output1KinkX-3f, Output1CenterY-SwitchSize*0.5f, ref newSwitchState, SwitchSize, false, false, topTexture, botTexture);
            if(newSwitchState != switchState) {
                pld.outA_is_latch = newSwitchState;
                dirty = true;
            }
            /* oh god it's about to get worse!!! */
            dirty = doInputControls(InputLeftEdge, Input1CenterY, pld.inputs[0], notTextureForInput(pld.lastInputs&3)) || dirty;
            dirty = doInputControls(InputLeftEdge, Input1CenterY+BoxStride*2, pld.inputs[1], notTextureForInput((pld.lastInputs>>2)&3)) || dirty;
            dirty = doInputControls(InputLeftEdge, Input1CenterY+BoxStride*6, pld.inputs[2], notTextureForInput((pld.lastInputs>>2)&3)) || dirty;
            dirty = doOutputControls(OutputLeftEdge, Output1CenterY, pld.outputs[0]) || dirty;
            dirty = doOutputControls(OutputLeftEdge, Output1CenterY+BoxStride*2, pld.outputs[1]) || dirty;
            dirty = doOutputControls(OutputLeftEdge, Output1CenterY+BoxStride*4, pld.outputs[2]) || dirty;
            if(dirty) {
                // Don't actually want this.
                //pld.execute();
            }
        }
        static bool doInputControls(float left, float top, Input input, string gateTextureName) {
            // top is actually the center of the topmost element
            Texture2D gateTexture = RimPLD.get_texture(gateTextureName);
            GUI.DrawTexture(
                new Rect(left+IOWidth+8, top+4, 16, 24),
                gateTexture
            );
            bool dirty = false;
            if(Widgets.ButtonText(
                new Rect(left, top-15f, IOWidth, 30f),
                CompPLD.getInputSourceLabel(input.source).Translate()
            )) {
                Find.WindowStack.Add(new FloatMenu(makeInputSourceOptions(input)));
                dirty = true;
            }
            if(input.needsDirection()) {
                if(doHat(ref input.dir, left + IOWidth - HatWidth, top + 15f + 3f))
                    dirty = true;
            }
            if(input.needsThreshold()) {
                string oldValue = input.thresholdForDisplay();
                string newValue = Widgets.TextField(
                    new Rect(left, top + 15, IOWidth-HatWidth, 30),
                    oldValue
                );
                if(newValue != oldValue) {
                    input.setThresholdFrom(newValue);
                    dirty = true;
                }
            }
            return dirty;
        }
        static bool doOutputControls(float left, float top, Output output) {
            // top is actually the center of the topmost element
            bool dirty = false;
            if(Widgets.ButtonText(
                new Rect(left, top-15f, IOWidth, 30f),
                CompPLD.getOutputSinkLabel(output.sink).Translate()
            )) {
                Find.WindowStack.Add(new FloatMenu(makeOutputSinkOptions(output)));
                dirty = true;
            }
            if(output.needsDirection()) {
                doHat(ref output.dir, left + IOWidth - HatWidth, top + 15f + 3f);
            }
            return dirty;
        }
        static List<FloatMenuOption> makeInputSourceOptions(Input input) {
            List<FloatMenuOption> ret = new List<FloatMenuOption>();
            // missing Enum.GetValues right now
            foreach(InputSource s in CompPLD.ALL_INPUT_SOURCES) {
                ret.Add(new FloatMenuOption(CompPLD.getInputSourceLabel(s).Translate(), () => { input.source = s; }));
            }
            return ret;
        }
        static List<FloatMenuOption> makeOutputSinkOptions(Output output) {
            List<FloatMenuOption> ret = new List<FloatMenuOption>();
            foreach(OutputSink s in CompPLD.ALL_OUTPUT_SINKS) {
                ret.Add(new FloatMenuOption(CompPLD.getOutputSinkLabel(s).Translate(), () => { output.sink = s; }));
            }
            return ret;
        }
        struct HatSegment {
            public float x, y;
            public Direction8Way dir;
            public string tex;
            public HatSegment(float x, float y, Direction8Way dir, string tex) {
                this.x = x * HatSegmentSize;
                this.y = y * HatSegmentSize;
                this.dir = dir;
                this.tex = tex;
            }
        }
        static readonly HatSegment[] HAT_SEGMENTS = new HatSegment[] {
            new HatSegment(0f, 0f, Direction8Way.NorthWest, "HatWN"),
            new HatSegment(1f, 0f, Direction8Way.North, "HatN"),
            new HatSegment(2f, 0f, Direction8Way.NorthEast, "HatEN"),
            new HatSegment(0f, 1f, Direction8Way.West, "HatW"),
            new HatSegment(1f, 1f, Direction8Way.Invalid, "HatC"),
            new HatSegment(2f, 1f, Direction8Way.East, "HatE"),
            new HatSegment(0f, 2f, Direction8Way.SouthWest, "HatWS"),
            new HatSegment(1f, 2f, Direction8Way.South, "HatS"),
            new HatSegment(2f, 2f, Direction8Way.SouthEast, "HatES"),
        };
        static bool doHat(ref Direction8Way dir, float x, float y) {
            bool ret = false;
            foreach(HatSegment seg in HAT_SEGMENTS) {
                if(seg.dir == dir) {
                    GUI.DrawTexture(new Rect(x, y, HatWidth, HatHeight), RimPLD.get_texture(seg.tex));
                }
                if(UnityEngine.Input.GetMouseButton(0) && Mouse.IsOver(new Rect(x + seg.x, y + seg.y, HatSegmentSize, HatSegmentSize))) {
                    if(dir != seg.dir) {
                        dir = seg.dir;
                        SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
                        ret = true;
                    }
                }
            }
            return ret;
        }
        static string notTextureForInput(int i) {
            switch(i) {
                default: return "NotDead";
                case 1: return "NotOn";
                case 2: return "NotOff";
            }
        }
    }
}