using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Game;
using Rasa.Structures.Char;
using Rasa.Services.Preloader.Missions.Pravus;
using Rasa.Services.Preloader.Missions.Wilderness;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.LootDispenser.Client;
using Rasa.Structures;
using Rasa.Test.Missions.Wilderness;
using static Rasa.Test.Missions.Wilderness.WildernessTwinPillarsTests;

namespace Rasa.Test.Missions
{
    [TestClass, DoNotParallelize]
    public class PravusOperationTests
    {
        private const string Pravus = "adv_foreas_concordia_wilderness_pravusresearch";
        private const string Wilderness = "adv_foreas_concordia_wilderness";

        [TestMethod]
        [DataRow(false, PartyLootMethod.Individual)]
        [DataRow(true, PartyLootMethod.Individual)]
        [DataRow(true, PartyLootMethod.FreeForAll)]
        [DataRow(true, PartyLootMethod.Rotation)]
        public void ActualChainLootFactoryRewardAndArmbandReturnArePlayable(bool withSquadMember, PartyLootMethod method)
        {
            using var h = Create();
            h.SpawnWorld(207, 183, 197);
            var baruhi = RequireNpc(h, 183, 106);
            var parsons = RequireNpc(h, 197, 119);
            Accept(h, RequireNpc(h, 207, 129), 570);
            Talk(h, baruhi, 570, 1);
            Reward(h, baruhi, 570, 1, 20846, 8000, 1200);
            Accept(h, baruhi, 574);
            CollectMachinaRemains(h);
            Talk(h, parsons, 574, 3);
            Reward(h, parsons, 574, 0, 20548, 8000, 1200);
            Accept(h, parsons, 593);

            h.EnterMap(1430, Pravus);
            h.Map.IsSquadInstance = true;
            h.Map.InstanceId = 1430001;
            h.SpawnWorld(640001, 640401, 640411);
            var nylla = RequireNpc(h, 640001, 640001);
            Talk(h, nylla, 593, 1);
            Reward(h, nylla, 593, null, null, 2500, 500);
            Accept(h, nylla, 575);
            var member = withSquadMember ? JoinOperation(h, nylla) : null;
            using var party = member == null ? null : new MissionQualifiedLootTests.PartyScope(method, h.Client, member);
            var operation = PravusOperation.For(h.Map);
            Assert.AreSame(operation, PravusOperation.For(h.Map), "Assignments must share one world encounter.");
            var guard = Living(h).First(creature => creature.DbId == 640303);
            if (member != null) member.SetWorldPosition(guard.Position, 0);
            Kill(h, guard);
            Assert.AreEqual(0L, Held(h, 11505));
            Assert.AreEqual(MissionObjectiveState.Incomplete, h.Client.Player.Missions[575].Objectives[3].State);
            var corpse = h.Map.LootDispensers[guard.CorpseLootEntityId];
            Assert.AreEqual(1U, corpse.LootItems.Single(item => item.ItemTemplateId == 11505 && item.MayTake(h.Client.Player.EntityId)).ItemQuantity);
            LootDispenserManager.Instance.RequestLootAllFromCorpse(h.Client,
                new RequestLootAllFromCorpsePacket { EntityId = corpse.EntityId });
            Assert.AreEqual(1L, Held(h, 11505));
            Assert.AreEqual(MissionObjectiveState.Completed, h.Client.Player.Missions[575].Objectives[3].State);
            if (member != null)
            {
                Assert.AreEqual(1, corpse.LootItems.Count(item => item.ItemTemplateId == 11505 && !item.Taken && item.MayTake(member.Player.EntityId)));
                LootDispenserManager.Instance.RequestLootAllFromCorpse(member,
                    new RequestLootAllFromCorpsePacket { EntityId = corpse.EntityId });
                Assert.AreEqual(MissionObjectiveState.Completed, member.Player.Missions[575].Objectives[3].State);
                member.SetWorldPosition(PravusOperation.Factory, 0);
            }
            Reload(h);
            Assert.AreEqual(1L, Held(h, 11505), "The key must survive a reconnect.");
            Talk(h, RequireNpc(h, 640411, 640312), 575, 4);
            h.MoveTo(PravusOperation.Factory);
            operation.Tick(1000);
            Assert.IsTrue(operation.FactoryStarted);
            Assert.AreEqual(12, Living(h).Count(c => c.SpawnPool.DbId is >= 640408 and <= 640410));
            Assert.AreEqual(3, Living(h).Count(c => c.SpawnPool.DbId == 640422));
            operation.Tick(30_000);
            operation.Tick(60_000);
            Assert.AreEqual(6, Living(h).Count(c => c.SpawnPool.DbId == 640422), "Production must be capped.");
            Assert.AreEqual(MissionObjectiveState.Incomplete, h.Client.Player.Missions[575].Objectives[2].State);
            Assert.IsTrue(PlacedDestructibles.Destroy(operation.Capsule, h.Client.Player));
            operation.Tick(61_000);
            Assert.AreEqual(MissionObjectiveState.Completed, h.Client.Player.Missions[575].Objectives[2].State);
            Assert.IsFalse(h.Client.Player.Missions[575].Completeable, "Capsule destruction is not the room clear.");
            PlacedDestructibles.Worker(h.Map);
            Assert.IsTrue(operation.Capsule.IsDown);
            foreach (var creature in Living(h).Where(c => c.SpawnPool.DbId is >= 640408 and <= 640410 or 640422).ToArray())
                Kill(h, creature);
            h.MoveTo(PravusOperation.Factory);
            operation.Tick(120_000);
            Assert.IsTrue(operation.FactoryCleared);
            Assert.IsTrue(h.Client.Player.Missions[575].Completeable);
            if (member != null) Assert.IsTrue(member.Player.Missions[575].Completeable);
            if (!withSquadMember)
            {
                Assert.IsTrue(h.Manager.TryAbandon(h.Client, 575));
                Assert.AreEqual(1L, Held(h, 11505));
                Accept(h, nylla, 575);
                Assert.AreEqual(MissionObjectiveState.Completed, h.Client.Player.Missions[575].Objectives[3].State);
                h.MoveTo(PravusOperation.Factory);
                operation.Tick(130_000);
                Assert.IsTrue(h.Client.Player.Missions[575].Completeable,
                    "Reaccepting in a cleared instance must reconcile its facts without spawning another factory.");
                Assert.AreEqual(0, Living(h).Count(c => c.SpawnPool.DbId is >= 640408 and <= 640410 or 640422));
            }
            Reload(h);
            var finaleNylla = RequireNpc(h, 640414, 640001);
            Reward(h, finaleNylla, 575, null, null, 10000, 1050);
            Assert.AreEqual(1L, Held(h, 3002));
            Assert.AreEqual(0L, Held(h, 11505), "Turn-in consumes the real access key.");
            Accept(h, finaleNylla, 1075);
            Assert.AreEqual(1L, Held(h, 45439));
            Reload(h);
            Assert.AreEqual(1L, Held(h, 45439));
            h.EnterMap(1220, Wilderness);
            Talk(h, baruhi, 1075, 1);
            Assert.AreEqual(0L, Held(h, 45439));
            Reward(h, baruhi, 1075, null, null, 5000, 1000);
            Assert.IsTrue(h.Client.Player.MissionSuccessHistory.Contains(1075));
        }

        [TestMethod]
        public void FrontlinesDeliversFiveAtATimeAndFifteenRealKillsUnlockPerkinsReward()
        {
            using var h = Create();
            h.EnterMap(1430, Pravus);
            h.SpawnWorld(640002, 640003);
            Accept(h, RequireNpc(h, 640002, 640002), 321);
            var operation = PravusOperation.For(h.Map);
            long now = 1000;
            for (var wave = 0; wave < 3; wave++)
            {
                h.MoveTo(PravusOperation.Front);
                operation.Tick(now);
                var pool = h.Map.SpawnPools.Single(p => p.DbId == 640421);
                Assert.AreEqual(5, pool.QueuedCreatures);
                operation.Tick(now + 1);
                Assert.AreEqual(5, pool.QueuedCreatures, "No duplicate ship while one is delivering.");
                for (var phase = 0; phase < 7; phase++) h.Objects.DropshipsWorker(h.Map, 10000);
                var enemies = Living(h).Where(c => c.DbId == 640301).ToArray();
                Assert.AreEqual(5, enemies.Length);
                foreach (var enemy in enemies) Kill(h, enemy);
                operation.Tick(now + 100);
                operation.Tick(now + 1000);
                Assert.AreEqual(0, pool.QueuedCreatures, "Give the squad a rest between detachments.");
                now += 20_000;
            }
            Assert.IsTrue(h.Client.Player.Missions[321].Completeable);
            Reward(h, RequireNpc(h, 640003, 640003), 321, 2, 2821, 7500, 1000);
            h.MoveTo(PravusOperation.Front);
            operation.Tick(now);
            Assert.AreEqual(0, h.Map.SpawnPools.Single(p => p.DbId == 640421).QueuedCreatures);
        }

        [TestMethod]
        public void RadioRequiresAllThreeSitesAndDestroyedObjectivesDoNotRespawn()
        {
            using var h = Create();
            h.EnterMap(1430, Pravus);
            h.SpawnWorld(640412);
            var johnson = RequireNpc(h, 640412, 640311);
            Accept(h, johnson, 323);
            var operation = PravusOperation.For(h.Map);
            foreach (var index in new[] { 2, 0, 1 })
            {
                var infestation = operation.Infestations[index];
                h.MoveTo(infestation.Object.Position);
                PlacedDestructibles.Destroy(infestation, h.Client.Player);
                operation.Tick(1000 + index);
                Assert.AreEqual(MissionObjectiveState.Completed, h.Client.Player.Missions[323].Objectives[(uint)(309 + index)].State);
                if (index == 2) Assert.IsFalse(h.Client.Player.Missions[323].Completeable);
            }
            PlacedDestructibles.Worker(h.Map);
            Assert.IsTrue(operation.Infestations.All(site => site.IsDown));
            Reward(h, johnson, 323, 0, 2821, 7000, 1100);
        }

        private static Creature[] Living(WildernessRuntimeTestHarness h) =>
            h.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList)
                .Where(c => c.State != CharacterState.Dead && c.State != CharacterState.Dying).Distinct().ToArray();

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void CamerasCanPreventTheAlarmAndAnotherMapCopyHasItsOwnObjects(bool destroyBoth)
        {
            using var h = Create();
            h.EnterMap(1430, Pravus);
            var operation = PravusOperation.For(h.Map);
            h.MoveTo(new(95, 42, -8));
            operation.Tick(1000);
            PlacedDestructibles.Destroy(operation.Cameras[0], h.Client.Player);
            if (destroyBoth) PlacedDestructibles.Destroy(operation.Cameras[1], h.Client.Player);
            operation.Tick(8000);
            Assert.AreEqual(destroyBoth ? 0 : 1, Living(h).Count(creature => creature.DbId == 640307));
            operation.Tick(30000);
            Assert.AreEqual(destroyBoth ? 0 : 1, Living(h).Count(creature => creature.DbId == 640307));
            var copy = new MapChannel { MapInfo = h.Map.MapInfo, ClientList = new(), NavMesh = h.Map.NavMesh, IsSquadInstance = true };
            h.Maps.MapChannelArray.Add(99999, copy);
            SpawnPoolManager.Instance.CloneTemplateMap(h.Map, copy);
            var other = PravusOperation.For(copy);
            Assert.AreNotSame(operation, other);
            Assert.IsFalse(other.Cameras[0].IsDown);
            Assert.AreEqual(1, copy.SpawnPools.Count(pool => pool.DbId == 640421));
        }

        [TestMethod]
        public void LogosDispatchUsesAllThreeExistingShrinesAndReturnsToStandley()
        {
            using var h = Create();
            h.SpawnWorld(212);
            var standley = RequireNpc(h, 212, 134);
            Accept(h, standley, 924);
            h.EnterMap(1430, Pravus);
            foreach (var id in new uint[] { 51, 41, 34 })
            {
                var shrine = h.Map.DynamicObjects.OfType<Logos>().Single(logos => logos.Id == id);
                h.MoveTo(shrine.Position);
                h.Objects.RequestUseObjectPacket(h.Client, new RequestUseObjectPacket
                {
                    EntityId = shrine.EntityId, ActionId = ActionId.UseObject,
                    ActionArgId = DynamicObjectManager.LogosUseArgId
                });
                ActorActionManager.Instance.DoWork(h.Map, 10001);
                Assert.IsTrue(h.Client.Player.Logos.Contains(id));
            }
            Assert.IsTrue(h.Client.Player.Missions[924].Completeable);
            Reload(h);
            h.EnterMap(1220, Wilderness);
            Reward(h, standley, 924, null, null, 6000, 1200);
        }

        private static void Kill(WildernessRuntimeTestHarness h, Creature creature)
        {
            h.MoveTo(creature.Position);
            var damage = creature.Attributes[Attributes.Health].Current + Math.Max(0, creature.Attributes[Attributes.Armor].Current);
            ActorManager.Instance.Damage(h.Map, creature, damage, h.Client.Player);
            Assert.AreEqual(CharacterState.Dead, creature.State);
            Assert.AreNotEqual(0UL, creature.CorpseLootEntityId, "Combat enemies must supply actual corpse loot.");
        }

        private static Client JoinOperation(WildernessRuntimeTestHarness h, Creature nylla)
        {
            var member = h.Context.CreateAdditionalClient(2, manager: h.Manager);
            member.Player.Attributes[Attributes.Health] = new(Attributes.Health, 1000, 1000, 1000, 0, 0);
            // This character joins after the leader has earned quest rewards. The common
            // fixture copies the leader's purse, but this new character has its own DB purse.
            using (var unit = h.CreateChar())
                CharacterManager.PutPurseRight(member, unit.Characters.Find(member.Player.Id));
            CellManager.Instance.RemoveFromWorld(member);
            member.Player.MapChannel.ClientList.Remove(member);
            member.Player.MapChannel = h.Map;
            member.Player.RuntimeMapChannel = h.Map;
            member.Player.MapContextId = 1430;
            member.Player.Race = Race.Human;
            member.Player.StartingExperienceCompleted = true;
            h.Map.ClientList.Add(member);
            CellManager.Instance.AddToWorld(member);
            using (var unit = h.CreateChar())
                unit.ExecuteTransaction(() =>
                {
                    unit.CharacterStartingExperience.Add(new CharacterStartingExperienceEntry(member.Player.Id,
                        WildernessMissionDataV1.Revision, CharacterStartingExperienceState.Skipped));
                    unit.CharacterMissions.Runtime.Archive(new CharacterMissionEntry(member.Player.Id, 593, (uint)MissionState.Completed)
                    {
                        AssignmentId = Guid.NewGuid().ToString("N"), Generation = 1,
                        ContentRevision = PravusMissionDataV1.Revision
                    }, h.UtcNow);
                });
            member.Player.MissionHistory[593] = MissionState.Completed;
            member.Player.MissionSuccessHistory.Add(593);
            member.SetWorldPosition(nylla.Position, 0);
            NpcManager.Instance.RequestNpcConverse(member,
                new RequestNPCConversePacket { EntityId = nylla.EntityId, ActionId = ActionId.Converse, ActionArgId = 1 });
            Assert.IsTrue(h.Manager.TryAcceptNpcMission(member, nylla.EntityId, 575));
            return member;
        }
    }
}
