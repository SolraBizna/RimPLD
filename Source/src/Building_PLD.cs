using RimWorld;
using System.Runtime.Remoting.Messaging;
using System.Text;
using Verse;

namespace RimPLD {
    [StaticConstructorOnStartup]
    public class Building_PLD : Building {
        CompPLD pld;
        CompPowerTrader power;
        public override void SpawnSetup(Map map, bool respawningAfterLoad) {
            base.SpawnSetup(map, respawningAfterLoad);
            pld = this.GetComp<CompPLD>();
            if(pld != null) {
                pld.execute();
            }
            power = this.GetComp<CompPowerTrader>();
        }
        public override void TickRare() {
            base.TickRare();
            if(pld != null) {
                int powerIsOn = 1;
                if(power != null) {
                    powerIsOn = power.PowerOn ? 1 : 0;
                }
                pld.execute(powerIsOn);
                if(powerIsOn == 0 && power != null) {
                    if(power.props is CompProperties_Power prop) {
                        power.PowerOutput = -prop.PowerConsumption;
                    } else {
                        power.PowerOutput = -20;
                    }
                } else {
                    // miniscule power leaks statically, the rest is
                    // from pull resistors
                    int watts = 2;
                    for(int n = 0; n < 6; ++n) {
                        if((pld.lastInters & (1<<n)) == 0)
                            ++watts;
                    }
                    for(int n = 0; n < 3; ++n) {
                        if((pld.lastOuters & (1<<n)) != 0)
                            ++watts;
                    }
                    power.PowerOutput = -watts;
                }
            }
        }
    }
}