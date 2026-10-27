using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class NetworkMatchManager
{
    [Header("Round Series")]
    [SerializeField, Min(1)] private int roundsPerMatch = 3;
    [SerializeField, Min(1f)] private float resultsDuration = 8f;
    [SerializeField, Min(1f)] private float finalResultsDuration = 12f;
    [SerializeField] private string lobbyScenePath = "Assets/Scenes/MultiplayerSandbox.unity";
    [Networked] public int CurrentRound { get; private set; }
    [Networked] public int RoundLimit { get; private set; }
    [Networked] public float ResultsDisplayDuration { get; private set; }
    [Networked] public TickTimer ResultsTimer { get; private set; }
    [Networked] public NetworkBool SeriesComplete { get; private set; }
    [Networked] public NetworkBool TransitionFailed { get; private set; }
    private NetworkMatchSeriesState seriesState;

    public float ResultsSecondsRemaining => Mathf.Max(0f, ResultsTimer.RemainingTime(Runner) ?? 0f);
    public float ResultsProgress => Mathf.Clamp01(1f - ResultsSecondsRemaining / Mathf.Max(1f, ResultsDisplayDuration));

    private void InitializeSeries()
    {
        seriesState = Runner.GetComponent<NetworkMatchSeriesState>();
        if (seriesState == null)
            seriesState = Runner.gameObject.AddComponent<NetworkMatchSeriesState>();
        CurrentRound = 1;
        RoundLimit = Mathf.Max(1, roundsPerMatch);
        ResultsDisplayDuration = Mathf.Max(1f, resultsDuration);
        ResultsTimer = TickTimer.None;
        SeriesComplete = false;
        TransitionFailed = false;
        if (seriesState.NextRound <= 1 || seriesState.Results == null || seriesState.Colors == null)
            return;

        CurrentRound = seriesState.NextRound;
        ResultCount = Mathf.Min(seriesState.Results.Length, Results.Length);
        for (int i = 0; i < ResultCount; i++)
            Results.Set(i, seriesState.Results[i]);
        for (int i = 0; i < Mathf.Min(seriesState.Colors.Length, ColorOwners.Length); i++)
        {
            ColorOwners.Set(i, seriesState.Colors[i]);
            if (seriesState.Colors[i] != PlayerRef.None)
                readyColors |= 1 << i;
        }
        seriesState.Clear();
        State = NetworkMatchState.PreparingRound;
    }

    private void PrepareNextRound()
    {
        if (ConnectedPlayerCount < minimumPlayers)
        {
            // Do not leave a series stuck waiting for a player who disconnected.
            CurrentRound = Mathf.Max(1, CurrentRound - 1);
            for (int i = 0; i < ResultCount; i++)
            {
                MatchPlayerResult result = Results[i];
                result.RoundPoints = 0;
                Results.Set(i, result);
            }
            SeriesComplete = true;
            State = NetworkMatchState.MatchOver;
            ResultsTimer = TickTimer.CreateFromSeconds(Runner, ResultsDisplayDuration);
            return;
        }
        if (!AllPlayersSelected())
            return;
        CountdownTimer = TickTimer.CreateFromSeconds(Runner, countdownDuration);
        State = NetworkMatchState.Countdown;
    }

    private void StartResultsDisplay()
    {
        SeriesComplete = CurrentRound >= RoundLimit || ConnectedPlayerCount < minimumPlayers;
        ResultsDisplayDuration = Mathf.Max(1f, CurrentRound >= RoundLimit ? finalResultsDuration : resultsDuration);
        TransitionFailed = false;
        ResultsTimer = TickTimer.CreateFromSeconds(Runner, ResultsDisplayDuration);
    }

    private void UpdateResultsDisplay()
    {
        if (!SeriesComplete && ConnectedPlayerCount < minimumPlayers)
            SeriesComplete = true;
        if (!Restarting && !TransitionFailed && ResultsTimer.Expired(Runner))
            AdvanceSeries();
    }

    private void AdvanceSeries()
    {
        if (!Object.HasStateAuthority || State != NetworkMatchState.MatchOver || Restarting)
            return;
        SeriesComplete = SeriesComplete || ConnectedPlayerCount < minimumPlayers;
        SceneRef scene = SeriesComplete ? Runner.GetSceneRef(lobbyScenePath) : Runner.GetSceneRef(gameObject);
        if (!scene.IsValid)
        {
            TransitionFailed = true;
            Debug.LogError("Unable to find the next round or lobby scene in the build scene list.", this);
            return;
        }

        if (SeriesComplete)
            seriesState.Clear();
        else
        {
            MatchPlayerResult[] results = new MatchPlayerResult[ResultCount];
            PlayerRef[] colors = new PlayerRef[ColorOwners.Length];
            for (int i = 0; i < results.Length; i++)
                results[i] = Results[i];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = ColorOwners[i];
            seriesState.Save(CurrentRound + 1, results, colors);
        }
        Restarting = true;
        LoadSeriesScene(scene);
    }

    private async void LoadSeriesScene(SceneRef scene)
    {
        try
        {
            await Runner.LoadScene(scene, LoadSceneMode.Single);
        }
        catch (System.Exception exception)
        {
            if (this == null || Object == null || !Object.IsValid)
                return;
            seriesState.Clear();
            Restarting = false;
            TransitionFailed = true;
            Debug.LogError($"Unable to continue the round series: {exception.Message}", this);
        }
    }
}
