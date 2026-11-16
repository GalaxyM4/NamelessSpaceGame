// RockRegistry.cs — rocas del terreno que se pueden inspeccionar sin componentes por roca
// (el campo de rocas tiene ~11 700 piedras; RocasMarteBootstrap lo rellena al cargar la escena).
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
