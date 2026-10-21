using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pravus
{
    /// <summary>Frozen first playable reconstruction; see docs/pravus-mission-reconstruction.txt.</summary>
    public static partial class PravusOperationV1
    {
        public const uint KeyTemplate = 11505, KeyClass = 20000013, ArmbandTemplate = 45439;
        public static readonly uint[] Missions = { 593, 575, 321, 323, 924, 1075 };

        public static void Up(MigrationBuilder migration)
        {
            World(migration);
            Escapist(migration);
            Production(migration);
            Frontlines(migration);
            Radio(migration);
            Logos(migration);
            Epilogue(migration);
        }

        public static void Down(MigrationBuilder migration)
        {
            PravusMissionDataV1.Remove(migration, Missions);
            RemoveWorld(migration);
        }

        private static MissionSceneDefinition Scene() => new()
        {
            Requirement = new CustomRequirement("character.starting-experience-completed"),
            Category = 10000048
        };

        private static void Finish(PravusMissionDataV1 mission, MissionSceneDefinition scene)
        {
            mission.Evidence(1, MissionEvidenceSourceKind.Client,
                "client:1.16.5.0/generated/client/missionobjective.pyo",
                "Native mission, objective, counter-slot and conversation identities recovered from the installed final client.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation,
                "https://www.tentonhammer.com/guides/pravus-research-guide",
                "Historical route, fifteen Frontlines kills, cameras, key-bearing guards, three Linkers, factory and finale.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction,
                "docs/pravus-mission-reconstruction.txt",
                "First playable pass: estimated waves, timing, trigger volumes, combat statistics, placement and rewards except sourced575 credits1050. Instance encounter is shared; assignment progress and quest items remain personal.");
            mission.Enable(scene);
        }

        private static void Escapist(MigrationBuilder migration)
        {
            var m = new PravusMissionDataV1(migration, 593, "The Escapist", 1871, 119, 640001, 10, true);
            m.Objective(1, 1875, 6721, 1, MissionObjectiveState.Incomplete);
            m.Transition(1); m.Conversation(1, 420);
            m.Reward(2500, 500);
            m.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            var scene = Scene();
            // This is a boundary between independently versioned content releases. Use the
            // typed completion requirement rather than a same-revision prerequisite row.
            scene.Requirement = new AllRequirements(new MissionRequirement[]
                { scene.Requirement, new MissionStateRequirement(574, MissionState.Completed) });
            scene.Dialogue = new() { new(1, 420, 1, transitionId: 1, sourceCreatureId: 640001) };
            Finish(m, scene);
        }

        private static void Production(MigrationBuilder migration)
        {
            var m = new PravusMissionDataV1(migration, 575, "The Means of Production", 1756, 640001, 640001, 10);
            m.Prerequisite(593);
            m.EncounterEvents(1, 2, 5);
            m.Objective(3, 20000025, 6640, 1, MissionObjectiveState.Incomplete);
            m.Objective(1, 1865, 6636, 2);
            m.Objective(2, 1867, 6637, 3);
            m.Objective(4, 20000028, 6638, 4, MissionObjectiveState.Incomplete, required: false);
            m.Objective(5, 20000033, 6639, 5);
            m.Transition(3); m.Progress(3, MissionProgressEventKind.ItemAcquired, KeyClass, target: 1);
            m.RevealAndActivate(3, 1);
            m.Area(1, 224, 9, 168, 65, 12);
            m.Transition(1); m.EnterArea(1, 1); m.RevealAndActivate(1, 2);
            m.Transition(2); m.Progress(2, MissionProgressEventKind.ScenarioEvent, 2, counter: 1);
            m.RevealAndActivate(2, 5);
            m.Transition(4); m.Conversation(4, 468);
            m.Transition(5); m.Progress(5, MissionProgressEventKind.ScenarioEvent, 5, counter: 1);
            m.Reward(10000, 1050, fixedItems: new[] { (3002U, 1U), (44917U, 3U) });
            m.Action(5, 1, MissionActionKind.GrantReward, reward: 1);
            var scene = Scene();
            BindEncounterSignals(scene, 575, 2, 5);
            scene.Items = new()
            {
                new("pravus-keypass", KeyTemplate, MissionItemScope.CharacterOwned, 1,
                    MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                    MissionItemCleanupDisposition.Retain, TurnInQuantity: 1,
                    Drop: new MissionItemDropDefinition(new[] { 640303U }, 3, 1430, 100, 1, shareWithNearbyParty: true))
            };
            scene.ExistingFactObjectiveIds = new() { 3 };
            scene.Dialogue = new() { new(4, 468, 1, transitionId: 1, sourceCreatureId: 640312) };
            scene.ObjectiveRequirements[1] = new MapRequirement(new uint[] { 1430 });
            scene.ObjectiveRequirements[2] = new MapRequirement(new uint[] { 1430 });
            scene.ObjectiveRequirements[5] = new MapRequirement(new uint[] { 1430 });
            Finish(m, scene);
        }

        private static void Frontlines(MigrationBuilder migration)
        {
            var m = new PravusMissionDataV1(migration, 321, "Assemble With Lieutenant Perkins", 15, 640002, 640003, 10, true);
            m.Objective(310, 115, 6522, 1, MissionObjectiveState.Incomplete, counterTextId: 1711);
            m.Transition(310); m.Progress(310, MissionProgressEventKind.CreatureKilled, 640301, counter: 0, target: 15);
            m.Reward(7500, 1000, selectableItems: new[] { (1285U, 1U), (601U, 1U), (2821U, 1U) });
            m.Action(310, 1, MissionActionKind.GrantReward, reward: 1);
            m.Indicator(310, 1, -100, 27, 14, 35);
            var scene = Scene();
            scene.Credit[310] = new MissionCreditPolicy(MissionCreditMode.NearbyParty, 100);
            scene.ObjectiveRequirements[310] = new MapRequirement(new uint[] { 1430 });
            Finish(m, scene);
        }

        private static void Radio(MigrationBuilder migration)
        {
            var m = new PravusMissionDataV1(migration, 323, "Pirate Radio", 51, 640311, 640311, 10);
            m.EncounterEvents(1, 309, 310, 311);
            // Availability is deliberately independent of the main operation for this first pass.
            foreach (var row in new[] { (309U, 20000050U, 7394U, 20000051U, 2U),
                                       (310U, 20000053U, 7395U, 20000054U, 1U),
                                       (311U, 20000056U, 7396U, 20000057U, 0U) })
            {
                m.Objective(row.Item1, row.Item2, row.Item3, row.Item1 - 308, MissionObjectiveState.Incomplete);
                m.Transition(row.Item1);
                m.Progress(row.Item1, MissionProgressEventKind.ScenarioEvent, row.Item1, counter: 1);
            }
            m.Reward(7000, 1100, selectableItems: new[] { (2821U, 1U), (1339U, 1U) });
            m.Action(311, 1, MissionActionKind.GrantReward, reward: 1);
            var scene = Scene();
            BindEncounterSignals(scene, 323, 309, 310, 311);
            Finish(m, scene);
        }

        private static void BindEncounterSignals(MissionSceneDefinition scene, uint missionId, params uint[] events)
        {
            scene.Script = "data.sequence";
            scene.Sequences[0] = new SceneSequenceDefinition();
            foreach (var id in events)
                scene.Sequences[id] = new SceneSequenceDefinition
                {
                    Signals = new() { new SceneMissionSignal(missionId, 1, id) }
                };
        }

        private static void Logos(MigrationBuilder migration)
        {
            var m = new PravusMissionDataV1(migration, 924, "Logos: Communication, Control, Machine", 4934, 134, 134, 10, true);
            // Token IDs resolved from the native Logos catalog in the existing server.
            foreach (var row in new[] { (6U, 4938U, 7598U, 51U), (7U, 4939U, 7599U, 41U), (8U, 4940U, 7600U, 34U) })
            {
                m.Objective(row.Item1, row.Item2, row.Item3, row.Item1 - 5, MissionObjectiveState.Incomplete);
                m.Transition(row.Item1); m.Progress(row.Item1, MissionProgressEventKind.LogosAcquired, row.Item4);
            }
            m.Reward(6000, 1200);
            m.Action(8, 1, MissionActionKind.GrantReward, reward: 1);
            var scene = Scene(); scene.ExistingFactObjectiveIds = new() { 6, 7, 8 };
            Finish(m, scene);
        }

        private static void Epilogue(MigrationBuilder migration)
        {
            var m = new PravusMissionDataV1(migration, 1075, "A Higher Calling", 6063, 640001, 106, 10, true);
            m.Prerequisite(575);
            m.Objective(1, 6067, 6644, 1, MissionObjectiveState.Incomplete);
            m.Transition(1); m.Conversation(1, 227);
            m.Action(1, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("return-nylla-armband", 1075, "nylla-armband", 1, MissionItemScope.AssignmentIssued));
            m.Reward(5000, 1000);
            m.Action(1, 2, MissionActionKind.GrantReward, reward: 1);
            var scene = Scene();
            scene.Items = new() { new("nylla-armband", ArmbandTemplate, MissionItemScope.AssignmentIssued, 1,
                MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove) };
            scene.AcceptanceItems = new() { new IssueMissionItemIntent("nylla-gives-armband", 1075, "nylla-armband", ArmbandTemplate, 1) };
            scene.Dialogue = new() { new(1, 227, 1, transitionId: 1, sourceCreatureId: 106) };
            Finish(m, scene);
        }
    }
}
