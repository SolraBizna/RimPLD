using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using Verse;

namespace RimPLD {
    public enum InputSource {
        None,
        PowerFlow,
        EnergyStore,
        Temperature,
        LightLevel,
        IsFlickedOn,
        OutputA,
        OutputB,
        OutputC,
    }
    public enum OutputSink {
        None, FlickOn, FlickOff, FlickToggle, Control,
    }
    public class Input : IExposable {
        public InputSource source = InputSource.None;
        public float threshold = 0f;
        public Direction8Way dir = Direction8Way.Invalid;
        public void ExposeData() {
            Scribe_Values.Look(ref source, "source", InputSource.None, false);
            // we don't save/load the unneeded values
            if(needsThreshold()) {
                Scribe_Values.Look(ref threshold, "threshold", 0f, false);
            }
            if(needsDirection()) {
                Scribe_Values.Look(ref dir, "Dir", Direction8Way.Invalid, false);
            }
        }
        public bool needsDirection() {
            switch(source) {
                case InputSource.None:
                    return false;
                default:
                    return true;
            }
        }
        public bool needsThreshold() {
            switch(source) {
                case InputSource.None:
                case InputSource.IsFlickedOn:
                case InputSource.OutputA:
                case InputSource.OutputB:
                case InputSource.OutputC:
                    return false;
                default:
                    return true;
            }
        }
        public float thresholdMin() {
            switch(source) {
                case InputSource.PowerFlow: return -1000000f;
                case InputSource.EnergyStore: return 0f;
                case InputSource.Temperature: return 0f;
                case InputSource.LightLevel: return 0f;
                default: return 0f;
            }
        }
        public float thresholdMax() {
            switch(source) {
                case InputSource.PowerFlow: return 1000000f;
                case InputSource.EnergyStore: return 1000000f;
                case InputSource.Temperature: return 1273.15f; // 1000°C
                case InputSource.LightLevel: return 100f;
                default: return 0f;
            }
        }
        public float precision() {
            switch(source) {
                case InputSource.PowerFlow: return 1f;
                case InputSource.EnergyStore: return 100f;
                case InputSource.Temperature: return 100f;
                case InputSource.LightLevel: return 1f;
                default: return 0f;
            }
        }
        public string displayFormat() {
            switch(source) {
                case InputSource.EnergyStore: return "0.##";
                case InputSource.Temperature: return "0.##";
                default: return "0";
            }
        }
        public string thresholdForDisplay() {
            float ret = threshold;
            if(source == InputSource.Temperature) {
                switch(Prefs.TemperatureMode) {
                case TemperatureDisplayMode.Celsius:
                    ret = threshold + 273.15f;
                    break;
                case TemperatureDisplayMode.Fahrenheit:
                    ret = (threshold + 273.15f) * 1.8f + 32f;
                    break;
                }
            }
            ret = Mathf.Floor(threshold * precision() + 0.5f) / precision();
            return ret.ToString(displayFormat());
        }
        public void setThresholdFrom(string instring) {
            try {
                float input = float.Parse(instring, System.Threading.Thread.CurrentThread.CurrentUICulture);
                input = Mathf.Floor(input * precision() + 0.5f) / precision();
                if(input > thresholdMax()) input = thresholdMax();
                else if(input < thresholdMin()) input = thresholdMin();
                if(source == InputSource.Temperature) {
                    switch(Prefs.TemperatureMode) {
                    case TemperatureDisplayMode.Celsius:
                        threshold = input - 273.15f;
                        return;
                    case TemperatureDisplayMode.Fahrenheit:
                        threshold = (input - 32f) / 1.8f - 273.15f;
                        return;
                    }
                }
                threshold = input;
            } catch (Exception) {} // ignore exceptions :)
        }
    }
    public class Output : IExposable {
        public OutputSink sink = OutputSink.None;
        public Direction8Way dir = Direction8Way.Invalid;
        public void ExposeData() {
            Scribe_Values.Look(ref sink, "sink", OutputSink.None, false);
            if(needsDirection()) {
                Scribe_Values.Look(ref dir, "dir", Direction8Way.Invalid, false);
            }
        }
        public bool needsDirection() {
            switch(sink) {
                case OutputSink.None:
                    return false;
                default:
                    return true;
            }
        }
    }
    public class CompPLD : ThingComp {
        public static readonly InputSource[] ALL_INPUT_SOURCES = new InputSource[] {
            InputSource.None,
            InputSource.PowerFlow,
            InputSource.EnergyStore,
            InputSource.Temperature,
            InputSource.LightLevel,
            InputSource.IsFlickedOn,
        };
        public static readonly OutputSink[] ALL_OUTPUT_SINKS = new OutputSink[] {
            OutputSink.None,
            OutputSink.FlickOff,
            OutputSink.FlickOn,
            OutputSink.FlickToggle,
            OutputSink.Control,
        };
        public static string getInputSourceLabel(InputSource s) {
            return "RimPLD.I."+s;
        }
        public static string getOutputSinkLabel(OutputSink s) {
            return "RimPLD.O."+s;
        }
        public const int NUM_INPUTS = 3;
        public const int NUM_OUTPUTS = 3;
        public const byte INPUT_A_HI = 1 << 0;
        public const byte INPUT_A_LO = 1 << 1;
        public const byte INPUT_B_HI = 1 << 2;
        public const byte INPUT_B_LO = 1 << 3;
        public const byte INPUT_L_HI = 1 << 4;
        public const byte INPUT_L_LO = 1 << 5;
        public const byte INPUT_C_HI = 1 << 6;
        public const byte INPUT_C_LO = 1 << 7;
        public long p1 = 0;
        public int p2 = 0;
        public byte lastInputs = 0;
        public byte lastInters = 0;
        public byte lastOuters = 0;
        public byte lastOutputs = 0;
        public Input[] inputs = new Input[NUM_INPUTS]{new Input(), new Input(), new Input()};
        public Output[] outputs = new Output[NUM_OUTPUTS]{new Output(), new Output(), new Output()};
        public byte andins1 {
            get { return (byte)((p1 >> 0) & 0xFF); }
            set { p1 = p1 & ~(0xFFL << 0) | ((long)value << 0); }
        }
        public byte andins2 {
            get { return (byte)((p1 >> 8) & 0xFF); }
            set { p1 = p1 & ~(0xFFL << 8) | ((long)value << 8); }
        }
        public byte andins3 {
            get { return (byte)((p1 >> 16) & 0xFF); }
            set { p1 = p1 & ~(0xFFL << 16) | ((long)value << 16); }
        }
        public byte andins4 {
            get { return (byte)((p1 >> 24) & 0xFF); }
            set { p1 = p1 & ~(0xFFL << 24) | ((long)value << 24); }
        }
        public byte andins5 {
            get { return (byte)((p1 >> 32) & 0xFF); }
            set { p1 = p1 & ~(0xFFL << 32) | ((long)value << 32); }
        }
        public byte andins6 {
            get { return (byte)((p1 >> 40) & 0xFF); }
            set { p1 = p1 & ~(0xFFL << 40) | ((long)value << 40); }
        }
        public byte orins1 {
            get { return (byte)((p2 >> 0) & 0x3F); }
            set { p2 = p2 & ~(0x3F << 0) | ((int)value << 0); }
        }
        public byte orins2 {
            get { return (byte)((p2 >> 6) & 0x3F); }
            set { p2 = p2 & ~(0x3F << 6) | ((int)value << 6); }
        }
        public byte orins3 {
            get { return (byte)((p2 >> 12) & 0x3F); }
            set { p2 = p2 & ~(0x3F << 12) | ((int)value << 12); }
        }
        public bool outA_is_latch {
            get { return ((p2 >> 18) & 1) != 0; }
            set { if(value) p2 |= (1 << 18); else p2 &= ~(1 << 18); }
        }
        public bool latch_state {
            get { return ((p2 >> 19) & 1) != 0; }
            set { if(value) p2 |= (1 << 19); else p2 &= ~(1 << 19); }
        }
        public bool powerWasOn {
            get { return ((lastOutputs >> 7) & 1) != 0; }
            set { if(value) lastOutputs = (byte)(lastOutputs | (1 << 7)); else lastOutputs = (byte)(lastOutputs & ~(1 << 7)); }
        }
        public override void PostExposeData() {
            base.PostExposeData();
            Scribe_Values.Look(ref p1, "p1", 0L, true);
            Scribe_Values.Look(ref p2, "p2", 0, true);
            // :|
            int fakeLastOutputs = (int)lastOutputs;
            Scribe_Values.Look(ref fakeLastOutputs, "lastOutputs", 0, true);
            if(lastOutputs != fakeLastOutputs) lastOutputs = (byte)fakeLastOutputs;
            List<Input> inputs = this.inputs.ToList();
            Scribe_Collections.Look(ref inputs, "inputs", LookMode.Deep);
            if(inputs != null && inputs.Count == NUM_INPUTS) {
                this.inputs = inputs.ToArray();
            }
            List<Output> outputs = this.outputs.ToList();
            Scribe_Collections.Look(ref outputs, "outputs", LookMode.Deep);
            if(outputs != null && outputs.Count == NUM_OUTPUTS) {
                this.outputs = outputs.ToArray();
            }
        }
        public void execute(int extPowerIsOn = -1) {
            bool powerIsOn = extPowerIsOn >= 0 ? extPowerIsOn != 0 : powerWasOn;
            byte inputs = 0, inters = 0, outers = 0, outputs = 0;
            if(!powerIsOn) {
                latch_state = Rand.Bool;
            } else {
                if(getInput(this.inputs[0]))
                    inputs |= INPUT_A_HI;
                else inputs |= INPUT_A_LO;
                if(getInput(this.inputs[1]))
                    inputs |= INPUT_B_HI;
                else inputs |= INPUT_B_LO;
                if(latch_state) inputs |= INPUT_L_HI; else inputs |= INPUT_L_LO;
                if(getInput(this.inputs[2]))
                    inputs |= INPUT_C_HI;
                else inputs |= INPUT_C_LO;
                inters = 0;
                if((inputs & andins1) == andins1) inters |= 1 << 0;
                if((inputs & andins2) == andins2) inters |= 1 << 1;
                if((inputs & andins3) == andins3) inters |= 1 << 2;
                if((inputs & andins4) == andins4) inters |= 1 << 3;
                if((inputs & andins5) == andins5) inters |= 1 << 4;
                if((inputs & andins6) == andins6) inters |= 1 << 5;
                outers = 0;
                bool latch_on = false;
                bool latch_off = false;
                if((inters & orins1) != 0) {
                    outers |= 1 << 0;
                    latch_on = true;
                }
                if((inters & orins2) != 0) {
                    outers |= 1 << 1;
                    latch_off = true;
                }
                if((inters & orins3) != 0) {
                    outers |= 1 << 2;
                    // TODO: LED?
                }
                if(latch_on != latch_off) {
                    if(latch_on) latch_state = true;
                    else if(latch_off) latch_state = false;
                }
                outputs = outers;
                if(outA_is_latch) {
                    outputs &= (byte)0xFE;
                    if(latch_state) {
                        outputs |= 1;
                    }
                }
            }
            lastInputs = inputs;
            lastInters = inters;
            lastOuters = outers;
            doOutput(this.outputs[0], (outputs & (1<<0)) != 0, (lastOutputs & (1<<0)) != 0);
            doOutput(this.outputs[1], (outputs & (1<<1)) != 0, (lastOutputs & (1<<1)) != 0);
            doOutput(this.outputs[2], (outputs & (1<<2)) != 0, (lastOutputs & (1<<2)) != 0);
            lastOutputs = outputs;
            powerWasOn = powerIsOn;
        }
        protected bool getInput(Input input) {
            IntVec3 pos = parent.Position;
            Map map = parent.Map;
            if(input.needsDirection()) pos = RimPLD.displace(pos, input.dir);
            switch(input.source) {
                case InputSource.PowerFlow: {
                    PowerNet net = pos.GetFirstThingWithComp<CompPowerTrader>(map)?.TryGetComp<CompPowerTrader>()?.PowerNet;
                    if(net != null) {
                        return net.CurrentEnergyGainRate() / CompPower.WattsToWattDaysPerTick >= input.threshold;
                    }
                } break;
                case InputSource.EnergyStore: {
                    PowerNet net = pos.GetFirstThingWithComp<CompPowerTrader>(map)?.TryGetComp<CompPowerTrader>()?.PowerNet;
                    if(net != null) {
                        return net.CurrentStoredEnergy() >= input.threshold;
                    }
                } break;
                case InputSource.Temperature: {
                    if(!pos.Impassable(map)) {
                        return pos.GetTemperature(map) >= input.threshold;
                    }
                } break;
                case InputSource.LightLevel:
                    return map.glowGrid.GameGlowAt(pos, false) * 100f >= input.threshold;
                case InputSource.OutputA: {
                    CompPLD pld = pos.GetFirstThingWithComp<CompPLD>(map)?.TryGetComp<CompPLD>();;
                    if(pld != null) {
                        return (pld.lastOutputs & 1) != 0;
                    }
                } break;
                case InputSource.OutputB: {
                    CompPLD pld = pos.GetFirstThingWithComp<CompPLD>(map)?.TryGetComp<CompPLD>();;
                    if(pld != null) {
                        return (pld.lastOutputs & 2) != 0;
                    }
                } break;
                case InputSource.OutputC: {
                    CompPLD pld = pos.GetFirstThingWithComp<CompPLD>(map)?.TryGetComp<CompPLD>();;
                    if(pld != null) {
                        return (pld.lastOutputs & 4) != 0;
                    }
                } break;
            }
            return false;
        }
        protected void doOutput(Output output, bool nextValue, bool prevValue) {
            IntVec3 pos = parent.Position;
            Map map = parent.Map;
            if(output.needsDirection()) pos = RimPLD.displace(pos, output.dir);
            switch(output.sink) {
                case OutputSink.Control: {
                    foreach(Thing thing in pos.GetThingList(map)) {
                        CompFlickable flick = thing.TryGetComp<CompFlickable>();
                        if(flick == null) continue;
                        if(flick.SwitchIsOn != nextValue) {
                            flick.DoFlick();
                            fixupWantFlick(flick);
                        }
                    }
                } break;
                case OutputSink.FlickOff: {
                    if(prevValue || !nextValue) break; // positive edge only
                    foreach(Thing thing in pos.GetThingList(map)) {
                        CompFlickable flick = thing.TryGetComp<CompFlickable>();
                        if(flick == null) continue;
                        if(flick.SwitchIsOn) {
                            flick.DoFlick();
                            fixupWantFlick(flick);
                        }
                    }
                } break;
                case OutputSink.FlickOn: {
                    if(prevValue || !nextValue) break; // positive edge only
                    foreach(Thing thing in pos.GetThingList(map)) {
                        CompFlickable flick = thing.TryGetComp<CompFlickable>();
                        if(flick == null) continue;
                        if(!flick.SwitchIsOn) {
                            flick.DoFlick();
                            fixupWantFlick(flick);
                        }
                    }
                } break;
                case OutputSink.FlickToggle: {
                    if(prevValue || !nextValue) break; // positive edge only
                    foreach(Thing thing in pos.GetThingList(map)) {
                        CompFlickable flick = thing.TryGetComp<CompFlickable>();
                        if(flick == null) continue;
                        flick.DoFlick();
                        fixupWantFlick(flick);
                    }
                } break;
            }
        }
        private static void fixupWantFlick(CompFlickable flick) {
            // GROSS
            typeof(CompFlickable)
                .GetField("wantSwitchOn", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(flick, flick.SwitchIsOn);
        }
    }
}