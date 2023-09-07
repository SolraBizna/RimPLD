using RimWorld;
using System.Collections.Generic;
using System.Deployment.Internal;
using UnityEngine;
using Verse;

namespace RimPLD {
  [StaticConstructorOnStartup]
  public static class RimPLD {
    static Dictionary<string, Texture2D> texture_cache = new Dictionary<string, Texture2D>();
    static RimPLD() {
    }
    public static Texture2D get_texture(string name) {
      if(name.Length > 3 && Prefs.UIScale != 1.0) name += "x2";
      Texture2D ret = texture_cache.TryGetValue(name);
      if(ret == null) {
        // doesn't work but oh well
        if(texture_cache.ContainsKey(name)) return null;
        ret = ContentFinder<Texture2D>.Get("UI/RimPLD/"+name);
        texture_cache.Add(name, ret);
      }
      return ret;
    }
    public static IntVec3 displace(IntVec3 v, Direction8Way dir) {
      switch(dir) {
        case Direction8Way.North: return v + IntVec3.North;
        case Direction8Way.NorthEast: return v + IntVec3.NorthEast;
        case Direction8Way.NorthWest: return v + IntVec3.NorthWest;
        case Direction8Way.South: return v + IntVec3.South;
        case Direction8Way.SouthEast: return v + IntVec3.SouthEast;
        case Direction8Way.SouthWest: return v + IntVec3.SouthWest;
        case Direction8Way.East: return v + IntVec3.East;
        case Direction8Way.West: return v + IntVec3.West;
        default: return v;
      }
    }
  }
}