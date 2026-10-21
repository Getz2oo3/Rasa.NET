using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Rasa.Data;
using Rasa.Game;
using Rasa.Packets.MapChannel.Server;
using Rasa.Structures;

namespace Rasa.Managers
{
    /// <summary>
    /// The shared world side of Pravus; personal progress, drops, rewards and conversations
    /// remain in MissionApplication. One operation belongs to one actual map copy, not to
    /// each assignment. See docs/pravus-mission-reconstruction.txt for reconstruction choices.
    /// </summary>
    internal sealed class PravusOperation
    {
        internal const uint MapId = 1430;
        internal const long WaveRestMs = 15_000, ProductionIntervalMs = 25_000;
        internal const int ProductionCap = 6;
        internal static readonly Vector3 Front = new(-125.6f, 27.64f, 13.9f);
        internal static readonly Vector3 Factory = new(224, 8.54f, 168);
        private static readonly ConditionalWeakTable<MapChannel, PravusOperation> Operations = new();
        private readonly MapChannel _map;
        private readonly SpawnPool _front, _production, _juggernaut;
        private readonly SpawnPool[] _guards;
        private readonly DynamicObject _gate, _prisonGate;
        internal PlacedDestructibles.Live Capsule { get; }
        internal IReadOnlyList<PlacedDestructibles.Live> Cameras { get; }
        internal IReadOnlyList<PlacedDestructibles.Live> Infestations { get; }
        internal bool FactoryStarted { get; private set; }
        internal bool FactoryCleared { get; private set; }
        private bool _alarmRaised, _frontWave;
        private long _nextFront, _nextProduction, _cameraDeadline;

        private PravusOperation(MapChannel map)
        {
            _map = map;
            _front = Pool(640421, 640301, 5, new(-96, 27.64f, 14), 8, dropship: true);
            _production = Pool(640422, 640302, 3, new(224, 9, 190), 6);
            _juggernaut = Pool(640423, 640307, 1, new(141, 42, -8), 2);
            _guards = new uint[] { 640408, 640409, 640410 }
                .Select(id => map.SpawnPools.Single(pool => pool.DbId == id)).ToArray();
            Cameras = new[] { Object(10290, new(94, 46, -17), 300), Object(10290, new(94, 46, 1), 300) };
            // Native factory components share the assembly origin; the capsule mesh carries
            // its own vertical offset. Presentation still needs a client walkthrough.
            foreach (var id in new uint[] { 9271, 9273, 9600 })
                Scenery(id, Factory);
            Capsule = Object(9272, Factory, 3500);
            // All three radar structures are present in the shipped map. The guide repeats
            // one northern coordinate; use the actual far-north, northwest and south sites.
            Infestations = new[]
            {
                Object(6225, new(-2.861f, 47.394f, 298.668f), 500),
                Object(6225, new(-54.641f, 32.858f, 163.034f), 500),
                Object(6225, new(-47.340f, 36.385f, -224.068f), 500)
            };
            _gate = Scenery(9337, new(232, 8, -8), -MathF.PI / 2, UseObjectState.DoorStateClosed);
            _prisonGate = Scenery(9337, new(312, 8, 32), 0, UseObjectState.DoorStateClosed);
        }

        internal static PravusOperation For(MapChannel map)
        {
            if (map?.MapInfo?.MapContextId != MapId ||
                !MissionApplication.Instance.LoadedMissions.TryGetValue(575, out var mission) ||
                mission.ContentRevision != "pravus_operation_v1")
                return null;
            return Operations.GetValue(map, key => new PravusOperation(key));
        }

        internal static void Worker(MapChannel map) => For(map)?.Tick(Environment.TickCount64);

        internal void Tick(long now)
        {
            var clients = _map.ClientList.Where(client => client?.State == ClientState.Ingame &&
                client.Player != null && ReferenceEquals(client.Player.MapChannel, _map)).ToArray();
            var living = clients.Where(client => client.Player.State != CharacterState.Dead &&
                client.Player.State != CharacterState.Dying).ToArray();

            // No finite total: a missed kill or a late squad member can still earn fifteen.
            // At most five delivered/queued at once, with a rest after the preceding detachment.
            if (_frontWave && _front.AliveCreatures + _front.QueuedCreatures == 0 && _front.DropshipQueue == 0)
            {
                _frontWave = false;
                _nextFront = now + WaveRestMs;
            }
            if (!_frontWave && now >= _nextFront && living.Any(client =>
                Incomplete(client, 321, 310) && Near(client, Front, 60, 15)))
            {
                Spawn(_front, true);
                _frontWave = true;
            }

            var keyHolder = living.FirstOrDefault(client => Complete(client, 575, 3));
            if (keyHolder != null)
            {
                Open(_gate, keyHolder);
                Open(_prisonGate, keyHolder);
            }

            if (!_alarmRaised && Cameras.Any(camera => !camera.IsDown))
            {
                if (_cameraDeadline == 0 && living.Any(client => Near(client, new(95, 42, -8), 24, 9)))
                    _cameraDeadline = now + 6000;
                if (_cameraDeadline != 0 && now >= _cameraDeadline)
                {
                    Spawn(_juggernaut);
                    _alarmRaised = true;
                }
            }
            else if (!_alarmRaised)
                _cameraDeadline = 0;

            if (!FactoryStarted && living.Any(client => Complete(client, 575, 3) && Near(client, Factory, 65, 14)))
            {
                foreach (var pool in _guards) Spawn(pool);
                FactoryStarted = true;
                _nextProduction = now;
            }

            // This is also reconciliation for a player returning after a restart or joining
            // a squad whose capsule is already down: facts are replayed only to active objectives.
            foreach (var client in clients)
            {
                if (Near(client, Factory, 85, 16) && Incomplete(client, 575, 1) && Complete(client, 575, 3))
                    MissionApplication.Instance.RecordProgress(client, MissionProgressEvent.Area(575, 1));
                if (FactoryStarted && Capsule.IsDown && Near(client, Factory, 100, 20))
                    Credit(client, 575, 2);
                for (var i = 0; i < Infestations.Count; i++)
                    if (Infestations[i].IsDown && Near(client, Infestations[i].Object.Position, 80, 20))
                        Credit(client, 323, (uint)(309 + i));
            }

            if (FactoryStarted && !Capsule.IsDown && now >= _nextProduction &&
                _production.AliveCreatures + _production.QueuedCreatures <= ProductionCap - 3 &&
                living.Any(client => Near(client, Factory, 90, 20)))
            {
                Spawn(_production);
                _nextProduction = now + ProductionIntervalMs;
            }
            if (FactoryStarted && Capsule.IsDown && !FactoryCleared &&
                _production.AliveCreatures + _production.QueuedCreatures == 0 &&
                _guards.All(pool => pool.AliveCreatures + pool.QueuedCreatures == 0))
            {
                Spawn(_map.SpawnPools.Single(pool => pool.DbId == 640414));
                FactoryCleared = true;
            }
            if (FactoryCleared)
                foreach (var client in clients.Where(client => Near(client, Factory, 100, 20)))
                    Credit(client, 575, 5);
        }

        private static bool Incomplete(Client client, uint mission, uint objective) =>
            client.Player.Missions.TryGetValue(mission, out var log) && log.State == MissionState.Active &&
            log.Objectives.TryGetValue(objective, out var state) && state.State == MissionObjectiveState.Incomplete;
        private static bool Complete(Client client, uint mission, uint objective) =>
            client.Player.Missions.TryGetValue(mission, out var log) && log.State == MissionState.Active &&
            log.Objectives.TryGetValue(objective, out var state) && state.State == MissionObjectiveState.Completed;
        private static void Credit(Client client, uint mission, uint objective)
        {
            if (Incomplete(client, mission, objective))
                MissionApplication.Instance.Scenes.Execute(client, mission, objective);
        }
        private static bool Near(Client client, Vector3 position, float radius, float height) =>
            Vector2.Distance(new(client.Player.Position.X, client.Player.Position.Z), new(position.X, position.Z)) <= radius &&
            Math.Abs(client.Player.Position.Y - position.Y) <= height;
        private Vector3 Ground(Vector3 point) => _map.NavMesh?.Nearest(point) ?? point;
        private SpawnPool Pool(uint id, uint creature, short count, Vector3 point, float radius, bool dropship = false)
        {
            // A GM may have visited the template channel before a squad copy was cloned.
            var existing = _map.SpawnPools.FirstOrDefault(pool => pool.DbId == id);
            if (existing != null) return existing;
            var pool = new SpawnPool
            {
                DbId = id, MapContextId = MapId, RuntimeMapChannel = _map, Position = Ground(point),
                Radius = radius, Mode = 2, AnimType = (short)(dropship ? 1 : 0),
                SpawnSlot = new() { new(creature, count, count) }
            };
            _map.SpawnPools.Add(pool);
            return pool;
        }
        private static void Spawn(SpawnPool pool, bool dropship = false)
        {
            var manager = SpawnPoolManager.Instance;
            var creatures = manager.CreateListOfCreatures(pool);
            if (dropship) manager.EnqueueDropship(pool.RuntimeMapChannel, pool, creatures);
            else manager.SpawnCreatures(pool, creatures);
            pool.HasSpawned = true;
        }
        private PlacedDestructibles.Live Object(uint id, Vector3 point, uint health)
        {
            var live = PlacedDestructibles.PutDown(_map, (EntityClasses)id, point, 0, health)
                ?? throw new InvalidOperationException($"Pravus object class {id} cannot be placed.");
            live.Respawns = false;
            return live;
        }
        private DynamicObject Scenery(uint id, Vector3 point, float yaw = 0, UseObjectState state = 0)
        {
            var obj = new DynamicObject
            {
                EntityClassId = (EntityClasses)id, DynamicObjectType = DynamicObjectType.Scenery,
                Position = point, Rotation = yaw, StateId = state, MapContextId = MapId,
                TargetCategory = TargetCategory.Object, IsInWorld = true, Comment = "Pravus operation"
            };
            CellManager.Instance.AddToWorld(_map, obj);
            return obj;
        }
        private void Open(DynamicObject door, Client client)
        {
            if (door.StateId == UseObjectState.DoorStateOpen) return;
            door.StateId = UseObjectState.DoorStateOpen;
            CellManager.Instance.CellCallMethod(_map, door, new UsePacket(client.Player.EntityId, door.StateId, 0));
        }
    }
}
