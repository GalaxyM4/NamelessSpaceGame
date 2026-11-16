// MissionProgressTests.cs — EditMode tests for counting the samples of a mission.
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Game.Missions.Tests
{
    public class MissionProgressTests
    {
        readonly List<Object> _created = new List<Object>();
        RockTypeData _basalt, _olivine, _gabbro;

        [SetUp]
        public void SetUp()
        {
            _basalt = Rock("Rochette");
            _olivine = Rock("Seitah");
            _gabbro = Rock("Singita");
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
        public void NotRequestedRock_DoesNotCount()
        {
            var p = new MissionProgress(Mission((_basalt, 5)));
            Assert.AreEqual(SampleCountResult.NotRequested, p.TryAdd(_gabbro));
            Assert.AreEqual(0, p.ValidCollected);
            Assert.IsFalse(p.AllComplete);
        }

        [Test]
        public void NullRock_DoesNotCount()
        {
            var p = new MissionProgress(Mission((_basalt, 1)));
            Assert.AreEqual(SampleCountResult.NotRequested, p.TryAdd(null));
            Assert.AreEqual(0, p.ValidCollected);
        }

        [Test]
        public void NeverExceedsRequestedAmount()
        {
            var p = new MissionProgress(Mission((_basalt, 2)));
            Assert.AreEqual(SampleCountResult.Counted, p.TryAdd(_basalt));
            Assert.AreEqual(SampleCountResult.Counted, p.TryAdd(_basalt));
            Assert.AreEqual(SampleCountResult.AlreadyComplete, p.TryAdd(_basalt));
            Assert.AreEqual(2, p.Find(_basalt).collected);
            Assert.AreEqual(2, p.ValidCollected);
            Assert.IsTrue(p.AllComplete);
        }

        [Test]
        public void SeveralTypes_CompleteOnlyWithAll()
        {
            var p = new MissionProgress(Mission((_olivine, 3), (_basalt, 2)));
            for (int i = 0; i < 3; i++) p.TryAdd(_olivine);
            Assert.IsTrue(p.Find(_olivine).IsComplete);
            Assert.IsFalse(p.AllComplete);

            p.TryAdd(_basalt);
            Assert.IsFalse(p.AllComplete);
            p.TryAdd(_basalt);
            Assert.IsTrue(p.AllComplete);
            Assert.AreEqual(5, p.ValidCollected);
            Assert.AreEqual(5, p.TotalRequired);
        }

        [Test]
        public void CompleteType_DoesNotCount_WhileAnotherIsOpen()
        {
            var p = new MissionProgress(Mission((_olivine, 1), (_basalt, 2)));
            p.TryAdd(_olivine);
            Assert.AreEqual(SampleCountResult.AlreadyComplete, p.TryAdd(_olivine));
            Assert.AreEqual(1, p.ValidCollected);
        }

        [Test]
        public void RepeatedObjectivesOfSameType_AreAdded()
        {
            var p = new MissionProgress(Mission((_basalt, 2), (_basalt, 3)));
            Assert.AreEqual(1, p.Objectives.Count);
            Assert.AreEqual(5, p.Find(_basalt).required);
        }

        [Test]
        public void NotEnoughTubes_CannotComplete()
        {
            var p = new MissionProgress(Mission((_basalt, 5)));
            p.TryAdd(_basalt); p.TryAdd(_basalt);          // 3 missing
            Assert.AreEqual(3, p.Remaining);
            Assert.IsTrue(p.CanStillComplete(3));            // exactly enough
            Assert.IsTrue(p.CanStillComplete(8));
            Assert.IsFalse(p.CanStillComplete(2));           // not enough anymore
        }

        [Test]
        public void MissionWithoutObjectives_IsNeverComplete()
        {
            var p = new MissionProgress(Mission());
            Assert.IsFalse(p.AllComplete);
        }
    }
}
