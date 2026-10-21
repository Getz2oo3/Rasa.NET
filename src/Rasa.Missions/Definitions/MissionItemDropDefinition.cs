using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Rasa.Missions.Definitions
{
    public sealed class MissionItemDropDefinition
    {
        [JsonInclude, JsonRequired] public IReadOnlyList<uint> CreatureIds { get; private init; }
        [JsonInclude, JsonRequired] public uint ObjectiveId { get; private init; }
        [JsonInclude, JsonRequired] public uint MapContextId { get; private init; }
        [JsonInclude, JsonRequired] public int ChancePercent { get; private init; }
        [JsonInclude, JsonRequired] public uint Quantity { get; private init; }
        /// <summary>Opt-in personal copies for the kill's eligible nearby squad members.</summary>
        [JsonInclude, JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool ShareWithNearbyParty { get; private init; }

        [JsonConstructor]
        public MissionItemDropDefinition(IReadOnlyList<uint> creatureIds, uint objectiveId,
            uint mapContextId, int chancePercent, uint quantity, bool shareWithNearbyParty = false)
        {
            if (creatureIds == null || creatureIds.Count == 0 ||
                creatureIds.Any(id => id == 0) || creatureIds.Distinct().Count() != creatureIds.Count ||
                mapContextId == 0 || chancePercent < 0 || chancePercent > 100 || quantity == 0)
                throw new ArgumentException("Invalid mission item drop source, map, chance or quantity.");
            CreatureIds = Array.AsReadOnly(creatureIds.ToArray());
            ObjectiveId = objectiveId;
            MapContextId = mapContextId;
            ChancePercent = chancePercent;
            Quantity = quantity;
            ShareWithNearbyParty = shareWithNearbyParty;
        }
    }
}
