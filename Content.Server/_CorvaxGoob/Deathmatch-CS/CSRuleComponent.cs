using Content.Shared.Storage;

namespace Content.Server._CorvaxGoob.Deathmatch_CS;

[RegisterComponent, Access(typeof(CSRuleSystem))]
public sealed partial class CSRuleComponent : Component
{
    /// <summary>
    /// An entity spawned after a player is killed.
    /// </summary>
    [DataField("rewardSpawns")]
    public List<EntitySpawnEntry> RewardSpawns = new();

    [DataField("numberOfSessions"), ViewVariables(VVAccess.ReadWrite)]
    public int NumberOfSessions = 1;
}
