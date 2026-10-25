using Fusion;
using UnityEngine;

public struct MatchPlayerResult : INetworkStruct
{
    public PlayerRef Player;
    public NetworkString<_32> Username;
    public float Damage;
    public int Kills;
    public float Duration;
    public NetworkBool Finished;
}

public partial class NetworkMatchManager
{
    [Networked, Capacity(10)] public NetworkArray<MatchPlayerResult> Results => default;
    [Networked] public int ResultCount { get; private set; }
    [Networked] private int matchStartTick { get; set; }

    private void BeginResults()
    {
        ResultCount = 0;
        matchStartTick = Runner.Tick.Raw;
        for (int i = 0; i < Results.Length; i++)
            Results.Set(i, default);
        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            if (player == PlayerRef.None || ResultCount >= Results.Length ||
                !Runner.TryGetPlayerObject(player, out NetworkObject obj) || obj == null)
                continue;
            NetworkPlayerIdentity identity = obj.GetComponent<NetworkPlayerIdentity>();
            Results.Set(ResultCount++, new MatchPlayerResult
            {
                Player = player,
                Username = identity != null ? identity.DisplayName : $"Player {player.AsIndex + 1}"
            });
        }
    }

    private float SurvivalDuration => Mathf.Max(0, Runner.Tick.Raw - matchStartTick) * Runner.DeltaTime;

    private int FindResult(PlayerRef player)
    {
        for (int i = 0; i < ResultCount; i++)
            if (Results[i].Player == player)
                return i;
        return -1;
    }

    private void FinishResult(int index)
    {
        MatchPlayerResult result = Results[index];
        if (result.Finished)
            return;
        result.Duration = SurvivalDuration;
        result.Finished = true;
        Results.Set(index, result);
    }

    private void UpdateSurvivalResults()
    {
        for (int i = 0; i < ResultCount; i++)
        {
            if (Results[i].Finished)
                continue;
            bool connected = false;
            foreach (PlayerRef player in Runner.ActivePlayers)
                if (player == Results[i].Player)
                    connected = true;
            if (!connected || !Runner.TryGetPlayerObject(Results[i].Player, out NetworkObject obj) ||
                obj == null || !obj.IsValid || obj.GetComponent<NetworkHealth>() == null ||
                obj.GetComponent<NetworkHealth>().IsDead)
                FinishResult(i);
        }
    }

    private void FinishResults()
    {
        for (int i = 0; i < ResultCount; i++)
            FinishResult(i);
    }

    public void RecordPlayerDamage(NetworkObject victim, NetworkId attackerId, float damage, bool killed)
    {
        if (!Object.HasStateAuthority || State != NetworkMatchState.Playing || victim == null || damage <= 0f)
            return;
        int victimIndex = FindResult(victim.InputAuthority);
        if (victimIndex < 0 || Results[victimIndex].Finished ||
            !Runner.TryGetPlayerObject(victim.InputAuthority, out NetworkObject registeredVictim) || registeredVictim != victim)
            return;
        if (killed)
            FinishResult(victimIndex);

        // Traps and zombies can end survival, but never award a player's damage or kill score.
        if (!attackerId.IsValid || !Runner.TryFindObject(attackerId, out NetworkObject attacker) || attacker == null ||
            attacker == victim || !Runner.TryGetPlayerObject(attacker.InputAuthority, out NetworkObject registeredAttacker) ||
            registeredAttacker != attacker)
            return;
        int attackerIndex = FindResult(attacker.InputAuthority);
        if (attackerIndex < 0)
            return;
        MatchPlayerResult result = Results[attackerIndex];
        result.Damage += damage;
        if (killed)
            result.Kills++;
        Results.Set(attackerIndex, result);
    }
}
