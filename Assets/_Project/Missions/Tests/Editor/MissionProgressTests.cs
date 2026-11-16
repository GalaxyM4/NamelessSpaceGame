// MissionProgressTests.cs — tests EditMode del conteo de muestras de una misión.
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Game.Missions.Tests
{
    public class MissionProgressTests
    {
        readonly List<Object> _created = new List<Object>();
        RockTypeData _basalto, _olivino, _gabro;

        [SetUp]
        public void SetUp()
        {
            _basalto = Rock("Rochette");
            _olivino = Rock("Seitah");
            _gabro = Rock("Singita");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _created) Object.DestroyImmediate(o);
            _created.Clear();
        }

        RockTypeData Rock(string id)
        {
            var r = ScriptableObject.CreateInstance<RockTypeData>();
            r.id = id; r.shortName = id;
            _created.Add(r);
            return r;
        }

        MissionDefinition Mission(params (RockTypeData type, int amount)[] objectives)
        {
            var m = ScriptableObject.CreateInstance<MissionDefinition>();
            m.id = "test";
            foreach (var (t, n) in objectives) m.objectives.Add(new MissionObjective { rockType = t, amount = n });
            _created.Add(m);
            return m;
        }

        [Test]
        public void RocaNoPedida_NoSuma()
        {
            var p = new MissionProgress(Mission((_basalto, 5)));
            Assert.AreEqual(SampleCountResult.NoPedida, p.TryAdd(_gabro));
            Assert.AreEqual(0, p.ValidCollected);
            Assert.IsFalse(p.AllComplete);
        }

        [Test]
        public void RocaNula_NoSuma()
        {
            var p = new MissionProgress(Mission((_basalto, 1)));
            Assert.AreEqual(SampleCountResult.NoPedida, p.TryAdd(null));
            Assert.AreEqual(0, p.ValidCollected);
        }

        [Test]
        public void NoSuperaLaCantidadPedida()
        {
            var p = new MissionProgress(Mission((_basalto, 2)));
            Assert.AreEqual(SampleCountResult.Contada, p.TryAdd(_basalto));
            Assert.AreEqual(SampleCountResult.Contada, p.TryAdd(_basalto));
            Assert.AreEqual(SampleCountResult.YaCompleto, p.TryAdd(_basalto));
            Assert.AreEqual(2, p.Find(_basalto).collected);
            Assert.AreEqual(2, p.ValidCollected);
            Assert.IsTrue(p.AllComplete);
        }

        [Test]
        public void VariosTipos_SoloCompletaConTodos()
        {
            var p = new MissionProgress(Mission((_olivino, 3), (_basalto, 2)));
            for (int i = 0; i < 3; i++) p.TryAdd(_olivino);
            Assert.IsTrue(p.Find(_olivino).IsComplete);
            Assert.IsFalse(p.AllComplete);

            p.TryAdd(_basalto);
            Assert.IsFalse(p.AllComplete);
            p.TryAdd(_basalto);
            Assert.IsTrue(p.AllComplete);
            Assert.AreEqual(5, p.ValidCollected);
            Assert.AreEqual(5, p.TotalRequired);
        }

        [Test]
        public void TipoCompleto_NoSuma_MientrasOtroSigueAbierto()
        {
            var p = new MissionProgress(Mission((_olivino, 1), (_basalto, 2)));
            p.TryAdd(_olivino);
            Assert.AreEqual(SampleCountResult.YaCompleto, p.TryAdd(_olivino));
            Assert.AreEqual(1, p.ValidCollected);
        }

        [Test]
        public void ObjetivosRepetidosDelMismoTipo_SeSuman()
        {
            var p = new MissionProgress(Mission((_basalto, 2), (_basalto, 3)));
            Assert.AreEqual(1, p.Objectives.Count);
            Assert.AreEqual(5, p.Find(_basalto).required);
        }

        [Test]
        public void SinTubosSuficientes_NoSePuedeCompletar()
        {
            var p = new MissionProgress(Mission((_basalto, 5)));
            p.TryAdd(_basalto); p.TryAdd(_basalto);          // faltan 3
            Assert.AreEqual(3, p.Remaining);
            Assert.IsTrue(p.CanStillComplete(3));            // justo los necesarios
            Assert.IsTrue(p.CanStillComplete(8));
            Assert.IsFalse(p.CanStillComplete(2));           // ya no alcanza
        }

        [Test]
        public void MisionSinObjetivos_NuncaEstaCompleta()
        {
            var p = new MissionProgress(Mission());
            Assert.IsFalse(p.AllComplete);
        }
    }
}
