// RockRegistry.cs — terrain rocks that can be inspected without a component per rock
// (the rock field has ~11,700 rocks; RocasMarteBootstrap fills it when the scene loads).
using System.Collections.Generic;
using UnityEngine;

namespace RocasMarte
{
    public static class RockRegistry
    {
        public struct Entry
        {
            public Vector3 position;
            public float radius;
            public string id;
        }

        public static readonly List<Entry> Field = new List<Entry>();
    }
}
