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
    public NetworkBool Participated;
    public int Placement, RoundPoints, TotalPoints;
    public float TotalDuration, TotalDamage;
    public int TotalKills;
}

public partial class NetworkMatchManager
{
    [Networked, Capacity(10)] public NetworkArray<MatchPlayerResult> Results => default;
    [Networked] public int ResultCount { get; private set; }
    [Networked] private int matchStartTick { get; set; }

    private void BeginResults()
    {
        matchStartTick = Runner.Tick.Raw;
        for (int i = 0; i < ResultCount; i++)
        {
            MatchPlayerResult result = Results[i];
            result.Damage = 0f;
            result.Kills = 0;
            result.Duration = 0f;
            result.Placement = 0;
            result.RoundPoints = 0;
            result.Participated = false;
            result.Finished = true;
            Results.Set(i, result);
        }
        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            if (player == PlayerRef.None ||
                !Runner.TryGetPlayerObject(player, out NetworkObject obj) || obj == null)
                continue;
            int index = FindResult(player);
            if (index < 0)
            {
                if (ResultCount >= Results.Length)
                    continue;
                index = ResultCount++;
                Results.Set(index, default);
            }
            NetworkPlayerIdentity identity = obj.GetComponent<NetworkPlayerIdentity>();
            MatchPlayerResult result = Results[index];
            result.Player = player;
            result.Username = identity != null ? identity.DisplayName : $"Player {player.AsIndex + 1}";
            result.Participated = true;
            result.Finished = false;
            Results.Set(index, result);
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

    private void AwardRoundPoints()
    {
        for (int i = 0; i < ResultCount; i++)
        {
            MatchPlayerResult result = Results[i];
            if (!result.Participated)
                continue;
            int placement = 1;
            for (int j = 0; j < ResultCount; j++)
            {
                MatchPlayerResult other = Results[j];
                if (other.Participated && MatchPointRules.CompareRound(other.Duration, other.Player == Winner,
                    result.Duration, result.Player == Winner) < 0)
                    placement++;
            }
            result.Placement = placement;
            result.RoundPoints = MatchPointRules.PlacementPoints(StartingPlayerCount, placement);
            result.TotalPoints += result.RoundPoints;
            result.TotalDuration += result.Duration;
            result.TotalDamage += result.Damage;
            result.TotalKills += result.Kills;
            Results.Set(i, result);
        }
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
