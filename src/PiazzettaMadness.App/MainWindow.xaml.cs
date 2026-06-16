using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PiazzettaMadness.App.Data;
using PiazzettaMadness.App.Forms;
using PiazzettaMadness.App.Live;

namespace PiazzettaMadness.App;

public partial class MainWindow : Window
{
    private readonly GameClock _gameClock = new(720000);
    private readonly GameClock _shotClock = new(24000);
    private readonly GameClock _contestClock = new(60000);
    private readonly ScoreboardBroadcaster _broadcaster = new();
    private readonly OnlineEntityClient? _onlineEntities = OnlineEntityClient.TryCreate();
    private readonly DispatcherTimer _timer;
    private readonly DispatcherTimer _liveSyncTimer;
    private readonly ScoreboardState _state = new();
    private AppDbContext _db = new();
    private ObservableCollection<Tournament> _tournaments = [];
    private ObservableCollection<EditionRow> _editionRows = [];
    private ObservableCollection<CourtRow> _courtRows = [];
    private ObservableCollection<GroupRow> _groupRows = [];
    private ObservableCollection<GroupTeamRow> _groupTeamRows = [];
    private ObservableCollection<MatchRow> _matchRows = [];
    private ObservableCollection<Team> _teams = [];
    private ObservableCollection<Player> _players = [];
    private ObservableCollection<Player> _availableRosterPlayers = [];
    private ObservableCollection<RosterRow> _rosterRows = [];
    private ObservableCollection<MatchOption> _matchOptions = [];
    private ObservableCollection<MatchOption> _liveMatchOptions = [];
    private ObservableCollection<LiveScorerOption> _homeScorerOptions = [];
    private ObservableCollection<LiveScorerOption> _awayScorerOptions = [];
    private ObservableCollection<CompetitionEventRow> _competitionEventRows = [];
    private ObservableCollection<ThreePointEntryRow> _threePointEntryRows = [];
    private ObservableCollection<ThreePointRoundRow> _threePointRoundRows = [];
    private ObservableCollection<ForfeitRow> _forfeitRows = [];
    private ObservableCollection<StandingRow> _standingRows = [];
    private ObservableCollection<Sponsor> _sponsors = [];
    private ObservableCollection<ScoreboardDisplayInfo> _openDisplays = [];
    private int? _currentEditionId;
    private int? _currentLiveMatchId;
    private int? _currentHomeTeamId;
    private int? _currentAwayTeamId;
    private string _currentLiveMatchStatus = "";
    private bool _isFreeLiveMode;
    private bool _isThreePointContestMode;
    private bool _isLoadingContest;
    private int _contestStation = 1;
    private string _contestStatus = "Ready";
    private ThreePointContestRound? _currentContestRound;
    private ThreePointContestEntry? _currentContestEntry;
    private bool _isRenderingState;
    private int _nextScoreboardDisplayId = 1;
    private readonly Dictionary<int, int> _onlineScoreboardStateIds = [];
    private bool _isSyncingLiveData;
    private bool _liveSyncWarningShown;
    private bool _hasPendingLiveSync;
    private int _liveChangeVersion;
    private DateTime _nextLiveSyncAttemptAt = DateTime.MinValue;
    private readonly HashSet<int> _changedMatchPlayerIds = [];
    private bool IsOfficialLiveSessionLocked => !_isFreeLiveMode && _currentLiveMatchStatus is "Live" or "Paused";
    private bool IsLiveSelectionLocked => IsOfficialLiveSessionLocked || _isSyncingLiveData || _hasPendingLiveSync;

    public MainWindow()
    {
        InitializeComponent();
        DatabasePathText.Text = $"Sessione live locale: {AppPaths.LiveDatabasePath}";

        if (_onlineEntities is null)
        {
            _timer = new DispatcherTimer();
            _liveSyncTimer = new DispatcherTimer();
            DisableApplicationForMissingOnlineConfig();
            return;
        }

        _currentLiveMatchId = LiveSessionStore.Restore(_db);
        if (_currentLiveMatchId is int restoredMatchId)
        {
            foreach (var playerId in _db.MatchPlayers
                         .Where(x => x.MatchId == restoredMatchId)
                         .Select(x => x.Id))
            {
                _changedMatchPlayerIds.Add(playerId);
            }

            _hasPendingLiveSync = true;
            _liveChangeVersion++;
        }

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _timer.Tick += OnTick;
        _timer.Start();
        _liveSyncTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(10)
        };
        _liveSyncTimer.Tick += (_, _) => CheckpointOrRetryLiveSync();
        _liveSyncTimer.Start();
        _broadcaster.TargetsChanged += (_, _) => Dispatcher.Invoke(RefreshOpenDisplays);

        RenderLocalState();
        LoadCrudData();
        RefreshOpenDisplays();
    }

    private void DisableApplicationForMissingOnlineConfig()
    {
        MainTabControl.IsEnabled = false;
        OpenScoreboardButton.IsEnabled = false;
        DatabasePathText.Text = "Configurazione online mancante: crea online-api.local.json per usare l'app.";
        MessageBox.Show(
            "Configurazione online mancante.\n\nCrea o copia il file online-api.local.json nella cartella dell'app, poi riavvia il programma.",
            "Piazzetta Madness",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private void OpenScoreboard_Click(object sender, RoutedEventArgs e)
    {
        var window = new ScoreboardWindow(_broadcaster, _nextScoreboardDisplayId++);
        window.Show();
        _ = BroadcastAsync();
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!LiveTabItem.IsSelected || e.IsRepeat || Keyboard.Modifiers != ModifierKeys.None ||
            IsKeyboardInputControl(e.OriginalSource as DependencyObject))
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Space:
                StartPause_Click(sender, e);
                break;
            case Key.NumPad0:
                StartPauseShotClock_Click(sender, e);
                break;
            case Key.NumPad1:
                ResetShotClock14_Click(sender, e);
                break;
            case Key.NumPad2:
                ResetShotClock_Click(sender, e);
                break;
            default:
                return;
        }

        e.Handled = true;
    }

    private static bool IsKeyboardInputControl(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is TextBoxBase or PasswordBox or ComboBox)
            {
                return true;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return false;
    }

    private void StartPause_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureLiveInteractionAllowed())
        {
            return;
        }

        if (_gameClock.IsRunning)
        {
            _gameClock.Pause();
        }
        else
        {
            _gameClock.Start();
        }

        PersistLiveState();
        RenderLocalState();
        _ = BroadcastAsync();
    }

    private void ResetShotClock_Click(object sender, RoutedEventArgs e)
    {
        ResetShotClock(24000);
    }

    private void ResetShotClock14_Click(object sender, RoutedEventArgs e)
    {
        ResetShotClock(14000);
    }

    private void ResetShotClock(int durationMs)
    {
        if (!EnsureLiveInteractionAllowed())
        {
            return;
        }

        _shotClock.Reset(durationMs);
        _shotClock.Start();

        PersistLiveState();
        RenderLocalState();
        _ = BroadcastAsync();
    }

    private void StartPauseShotClock_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureLiveInteractionAllowed())
        {
            return;
        }

        if (_shotClock.IsRunning)
        {
            _shotClock.Pause();
        }
        else
        {
            _shotClock.Start();
        }

        PersistLiveState();
        RenderLocalState();
        _ = BroadcastAsync();
    }

    private void StopGameClock_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureLiveInteractionAllowed())
        {
            return;
        }

        _gameClock.Pause();
        PersistLiveState();
        RenderLocalState();
        _ = BroadcastAsync();
    }

    private void ResetGameClock_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureLiveInteractionAllowed())
        {
            return;
        }

        if (MessageBox.Show(
                "Vuoi davvero resettare il tempo partita?",
                "Reset tempo partita",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        var duration = GetCurrentMatchPeriodDurationMs();
        var gameWasRunning = _gameClock.IsRunning;
        var shotWasRunning = _shotClock.IsRunning;
        _gameClock.Reset(duration);
        _shotClock.Reset(GetCurrentMatchShotClockMs());
        if (gameWasRunning)
        {
            _gameClock.Start();
        }

        if (shotWasRunning)
        {
            _shotClock.Start();
        }

        PersistLiveState();
        RenderLocalState();
        _ = BroadcastAsync();
    }

    private void SetGameClock_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureLiveInteractionAllowed())
        {
            return;
        }

        var oldTimeMs = _gameClock.Update();
        var dialog = new GameClockAdjustmentWindow(oldTimeMs)
        {
            Owner = this
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        _gameClock.Reset(dialog.SelectedDurationMs);

        if (!_isFreeLiveMode && _currentLiveMatchId is not null)
        {
            _db.MatchEvents.Add(new MatchEvent
            {
                MatchId = _currentLiveMatchId.Value,
                Period = _state.Period,
                PeriodType = "Regular",
                GameClockMsRemaining = dialog.SelectedDurationMs,
                ShotClockMsRemaining = _shotClock.Update(),
                EventType = "ClockCorrection",
                IsCorrection = true,
                Description = $"Tempo corretto manualmente da {FormatGameClock(oldTimeMs)} a {FormatGameClock(dialog.SelectedDurationMs)}",
                CreatedAt = Now()
            });

            PersistLiveState(saveChanges: false);
            SaveChanges();
        }
        else
        {
            PersistLiveState();
        }

        RenderLocalState();
        _ = BroadcastAsync();
    }

    private int GetCurrentMatchPeriodDurationMs()
    {
        if (_currentLiveMatchId is null)
        {
            return 720000;
        }

        return _db.Matches
            .Where(x => x.Id == _currentLiveMatchId.Value)
            .Select(x => x.PeriodDurationMs)
            .FirstOrDefault() is var duration && duration > 0 ? duration : 720000;
    }

    private int GetCurrentMatchShotClockMs()
    {
        if (_currentLiveMatchId is null)
        {
            return 24000;
        }

        return _db.Matches
            .Where(x => x.Id == _currentLiveMatchId.Value)
            .Select(x => x.ShotClockMs)
            .FirstOrDefault() is var duration && duration > 0 ? duration : 24000;
    }

    private void ResetGame_Click(object sender, RoutedEventArgs e)
    {
        if (!_isFreeLiveMode &&
            (_currentLiveMatchId is null || _currentLiveMatchStatus is not ("Live" or "Paused")))
        {
            MessageBox.Show(
                "Il reset completo è disponibile solo per una partita in corso o in pausa.",
                "Reset partita",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show(
                "Questa operazione annullerà la sessione live, riporterà la partita allo stato Scheduled e azzererà punteggi, falli, timeout, periodo e cronometri. La cronologia resterà disponibile. Continuare?",
                "Reset partita",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        _state.HomeScore = 0;
        _state.AwayScore = 0;
        _state.HomeFouls = 0;
        _state.AwayFouls = 0;
        _state.HomeTimeouts = 0;
        _state.AwayTimeouts = 0;
        _state.Period = 1;
        _gameClock.Reset(GetCurrentMatchPeriodDurationMs());
        _shotClock.Reset(GetCurrentMatchShotClockMs());

        if (_currentLiveMatchId is not null)
        {
            var matchId = _currentLiveMatchId.Value;
            var match = _db.Matches.FirstOrDefault(x => x.Id == matchId);
            if (match is not null)
            {
                match.Status = "Scheduled";
                match.WinnerTeamId = null;
                match.WinReason = null;
                match.ActualStartAt = null;
                match.ActualEndAt = null;
                match.UpdatedAt = Now();
                _currentLiveMatchStatus = "Scheduled";
            }

            foreach (var side in _db.MatchTeams.Where(x => x.MatchId == matchId))
            {
                side.Score = 0;
                side.FoulsCurrentPeriod = 0;
                side.TimeoutsUsedTotal = 0;
                side.TimeoutsUsedPeriod = 0;
                side.IsWinner = false;
                side.ForfeitScore = null;
            }

            foreach (var player in _db.MatchPlayers.Where(x => x.MatchId == matchId))
            {
                player.Points = 0;
                player.PersonalFouls = 0;
                player.IsFouledOut = false;
                player.IsEjected = false;
                _changedMatchPlayerIds.Add(player.Id);
            }

            foreach (var scorer in _homeScorerOptions.Concat(_awayScorerOptions))
            {
                scorer.Points = 0;
                scorer.Fouls = 0;
            }

            _db.MatchEvents.Add(new MatchEvent
            {
                MatchId = matchId,
                Period = 1,
                PeriodType = "Regular",
                GameClockMsRemaining = GetCurrentMatchPeriodDurationMs(),
                ShotClockMsRemaining = GetCurrentMatchShotClockMs(),
                EventType = "MatchReset",
                IsCorrection = true,
                Description = "Sessione live annullata e partita riportata allo stato Scheduled",
                CreatedAt = Now()
            });
        }

        PersistLiveState(saveChanges: false);
        var saved = SaveChanges();
        if (_currentLiveMatchId is int resetMatchId)
        {
            RefreshLiveMatchOptionLabel(resetMatchId);
        }
        HomeLivePlayersGrid.Items.Refresh();
        AwayLivePlayersGrid.Items.Refresh();
        RenderLocalState();
        if (saved)
        {
            _ = BroadcastAsync();
        }
    }

    private void HomePlusOne_Click(object sender, RoutedEventArgs e) => AddScore(home: true, points: 1);
    private void HomePlusTwo_Click(object sender, RoutedEventArgs e) => AddScore(home: true, points: 2);
    private void HomePlusThree_Click(object sender, RoutedEventArgs e) => AddScore(home: true, points: 3);
    private void HomeMinusOne_Click(object sender, RoutedEventArgs e) => AddScore(home: true, points: -1);
    private void AwayPlusOne_Click(object sender, RoutedEventArgs e) => AddScore(home: false, points: 1);
    private void AwayPlusTwo_Click(object sender, RoutedEventArgs e) => AddScore(home: false, points: 2);
    private void AwayPlusThree_Click(object sender, RoutedEventArgs e) => AddScore(home: false, points: 3);
    private void AwayMinusOne_Click(object sender, RoutedEventArgs e) => AddScore(home: false, points: -1);

    private void AddScore(bool home, int points)
    {
        if (!EnsureLiveInteractionAllowed())
        {
            return;
        }

        AddScore(home, points, scorer: null);
    }

    private void AddScore(bool home, int points, LiveScorerOption? scorer)
    {
        if (home)
        {
            _state.HomeScore = Math.Max(0, _state.HomeScore + points);
        }
        else
        {
            _state.AwayScore = Math.Max(0, _state.AwayScore + points);
        }

        PersistScore(home, points, scorer);
        RenderLocalState();
        _ = BroadcastAsync();
    }

    private void PlayerPlusOne_Click(object sender, RoutedEventArgs e) => AddScoreFromButton(sender, 1, showFreeThrowCelebration: true);
    private void PlayerPlusTwo_Click(object sender, RoutedEventArgs e) => AddScoreFromButton(sender, 2);
    private void PlayerPlusThree_Click(object sender, RoutedEventArgs e) => AddScoreFromButton(sender, 3, showThreePointCelebration: true);
    private void PlayerMinusOne_Click(object sender, RoutedEventArgs e) => AddScoreFromButton(sender, -1);
    private void PlayerFoulPlus_Click(object sender, RoutedEventArgs e) => AddFoulFromButton(sender, 1);
    private void PlayerFoulMinus_Click(object sender, RoutedEventArgs e) => AddFoulFromButton(sender, -1);
    private void PlayerDunk_Click(object sender, RoutedEventArgs e) => ShowPlayerCelebrationFromButton(sender, "dunk");
    private void PlayerBlock_Click(object sender, RoutedEventArgs e) => ShowPlayerCelebrationFromButton(sender, "block");
    private void HomeTeamFoulPlus_Click(object sender, RoutedEventArgs e) => AddTeamFoul(home: true, delta: 1, scorer: null);
    private void HomeTeamFoulMinus_Click(object sender, RoutedEventArgs e) => AddTeamFoul(home: true, delta: -1, scorer: null);
    private void AwayTeamFoulPlus_Click(object sender, RoutedEventArgs e) => AddTeamFoul(home: false, delta: 1, scorer: null);
    private void AwayTeamFoulMinus_Click(object sender, RoutedEventArgs e) => AddTeamFoul(home: false, delta: -1, scorer: null);

    private void AddScoreFromButton(
        object sender,
        int points,
        bool showThreePointCelebration = false,
        bool showFreeThrowCelebration = false)
    {
        if (_isFreeLiveMode)
        {
            return;
        }

        if (sender is not FrameworkElement { DataContext: LiveScorerOption scorer })
        {
            return;
        }

        var home = scorer.TeamId == _currentHomeTeamId;
        AddScore(home, points, scorer);

        if (AnimationsEnabledCheckBox.IsChecked == true && showThreePointCelebration)
        {
            _ = _broadcaster.ShowThreePointCelebrationAsync(
                scorer.PlayerName,
                scorer.JerseyNumber,
                home ? _state.HomeName : _state.AwayName,
                home ? _state.HomeColor : _state.AwayColor);
        }

        if (AnimationsEnabledCheckBox.IsChecked == true && showFreeThrowCelebration)
        {
            _ = _broadcaster.ShowPlayerCelebrationAsync(
                "freeThrow",
                scorer.PlayerName,
                scorer.JerseyNumber,
                home ? _state.HomeName : _state.AwayName,
                home ? _state.HomeColor : _state.AwayColor);
        }
    }

    private void ShowPlayerCelebrationFromButton(object sender, string celebration)
    {
        if (AnimationsEnabledCheckBox.IsChecked != true ||
            !EnsureLiveInteractionAllowed() ||
            sender is not FrameworkElement { DataContext: LiveScorerOption scorer })
        {
            return;
        }

        var home = scorer.TeamId == _currentHomeTeamId;
        _ = _broadcaster.ShowPlayerCelebrationAsync(
            celebration,
            scorer.PlayerName,
            scorer.JerseyNumber,
            home ? _state.HomeName : _state.AwayName,
            home ? _state.HomeColor : _state.AwayColor);
    }

    private void AddFoulFromButton(object sender, int delta)
    {
        if (_isFreeLiveMode)
        {
            return;
        }

        if (sender is not FrameworkElement { DataContext: LiveScorerOption scorer })
        {
            return;
        }

        AddTeamFoul(scorer.TeamId == _currentHomeTeamId, delta, scorer);
    }

    private void AddTeamFoul(bool home, int delta, LiveScorerOption? scorer)
    {
        if (!EnsureLiveInteractionAllowed())
        {
            return;
        }

        if (home)
        {
            _state.HomeFouls = Math.Max(0, _state.HomeFouls + delta);
        }
        else
        {
            _state.AwayFouls = Math.Max(0, _state.AwayFouls + delta);
        }

        PersistFoul(home, delta, scorer);
        RenderLocalState();
        _ = BroadcastAsync();
    }

    private void PeriodCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_isRenderingState || HomeScoreText is null || PeriodCombo.SelectedItem is not System.Windows.Controls.ComboBoxItem item)
        {
            return;
        }

        if (!EnsureLiveInteractionAllowed(showMessage: false))
        {
            RenderLocalState();
            return;
        }

        _state.Period = Convert.ToInt32(item.Tag);
        PersistLiveState();
        RenderLocalState();
        _ = BroadcastAsync();
    }

    private void LiveModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LiveMatchCombo is null)
        {
            return;
        }

        if (IsLiveSelectionLocked)
        {
            LiveModeCombo.SelectedIndex = 0;
            return;
        }

        if (LiveModeCombo.SelectedItem is not ComboBoxItem item)
        {
            return;
        }

        _isThreePointContestMode = string.Equals(item.Tag?.ToString(), "ThreePointContest", StringComparison.OrdinalIgnoreCase);
        _isFreeLiveMode = string.Equals(item.Tag?.ToString(), "Free", StringComparison.OrdinalIgnoreCase);
        UpdateLiveModePanels();

        if (_isThreePointContestMode)
        {
            EnterThreePointContestMode();
            return;
        }

        if (_isFreeLiveMode)
        {
            EnterFreeLiveMode();
        }
        else
        {
            _currentLiveMatchStatus = "";
            LoadSelectedLiveMatchOrDefault();
        }

        RenderLocalState();
        _ = BroadcastAsync();
    }

    private void UpdateLiveModePanels()
    {
        if (ThreePointContestPanel is null)
        {
            return;
        }

        var contestVisibility = _isThreePointContestMode ? Visibility.Visible : Visibility.Collapsed;
        var matchVisibility = _isThreePointContestMode ? Visibility.Collapsed : Visibility.Visible;
        ThreePointContestPanel.Visibility = contestVisibility;
        HomeLivePanel.Visibility = matchVisibility;
        ClockLivePanel.Visibility = matchVisibility;
        AwayLivePanel.Visibility = matchVisibility;
        LiveMatchLabel.Visibility = matchVisibility;
        LiveMatchCombo.Visibility = matchVisibility;
        LiveMatchCombo.IsEnabled = !_isFreeLiveMode && !IsLiveSelectionLocked;
        PrepareMatchButton.Visibility = matchVisibility;
        StartMatchButton.Visibility = matchVisibility;
        PauseMatchButton.Visibility = matchVisibility;
        ResumeMatchButton.Visibility = matchVisibility;
        FinishMatchButton.Visibility = matchVisibility;
        AnimationsEnabledCheckBox.Visibility = matchVisibility;
    }

    private async void EnterThreePointContestMode()
    {
        _gameClock.Pause();
        _shotClock.Pause();
        ContestDataMessageText.Text = "Aggiornamento dati contest...";

        if (_onlineEntities is not null)
        {
            try
            {
                var events = await _onlineEntities.GetCompetitionEventsAsync();
                var entries = await _onlineEntities.GetThreePointContestEntriesAsync();
                var rounds = await _onlineEntities.GetThreePointContestRoundsAsync();
                UpsertLocalCompetitionEvents(events);
                UpsertLocalThreePointContestEntries(entries);
                UpsertLocalThreePointContestRounds(rounds);
                _db.SaveChanges();
                LiveSyncStatusText.Text = "Sync contest: aggiornata";
            }
            catch (Exception exception)
            {
                LiveSyncStatusText.Text = $"Sync contest fallita: {exception.Message}";
            }
        }

        _isLoadingContest = true;
        ContestEventCombo.ItemsSource = _db.CompetitionEvents
            .Where(x => x.EventType == "ThreePointContest" && x.Status != "Cancelled")
            .OrderBy(x => x.ScheduledStartAt)
            .ThenBy(x => x.Name)
            .ToList();
        ContestEventCombo.SelectedItem ??= ContestEventCombo.Items.Cast<CompetitionEvent>().FirstOrDefault();
        _isLoadingContest = false;
        LoadContestEntries();
        RenderContestState();
        _ = BroadcastContestAsync();
    }

    private void ContestEventCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingContest || !_isThreePointContestMode)
        {
            return;
        }

        LoadContestEntries();
    }

    private void LoadContestEntries()
    {
        _isLoadingContest = true;
        var eventId = (ContestEventCombo.SelectedItem as CompetitionEvent)?.Id;
        var teams = _db.Teams.ToDictionary(x => x.Id);
        var players = _db.Players.ToDictionary(x => x.Id);
        var options = eventId is null
            ? []
            : _db.ThreePointContestEntries
                .Where(x => x.CompetitionEventId == eventId.Value)
                .OrderBy(x => x.SeedOrder)
                .ThenBy(x => x.Id)
                .ToList()
                .Select(entry => new ContestEntryOption(
                    entry,
                    teams.TryGetValue(entry.TeamId, out var team) ? team.Name : $"Squadra #{entry.TeamId}",
                    players.TryGetValue(entry.PlayerId, out var player) ? $"{player.LastName} {player.FirstName}".Trim() : $"Giocatore #{entry.PlayerId}"))
                .ToList();
        ContestEntryCombo.ItemsSource = options;
        ContestEntryCombo.SelectedItem = options.FirstOrDefault();
        _isLoadingContest = false;
        _ = LoadContestRoundsAsync();
    }

    private void ContestEntryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoadingContest && _isThreePointContestMode)
        {
            _ = LoadContestRoundsAsync();
        }
    }

    private Task LoadContestRoundsAsync()
    {
        _isLoadingContest = true;
        _currentContestEntry = (ContestEntryCombo.SelectedItem as ContestEntryOption)?.Entry;
        if (_currentContestEntry is null)
        {
            ContestRoundCombo.ItemsSource = null;
            _currentContestRound = null;
            _isLoadingContest = false;
            RenderContestState();
            return Task.CompletedTask;
        }

        var rounds = _db.ThreePointContestRounds
            .Where(x => x.EntryId == _currentContestEntry.Id)
            .OrderBy(x => x.RoundNumber)
            .ThenBy(x => x.RoundType)
            .ToList();
        var options = rounds.Select(x => new ContestRoundOption(x)).ToList();
        ContestRoundCombo.ItemsSource = options;
        ContestRoundCombo.SelectedItem = options.FirstOrDefault();
        _isLoadingContest = false;
        LoadSelectedContestRound();
        return Task.CompletedTask;
    }

    private void ContestRoundCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoadingContest && _isThreePointContestMode)
        {
            LoadSelectedContestRound();
        }
    }

    private void LoadSelectedContestRound()
    {
        _currentContestRound = (ContestRoundCombo.SelectedItem as ContestRoundOption)?.Round;
        var live = DeserializeContestLiveState(_currentContestRound?.Notes);
        _contestStatus = live.Status;
        _contestStation = Math.Clamp(live.Station, 1, 5);
        _contestClock.Reset(Math.Clamp(live.ClockMs, 0, 60000));
        if (_contestStatus == "Live" && _contestClock.RemainingMs > 0)
        {
            _contestClock.Start();
        }

        RenderContestState();
        _ = BroadcastContestAsync();
    }

    private void ContestStation_Checked(object sender, RoutedEventArgs e)
    {
        if (!IsInitialized || _isLoadingContest || sender is not FrameworkElement element || !int.TryParse(element.Tag?.ToString(), out var station))
        {
            return;
        }

        _contestStation = station;
        PersistContestState();
    }

    private void ContestPlusOne_Click(object sender, RoutedEventArgs e) => ChangeContestScore(1);
    private void ContestPlusTwo_Click(object sender, RoutedEventArgs e) => ChangeContestScore(2);
    private void ContestMinusOne_Click(object sender, RoutedEventArgs e) => ChangeContestScore(-1);

    private void ChangeContestScore(int delta)
    {
        if (_currentContestRound is null || _contestStatus == "Ready")
        {
            MessageBox.Show("Avvia prima la prova.", "3 Point Contest", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SetContestStationScore(_currentContestRound, _contestStation, Math.Max(0, GetContestStationScore(_currentContestRound, _contestStation) + delta));
        _currentContestRound.TotalScore = GetContestStationScores(_currentContestRound).Sum();
        PersistContestState();
    }

    private void ContestStartPause_Click(object sender, RoutedEventArgs e)
    {
        if (_currentContestRound is null)
        {
            MessageBox.Show("Seleziona un tiratore.", "3 Point Contest", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_contestStatus is "Ready" or "Paused")
        {
            if (_contestClock.RemainingMs == 0)
            {
                _contestClock.Reset(60000);
            }
            _contestClock.Start();
            _contestStatus = "Live";
        }
        else if (_contestStatus == "Live")
        {
            _contestClock.Pause();
            _contestStatus = "Paused";
        }

        PersistContestState();
    }

    private void ContestFinish_Click(object sender, RoutedEventArgs e)
    {
        if (_currentContestRound is null) return;
        _contestClock.Pause();
        _contestStatus = "Finished";
        PersistContestState();
    }

    private void ContestReset_Click(object sender, RoutedEventArgs e)
    {
        if (_currentContestRound is null) return;
        if (MessageBox.Show("Azzerare cronometro e punteggi della prova?", "Reset 3 Point Contest", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        _contestClock.Reset(60000);
        _contestStatus = "Ready";
        _contestStation = 1;
        for (var station = 1; station <= 5; station++) SetContestStationScore(_currentContestRound, station, 0);
        _currentContestRound.TotalScore = 0;
        PersistContestState();
    }

    private void EnterFreeLiveMode()
    {
        _currentLiveMatchId = null;
        _currentHomeTeamId = null;
        _currentAwayTeamId = null;
        _currentLiveMatchStatus = "Free";
        LiveMatchCombo.SelectedItem = null;

        _state.HomeName = "Casa";
        _state.AwayName = "Ospite";
        _state.HomeColor = "#f77f00";
        _state.AwayColor = "#457b9d";
        _state.HomeScore = 0;
        _state.AwayScore = 0;
        _state.HomeFouls = 0;
        _state.AwayFouls = 0;
        _state.HomeTimeouts = 0;
        _state.AwayTimeouts = 0;
        _state.Period = 1;
        _gameClock.Reset(720000);
        _shotClock.Reset(24000);

        HomeNameText.Text = _state.HomeName;
        AwayNameText.Text = _state.AwayName;
        _homeScorerOptions = [];
        _awayScorerOptions = [];
        HomeLivePlayersGrid.ItemsSource = _homeScorerOptions;
        AwayLivePlayersGrid.ItemsSource = _awayScorerOptions;
        HomeScorerCombo.ItemsSource = _homeScorerOptions;
        AwayScorerCombo.ItemsSource = _awayScorerOptions;
    }

    private async void PrepareMatch_Click(object sender, RoutedEventArgs e)
    {
        if (_isFreeLiveMode)
        {
            return;
        }

        if (!TryGetCurrentLiveMatch(out var match))
        {
            return;
        }

        if (match.Status == "Finished")
        {
            MessageBox.Show("La partita è già chiusa. Per modificarla va prima riaperta con una funzione dedicata.", "Partita live", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (match.Status == "Scheduled")
        {
            match.Status = "Ready";
            match.UpdatedAt = Now();
            SaveChanges();
            _currentLiveMatchStatus = "Ready";
            RefreshLiveMatchOptionLabel(match.Id);
        }

        if (!await InitializeMatchPlayersAsync(match))
        {
            return;
        }

        LoadLiveMatch(match.Id);
    }

    private async void StartMatch_Click(object sender, RoutedEventArgs e)
    {
        if (_isFreeLiveMode)
        {
            return;
        }

        if (!TryGetCurrentLiveMatch(out var match))
        {
            return;
        }

        if (match.Status is not ("Scheduled" or "Ready"))
        {
            MessageBox.Show(
                $"La partita non può essere avviata dallo stato {FormatMatchStatus(match.Status)}.",
                "Partita live",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var otherActiveMatch = _db.Matches.FirstOrDefault(x => x.Id != match.Id && (x.Status == "Live" || x.Status == "Paused"));
        if (otherActiveMatch is not null)
        {
            MessageBox.Show(
                $"Esiste già un'altra partita attiva (ID {otherActiveMatch.Id}, stato {FormatMatchStatus(otherActiveMatch.Status)}). Chiudila prima di iniziare questa partita.",
                "Partita già attiva",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (MessageBox.Show("Vuoi iniziare ufficialmente la partita?", "Inizia partita", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        if (!await InitializeMatchPlayersAsync(match))
        {
            return;
        }

        SetOfficialMatchStatus(match, "Live", setActualStart: true, save: false);
        PersistLiveState(saveChanges: false);
        SaveChanges();
        RenderLocalState();
        _ = BroadcastAsync();
    }

    private async Task<bool> InitializeMatchPlayersAsync(Match match)
    {
        try
        {
            var existingPlayers = _db.MatchPlayers
                .Where(x => x.MatchId == match.Id)
                .ToList();
            if (existingPlayers.Count > 0)
            {
                var existingSides = _db.MatchTeams.Where(x => x.MatchId == match.Id).ToList();
                LoadLiveScorers(
                    match.Id,
                    existingSides.Single(x => x.Side == "Home").TeamId,
                    existingSides.Single(x => x.Side == "Away").TeamId);
                return true;
            }

            var matchPlayers = _onlineEntities is not null
                ? await _onlineEntities.InitializeMatchPlayersAsync(match.Id)
                : CreateLocalMatchPlayers(match.Id);
            foreach (var matchPlayer in matchPlayers)
            {
                UpsertLocalMatchPlayer(matchPlayer);
            }

            _db.SaveChanges();
            var sides = _db.MatchTeams.Where(x => x.MatchId == match.Id).ToList();
            LoadLiveScorers(
                match.Id,
                sides.Single(x => x.Side == "Home").TeamId,
                sides.Single(x => x.Side == "Away").TeamId);
            return true;
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Giocatori partita", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private List<MatchPlayer> CreateLocalMatchPlayers(int matchId)
    {
        var sides = _db.MatchTeams.Where(x => x.MatchId == matchId).ToList();
        var teamIds = sides.Select(x => x.TeamId).ToHashSet();
        return _db.TeamRosters
            .Where(x => teamIds.Contains(x.TeamId) && x.IsActive)
            .OrderBy(x => x.TeamId)
            .ThenBy(x => x.JerseyNumber)
            .ThenBy(x => x.PlayerId)
            .Select(roster => new MatchPlayer
            {
                MatchId = matchId,
                TeamId = roster.TeamId,
                PlayerId = roster.PlayerId,
                JerseyNumber = roster.JerseyNumber,
                IsStartingFive = false,
                IsOnCourt = false,
                Points = 0,
                PersonalFouls = 0,
                IsFouledOut = false,
                IsEjected = false
            })
            .ToList();
    }

    private void PauseMatch_Click(object sender, RoutedEventArgs e)
    {
        if (_isFreeLiveMode)
        {
            return;
        }

        if (!TryGetCurrentLiveMatch(out var match) || match.Status != "Live")
        {
            return;
        }

        if (MessageBox.Show("Vuoi mettere ufficialmente in pausa la partita?", "Pausa partita", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        _gameClock.Pause();
        _shotClock.Pause();
        SetOfficialMatchStatus(match, "Paused", save: false);
        PersistLiveState(saveChanges: false);
        SaveChanges();
        RenderLocalState();
        _ = BroadcastAsync();
    }

    private void ResumeMatch_Click(object sender, RoutedEventArgs e)
    {
        if (_isFreeLiveMode)
        {
            return;
        }

        if (!TryGetCurrentLiveMatch(out var match) || match.Status != "Paused")
        {
            return;
        }

        if (MessageBox.Show("Vuoi riprendere ufficialmente la partita?", "Riprendi partita", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        SetOfficialMatchStatus(match, "Live", save: false);
        PersistLiveState(saveChanges: false);
        SaveChanges();
        RenderLocalState();
        _ = BroadcastAsync();
    }

    private void FinishMatch_Click(object sender, RoutedEventArgs e)
    {
        if (_isFreeLiveMode)
        {
            return;
        }

        if (!TryGetCurrentLiveMatch(out var match) || match.Status is not ("Live" or "Paused"))
        {
            return;
        }

        if (_state.HomeScore == _state.AwayScore)
        {
            MessageBox.Show("La partita è in parità. Gestisci prima overtime o regola di spareggio, poi chiudi.", "Chiudi partita", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (MessageBox.Show("Vuoi chiudere ufficialmente la partita? Dopo la chiusura i controlli live saranno bloccati.", "Chiudi partita", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        _gameClock.Pause();
        _shotClock.Pause();

        var homeSide = _db.MatchTeams.FirstOrDefault(x => x.MatchId == match.Id && x.Side == "Home");
        var awaySide = _db.MatchTeams.FirstOrDefault(x => x.MatchId == match.Id && x.Side == "Away");
        if (homeSide is null || awaySide is null)
        {
            return;
        }

        homeSide.Score = _state.HomeScore;
        awaySide.Score = _state.AwayScore;
        homeSide.IsWinner = _state.HomeScore > _state.AwayScore;
        awaySide.IsWinner = _state.AwayScore > _state.HomeScore;
        match.WinnerTeamId = homeSide.IsWinner ? homeSide.TeamId : awaySide.TeamId;
        match.WinReason = "Regular";

        SetOfficialMatchStatus(match, "Finished", setActualEnd: true, save: false);
        PersistLiveState(saveChanges: false);
        SaveChanges();
        RenderLocalState();
        _ = BroadcastAsync();
    }

    private void ReloadCrud_Click(object sender, RoutedEventArgs e)
    {
        LoadCrudData();
    }

    private async void AddSponsor_Click(object sender, RoutedEventArgs e)
    {
        var now = Now();
        var sponsor = new Sponsor
        {
            IsActive = true,
            SortOrder = _sponsors.Count + 1,
            CreatedAt = now,
            UpdatedAt = now
        };

        var form = new SponsorFormWindow(sponsor, isNew: true) { Owner = this };
        if (form.ShowDialog() != true)
        {
            return;
        }

        if (_onlineEntities is not null)
        {
            try
            {
                sponsor = await _onlineEntities.CreateSponsorAsync(sponsor);
                UpsertLocalSponsor(sponsor);
                _db.SaveChanges();
                LoadCrudData();
                SponsorsGrid.SelectedItem = _sponsors.FirstOrDefault(x => x.Id == sponsor.Id);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Sponsor online", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return;
        }

        _db.Sponsors.Add(sponsor);
        if (SaveChanges())
        {
            LoadCrudData();
            SponsorsGrid.SelectedItem = _sponsors.FirstOrDefault(x => x.Id == sponsor.Id);
        }
    }

    private async void EditSponsor_Click(object sender, RoutedEventArgs e)
    {
        if (SponsorsGrid.SelectedItem is not Sponsor sponsor)
        {
            MessageBox.Show("Seleziona uno sponsor da modificare.", "Sponsor", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var form = new SponsorFormWindow(sponsor, isNew: false) { Owner = this };
        if (form.ShowDialog() == true)
        {
            sponsor.UpdatedAt = Now();
            if (_onlineEntities is not null)
            {
                try
                {
                    sponsor = await _onlineEntities.UpdateSponsorAsync(sponsor);
                    UpsertLocalSponsor(sponsor);
                    _db.SaveChanges();
                    LoadCrudData();
                    SponsorsGrid.SelectedItem = _sponsors.FirstOrDefault(x => x.Id == sponsor.Id);
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, "Sponsor online", MessageBoxButton.OK, MessageBoxImage.Error);
                    LoadCrudData();
                }

                return;
            }

            if (SaveChanges())
            {
                LoadCrudData();
            }
        }
    }

    private async void DeleteSponsor_Click(object sender, RoutedEventArgs e)
    {
        if (SponsorsGrid.SelectedItem is not Sponsor sponsor)
        {
            MessageBox.Show("Seleziona uno sponsor da eliminare.", "Sponsor", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show($"Eliminare lo sponsor '{sponsor.Name}'?", "Sponsor", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        if (_onlineEntities is not null)
        {
            try
            {
                await _onlineEntities.DeleteSponsorAsync(sponsor.Id);
                var local = _db.Sponsors.FirstOrDefault(x => x.Id == sponsor.Id);
                if (local is not null)
                {
                    _db.Sponsors.Remove(local);
                    _db.SaveChanges();
                }

                LoadCrudData();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Sponsor online", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return;
        }

        _db.Sponsors.Remove(sponsor);
        if (SaveChanges())
        {
            LoadCrudData();
        }
    }

    private void SponsorsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (SponsorsGrid.SelectedItem is Sponsor)
        {
            EditSponsor_Click(sender, e);
        }
    }

    private void RefreshDisplays_Click(object sender, RoutedEventArgs e)
    {
        RefreshOpenDisplays();
    }

    private void RefreshOpenDisplays()
    {
        _openDisplays = new ObservableCollection<ScoreboardDisplayInfo>(_broadcaster.GetDisplays());
        OpenDisplaysGrid.ItemsSource = _openDisplays;
        OpenDisplaysCountText.Text = _openDisplays.Count switch
        {
            0 => "Nessun tabellone aperto",
            1 => "1 tabellone aperto",
            _ => $"{_openDisplays.Count} tabelloni aperti"
        };
    }

    private void ApplyScoreboardDisplay_Click(object sender, RoutedEventArgs e)
    {
        _ = ApplyScoreboardDisplayAsync();
    }

    private async Task ApplyScoreboardDisplayAsync()
    {
        if (ShowContestRadio.IsChecked == true)
        {
            if (!_isThreePointContestMode || _currentContestRound is null)
            {
                MessageBox.Show("Seleziona la modalità 3 Point Contest e un tiratore prima di mostrare il tabellone.", "Display tabelloni", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            await BroadcastContestAsync();
            await _broadcaster.ShowContestAsync();
            ScoreboardDisplayStatusText.Text = "Contenuto attuale: 3 Point Contest su tutti i tabelloni";
            return;
        }

        if (ShowPlayerStatsRadio.IsChecked == true)
        {
            if (!IsOfficialLiveSessionLocked)
            {
                MessageBox.Show(
                    "Le statistiche giocatori possono essere mostrate solo durante una partita in corso o in pausa.",
                    "Display tabelloni",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            await BroadcastAsync();
            await _broadcaster.ShowPlayerStatsAsync();
            ScoreboardDisplayStatusText.Text = "Contenuto attuale: Statistiche giocatori su tutti i tabelloni";
            return;
        }

        if (ShowSponsorsRadio.IsChecked == true)
        {
            await _broadcaster.ShowSponsorsAsync(GetActiveSponsorSlides());
            ScoreboardDisplayStatusText.Text = "Contenuto attuale: Carosello sponsor su tutti i tabelloni";
            return;
        }

        await _broadcaster.ShowScoreboardAsync();
        await BroadcastAsync();
        ScoreboardDisplayStatusText.Text = "Contenuto attuale: Partita e punteggi su tutti i tabelloni";
    }

    private List<SponsorSlide> GetActiveSponsorSlides()
    {
        return _db.Sponsors
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToList()
            .Select(x => new SponsorSlide(x.Name, x.Description, x.ImagePath))
            .ToList();
    }

    private async void AddTeam_Click(object sender, RoutedEventArgs e)
    {
        if (_currentEditionId is null)
        {
            MessageBox.Show("Crea prima almeno una edizione.", "Squadre", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var now = Now();
        var team = new Team
        {
            EditionId = _currentEditionId.Value,
            Name = "",
            ShortName = "",
            PrimaryColor = "#2563eb",
            SecondaryColor = "#111827",
            CreatedAt = now,
            UpdatedAt = now
        };

        var form = new TeamFormWindow(team, isNew: true)
        {
            Owner = this
        };

        if (form.ShowDialog() != true)
        {
            return;
        }

        if (_onlineEntities is not null)
        {
            try
            {
                team = await _onlineEntities.CreateTeamAsync(team);
                UpsertLocalTeam(team);
                _db.SaveChanges();
                LoadCrudData();
                TeamsGrid.SelectedItem = _teams.FirstOrDefault(x => x.Id == team.Id);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Squadre online", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return;
        }

        _db.Teams.Add(team);
        if (SaveChanges())
        {
            LoadCrudData();
            TeamsGrid.SelectedItem = _teams.FirstOrDefault(x => x.Id == team.Id);
        }
    }

    private async void EditTeam_Click(object sender, RoutedEventArgs e)
    {
        if (TeamsGrid.SelectedItem is not Team team)
        {
            MessageBox.Show("Seleziona una squadra da modificare.", "Squadre", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var form = new TeamFormWindow(team, isNew: false)
        {
            Owner = this
        };

        if (form.ShowDialog() == true)
        {
            team.UpdatedAt = Now();
            if (_onlineEntities is not null)
            {
                try
                {
                    team = await _onlineEntities.UpdateTeamAsync(team);
                    UpsertLocalTeam(team);
                    _db.SaveChanges();
                    LoadCrudData();
                    TeamsGrid.SelectedItem = _teams.FirstOrDefault(x => x.Id == team.Id);
                    ApplyTeamsToLivePreview();
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, "Squadre online", MessageBoxButton.OK, MessageBoxImage.Error);
                    LoadCrudData();
                }

                return;
            }

            if (SaveChanges())
            {
                TeamsGrid.Items.Refresh();
                ApplyTeamsToLivePreview();
            }
        }
    }

    private async void DeleteTeam_Click(object sender, RoutedEventArgs e)
    {
        if (TeamsGrid.SelectedItem is not Team team)
        {
            return;
        }

        if (HasBlockingLinks("squadra", GetTeamLinks(team.Id)))
        {
            return;
        }

        var result = MessageBox.Show(
            $"Eliminare la squadra '{team.Name}'?",
            "Conferma eliminazione",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        if (_onlineEntities is not null)
        {
            try
            {
                await _onlineEntities.DeleteTeamAsync(team.Id);
                var local = _db.Teams.FirstOrDefault(x => x.Id == team.Id);
                if (local is not null)
                {
                    _db.Teams.Remove(local);
                    _db.SaveChanges();
                }

                LoadCrudData();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Squadre online", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return;
        }

        _db.Teams.Remove(team);
        if (SaveChanges())
        {
            LoadCrudData();
        }
    }

    private async void AddPlayer_Click(object sender, RoutedEventArgs e)
    {
        var now = Now();
        var player = new Player
        {
            FirstName = "",
            LastName = "",
            CreatedAt = now,
            UpdatedAt = now
        };

        var form = new PlayerFormWindow(player, isNew: true)
        {
            Owner = this
        };

        if (form.ShowDialog() != true)
        {
            return;
        }

        if (_onlineEntities is not null)
        {
            try
            {
                player = await _onlineEntities.CreatePlayerAsync(player);
                UpsertLocalPlayer(player);
                _db.SaveChanges();
                LoadCrudData();
                PlayersGrid.SelectedItem = _players.FirstOrDefault(x => x.Id == player.Id);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Giocatori online", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return;
        }

        _db.Players.Add(player);
        if (SaveChanges())
        {
            LoadCrudData();
            PlayersGrid.SelectedItem = _players.FirstOrDefault(x => x.Id == player.Id);
        }
    }

    private async void EditPlayer_Click(object sender, RoutedEventArgs e)
    {
        if (PlayersGrid.SelectedItem is not Player player)
        {
            MessageBox.Show("Seleziona un giocatore da modificare.", "Giocatori", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var form = new PlayerFormWindow(player, isNew: false)
        {
            Owner = this
        };

        if (form.ShowDialog() == true)
        {
            player.UpdatedAt = Now();
            if (_onlineEntities is not null)
            {
                try
                {
                    player = await _onlineEntities.UpdatePlayerAsync(player);
                    UpsertLocalPlayer(player);
                    _db.SaveChanges();
                    LoadCrudData();
                    PlayersGrid.SelectedItem = _players.FirstOrDefault(x => x.Id == player.Id);
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, "Giocatori online", MessageBoxButton.OK, MessageBoxImage.Error);
                    LoadCrudData();
                }

                return;
            }

            if (SaveChanges())
            {
                PlayersGrid.Items.Refresh();
                LoadRosterForSelectedTeam();
            }
        }
    }

    private async void DeletePlayer_Click(object sender, RoutedEventArgs e)
    {
        if (PlayersGrid.SelectedItem is not Player player)
        {
            return;
        }

        if (HasBlockingLinks("giocatore", GetPlayerLinks(player.Id)))
        {
            return;
        }

        var result = MessageBox.Show(
            $"Eliminare il giocatore '{player.FirstName} {player.LastName}'?",
            "Conferma eliminazione",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        if (_onlineEntities is not null)
        {
            try
            {
                await _onlineEntities.DeletePlayerAsync(player.Id);
                var local = _db.Players.FirstOrDefault(x => x.Id == player.Id);
                if (local is not null)
                {
                    _db.Players.Remove(local);
                    _db.SaveChanges();
                }

                LoadCrudData();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Giocatori online", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return;
        }

        _db.Players.Remove(player);
        if (SaveChanges())
        {
            LoadCrudData();
        }
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_isThreePointContestMode)
        {
            var wasRunning = _contestClock.IsRunning;
            _contestClock.Update();
            if (wasRunning && !_contestClock.IsRunning && _contestClock.RemainingMs == 0)
            {
                _contestStatus = "Finished";
                PersistContestState();
            }
            else
            {
                RenderContestState();
                _ = BroadcastContestAsync();
            }
            return;
        }

        _state.GameClockMs = _gameClock.Update();
        _state.ShotClockMs = _shotClock.Update();
        _state.IsGameClockRunning = _gameClock.IsRunning;
        _state.IsShotClockRunning = _shotClock.IsRunning;
        RenderLocalState();
        _ = BroadcastAsync();
    }

    private void RenderContestState()
    {
        if (ContestClockText is null || ContestStatusText is null ||
            ContestDataMessageText is null ||
            ContestCurrentStationText is null || ContestTotalScoreText is null ||
            ContestStation1Text is null || ContestStation2Text is null || ContestStation3Text is null ||
            ContestStation4Text is null || ContestStation5Text is null || ContestStartPauseButton is null ||
            ContestStation1Radio is null || ContestStation2Radio is null || ContestStation3Radio is null ||
            ContestStation4Radio is null || ContestStation5Radio is null)
        {
            return;
        }

        var scores = _currentContestRound is null ? [0, 0, 0, 0, 0] : GetContestStationScores(_currentContestRound);
        ContestClockText.Text = Math.Max(0, (int)Math.Ceiling(_contestClock.RemainingMs / 1000d)).ToString();
        ContestStatusText.Text = $"Stato: {FormatContestStatus(_contestStatus)}";
        var selectionLocked = _contestStatus is "Live" or "Paused";
        ContestEventCombo.IsEnabled = !selectionLocked;
        ContestEntryCombo.IsEnabled = !selectionLocked;
        ContestRoundCombo.IsEnabled = !selectionLocked;
        LiveModeCombo.IsEnabled = !selectionLocked;
        DatabaseTabItem.IsEnabled = !selectionLocked;
        ContestDataMessageText.Text = ContestEventCombo.SelectedItem is null
            ? "Nessun evento 3 Point Contest disponibile. Crealo nella sezione Database."
            : ContestEntryCombo.SelectedItem is null
                ? "Nessun tiratore associato all'evento. Aggiungilo nel CRUD 3 Point Contest."
                : ContestRoundCombo.SelectedItem is null
                    ? "Nessuna prova disponibile per il tiratore selezionato. Creala nel CRUD 3 Point Contest."
                    : "Dati collegati al database e pronti per la gestione live.";
        ContestDataMessageText.Foreground = ContestRoundCombo.SelectedItem is null
            ? new SolidColorBrush(Color.FromRgb(180, 83, 9))
            : new SolidColorBrush(Color.FromRgb(22, 101, 52));
        ContestCurrentStationText.Text = $"Postazione {_contestStation}: {scores[_contestStation - 1]}";
        ContestTotalScoreText.Text = $"Totale: {scores.Sum()}";
        ContestStation1Text.Text = $"1: {scores[0]}";
        ContestStation2Text.Text = $"2: {scores[1]}";
        ContestStation3Text.Text = $"3: {scores[2]}";
        ContestStation4Text.Text = $"4: {scores[3]}";
        ContestStation5Text.Text = $"5: {scores[4]}";
        ContestStartPauseButton.Content = _contestStatus == "Live" ? "Pausa" : _contestStatus == "Paused" ? "Riprendi" : "Inizia";
        ContestStartPauseButton.IsEnabled = _currentContestRound is not null && _contestStatus != "Finished";

        _isLoadingContest = true;
        try
        {
            var radios = new[] { ContestStation1Radio, ContestStation2Radio, ContestStation3Radio, ContestStation4Radio, ContestStation5Radio };
            radios[_contestStation - 1].IsChecked = true;
        }
        finally
        {
            _isLoadingContest = false;
        }
    }

    private void PersistContestState()
    {
        if (_currentContestRound is null)
        {
            RenderContestState();
            return;
        }

        _currentContestRound.TotalScore = GetContestStationScores(_currentContestRound).Sum();
        _currentContestRound.Notes = SerializeContestLiveState();
        if (_currentContestEntry is not null)
        {
            UpdateThreePointEntryTotal(_currentContestEntry.Id);
        }

        var competitionEvent = ContestEventCombo.SelectedItem as CompetitionEvent;
        if (competitionEvent is not null)
        {
            competitionEvent.Status = _contestStatus switch
            {
                "Live" or "Paused" => "Live",
                "Finished" => "Completed",
                _ => "Scheduled"
            };
        }

        _db.SaveChanges();
        RefreshThreePointContestCrudViews();
        RenderContestState();
        _ = SaveContestOnlineAsync(competitionEvent);
        _ = BroadcastContestAsync();
    }

    private void RefreshThreePointContestCrudViews()
    {
        ThreePointEntriesGrid?.Items.Refresh();
        ThreePointRoundsGrid?.Items.Refresh();
    }

    private async Task SaveContestOnlineAsync(CompetitionEvent? competitionEvent)
    {
        if (_onlineEntities is null || _currentContestRound is null)
        {
            return;
        }

        try
        {
            var updatedRound = await _onlineEntities.UpdateThreePointContestRoundAsync(_currentContestRound);
            CopyThreePointContestRound(updatedRound, _currentContestRound);
            if (_currentContestEntry is not null)
            {
                var updatedEntry = await _onlineEntities.UpdateThreePointContestEntryAsync(_currentContestEntry);
                CopyThreePointContestEntry(updatedEntry, _currentContestEntry);
            }
            if (competitionEvent is not null)
            {
                var updatedEvent = await _onlineEntities.UpdateCompetitionEventAsync(competitionEvent);
                CopyCompetitionEvent(updatedEvent, competitionEvent);
            }
            _db.SaveChanges();
            RefreshThreePointContestCrudViews();
        }
        catch (Exception exception)
        {
            LiveSyncStatusText.Text = $"Sync contest fallita: {exception.Message}";
        }
    }

    private async Task BroadcastContestAsync()
    {
        var option = ContestEntryCombo.SelectedItem as ContestEntryOption;
        var team = option is null ? null : _db.Teams.FirstOrDefault(x => x.Id == option.Entry.TeamId);
        var scores = _currentContestRound is null ? [0, 0, 0, 0, 0] : GetContestStationScores(_currentContestRound);
        await _broadcaster.BroadcastContestAsync(new ThreePointContestDisplayState
        {
            IsActive = _isThreePointContestMode && _currentContestRound is not null,
            EventName = (ContestEventCombo.SelectedItem as CompetitionEvent)?.Name ?? "3 Point Contest",
            PlayerName = option?.PlayerName ?? "",
            TeamName = option?.TeamName ?? "",
            TeamColor = string.IsNullOrWhiteSpace(team?.PrimaryColor) ? "#ea6324" : team.PrimaryColor,
            Score = scores.Sum(),
            CurrentStation = _contestStation,
            StationScores = scores,
            ClockMs = _contestClock.Update(),
            Status = _contestStatus
        });
    }

    private string SerializeContestLiveState() => JsonSerializer.Serialize(new ContestRoundLiveState
    {
        Status = _contestStatus,
        Station = _contestStation,
        ClockMs = _contestClock.Update()
    });

    private static ContestRoundLiveState DeserializeContestLiveState(string? notes)
    {
        if (!string.IsNullOrWhiteSpace(notes))
        {
            try
            {
                return JsonSerializer.Deserialize<ContestRoundLiveState>(notes) ?? new ContestRoundLiveState();
            }
            catch (JsonException)
            {
                // Existing free-form notes are treated as a fresh live state.
            }
        }
        return new ContestRoundLiveState();
    }

    private static int[] GetContestStationScores(ThreePointContestRound round) =>
        [round.Station1Score, round.Station2Score, round.Station3Score, round.Station4Score, round.Station5Score];

    private static int GetContestStationScore(ThreePointContestRound round, int station) => GetContestStationScores(round)[station - 1];

    private static void SetContestStationScore(ThreePointContestRound round, int station, int score)
    {
        switch (station)
        {
            case 1: round.Station1Score = score; break;
            case 2: round.Station2Score = score; break;
            case 3: round.Station3Score = score; break;
            case 4: round.Station4Score = score; break;
            case 5: round.Station5Score = score; break;
        }
    }

    private static string FormatContestStatus(string status) => status switch
    {
        "Live" => "in corso",
        "Paused" => "in pausa",
        "Finished" => "concluso",
        _ => "pronto"
    };

    private void RenderLocalState()
    {
        if (HomeScoreText is null || AwayScoreText is null || HomeFoulsText is null || AwayFoulsText is null ||
            PeriodText is null || PeriodCombo is null || GameClockText is null || ShotClockText is null ||
            TeamFoulsText is null || StartPauseButton is null || ShotStartPauseButton is null ||
            LiveMatchStatusText is null || HomeLivePanel is null || ClockLivePanel is null || AwayLivePanel is null)
        {
            return;
        }

        _isRenderingState = true;
        HomeScoreText.Text = _state.HomeScore.ToString();
        AwayScoreText.Text = _state.AwayScore.ToString();
        HomeFoulsText.Text = $"Falli {_state.HomeFouls}";
        AwayFoulsText.Text = $"Falli {_state.AwayFouls}";
        PeriodText.Text = _state.Period.ToString();
        foreach (var item in PeriodCombo.Items.OfType<System.Windows.Controls.ComboBoxItem>())
        {
            if (Convert.ToInt32(item.Tag) == _state.Period)
            {
                PeriodCombo.SelectedItem = item;
                break;
            }
        }

        GameClockText.Text = FormatGameClock(_state.GameClockMs);
        ShotClockText.Text = Math.Ceiling(_state.ShotClockMs / 1000d).ToString("0");
        TeamFoulsText.Text = $"Falli {_state.HomeFouls} - {_state.AwayFouls}";
        SetClockButtonState(StartPauseButton, _gameClock.IsRunning, "▶ Start [Spazio]", "⏸ Pausa [Spazio]");
        SetClockButtonState(ShotStartPauseButton, _shotClock.IsRunning, "▶ Start 24 [0]", "⏸ Pausa 24 [0]");
        LiveMatchCombo.IsEnabled = !_isFreeLiveMode && !IsLiveSelectionLocked;
        LiveModeCombo.IsEnabled = !IsLiveSelectionLocked;
        DatabaseTabItem.IsEnabled = !IsOfficialLiveSessionLocked;
        if (IsOfficialLiveSessionLocked && MainTabControl.SelectedItem == DatabaseTabItem)
        {
            MainTabControl.SelectedItem = LiveTabItem;
        }
        LiveMatchStatusText.Text = _isFreeLiveMode ? "Modalità: libera" : $"Stato: {FormatMatchStatus(_currentLiveMatchStatus)}";
        var liveEnabled = _isFreeLiveMode || _currentLiveMatchStatus == "Live";
        HomeLivePanel.IsEnabled = liveEnabled;
        ClockLivePanel.IsEnabled = liveEnabled;
        AwayLivePanel.IsEnabled = liveEnabled;
        UpdateOfficialMatchButtons();
        _isRenderingState = false;
    }

    private static void SetClockButtonState(Button button, bool isRunning, string startText, string pauseText)
    {
        button.Content = isRunning ? pauseText : startText;
        button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isRunning ? "#f59e0b" : "#16a34a"));
        button.Foreground = Brushes.White;
    }

    private void UpdateOfficialMatchButtons()
    {
        if (PrepareMatchButton is null || StartMatchButton is null || PauseMatchButton is null || ResumeMatchButton is null || FinishMatchButton is null)
        {
            return;
        }

        if (_isFreeLiveMode)
        {
            PrepareMatchButton.IsEnabled = false;
            StartMatchButton.IsEnabled = false;
            PauseMatchButton.IsEnabled = false;
            ResumeMatchButton.IsEnabled = false;
            FinishMatchButton.IsEnabled = false;
            return;
        }

        var hasMatch = _currentLiveMatchId is not null;
        PrepareMatchButton.IsEnabled = hasMatch && _currentLiveMatchStatus is "" or "Scheduled" or "Ready";
        StartMatchButton.IsEnabled = hasMatch && _currentLiveMatchStatus is "Scheduled" or "Ready";
        PauseMatchButton.IsEnabled = hasMatch && _currentLiveMatchStatus == "Live";
        ResumeMatchButton.IsEnabled = hasMatch && _currentLiveMatchStatus == "Paused";
        FinishMatchButton.IsEnabled = hasMatch && _currentLiveMatchStatus is "Live" or "Paused";
    }

    private static string FormatMatchStatus(string status) => status switch
    {
        "Scheduled" => "programmata",
        "Ready" => "preparata",
        "Live" => "in corso",
        "Paused" => "in pausa",
        "Finished" => "chiusa",
        "Cancelled" => "annullata",
        _ => "-"
    };

    private void LiveMatchCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (LiveMatchCombo.SelectedItem is MatchOption option)
        {
            if (IsLiveSelectionLocked && option.Id != _currentLiveMatchId)
            {
                LiveMatchCombo.SelectedItem = _liveMatchOptions.FirstOrDefault(x => x.Id == _currentLiveMatchId);
                return;
            }

            LoadLiveMatch(option.Id);
        }
    }

    private void LoadCrudData()
    {
        _players = LoadPlayers();

        _tournaments = LoadTournaments();

        var tournamentsById = _tournaments.ToDictionary(tournament => tournament.Id);
        var editions = LoadEditions();
        _currentEditionId = editions
            .OrderByDescending(edition => edition.Year)
            .ThenBy(edition => edition.Id)
            .Select(edition => (int?)edition.Id)
            .FirstOrDefault();
        _teams = LoadTeams();
        var editionsById = editions.ToDictionary(edition => edition.Id);

        _editionRows = new ObservableCollection<EditionRow>(
            editions
                .Select(edition => new EditionRow(
                    edition,
                    tournamentsById.TryGetValue(edition.TournamentId, out var tournament) ? tournament.Name : $"Torneo #{edition.TournamentId}")));

        _courtRows = new ObservableCollection<CourtRow>(
            LoadCourts()
                .Select(court => new CourtRow(
                    court,
                    editionsById.TryGetValue(court.EditionId, out var edition) ? edition.Name : $"Edizione #{court.EditionId}")));

        _groupRows = new ObservableCollection<GroupRow>(
            LoadTournamentGroups()
                .Select(group => new GroupRow(
                    group,
                    editionsById.TryGetValue(group.EditionId, out var edition) ? edition.Name : $"Edizione #{group.EditionId}")));

        var groupsById = _groupRows.ToDictionary(row => row.Group.Id);
        var teamsById = _db.Teams.ToDictionary(team => team.Id);
        var courtsById = _courtRows.ToDictionary(row => row.Court.Id);
        _groupTeamRows = new ObservableCollection<GroupTeamRow>(
            LoadGroupTeams()
                .Select(groupTeam => new GroupTeamRow(
                    groupTeam,
                    groupsById.TryGetValue(groupTeam.GroupId, out var groupRow) ? groupRow.Group.Name : $"Girone #{groupTeam.GroupId}",
                    teamsById.TryGetValue(groupTeam.TeamId, out var team) ? team.Name : $"Squadra #{groupTeam.TeamId}")));

        var matches = LoadMatches()
            .OrderBy(match => match.ScheduledStartAt)
            .ThenBy(match => match.Id)
            .ToList();
        var matchTeams = LoadMatchTeams().GroupBy(x => x.MatchId).ToDictionary(x => x.Key, x => x.ToList());
        _matchRows = new ObservableCollection<MatchRow>(
            matches
                .Select(match =>
                {
                    matchTeams.TryGetValue(match.Id, out var sides);
                    var home = sides?.FirstOrDefault(x => x.Side == "Home");
                    var away = sides?.FirstOrDefault(x => x.Side == "Away");
                    return new MatchRow(
                        match,
                        editionsById.TryGetValue(match.EditionId, out var edition) ? edition.Name : $"Edizione #{match.EditionId}",
                        match.GroupId is not null && groupsById.TryGetValue(match.GroupId.Value, out var groupRow) ? groupRow.Group.Name : "",
                        match.CourtId is not null && courtsById.TryGetValue(match.CourtId.Value, out var courtRow) ? courtRow.Court.Name : "",
                        home is not null && teamsById.TryGetValue(home.TeamId, out var homeTeam) ? homeTeam.Name : "",
                        away is not null && teamsById.TryGetValue(away.TeamId, out var awayTeam) ? awayTeam.Name : "");
                }));

        _competitionEventRows = new ObservableCollection<CompetitionEventRow>(
            LoadCompetitionEvents()
                .Where(x => x.EventType == "ThreePointContest")
                .OrderBy(x => x.ScheduledStartAt)
                .ThenBy(x => x.Name)
                .Select(evt => new CompetitionEventRow(
                    evt,
                    editionsById.TryGetValue(evt.EditionId, out var edition) ? edition.Name : $"Edizione #{evt.EditionId}")));

        var eventsById = _competitionEventRows.ToDictionary(x => x.Event.Id);
        var allTeamsById = _db.Teams.ToDictionary(x => x.Id);
        var allPlayersById = _db.Players.ToDictionary(x => x.Id);
        _threePointEntryRows = new ObservableCollection<ThreePointEntryRow>(
            LoadThreePointContestEntries()
                .OrderBy(x => x.CompetitionEventId)
                .ThenBy(x => x.SeedOrder)
                .Select(entry => new ThreePointEntryRow(
                    entry,
                    eventsById.TryGetValue(entry.CompetitionEventId, out var evt) ? evt.Event.Name : $"Evento #{entry.CompetitionEventId}",
                    allTeamsById.TryGetValue(entry.TeamId, out var team) ? team.Name : $"Squadra #{entry.TeamId}",
                    allPlayersById.TryGetValue(entry.PlayerId, out var player) ? $"{player.LastName} {player.FirstName}".Trim() : $"Giocatore #{entry.PlayerId}")));

        var entriesById = _threePointEntryRows.ToDictionary(x => x.Entry.Id);
        _threePointRoundRows = new ObservableCollection<ThreePointRoundRow>(
            LoadThreePointContestRounds()
                .OrderBy(x => x.EntryId).ThenBy(x => x.RoundNumber)
                .Select(round => new ThreePointRoundRow(
                    round,
                    entriesById.TryGetValue(round.EntryId, out var entry) ? $"{entry.TeamName} - {entry.PlayerName}" : $"Partecipante #{round.EntryId}")));

        var matchesById = _matchRows.ToDictionary(x => x.Match.Id);
        _forfeitRows = new ObservableCollection<ForfeitRow>(
            LoadForfeitResults()
                .OrderBy(x => x.MatchId)
                .Select(forfeit => new ForfeitRow(
                    forfeit,
                    matchesById.TryGetValue(forfeit.MatchId, out var match) ? $"{match.HomeTeamName} vs {match.AwayTeamName}" : $"Partita #{forfeit.MatchId}",
                    forfeit.WinningTeamId is not null && allTeamsById.TryGetValue(forfeit.WinningTeamId.Value, out var winner) ? winner.Name : "",
                    $"{forfeit.HomeAssignedScore}-{forfeit.AwayAssignedScore}")));

        var allGroupsById = _db.TournamentGroups.ToDictionary(x => x.Id);
        _standingRows = new ObservableCollection<StandingRow>(
            LoadStandings()
                .OrderBy(x => x.GroupId).ThenBy(x => x.Position)
                .Select(standing => new StandingRow(
                    standing,
                    allGroupsById.TryGetValue(standing.GroupId, out var group) ? group.Name : $"Girone #{standing.GroupId}",
                    allTeamsById.TryGetValue(standing.TeamId, out var team) ? team.Name : $"Squadra #{standing.TeamId}")));

        _sponsors = LoadSponsors();

        TournamentsGrid.ItemsSource = _tournaments;
        EditionsGrid.ItemsSource = _editionRows;
        CourtsGrid.ItemsSource = _courtRows;
        GroupsGrid.ItemsSource = _groupRows;
        GroupTeamsGrid.ItemsSource = _groupTeamRows;
        MatchesGrid.ItemsSource = _matchRows;
        CompetitionEventsGrid.ItemsSource = _competitionEventRows;
        ThreePointEntriesGrid.ItemsSource = _threePointEntryRows;
        ThreePointRoundsGrid.ItemsSource = _threePointRoundRows;
        ForfeitsGrid.ItemsSource = _forfeitRows;
        StandingsGrid.ItemsSource = _standingRows;
        SponsorsGrid.ItemsSource = _sponsors;
        TeamsGrid.ItemsSource = _teams;
        PlayersGrid.ItemsSource = _players;
        RosterTeamCombo.ItemsSource = _teams;
        RosterTeamCombo.SelectedItem ??= _teams.FirstOrDefault();
        LoadLiveMatchOptions();
        LoadSelectedLiveMatchOrDefault();
        LoadRosterForSelectedTeam();
    }

    private ObservableCollection<Player> LoadPlayers()
    {
        if (_onlineEntities is not null)
        {
            try
            {
                var players = _onlineEntities.GetPlayersAsync().GetAwaiter().GetResult();
                UpsertLocalPlayers(players);
                return new ObservableCollection<Player>(players);
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    $"Lettura giocatori online non riuscita, uso i dati locali: {exception.Message}",
                    "Giocatori online",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        return new ObservableCollection<Player>(
            _db.Players
                .OrderBy(player => player.LastName)
                .ThenBy(player => player.FirstName)
                .ToList());
    }

    private ObservableCollection<Sponsor> LoadSponsors()
    {
        if (_onlineEntities is not null)
        {
            try
            {
                var sponsors = _onlineEntities.GetSponsorsAsync().GetAwaiter().GetResult();
                ReplaceLocalSponsors(sponsors);
                return new ObservableCollection<Sponsor>(sponsors);
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    $"Lettura sponsor online non riuscita, uso i dati locali: {exception.Message}",
                    "Sponsor online",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        return new ObservableCollection<Sponsor>(
            _db.Sponsors
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .ToList());
    }

    private void ReplaceLocalSponsors(IReadOnlyCollection<Sponsor> sponsors)
    {
        _db.Sponsors.RemoveRange(_db.Sponsors);
        foreach (var sponsor in sponsors)
        {
            _db.Sponsors.Add(CloneSponsor(sponsor));
        }

        _db.SaveChanges();
    }

    private void UpsertLocalSponsor(Sponsor sponsor)
    {
        var local = _db.Sponsors.FirstOrDefault(x => x.Id == sponsor.Id);
        if (local is null)
        {
            _db.Sponsors.Add(CloneSponsor(sponsor));
            return;
        }

        CopySponsor(sponsor, local);
    }

    private void UpsertLocalPlayers(IEnumerable<Player> players)
    {
        ReconcileLocalRows(players, _db.Players, x => x.Id, UpsertLocalPlayer);
    }

    private void UpsertLocalPlayer(Player player)
    {
        var local = _db.Players.FirstOrDefault(x => x.Id == player.Id);
        if (local is null)
        {
            _db.Players.Add(ClonePlayer(player));
            return;
        }

        CopyPlayer(player, local);
    }

    private static Sponsor CloneSponsor(Sponsor source)
    {
        var target = new Sponsor();
        CopySponsor(source, target);
        return target;
    }

    private static void CopySponsor(Sponsor source, Sponsor target)
    {
        target.Id = source.Id;
        target.Name = source.Name;
        target.Description = source.Description;
        target.ImagePath = source.ImagePath;
        target.IsActive = source.IsActive;
        target.SortOrder = source.SortOrder;
        target.CreatedAt = source.CreatedAt;
        target.UpdatedAt = source.UpdatedAt;
    }

    private static Player ClonePlayer(Player source)
    {
        var target = new Player();
        CopyPlayer(source, target);
        return target;
    }

    private static void CopyPlayer(Player source, Player target)
    {
        target.Id = source.Id;
        target.FirstName = source.FirstName;
        target.LastName = source.LastName;
        target.Nickname = source.Nickname;
        target.FiscalCode = source.FiscalCode;
        target.Address = source.Address;
        target.PhoneNumber = source.PhoneNumber;
        target.Email = source.Email;
        target.BirthDate = source.BirthDate;
        target.PhotoPath = source.PhotoPath;
        target.CreatedAt = source.CreatedAt;
        target.UpdatedAt = source.UpdatedAt;
    }

    private void UpsertLocalTournaments(IEnumerable<Tournament> tournaments)
    {
        ReconcileLocalRows(tournaments, _db.Tournaments, x => x.Id, UpsertLocalTournament);
    }

    private void UpsertLocalTournament(Tournament tournament)
    {
        var local = _db.Tournaments.FirstOrDefault(x => x.Id == tournament.Id);
        if (local is null)
        {
            _db.Tournaments.Add(CloneTournament(tournament));
            return;
        }

        CopyTournament(tournament, local);
    }

    private static Tournament CloneTournament(Tournament source)
    {
        var target = new Tournament();
        CopyTournament(source, target);
        return target;
    }

    private static void CopyTournament(Tournament source, Tournament target)
    {
        target.Id = source.Id;
        target.Name = source.Name;
        target.Description = source.Description;
        target.CreatedAt = source.CreatedAt;
        target.UpdatedAt = source.UpdatedAt;
    }

    private List<Edition> LoadEditions()
    {
        if (_onlineEntities is not null)
        {
            try
            {
                var editions = _onlineEntities.GetEditionsAsync().GetAwaiter().GetResult();
                UpsertLocalEditions(editions);
                return editions;
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Lettura edizioni online non riuscita, uso i dati locali: {exception.Message}", "Edizioni online", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        return _db.Editions.OrderByDescending(edition => edition.Year).ThenBy(edition => edition.Name).ToList();
    }

    private ObservableCollection<Team> LoadTeams()
    {
        if (_onlineEntities is not null)
        {
            try
            {
                var teams = _onlineEntities.GetTeamsAsync().GetAwaiter().GetResult();
                UpsertLocalTeams(teams);
                return new ObservableCollection<Team>(
                    teams.Where(team => _currentEditionId != null && team.EditionId == _currentEditionId.Value)
                        .OrderBy(team => team.Name));
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Lettura squadre online non riuscita, uso i dati locali: {exception.Message}", "Squadre online", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        return new ObservableCollection<Team>(
            _db.Teams
                .Where(team => _currentEditionId != null && team.EditionId == _currentEditionId.Value)
                .OrderBy(team => team.Name)
                .ToList());
    }

    private List<Court> LoadCourts()
    {
        if (_onlineEntities is not null)
        {
            try
            {
                var courts = _onlineEntities.GetCourtsAsync().GetAwaiter().GetResult();
                UpsertLocalCourts(courts);
                return courts;
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Lettura campi online non riuscita, uso i dati locali: {exception.Message}", "Campi online", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        return _db.Courts.OrderBy(court => court.Name).ToList();
    }

    private void ReplaceLocalEditions(IReadOnlyCollection<Edition> editions)
    {
        _db.Editions.RemoveRange(_db.Editions);
        foreach (var edition in editions)
        {
            _db.Editions.Add(CloneEdition(edition));
        }

        _db.SaveChanges();
    }

    private void UpsertLocalEditions(IEnumerable<Edition> editions)
    {
        ReconcileLocalRows(editions, _db.Editions, x => x.Id, UpsertLocalEdition);
    }

    private void UpsertLocalTeams(IEnumerable<Team> teams)
    {
        ReconcileLocalRows(teams, _db.Teams, x => x.Id, UpsertLocalTeam);
    }

    private void UpsertLocalCourts(IEnumerable<Court> courts)
    {
        ReconcileLocalRows(courts, _db.Courts, x => x.Id, UpsertLocalCourt);
    }

    private void ReplaceLocalTeams(IReadOnlyCollection<Team> teams)
    {
        _db.Teams.RemoveRange(_db.Teams);
        foreach (var team in teams)
        {
            _db.Teams.Add(CloneTeam(team));
        }

        _db.SaveChanges();
    }

    private void ReplaceLocalCourts(IReadOnlyCollection<Court> courts)
    {
        _db.Courts.RemoveRange(_db.Courts);
        foreach (var court in courts)
        {
            _db.Courts.Add(CloneCourt(court));
        }

        _db.SaveChanges();
    }

    private void UpsertLocalEdition(Edition edition)
    {
        var local = _db.Editions.FirstOrDefault(x => x.Id == edition.Id);
        if (local is null)
        {
            _db.Editions.Add(CloneEdition(edition));
            return;
        }

        CopyEdition(edition, local);
    }

    private void UpsertLocalTeam(Team team)
    {
        var local = _db.Teams.FirstOrDefault(x => x.Id == team.Id);
        if (local is null)
        {
            _db.Teams.Add(CloneTeam(team));
            return;
        }

        CopyTeam(team, local);
    }

    private void UpsertLocalCourt(Court court)
    {
        var local = _db.Courts.FirstOrDefault(x => x.Id == court.Id);
        if (local is null)
        {
            _db.Courts.Add(CloneCourt(court));
            return;
        }

        CopyCourt(court, local);
    }

    private static Edition CloneEdition(Edition source)
    {
        var target = new Edition();
        CopyEdition(source, target);
        return target;
    }

    private static void CopyEdition(Edition source, Edition target)
    {
        target.Id = source.Id;
        target.TournamentId = source.TournamentId;
        target.Name = source.Name;
        target.Year = source.Year;
        target.StartDate = source.StartDate;
        target.EndDate = source.EndDate;
        target.Status = source.Status;
        target.CreatedAt = source.CreatedAt;
        target.UpdatedAt = source.UpdatedAt;
    }

    private static Team CloneTeam(Team source)
    {
        var target = new Team();
        CopyTeam(source, target);
        return target;
    }

    private static void CopyTeam(Team source, Team target)
    {
        target.Id = source.Id;
        target.EditionId = source.EditionId;
        target.Name = source.Name;
        target.ShortName = source.ShortName;
        target.PrimaryColor = source.PrimaryColor;
        target.SecondaryColor = source.SecondaryColor;
        target.LogoPath = source.LogoPath;
        target.CreatedAt = source.CreatedAt;
        target.UpdatedAt = source.UpdatedAt;
    }

    private static Court CloneCourt(Court source)
    {
        var target = new Court();
        CopyCourt(source, target);
        return target;
    }

    private static void CopyCourt(Court source, Court target)
    {
        target.Id = source.Id;
        target.EditionId = source.EditionId;
        target.Name = source.Name;
        target.Location = source.Location;
    }

    private List<TournamentGroup> LoadTournamentGroups()
    {
        if (_onlineEntities is not null)
        {
            try
            {
                var groups = _onlineEntities.GetTournamentGroupsAsync().GetAwaiter().GetResult();
                UpsertLocalTournamentGroups(groups);
                return groups;
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Lettura gironi online non riuscita, uso i dati locali: {exception.Message}", "Gironi online", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        return _db.TournamentGroups.OrderBy(group => group.SortOrder).ThenBy(group => group.Code).ToList();
    }

    private List<GroupTeam> LoadGroupTeams()
    {
        if (_onlineEntities is not null)
        {
            try
            {
                var groupTeams = _onlineEntities.GetGroupTeamsAsync().GetAwaiter().GetResult();
                UpsertLocalGroupTeams(groupTeams);
                return groupTeams;
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Lettura squadre gironi online non riuscita, uso i dati locali: {exception.Message}", "Squadre gironi online", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        return _db.GroupTeams.OrderBy(groupTeam => groupTeam.GroupId).ThenBy(groupTeam => groupTeam.SeedLabel).ToList();
    }

    private List<TeamRoster> LoadTeamRosters()
    {
        if (_onlineEntities is not null)
        {
            try
            {
                var rosters = _onlineEntities.GetTeamRostersAsync().GetAwaiter().GetResult();
                UpsertLocalTeamRosters(rosters);
                return rosters;
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Lettura roster online non riuscita, uso i dati locali: {exception.Message}", "Roster online", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        return _db.TeamRosters.OrderBy(roster => roster.TeamId).ThenBy(roster => roster.JerseyNumber).ToList();
    }

    private void UpsertLocalTournamentGroups(IEnumerable<TournamentGroup> groups)
    {
        ReconcileLocalRows(groups, _db.TournamentGroups, x => x.Id, UpsertLocalTournamentGroup);
    }

    private void UpsertLocalGroupTeams(IEnumerable<GroupTeam> groupTeams)
    {
        ReconcileLocalRows(groupTeams, _db.GroupTeams, x => x.Id, UpsertLocalGroupTeam);
    }

    private void UpsertLocalTeamRosters(IEnumerable<TeamRoster> rosters)
    {
        ReconcileLocalRows(rosters, _db.TeamRosters, x => x.Id, UpsertLocalTeamRoster);
    }

    private void UpsertLocalTournamentGroup(TournamentGroup group)
    {
        var local = _db.TournamentGroups.FirstOrDefault(x => x.Id == group.Id);
        if (local is null)
        {
            _db.TournamentGroups.Add(CloneTournamentGroup(group));
            return;
        }

        CopyTournamentGroup(group, local);
    }

    private void UpsertLocalGroupTeam(GroupTeam groupTeam)
    {
        var local = _db.GroupTeams.FirstOrDefault(x => x.Id == groupTeam.Id);
        if (local is null)
        {
            _db.GroupTeams.Add(CloneGroupTeam(groupTeam));
            return;
        }

        CopyGroupTeam(groupTeam, local);
    }

    private void UpsertLocalTeamRoster(TeamRoster roster)
    {
        var local = _db.TeamRosters.FirstOrDefault(x => x.Id == roster.Id);
        if (local is null)
        {
            _db.TeamRosters.Add(CloneTeamRoster(roster));
            return;
        }

        CopyTeamRoster(roster, local);
    }

    private static TournamentGroup CloneTournamentGroup(TournamentGroup source)
    {
        var target = new TournamentGroup();
        CopyTournamentGroup(source, target);
        return target;
    }

    private static void CopyTournamentGroup(TournamentGroup source, TournamentGroup target)
    {
        target.Id = source.Id;
        target.EditionId = source.EditionId;
        target.Name = source.Name;
        target.Code = source.Code;
        target.SortOrder = source.SortOrder;
    }

    private static GroupTeam CloneGroupTeam(GroupTeam source)
    {
        var target = new GroupTeam();
        CopyGroupTeam(source, target);
        return target;
    }

    private static void CopyGroupTeam(GroupTeam source, GroupTeam target)
    {
        target.Id = source.Id;
        target.GroupId = source.GroupId;
        target.TeamId = source.TeamId;
        target.SeedLabel = source.SeedLabel;
    }

    private static TeamRoster CloneTeamRoster(TeamRoster source)
    {
        var target = new TeamRoster();
        CopyTeamRoster(source, target);
        return target;
    }

    private static void CopyTeamRoster(TeamRoster source, TeamRoster target)
    {
        target.Id = source.Id;
        target.TeamId = source.TeamId;
        target.PlayerId = source.PlayerId;
        target.JerseyNumber = source.JerseyNumber;
        target.Role = source.Role;
        target.IsCaptain = source.IsCaptain;
        target.IsActive = source.IsActive;
        target.CreatedAt = source.CreatedAt;
        target.UpdatedAt = source.UpdatedAt;
    }

    private List<Match> LoadMatches()
    {
        if (_onlineEntities is not null)
        {
            try
            {
                var matches = _onlineEntities.GetMatchesAsync().GetAwaiter().GetResult();
                UpsertLocalMatches(matches);
                if (_hasPendingLiveSync && _currentLiveMatchId is int pendingMatchId)
                {
                    var localPendingMatch = _db.Matches.FirstOrDefault(x => x.Id == pendingMatchId);
                    if (localPendingMatch is not null)
                    {
                        return matches
                            .Select(x => x.Id == pendingMatchId ? localPendingMatch : x)
                            .ToList();
                    }
                }

                return matches;
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Lettura partite online non riuscita, uso i dati locali: {exception.Message}", "Partite online", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        return _db.Matches.OrderBy(match => match.ScheduledStartAt).ThenBy(match => match.Id).ToList();
    }

    private List<MatchTeam> LoadMatchTeams()
    {
        if (_onlineEntities is not null)
        {
            try
            {
                var matchTeams = _onlineEntities.GetMatchTeamsAsync().GetAwaiter().GetResult();
                UpsertLocalMatchTeams(matchTeams);
                if (_hasPendingLiveSync && _currentLiveMatchId is int pendingMatchId)
                {
                    var localPendingSides = _db.MatchTeams.Where(x => x.MatchId == pendingMatchId).ToList();
                    return matchTeams
                        .Where(x => x.MatchId != pendingMatchId)
                        .Concat(localPendingSides)
                        .ToList();
                }

                return matchTeams;
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Lettura squadre partita online non riuscita, uso i dati locali: {exception.Message}", "Partite online", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        return _db.MatchTeams.OrderBy(x => x.MatchId).ThenBy(x => x.Side).ToList();
    }

    private List<MatchPlayer> LoadMatchPlayers()
    {
        if (_onlineEntities is not null)
        {
            try
            {
                var matchPlayers = _onlineEntities.GetMatchPlayersAsync().GetAwaiter().GetResult();
                UpsertLocalMatchPlayers(matchPlayers);
                return matchPlayers;
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Lettura giocatori partita online non riuscita, uso i dati locali: {exception.Message}", "Giocatori partita online", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        return _db.MatchPlayers.OrderBy(x => x.MatchId).ThenBy(x => x.TeamId).ThenBy(x => x.JerseyNumber).ToList();
    }

    private void UpsertLocalMatches(IEnumerable<Match> matches)
    {
        ReconcileLocalRows(matches, _db.Matches, x => x.Id, UpsertLocalMatch);
    }

    private void UpsertLocalMatchTeams(IEnumerable<MatchTeam> matchTeams)
    {
        ReconcileLocalRows(matchTeams, _db.MatchTeams, x => x.Id, UpsertLocalMatchTeam);
    }

    private void UpsertLocalMatchPlayers(IEnumerable<MatchPlayer> matchPlayers)
    {
        ReconcileLocalRows(matchPlayers, _db.MatchPlayers, x => x.Id, UpsertLocalMatchPlayer);
    }

    private void UpsertLocalMatch(Match match)
    {
        if (_hasPendingLiveSync && _currentLiveMatchId == match.Id)
        {
            return;
        }

        var local = _db.Matches.FirstOrDefault(x => x.Id == match.Id);
        if (local is null)
        {
            _db.Matches.Add(CloneMatch(match));
            return;
        }

        CopyMatch(match, local);
    }

    private void UpsertLocalMatchTeam(MatchTeam matchTeam)
    {
        if (_hasPendingLiveSync && _currentLiveMatchId == matchTeam.MatchId)
        {
            return;
        }

        var local = _db.MatchTeams.FirstOrDefault(x => x.Id == matchTeam.Id);
        if (local is null)
        {
            _db.MatchTeams.Add(CloneMatchTeam(matchTeam));
            return;
        }

        CopyMatchTeam(matchTeam, local);
    }

    private void UpsertLocalMatchPlayer(MatchPlayer matchPlayer)
    {
        var local = _db.MatchPlayers.FirstOrDefault(x => x.Id == matchPlayer.Id);
        if (local is null)
        {
            _db.MatchPlayers.Add(CloneMatchPlayer(matchPlayer));
            return;
        }

        CopyMatchPlayer(matchPlayer, local);
    }

    private static Match CloneMatch(Match source)
    {
        var target = new Match();
        CopyMatch(source, target);
        return target;
    }

    private static void CopyMatch(Match source, Match target)
    {
        target.Id = source.Id;
        target.EditionId = source.EditionId;
        target.GroupId = source.GroupId;
        target.CourtId = source.CourtId;
        target.Name = source.Name;
        target.Phase = source.Phase;
        target.Round = source.Round;
        target.ScheduledStartAt = source.ScheduledStartAt;
        target.ScheduledEndAt = source.ScheduledEndAt;
        target.ActualStartAt = source.ActualStartAt;
        target.ActualEndAt = source.ActualEndAt;
        target.Status = source.Status;
        target.PeriodDurationMs = source.PeriodDurationMs;
        target.ShotClockMs = source.ShotClockMs;
        target.MaxScore = source.MaxScore;
        target.MaxScoreEnabled = source.MaxScoreEnabled;
        target.WinnerTeamId = source.WinnerTeamId;
        target.WinReason = source.WinReason;
        target.CreatedAt = source.CreatedAt;
        target.UpdatedAt = source.UpdatedAt;
    }

    private static MatchTeam CloneMatchTeam(MatchTeam source)
    {
        var target = new MatchTeam();
        CopyMatchTeam(source, target);
        return target;
    }

    private static void CopyMatchTeam(MatchTeam source, MatchTeam target)
    {
        target.Id = source.Id;
        target.MatchId = source.MatchId;
        target.TeamId = source.TeamId;
        target.Side = source.Side;
        target.Score = source.Score;
        target.FoulsCurrentPeriod = source.FoulsCurrentPeriod;
        target.TimeoutsUsedTotal = source.TimeoutsUsedTotal;
        target.TimeoutsUsedPeriod = source.TimeoutsUsedPeriod;
        target.IsWinner = source.IsWinner;
        target.ForfeitScore = source.ForfeitScore;
    }

    private static MatchPlayer CloneMatchPlayer(MatchPlayer source)
    {
        var target = new MatchPlayer();
        CopyMatchPlayer(source, target);
        return target;
    }

    private static void CopyMatchPlayer(MatchPlayer source, MatchPlayer target)
    {
        target.Id = source.Id;
        target.MatchId = source.MatchId;
        target.TeamId = source.TeamId;
        target.PlayerId = source.PlayerId;
        target.JerseyNumber = source.JerseyNumber;
        target.IsStartingFive = source.IsStartingFive;
        target.IsOnCourt = source.IsOnCourt;
        target.Points = source.Points;
        target.PersonalFouls = source.PersonalFouls;
        target.IsFouledOut = source.IsFouledOut;
        target.IsEjected = source.IsEjected;
    }

    private List<CompetitionEvent> LoadCompetitionEvents()
    {
        if (_onlineEntities is not null)
        {
            try
            {
                var events = _onlineEntities.GetCompetitionEventsAsync().GetAwaiter().GetResult();
                UpsertLocalCompetitionEvents(events);
                return events;
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Lettura eventi online non riuscita, uso i dati locali: {exception.Message}", "Eventi online", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        return _db.CompetitionEvents.OrderBy(x => x.ScheduledStartAt).ThenBy(x => x.Name).ToList();
    }

    private List<ThreePointContestEntry> LoadThreePointContestEntries()
    {
        if (_onlineEntities is not null)
        {
            try
            {
                var entries = _onlineEntities.GetThreePointContestEntriesAsync().GetAwaiter().GetResult();
                UpsertLocalThreePointContestEntries(entries);
                return entries;
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Lettura partecipanti 3 punti online non riuscita, uso i dati locali: {exception.Message}", "3 Point Contest online", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        return _db.ThreePointContestEntries.OrderBy(x => x.CompetitionEventId).ThenBy(x => x.SeedOrder).ToList();
    }

    private List<ThreePointContestRound> LoadThreePointContestRounds()
    {
        if (_onlineEntities is not null)
        {
            try
            {
                var rounds = _onlineEntities.GetThreePointContestRoundsAsync().GetAwaiter().GetResult();
                UpsertLocalThreePointContestRounds(rounds);
                return rounds;
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Lettura round 3 punti online non riuscita, uso i dati locali: {exception.Message}", "3 Point Contest online", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        return _db.ThreePointContestRounds.OrderBy(x => x.EntryId).ThenBy(x => x.RoundNumber).ToList();
    }

    private List<ForfeitResult> LoadForfeitResults()
    {
        if (_onlineEntities is not null)
        {
            try
            {
                var forfeits = _onlineEntities.GetForfeitResultsAsync().GetAwaiter().GetResult();
                UpsertLocalForfeitResults(forfeits);
                return forfeits;
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Lettura tavolino online non riuscita, uso i dati locali: {exception.Message}", "Tavolino online", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        return _db.ForfeitResults.OrderBy(x => x.MatchId).ToList();
    }

    private List<Standing> LoadStandings()
    {
        if (_onlineEntities is not null)
        {
            try
            {
                var standings = _onlineEntities.GetStandingsAsync().GetAwaiter().GetResult();
                UpsertLocalStandings(standings);
                return standings;
            }
            catch (Exception exception)
            {
                MessageBox.Show($"Lettura classifica online non riuscita, uso i dati locali: {exception.Message}", "Classifica online", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        return _db.Standings.OrderBy(x => x.GroupId).ThenBy(x => x.Position).ToList();
    }

    private void UpsertLocalCompetitionEvents(IEnumerable<CompetitionEvent> events)
    {
        ReconcileLocalRows(events, _db.CompetitionEvents, x => x.Id, UpsertLocalCompetitionEvent);
    }

    private void UpsertLocalThreePointContestEntries(IEnumerable<ThreePointContestEntry> entries)
    {
        ReconcileLocalRows(entries, _db.ThreePointContestEntries, x => x.Id, UpsertLocalThreePointContestEntry);
    }

    private void UpsertLocalThreePointContestRounds(IEnumerable<ThreePointContestRound> rounds)
    {
        ReconcileLocalRows(rounds, _db.ThreePointContestRounds, x => x.Id, UpsertLocalThreePointContestRound);
    }

    private void UpsertLocalForfeitResults(IEnumerable<ForfeitResult> forfeits)
    {
        ReconcileLocalRows(forfeits, _db.ForfeitResults, x => x.Id, UpsertLocalForfeitResult);
    }

    private void UpsertLocalStandings(IEnumerable<Standing> standings)
    {
        ReconcileLocalRows(standings, _db.Standings, x => x.Id, UpsertLocalStanding);
    }

    private void ReplaceLocalStandings(IReadOnlyCollection<Standing> standings) =>
        ReconcileLocalRows(standings, _db.Standings, x => x.Id, UpsertLocalStanding);

    private void ReconcileLocalRows<T>(
        IEnumerable<T> onlineRows,
        DbSet<T> localSet,
        Func<T, int> getId,
        Action<T> upsert)
        where T : class
    {
        var rows = onlineRows.ToList();
        var onlineIds = rows.Select(getId).ToHashSet();
        var removedRows = localSet.AsEnumerable().Where(x => !onlineIds.Contains(getId(x))).ToList();
        localSet.RemoveRange(removedRows);

        foreach (var row in rows)
        {
            upsert(row);
        }

        _db.SaveChanges();
    }

    private void UpsertLocalCompetitionEvent(CompetitionEvent evt)
    {
        var local = _db.CompetitionEvents.FirstOrDefault(x => x.Id == evt.Id);
        if (local is null)
        {
            _db.CompetitionEvents.Add(CloneCompetitionEvent(evt));
            return;
        }

        CopyCompetitionEvent(evt, local);
    }

    private void UpsertLocalThreePointContestEntry(ThreePointContestEntry entry)
    {
        var local = _db.ThreePointContestEntries.FirstOrDefault(x => x.Id == entry.Id);
        if (local is null)
        {
            _db.ThreePointContestEntries.Add(CloneThreePointContestEntry(entry));
            return;
        }

        CopyThreePointContestEntry(entry, local);
    }

    private void UpsertLocalThreePointContestRound(ThreePointContestRound round)
    {
        var local = _db.ThreePointContestRounds.FirstOrDefault(x => x.Id == round.Id);
        if (local is null)
        {
            _db.ThreePointContestRounds.Add(CloneThreePointContestRound(round));
            return;
        }

        CopyThreePointContestRound(round, local);
    }

    private void UpsertLocalForfeitResult(ForfeitResult forfeit)
    {
        var local = _db.ForfeitResults.FirstOrDefault(x => x.Id == forfeit.Id);
        if (local is null)
        {
            _db.ForfeitResults.Add(CloneForfeitResult(forfeit));
            return;
        }

        CopyForfeitResult(forfeit, local);
    }

    private void UpsertLocalStanding(Standing standing)
    {
        var local = _db.Standings.FirstOrDefault(x => x.Id == standing.Id);
        if (local is null)
        {
            _db.Standings.Add(CloneStanding(standing));
            return;
        }

        CopyStanding(standing, local);
    }

    private static CompetitionEvent CloneCompetitionEvent(CompetitionEvent source)
    {
        var target = new CompetitionEvent();
        CopyCompetitionEvent(source, target);
        return target;
    }

    private static void CopyCompetitionEvent(CompetitionEvent source, CompetitionEvent target)
    {
        target.Id = source.Id;
        target.EditionId = source.EditionId;
        target.EventType = source.EventType;
        target.Name = source.Name;
        target.ScheduledStartAt = source.ScheduledStartAt;
        target.ScheduledEndAt = source.ScheduledEndAt;
        target.Status = source.Status;
    }

    private static ThreePointContestEntry CloneThreePointContestEntry(ThreePointContestEntry source)
    {
        var target = new ThreePointContestEntry();
        CopyThreePointContestEntry(source, target);
        return target;
    }

    private static void CopyThreePointContestEntry(ThreePointContestEntry source, ThreePointContestEntry target)
    {
        target.Id = source.Id;
        target.CompetitionEventId = source.CompetitionEventId;
        target.TeamId = source.TeamId;
        target.PlayerId = source.PlayerId;
        target.SeedOrder = source.SeedOrder;
        target.TotalScore = source.TotalScore;
        target.FinalPosition = source.FinalPosition;
    }

    private static ThreePointContestRound CloneThreePointContestRound(ThreePointContestRound source)
    {
        var target = new ThreePointContestRound();
        CopyThreePointContestRound(source, target);
        return target;
    }

    private static void CopyThreePointContestRound(ThreePointContestRound source, ThreePointContestRound target)
    {
        target.Id = source.Id;
        target.EntryId = source.EntryId;
        target.RoundNumber = source.RoundNumber;
        target.RoundType = source.RoundType;
        target.Station1Score = source.Station1Score;
        target.Station2Score = source.Station2Score;
        target.Station3Score = source.Station3Score;
        target.Station4Score = source.Station4Score;
        target.Station5Score = source.Station5Score;
        target.TotalScore = source.TotalScore;
        target.Notes = source.Notes;
    }

    private static ForfeitResult CloneForfeitResult(ForfeitResult source)
    {
        var target = new ForfeitResult();
        CopyForfeitResult(source, target);
        return target;
    }

    private static void CopyForfeitResult(ForfeitResult source, ForfeitResult target)
    {
        target.Id = source.Id;
        target.MatchId = source.MatchId;
        target.WinningTeamId = source.WinningTeamId;
        target.LosingTeamId = source.LosingTeamId;
        target.HomeAssignedScore = source.HomeAssignedScore;
        target.AwayAssignedScore = source.AwayAssignedScore;
        target.Reason = source.Reason;
        target.Notes = source.Notes;
        target.CreatedAt = source.CreatedAt;
    }

    private static Standing CloneStanding(Standing source)
    {
        var target = new Standing();
        CopyStanding(source, target);
        return target;
    }

    private static void CopyStanding(Standing source, Standing target)
    {
        target.Id = source.Id;
        target.GroupId = source.GroupId;
        target.TeamId = source.TeamId;
        target.Played = source.Played;
        target.Wins = source.Wins;
        target.Losses = source.Losses;
        target.PointsFor = source.PointsFor;
        target.PointsAgainst = source.PointsAgainst;
        target.PointDifference = source.PointDifference;
        target.RankingPoints = source.RankingPoints;
        target.Position = source.Position;
        target.TieBreakNote = source.TieBreakNote;
    }

    private ObservableCollection<Tournament> LoadTournaments()
    {
        if (_onlineEntities is not null)
        {
            try
            {
                var tournaments = _onlineEntities.GetTournamentsAsync().GetAwaiter().GetResult();
                UpsertLocalTournaments(tournaments);
                return new ObservableCollection<Tournament>(tournaments);
            }
            catch
            {
                // If the online API is temporarily unavailable, keep the app usable with local data.
            }
        }

        return new ObservableCollection<Tournament>(
            _db.Tournaments
                .OrderBy(tournament => tournament.Name)
                .ToList());
    }

    private async void AddTournament_Click(object sender, RoutedEventArgs e)
    {
        var now = Now();
        var tournament = new Tournament
        {
            Name = "",
            CreatedAt = now,
            UpdatedAt = now
        };

        var form = new TournamentFormWindow(tournament, isNew: true) { Owner = this };
        if (form.ShowDialog() != true)
        {
            return;
        }

        if (_onlineEntities is not null)
        {
            try
            {
                var created = await _onlineEntities.CreateTournamentAsync(tournament);
                UpsertLocalTournament(created);
                _db.SaveChanges();
                LoadCrudData();
                TournamentsGrid.SelectedItem = _tournaments.FirstOrDefault(x => x.Id == created.Id);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Tornei online", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return;
        }

        _db.Tournaments.Add(tournament);
        if (SaveChanges())
        {
            LoadCrudData();
            TournamentsGrid.SelectedItem = _tournaments.FirstOrDefault(x => x.Id == tournament.Id);
        }
    }

    private async void EditTournament_Click(object sender, RoutedEventArgs e)
    {
        if (TournamentsGrid.SelectedItem is not Tournament tournament)
        {
            MessageBox.Show("Seleziona un torneo da modificare.", "Tornei", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var form = new TournamentFormWindow(tournament, isNew: false) { Owner = this };
        if (form.ShowDialog() == true)
        {
            tournament.UpdatedAt = Now();
            if (_onlineEntities is not null)
            {
                try
                {
                    var updated = await _onlineEntities.UpdateTournamentAsync(tournament);
                    UpsertLocalTournament(updated);
                    _db.SaveChanges();
                    LoadCrudData();
                    TournamentsGrid.SelectedItem = _tournaments.FirstOrDefault(x => x.Id == updated.Id);
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, "Tornei online", MessageBoxButton.OK, MessageBoxImage.Error);
                    LoadCrudData();
                }

                return;
            }

            if (SaveChanges())
            {
                LoadCrudData();
            }
        }
    }

    private async void DeleteTournament_Click(object sender, RoutedEventArgs e)
    {
        if (TournamentsGrid.SelectedItem is not Tournament tournament)
        {
            return;
        }

        if (_onlineEntities is null && HasBlockingLinks("torneo", GetTournamentLinks(tournament.Id)))
        {
            return;
        }

        var result = MessageBox.Show(
            $"Eliminare il torneo '{tournament.Name}'?",
            "Conferma eliminazione",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        if (_onlineEntities is not null)
        {
            try
            {
                await _onlineEntities.DeleteTournamentAsync(tournament.Id);
                var local = _db.Tournaments.FirstOrDefault(x => x.Id == tournament.Id);
                if (local is not null)
                {
                    _db.Tournaments.Remove(local);
                    _db.SaveChanges();
                }

                LoadCrudData();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Tornei online", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return;
        }

        _db.Tournaments.Remove(tournament);
        if (SaveChanges())
        {
            LoadCrudData();
        }
    }

    private async void AddEdition_Click(object sender, RoutedEventArgs e)
    {
        if (_tournaments.Count == 0)
        {
            MessageBox.Show("Crea prima almeno un torneo.", "Edizioni", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var now = Now();
        var edition = new Edition
        {
            TournamentId = _tournaments[0].Id,
            Name = "",
            Year = DateTime.Now.Year,
            Status = "Draft",
            CreatedAt = now,
            UpdatedAt = now
        };

        var form = new EditionFormWindow(edition, _tournaments.ToList(), isNew: true) { Owner = this };
        if (form.ShowDialog() != true)
        {
            return;
        }

        if (_onlineEntities is not null)
        {
            try
            {
                edition = await _onlineEntities.CreateEditionAsync(edition);
                UpsertLocalEdition(edition);
                _db.SaveChanges();
                LoadCrudData();
                EditionsGrid.SelectedItem = _editionRows.FirstOrDefault(x => x.Edition.Id == edition.Id);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Edizioni online", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return;
        }

        _db.Editions.Add(edition);
        if (SaveChanges())
        {
            LoadCrudData();
            EditionsGrid.SelectedItem = _editionRows.FirstOrDefault(x => x.Edition.Id == edition.Id);
        }
    }

    private async void EditEdition_Click(object sender, RoutedEventArgs e)
    {
        if (EditionsGrid.SelectedItem is not EditionRow row)
        {
            MessageBox.Show("Seleziona una edizione da modificare.", "Edizioni", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var form = new EditionFormWindow(row.Edition, _tournaments.ToList(), isNew: false) { Owner = this };
        if (form.ShowDialog() == true)
        {
            row.Edition.UpdatedAt = Now();
            if (_onlineEntities is not null)
            {
                try
                {
                    var updated = await _onlineEntities.UpdateEditionAsync(row.Edition);
                    UpsertLocalEdition(updated);
                    _db.SaveChanges();
                    LoadCrudData();
                    EditionsGrid.SelectedItem = _editionRows.FirstOrDefault(x => x.Edition.Id == updated.Id);
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, "Edizioni online", MessageBoxButton.OK, MessageBoxImage.Error);
                    LoadCrudData();
                }

                return;
            }

            if (SaveChanges())
            {
                LoadCrudData();
            }
        }
    }

    private async void DeleteEdition_Click(object sender, RoutedEventArgs e)
    {
        if (EditionsGrid.SelectedItem is not EditionRow row)
        {
            return;
        }

        if (HasBlockingLinks("edizione", GetEditionLinks(row.Edition.Id)))
        {
            return;
        }

        var result = MessageBox.Show(
            $"Eliminare l'edizione '{row.Edition.Name}'?",
            "Conferma eliminazione",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        if (_onlineEntities is not null)
        {
            try
            {
                await _onlineEntities.DeleteEditionAsync(row.Edition.Id);
                var local = _db.Editions.FirstOrDefault(x => x.Id == row.Edition.Id);
                if (local is not null)
                {
                    _db.Editions.Remove(local);
                    _db.SaveChanges();
                }

                LoadCrudData();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Edizioni online", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return;
        }

        _db.Editions.Remove(row.Edition);
        if (SaveChanges())
        {
            LoadCrudData();
        }
    }

    private async void AddCourt_Click(object sender, RoutedEventArgs e)
    {
        var editions = _editionRows.Select(row => row.Edition).ToList();
        if (editions.Count == 0)
        {
            MessageBox.Show("Crea prima almeno una edizione.", "Campi", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var court = new Court
        {
            EditionId = editions[0].Id
        };

        var form = new CourtFormWindow(court, editions, isNew: true) { Owner = this };
        if (form.ShowDialog() != true)
        {
            return;
        }

        if (_onlineEntities is not null)
        {
            try
            {
                court = await _onlineEntities.CreateCourtAsync(court);
                UpsertLocalCourt(court);
                _db.SaveChanges();
                LoadCrudData();
                CourtsGrid.SelectedItem = _courtRows.FirstOrDefault(x => x.Court.Id == court.Id);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Campi online", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return;
        }

        _db.Courts.Add(court);
        if (SaveChanges())
        {
            LoadCrudData();
            CourtsGrid.SelectedItem = _courtRows.FirstOrDefault(x => x.Court.Id == court.Id);
        }
    }

    private async void EditCourt_Click(object sender, RoutedEventArgs e)
    {
        if (CourtsGrid.SelectedItem is not CourtRow row)
        {
            MessageBox.Show("Seleziona un campo da modificare.", "Campi", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var form = new CourtFormWindow(row.Court, _editionRows.Select(x => x.Edition).ToList(), isNew: false) { Owner = this };
        if (form.ShowDialog() == true)
        {
            if (_onlineEntities is not null)
            {
                try
                {
                    var updated = await _onlineEntities.UpdateCourtAsync(row.Court);
                    UpsertLocalCourt(updated);
                    _db.SaveChanges();
                    LoadCrudData();
                    CourtsGrid.SelectedItem = _courtRows.FirstOrDefault(x => x.Court.Id == updated.Id);
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, "Campi online", MessageBoxButton.OK, MessageBoxImage.Error);
                    LoadCrudData();
                }

                return;
            }

            if (SaveChanges())
            {
                LoadCrudData();
            }
        }
    }

    private async void DeleteCourt_Click(object sender, RoutedEventArgs e)
    {
        if (CourtsGrid.SelectedItem is not CourtRow row)
        {
            return;
        }

        if (HasBlockingLinks("campo", GetCourtLinks(row.Court.Id)))
        {
            return;
        }

        var result = MessageBox.Show(
            $"Eliminare il campo '{row.Court.Name}'?",
            "Conferma eliminazione",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        if (_onlineEntities is not null)
        {
            try
            {
                await _onlineEntities.DeleteCourtAsync(row.Court.Id);
                var local = _db.Courts.FirstOrDefault(x => x.Id == row.Court.Id);
                if (local is not null)
                {
                    _db.Courts.Remove(local);
                    _db.SaveChanges();
                }

                LoadCrudData();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Campi online", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return;
        }

        _db.Courts.Remove(row.Court);
        if (SaveChanges())
        {
            LoadCrudData();
        }
    }

    private async void AddGroup_Click(object sender, RoutedEventArgs e)
    {
        var editions = _editionRows.Select(row => row.Edition).ToList();
        if (editions.Count == 0)
        {
            MessageBox.Show("Crea prima almeno una edizione.", "Gironi", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var group = new TournamentGroup
        {
            EditionId = editions[0].Id,
            SortOrder = _groupRows.Count + 1
        };

        var form = new GroupFormWindow(group, editions, isNew: true) { Owner = this };
        if (form.ShowDialog() != true)
        {
            return;
        }

        if (_onlineEntities is not null)
        {
            try
            {
                group = await _onlineEntities.CreateTournamentGroupAsync(group);
                UpsertLocalTournamentGroup(group);
                _db.SaveChanges();
                LoadCrudData();
                GroupsGrid.SelectedItem = _groupRows.FirstOrDefault(x => x.Group.Id == group.Id);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Gironi online", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return;
        }

        _db.TournamentGroups.Add(group);
        if (SaveChanges())
        {
            LoadCrudData();
            GroupsGrid.SelectedItem = _groupRows.FirstOrDefault(x => x.Group.Id == group.Id);
        }
    }

    private async void EditGroup_Click(object sender, RoutedEventArgs e)
    {
        if (GroupsGrid.SelectedItem is not GroupRow row)
        {
            MessageBox.Show("Seleziona un girone da modificare.", "Gironi", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var form = new GroupFormWindow(row.Group, _editionRows.Select(x => x.Edition).ToList(), isNew: false) { Owner = this };
        if (form.ShowDialog() == true)
        {
            if (_onlineEntities is not null)
            {
                try
                {
                    var updated = await _onlineEntities.UpdateTournamentGroupAsync(row.Group);
                    UpsertLocalTournamentGroup(updated);
                    _db.SaveChanges();
                    LoadCrudData();
                    GroupsGrid.SelectedItem = _groupRows.FirstOrDefault(x => x.Group.Id == updated.Id);
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, "Gironi online", MessageBoxButton.OK, MessageBoxImage.Error);
                    LoadCrudData();
                }

                return;
            }

            if (SaveChanges())
            {
                LoadCrudData();
            }
        }
    }

    private async void DeleteGroup_Click(object sender, RoutedEventArgs e)
    {
        if (GroupsGrid.SelectedItem is not GroupRow row)
        {
            return;
        }

        if (HasBlockingLinks("girone", GetGroupLinks(row.Group.Id)))
        {
            return;
        }

        var result = MessageBox.Show(
            $"Eliminare il girone '{row.Group.Name}'?",
            "Conferma eliminazione",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        if (_onlineEntities is not null)
        {
            try
            {
                await _onlineEntities.DeleteTournamentGroupAsync(row.Group.Id);
                var local = _db.TournamentGroups.FirstOrDefault(x => x.Id == row.Group.Id);
                if (local is not null)
                {
                    _db.TournamentGroups.Remove(local);
                    _db.SaveChanges();
                }

                LoadCrudData();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Gironi online", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return;
        }

        _db.TournamentGroups.Remove(row.Group);
        if (SaveChanges())
        {
            LoadCrudData();
        }
    }

    private async void AddGroupTeam_Click(object sender, RoutedEventArgs e)
    {
        var groupOptions = BuildGroupOptions();
        var teams = _db.Teams.OrderBy(team => team.Name).ToList();

        if (groupOptions.Count == 0 || teams.Count == 0)
        {
            MessageBox.Show("Servono almeno un girone e una squadra.", "Squadre gironi", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var groupTeam = new GroupTeam
        {
            GroupId = groupOptions[0].Id,
            TeamId = teams[0].Id
        };

        var form = new GroupTeamFormWindow(groupTeam, groupOptions, teams, isNew: true) { Owner = this };
        if (form.ShowDialog() != true)
        {
            return;
        }

        if (GroupTeamAlreadyExists(groupTeam))
        {
            MessageBox.Show("Questa squadra e gia assegnata al girone selezionato.", "Dati non validi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_onlineEntities is not null)
        {
            try
            {
                groupTeam = await _onlineEntities.CreateGroupTeamAsync(groupTeam);
                UpsertLocalGroupTeam(groupTeam);
                _db.SaveChanges();
                LoadCrudData();
                GroupTeamsGrid.SelectedItem = _groupTeamRows.FirstOrDefault(x => x.GroupTeam.Id == groupTeam.Id);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Squadre gironi online", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return;
        }

        _db.GroupTeams.Add(groupTeam);
        if (SaveChanges())
        {
            LoadCrudData();
            GroupTeamsGrid.SelectedItem = _groupTeamRows.FirstOrDefault(x => x.GroupTeam.Id == groupTeam.Id);
        }
    }

    private async void EditGroupTeam_Click(object sender, RoutedEventArgs e)
    {
        if (GroupTeamsGrid.SelectedItem is not GroupTeamRow row)
        {
            MessageBox.Show("Seleziona una assegnazione da modificare.", "Squadre gironi", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var form = new GroupTeamFormWindow(row.GroupTeam, BuildGroupOptions(), _db.Teams.OrderBy(team => team.Name).ToList(), isNew: false) { Owner = this };
        if (form.ShowDialog() != true)
        {
            return;
        }

        if (GroupTeamAlreadyExists(row.GroupTeam))
        {
            MessageBox.Show("Questa squadra e gia assegnata al girone selezionato.", "Dati non validi", MessageBoxButton.OK, MessageBoxImage.Warning);
            LoadCrudData();
            return;
        }

        if (_onlineEntities is not null)
        {
            try
            {
                var updated = await _onlineEntities.UpdateGroupTeamAsync(row.GroupTeam);
                UpsertLocalGroupTeam(updated);
                _db.SaveChanges();
                LoadCrudData();
                GroupTeamsGrid.SelectedItem = _groupTeamRows.FirstOrDefault(x => x.GroupTeam.Id == updated.Id);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Squadre gironi online", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadCrudData();
            }

            return;
        }

        if (SaveChanges())
        {
            LoadCrudData();
        }
    }

    private async void DeleteGroupTeam_Click(object sender, RoutedEventArgs e)
    {
        if (GroupTeamsGrid.SelectedItem is not GroupTeamRow row)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Rimuovere '{row.TeamName}' da '{row.GroupName}'?",
            "Conferma eliminazione",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        if (_onlineEntities is not null)
        {
            try
            {
                await _onlineEntities.DeleteGroupTeamAsync(row.GroupTeam.Id);
                var local = _db.GroupTeams.FirstOrDefault(x => x.Id == row.GroupTeam.Id);
                if (local is not null)
                {
                    _db.GroupTeams.Remove(local);
                    _db.SaveChanges();
                }

                LoadCrudData();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Squadre gironi online", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return;
        }

        _db.GroupTeams.Remove(row.GroupTeam);
        if (SaveChanges())
        {
            LoadCrudData();
        }
    }

    private async void AddMatch_Click(object sender, RoutedEventArgs e)
    {
        var editions = _editionRows.Select(row => row.Edition).ToList();
        if (editions.Count == 0 || _db.Teams.Count() < 2)
        {
            MessageBox.Show("Servono almeno una edizione e due squadre.", "Partite", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var now = Now();
        var match = new Match
        {
            EditionId = _currentEditionId ?? editions[0].Id,
            Phase = "GroupStage",
            Status = "Scheduled",
            PeriodDurationMs = 720000,
            ShotClockMs = 24000,
            MaxScore = 45,
            MaxScoreEnabled = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        var form = new MatchFormWindow(
            match,
            null,
            null,
            editions,
            _db.TournamentGroups.ToList(),
            _db.Courts.ToList(),
            _db.Teams.ToList(),
            isNew: true)
        { Owner = this };

        if (form.ShowDialog() != true)
        {
            return;
        }

        match.UpdatedAt = Now();
        if (_onlineEntities is not null)
        {
            try
            {
                var bundle = await _onlineEntities.SaveMatchBundleAsync(match, form.HomeMatchTeam, form.AwayMatchTeam);
                match = bundle.Match;
                _db.ChangeTracker.Clear();
                LoadCrudData();
                MatchesGrid.SelectedItem = _matchRows.FirstOrDefault(x => x.Match.Id == match.Id);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Partite online", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return;
        }

        _db.Matches.Add(match);
        if (!SaveChanges())
        {
            return;
        }

        form.HomeMatchTeam.MatchId = match.Id;
        form.AwayMatchTeam.MatchId = match.Id;
        _db.MatchTeams.Add(form.HomeMatchTeam);
        _db.MatchTeams.Add(form.AwayMatchTeam);
        if (SaveChanges())
        {
            LoadCrudData();
            MatchesGrid.SelectedItem = _matchRows.FirstOrDefault(x => x.Match.Id == match.Id);
        }
    }

    private async void EditMatch_Click(object sender, RoutedEventArgs e)
    {
        if (MatchesGrid.SelectedItem is not MatchRow row)
        {
            MessageBox.Show("Seleziona una partita da modificare.", "Partite", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var teams = _db.MatchTeams.Where(x => x.MatchId == row.Match.Id).ToList();
        var home = teams.FirstOrDefault(x => x.Side == "Home");
        var away = teams.FirstOrDefault(x => x.Side == "Away");

        var form = new MatchFormWindow(
            row.Match,
            home,
            away,
            _editionRows.Select(x => x.Edition).ToList(),
            _db.TournamentGroups.ToList(),
            _db.Courts.ToList(),
            _db.Teams.ToList(),
            isNew: false)
        { Owner = this };

        if (form.ShowDialog() != true)
        {
            return;
        }

        row.Match.UpdatedAt = Now();
        if (_onlineEntities is not null)
        {
            try
            {
                await _onlineEntities.SaveMatchBundleAsync(row.Match, form.HomeMatchTeam, form.AwayMatchTeam);
                _db.ChangeTracker.Clear();
                LoadCrudData();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Partite online", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadCrudData();
            }

            return;
        }

        if (home is null)
        {
            form.HomeMatchTeam.MatchId = row.Match.Id;
            _db.MatchTeams.Add(form.HomeMatchTeam);
        }

        if (away is null)
        {
            form.AwayMatchTeam.MatchId = row.Match.Id;
            _db.MatchTeams.Add(form.AwayMatchTeam);
        }

        if (SaveChanges())
        {
            LoadCrudData();
        }
    }

    private async void DeleteMatch_Click(object sender, RoutedEventArgs e)
    {
        if (MatchesGrid.SelectedItem is not MatchRow row)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Eliminare la partita '{row.HomeTeamName} vs {row.AwayTeamName}'?",
            "Conferma eliminazione",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        if (_onlineEntities is not null)
        {
            try
            {
                await _onlineEntities.DeleteMatchBundleAsync(row.Match.Id);
                if (_currentLiveMatchId == row.Match.Id)
                {
                    _onlineScoreboardStateIds.Remove(row.Match.Id);
                    LiveSessionStore.Clear();
                    EnterFreeLiveMode();
                }

                _db.ChangeTracker.Clear();
                LoadCrudData();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Partite online", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return;
        }

        foreach (var side in _db.MatchTeams.Where(x => x.MatchId == row.Match.Id).ToList())
        {
            _db.MatchTeams.Remove(side);
        }

        _db.Matches.Remove(row.Match);
        if (SaveChanges())
        {
            LoadCrudData();
        }
    }

    private bool SaveRosterChanges()
    {
        RosterGrid.CommitEdit();
        RosterGrid.CommitEdit(System.Windows.Controls.DataGridEditingUnit.Row, true);

        foreach (var row in _rosterRows)
        {
            row.ApplyToEntity();
            row.Roster.UpdatedAt = Now();
            if (_onlineEntities is not null)
            {
                try
                {
                    var updated = _onlineEntities.UpdateTeamRosterAsync(row.Roster).GetAwaiter().GetResult();
                    CopyTeamRoster(updated, row.Roster);
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, "Roster online", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
        }

        if (_onlineEntities is not null)
        {
            _db.SaveChanges();
            return true;
        }

        return SaveChanges();
    }

    private bool SaveChanges()
    {
        try
        {
            _db.SaveChanges();
            if (_currentLiveMatchId is not null && !_isFreeLiveMode)
            {
                LiveSessionStore.Save(_db, _currentLiveMatchId.Value);
                _hasPendingLiveSync = true;
                _liveChangeVersion++;
                SetLiveSyncStatus("Salvato localmente", "#fef3c7", "#92400e");
            }
            TrySyncCurrentLiveData();
            return true;
        }
        catch (DbUpdateException ex)
        {
            MessageBox.Show(
                $"Salvataggio non riuscito: {ex.GetBaseException().Message}",
                "Errore database",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
        }
    }

    private void CheckpointOrRetryLiveSync()
    {
        if (!_isFreeLiveMode && _currentLiveMatchId is not null && _currentLiveMatchStatus == "Live" &&
            (_gameClock.IsRunning || _shotClock.IsRunning))
        {
            PersistLiveState();
            return;
        }

        TrySyncCurrentLiveData();
    }

    private async void TrySyncCurrentLiveData()
    {
        if (_onlineEntities is null || _currentLiveMatchId is null || _isFreeLiveMode ||
            _isSyncingLiveData || !_hasPendingLiveSync || DateTime.UtcNow < _nextLiveSyncAttemptAt)
        {
            return;
        }

        _isSyncingLiveData = true;
        SetLiveSyncStatus("Sincronizzazione...", "#dbeafe", "#1d4ed8");
        var syncVersion = _liveChangeVersion;
        var changedPlayerIds = _changedMatchPlayerIds.ToHashSet();
        try
        {
            var matchId = _currentLiveMatchId.Value;
            var match = _db.Matches.FirstOrDefault(x => x.Id == matchId);
            var state = _db.ScoreboardStates.FirstOrDefault(x => x.MatchId == matchId);
            if (match is null || state is null)
            {
                _hasPendingLiveSync = false;
                return;
            }

            var sides = _db.MatchTeams.Where(x => x.MatchId == matchId).ToList();
            var changedPlayers = _db.MatchPlayers
                .Where(x => changedPlayerIds.Contains(x.Id))
                .ToList();

            var pendingEvents = _db.MatchEvents
                .Where(x => x.MatchId == matchId && x.SyncedAt == null)
                .OrderBy(x => x.Id)
                .ToList();

            await _onlineEntities.SyncLiveAsync(match, sides, changedPlayers, state, pendingEvents);

            if (pendingEvents.Count > 0)
            {
                var syncedAt = Now();
                foreach (var matchEvent in pendingEvents)
                {
                    matchEvent.SyncedAt = syncedAt;
                }
                _db.SaveChanges();
            }

            if (_liveChangeVersion == syncVersion)
            {
                foreach (var playerId in changedPlayerIds)
                {
                    _changedMatchPlayerIds.Remove(playerId);
                }

                _hasPendingLiveSync = false;
                if (match.Status is "Scheduled" or "Finished" or "Cancelled")
                {
                    LiveSessionStore.Clear();
                }
            }

            _nextLiveSyncAttemptAt = DateTime.MinValue;
            _liveSyncWarningShown = false;
            SetLiveSyncStatus(
                _hasPendingLiveSync ? "Nuove modifiche in attesa" : "Sincronizzato",
                _hasPendingLiveSync ? "#fef3c7" : "#dcfce7",
                _hasPendingLiveSync ? "#92400e" : "#166534");
        }
        catch (Exception ex)
        {
            if (ex is OnlineEntityClient.OnlineApiException { StatusCode: 429 })
            {
                _nextLiveSyncAttemptAt = DateTime.UtcNow.AddMinutes(1);
            }

            if (!_liveSyncWarningShown)
            {
                var message = ex is OnlineEntityClient.OnlineApiException { StatusCode: 429 }
                    ? "Aruba ha temporaneamente limitato le richieste. I dati restano salvati in locale e la sincronizzazione verrà ritentata automaticamente tra un minuto."
                    : $"Dati salvati localmente. Sincronizzazione online non riuscita: {ex.Message}";
                MessageBox.Show(
                    message,
                    "Sync live",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                _liveSyncWarningShown = true;
            }

            SetLiveSyncStatus(
                ex is OnlineEntityClient.OnlineApiException { StatusCode: 429 } ? "Server occupato: nuovo tentativo automatico" : "Offline: modifiche da sincronizzare",
                "#fee2e2",
                "#991b1b");
        }
        finally
        {
            _isSyncingLiveData = false;
            RenderLocalState();
            if (_hasPendingLiveSync && DateTime.UtcNow >= _nextLiveSyncAttemptAt)
            {
                TrySyncCurrentLiveData();
            }
        }
    }

    private void SetLiveSyncStatus(string text, string background, string foreground)
    {
        if (LiveSyncStatusText is null || LiveSyncStatusBorder is null)
        {
            return;
        }

        LiveSyncStatusText.Text = $"Sync: {text}";
        LiveSyncStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(foreground));
        LiveSyncStatusBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(background));
    }

    private async Task UpsertOnlineScoreboardStateAsync(ScoreboardStateRecord state)
    {
        if (!_onlineScoreboardStateIds.TryGetValue(state.MatchId, out var onlineId))
        {
            var onlineStates = await _onlineEntities!.GetScoreboardStatesAsync();
            onlineId = onlineStates.FirstOrDefault(x => x.MatchId == state.MatchId)?.Id ?? 0;
        }

        if (onlineId == 0)
        {
            var created = await _onlineEntities!.CreateScoreboardStateAsync(state);
            _onlineScoreboardStateIds[state.MatchId] = created.Id;
            return;
        }

        try
        {
            var updated = await _onlineEntities!.UpdateScoreboardStateAsync(onlineId, state);
            _onlineScoreboardStateIds[state.MatchId] = updated.Id;
        }
        catch (OnlineEntityClient.OnlineApiException exception) when (exception.StatusCode == 404)
        {
            _onlineScoreboardStateIds.Remove(state.MatchId);
            var created = await _onlineEntities!.CreateScoreboardStateAsync(state);
            _onlineScoreboardStateIds[state.MatchId] = created.Id;
        }
    }

    private static bool IsSameLiveEvent(MatchEvent left, MatchEvent right) =>
        left.MatchId == right.MatchId &&
        left.Period == right.Period &&
        left.GameClockMsRemaining == right.GameClockMsRemaining &&
        left.TeamId == right.TeamId &&
        left.PlayerId == right.PlayerId &&
        left.EventType == right.EventType &&
        left.Points == right.Points &&
        left.IsCorrection == right.IsCorrection &&
        string.Equals(left.CreatedAt, right.CreatedAt, StringComparison.OrdinalIgnoreCase);

    private bool HasBlockingLinks(string entityName, IReadOnlyList<LinkCount> links)
    {
        var activeLinks = links.Where(link => link.Count > 0).ToList();
        if (activeLinks.Count == 0)
        {
            return false;
        }

        var details = string.Join(Environment.NewLine, activeLinks.Select(link => $"- {link.Label}: {link.Count}"));
        MessageBox.Show(
            $"Non puoi eliminare questo {entityName} perche esistono dati collegati:{Environment.NewLine}{Environment.NewLine}{details}",
            "Eliminazione bloccata",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        return true;
    }

    private IReadOnlyList<LinkCount> GetTournamentLinks(int tournamentId)
    {
        return
        [
            new("Edizioni", CountLinks("editions", "tournament_id", tournamentId))
        ];
    }

    private IReadOnlyList<LinkCount> GetEditionLinks(int editionId)
    {
        return
        [
            new("Campi", CountLinks("courts", "edition_id", editionId)),
            new("Squadre", CountLinks("teams", "edition_id", editionId)),
            new("Gironi", CountLinks("tournament_groups", "edition_id", editionId)),
            new("Partite", CountLinks("matches", "edition_id", editionId)),
            new("Eventi", CountLinks("competition_events", "edition_id", editionId)),
            new("Sync queue", CountLinks("sync_queue", "edition_id", editionId))
        ];
    }

    private IReadOnlyList<LinkCount> GetTeamLinks(int teamId)
    {
        return
        [
            new("Roster", CountLinks("team_rosters", "team_id", teamId)),
            new("Gironi", CountLinks("group_teams", "team_id", teamId)),
            new("Classifiche", CountLinks("standings", "team_id", teamId)),
            new("Squadre partita", CountLinks("match_teams", "team_id", teamId)),
            new("Giocatori partita", CountLinks("match_players", "team_id", teamId)),
            new("Possesso tabellone", CountLinks("scoreboard_states", "possession_team_id", teamId)),
            new("Eventi partita", CountLinks("match_events", "team_id", teamId)),
            new("Vittorie a tavolino", CountLinks("forfeit_results", "winning_team_id", teamId)),
            new("Sconfitte a tavolino", CountLinks("forfeit_results", "losing_team_id", teamId)),
            new("3 Point Contest", CountLinks("three_point_contest_entries", "team_id", teamId))
        ];
    }

    private IReadOnlyList<LinkCount> GetCourtLinks(int courtId)
    {
        return
        [
            new("Partite", CountLinks("matches", "court_id", courtId))
        ];
    }

    private IReadOnlyList<LinkCount> GetGroupLinks(int groupId)
    {
        return
        [
            new("Squadre girone", CountLinks("group_teams", "group_id", groupId)),
            new("Classifiche", CountLinks("standings", "group_id", groupId)),
            new("Partite", CountLinks("matches", "group_id", groupId))
        ];
    }

    private List<GroupTeamFormWindow.GroupOption> BuildGroupOptions()
    {
        var editionsById = _editionRows.ToDictionary(row => row.Edition.Id, row => row.Edition.Name);
        return _db.TournamentGroups
            .OrderBy(group => group.SortOrder)
            .ThenBy(group => group.Code)
            .ToList()
            .Select(group =>
            {
                var editionName = editionsById.TryGetValue(group.EditionId, out var name) ? name : $"Edizione #{group.EditionId}";
                return new GroupTeamFormWindow.GroupOption(group.Id, $"{editionName} - {group.Name}");
            })
            .ToList();
    }

    private bool GroupTeamAlreadyExists(GroupTeam candidate)
    {
        return _db.GroupTeams.Any(groupTeam =>
            groupTeam.Id != candidate.Id &&
            groupTeam.GroupId == candidate.GroupId &&
            groupTeam.TeamId == candidate.TeamId);
    }

    private bool ThreePointEntryAlreadyExists(ThreePointContestEntry candidate)
    {
        return _db.ThreePointContestEntries.Any(entry =>
            entry.Id != candidate.Id &&
            entry.CompetitionEventId == candidate.CompetitionEventId &&
            (entry.TeamId == candidate.TeamId || entry.PlayerId == candidate.PlayerId));
    }

    private bool ThreePointRoundAlreadyExists(ThreePointContestRound candidate)
    {
        return _db.ThreePointContestRounds.Any(round =>
            round.Id != candidate.Id &&
            round.EntryId == candidate.EntryId &&
            round.RoundNumber == candidate.RoundNumber &&
            round.RoundType == candidate.RoundType);
    }

    private List<ThreePointRoundFormWindow.EntryOption> BuildThreePointEntryOptions()
    {
        return _threePointEntryRows
            .Select(row => new ThreePointRoundFormWindow.EntryOption(row.Entry.Id, $"{row.EventName} - {row.TeamName} - {row.PlayerName}"))
            .ToList();
    }

    private List<ForfeitFormWindow.MatchOption> BuildForfeitMatchOptions()
    {
        return _matchRows
            .Select(row => new ForfeitFormWindow.MatchOption(row.Match.Id, $"{row.Match.ScheduledStartAt ?? "Senza data"} - {row.HomeTeamName} vs {row.AwayTeamName}"))
            .ToList();
    }

    private void UpdateThreePointEntryTotal(int entryId)
    {
        var entry = _db.ThreePointContestEntries.FirstOrDefault(x => x.Id == entryId);
        if (entry is null) return;
        entry.TotalScore = _db.ThreePointContestRounds
            .Where(x => x.EntryId == entryId)
            .ToList()
            .Sum(x => x.TotalScore);
    }

    private void ApplyForfeitToMatch(ForfeitResult forfeit)
    {
        var match = _db.Matches.FirstOrDefault(x => x.Id == forfeit.MatchId);
        if (match is null) return;

        match.WinnerTeamId = forfeit.WinningTeamId;
        match.WinReason = "Forfeit";
        match.Status = "Finished";
        match.UpdatedAt = Now();

        var home = _db.MatchTeams.FirstOrDefault(x => x.MatchId == match.Id && x.Side == "Home");
        var away = _db.MatchTeams.FirstOrDefault(x => x.MatchId == match.Id && x.Side == "Away");
        if (home is not null)
        {
            home.Score = forfeit.HomeAssignedScore;
            home.IsWinner = home.TeamId == forfeit.WinningTeamId;
        }
        if (away is not null)
        {
            away.Score = forfeit.AwayAssignedScore;
            away.IsWinner = away.TeamId == forfeit.WinningTeamId;
        }
    }

    private async Task PushThreePointEntryTotalOnlineAsync(params int[] entryIds)
    {
        foreach (var entryId in entryIds.Distinct())
        {
            UpdateThreePointEntryTotal(entryId);
            var entry = _db.ThreePointContestEntries.FirstOrDefault(x => x.Id == entryId);
            if (entry is null || _onlineEntities is null)
            {
                continue;
            }

            var updated = await _onlineEntities.UpdateThreePointContestEntryAsync(entry);
            CopyThreePointContestEntry(updated, entry);
        }
    }

    private void RecalculateStandings()
    {
        _db.Standings.RemoveRange(_db.Standings);
        SaveChanges();

        var groups = _db.TournamentGroups.ToList();
        foreach (var group in groups)
        {
            var teamIds = _db.GroupTeams.Where(x => x.GroupId == group.Id).Select(x => x.TeamId).ToHashSet();
            var table = teamIds.ToDictionary(teamId => teamId, teamId => new Standing { GroupId = group.Id, TeamId = teamId });
            var matches = _db.Matches.Where(x => x.GroupId == group.Id && x.Status == "Finished").ToList();

            foreach (var match in matches)
            {
                var sides = _db.MatchTeams.Where(x => x.MatchId == match.Id).ToList();
                var home = sides.FirstOrDefault(x => x.Side == "Home");
                var away = sides.FirstOrDefault(x => x.Side == "Away");
                if (home is null || away is null || !table.ContainsKey(home.TeamId) || !table.ContainsKey(away.TeamId)) continue;

                ApplyStandingGame(table[home.TeamId], table[away.TeamId], home.Score, away.Score);
            }

            var ordered = table.Values
                .OrderByDescending(x => x.RankingPoints)
                .ThenByDescending(x => x.PointDifference)
                .ThenByDescending(x => x.PointsFor)
                .ToList();

            for (var i = 0; i < ordered.Count; i++)
            {
                ordered[i].Position = i + 1;
                _db.Standings.Add(ordered[i]);
            }
        }
    }

    private static void ApplyStandingGame(Standing home, Standing away, int homeScore, int awayScore)
    {
        home.Played++;
        away.Played++;
        home.PointsFor += homeScore;
        home.PointsAgainst += awayScore;
        away.PointsFor += awayScore;
        away.PointsAgainst += homeScore;
        home.PointDifference = home.PointsFor - home.PointsAgainst;
        away.PointDifference = away.PointsFor - away.PointsAgainst;

        if (homeScore > awayScore)
        {
            home.Wins++; away.Losses++; home.RankingPoints += 2;
        }
        else if (awayScore > homeScore)
        {
            away.Wins++; home.Losses++; away.RankingPoints += 2;
        }
    }

    private IReadOnlyList<LinkCount> GetPlayerLinks(int playerId)
    {
        return
        [
            new("Roster", CountLinks("team_rosters", "player_id", playerId)),
            new("Giocatori partita", CountLinks("match_players", "player_id", playerId)),
            new("Eventi partita", CountLinks("match_events", "player_id", playerId)),
            new("3 Point Contest", CountLinks("three_point_contest_entries", "player_id", playerId))
        ];
    }

    private static long CountLinks(string tableName, string columnName, int id)
    {
        using var connection = new SqliteConnection($"Data Source={AppPaths.DatabasePath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {tableName} WHERE {columnName} = $id;";
        command.Parameters.AddWithValue("$id", id);
        return Convert.ToInt64(command.ExecuteScalar());
    }

    private void TeamsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (TeamsGrid.SelectedItem is Team)
        {
            EditTeam_Click(sender, e);
        }
    }

    private void TournamentsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (TournamentsGrid.SelectedItem is Tournament)
        {
            EditTournament_Click(sender, e);
        }
    }

    private void EditionsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (EditionsGrid.SelectedItem is EditionRow)
        {
            EditEdition_Click(sender, e);
        }
    }

    private void CourtsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (CourtsGrid.SelectedItem is CourtRow)
        {
            EditCourt_Click(sender, e);
        }
    }

    private void GroupsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (GroupsGrid.SelectedItem is GroupRow)
        {
            EditGroup_Click(sender, e);
        }
    }

    private void GroupTeamsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (GroupTeamsGrid.SelectedItem is GroupTeamRow)
        {
            EditGroupTeam_Click(sender, e);
        }
    }

    private void MatchesGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (MatchesGrid.SelectedItem is MatchRow)
        {
            EditMatch_Click(sender, e);
        }
    }

    private void CompetitionEventsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (CompetitionEventsGrid.SelectedItem is CompetitionEventRow)
        {
            EditCompetitionEvent_Click(sender, e);
        }
    }

    private void ThreePointEntriesGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (ThreePointEntriesGrid.SelectedItem is ThreePointEntryRow)
        {
            EditThreePointEntry_Click(sender, e);
        }
    }

    private void ThreePointRoundsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (ThreePointRoundsGrid.SelectedItem is ThreePointRoundRow) EditThreePointRound_Click(sender, e);
    }

    private void ForfeitsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (ForfeitsGrid.SelectedItem is ForfeitRow) EditForfeit_Click(sender, e);
    }

    private void PlayersGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (PlayersGrid.SelectedItem is Player)
        {
            EditPlayer_Click(sender, e);
        }
    }

    private void ApplyTeamsToLivePreview()
    {
        var home = _teams.ElementAtOrDefault(0);
        var away = _teams.ElementAtOrDefault(1);

        if (home is not null)
        {
            _state.HomeName = string.IsNullOrWhiteSpace(home.ShortName) ? home.Name : home.ShortName;
            _state.HomeColor = string.IsNullOrWhiteSpace(home.PrimaryColor) ? "#f77f00" : home.PrimaryColor;
            HomeNameText.Text = _state.HomeName;
        }

        if (away is not null)
        {
            _state.AwayName = string.IsNullOrWhiteSpace(away.ShortName) ? away.Name : away.ShortName;
            _state.AwayColor = string.IsNullOrWhiteSpace(away.PrimaryColor) ? "#457b9d" : away.PrimaryColor;
            AwayNameText.Text = _state.AwayName;
        }

        _ = BroadcastAsync();
    }

    private void LoadLiveMatchOptions()
    {
        var statusesById = _db.Matches.ToDictionary(x => x.Id, x => x.Status);
        _liveMatchOptions = new ObservableCollection<MatchOption>(
            _matchRows
                .Where(row => !string.IsNullOrWhiteSpace(row.HomeTeamName) && !string.IsNullOrWhiteSpace(row.AwayTeamName))
                .Select(row => new MatchOption(
                    row.Match.Id,
                    $"{(statusesById.TryGetValue(row.Match.Id, out var status) ? status : row.Match.Status)} - {row.HomeTeamName} vs {row.AwayTeamName} ({row.Match.Phase})")));

        LiveMatchCombo.ItemsSource = _liveMatchOptions;
    }

    private void LoadSelectedLiveMatchOrDefault()
    {
        if (_isFreeLiveMode)
        {
            return;
        }

        var selected = _currentLiveMatchId is not null
            ? _liveMatchOptions.FirstOrDefault(x => x.Id == _currentLiveMatchId.Value)
            : null;

        selected ??= _liveMatchOptions.FirstOrDefault(x => x.DisplayName.StartsWith("Live", StringComparison.OrdinalIgnoreCase))
            ?? _liveMatchOptions.FirstOrDefault(x => x.DisplayName.StartsWith("Paused", StringComparison.OrdinalIgnoreCase))
            ?? _liveMatchOptions.FirstOrDefault(x => x.DisplayName.StartsWith("Ready", StringComparison.OrdinalIgnoreCase))
            ?? _liveMatchOptions.FirstOrDefault();

        LiveMatchCombo.SelectedItem = selected;
        if (selected is not null)
        {
            LoadLiveMatch(selected.Id);
        }
        else
        {
            ApplyTeamsToLivePreview();
        }
    }

    private void LoadLiveMatch(int matchId)
    {
        if (_isFreeLiveMode)
        {
            return;
        }

        var match = _db.Matches.FirstOrDefault(x => x.Id == matchId);
        if (match is null)
        {
            return;
        }

        var sides = _db.MatchTeams.Where(x => x.MatchId == matchId).ToList();
        var homeSide = sides.FirstOrDefault(x => x.Side == "Home");
        var awaySide = sides.FirstOrDefault(x => x.Side == "Away");
        if (homeSide is null || awaySide is null)
        {
            MessageBox.Show("La partita selezionata non ha ancora entrambe le squadre assegnate.", "Partita live", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var homeTeam = _db.Teams.FirstOrDefault(x => x.Id == homeSide.TeamId);
        var awayTeam = _db.Teams.FirstOrDefault(x => x.Id == awaySide.TeamId);
        if (homeTeam is null || awayTeam is null)
        {
            return;
        }

        _currentLiveMatchId = matchId;
        _currentHomeTeamId = homeSide.TeamId;
        _currentAwayTeamId = awaySide.TeamId;
        _currentLiveMatchStatus = match.Status;

        _gameClock.Pause();
        _shotClock.Pause();

        var scoreboardState = _db.ScoreboardStates.FirstOrDefault(x => x.MatchId == matchId);
        _state.HomeName = string.IsNullOrWhiteSpace(homeTeam.ShortName) ? homeTeam.Name : homeTeam.ShortName;
        _state.AwayName = string.IsNullOrWhiteSpace(awayTeam.ShortName) ? awayTeam.Name : awayTeam.ShortName;
        _state.HomeColor = string.IsNullOrWhiteSpace(homeTeam.PrimaryColor) ? "#f77f00" : homeTeam.PrimaryColor;
        _state.AwayColor = string.IsNullOrWhiteSpace(awayTeam.PrimaryColor) ? "#457b9d" : awayTeam.PrimaryColor;
        _state.HomeScore = scoreboardState?.HomeScore ?? homeSide.Score;
        _state.AwayScore = scoreboardState?.AwayScore ?? awaySide.Score;
        _state.HomeFouls = scoreboardState?.HomeFoulsCurrentPeriod ?? homeSide.FoulsCurrentPeriod;
        _state.AwayFouls = scoreboardState?.AwayFoulsCurrentPeriod ?? awaySide.FoulsCurrentPeriod;
        _state.HomeTimeouts = scoreboardState?.HomeTimeoutsUsedTotal ?? homeSide.TimeoutsUsedTotal;
        _state.AwayTimeouts = scoreboardState?.AwayTimeoutsUsedTotal ?? awaySide.TimeoutsUsedTotal;
        _state.Period = scoreboardState?.CurrentPeriod ?? 1;

        var gameClockMs = scoreboardState?.GameClockMsRemaining ?? match.PeriodDurationMs;
        var shotClockMs = scoreboardState?.ShotClockMsRemaining ?? match.ShotClockMs;
        _gameClock.Reset(gameClockMs);
        _shotClock.Reset(shotClockMs);

        HomeNameText.Text = _state.HomeName;
        AwayNameText.Text = _state.AwayName;

        LoadLiveScorers(matchId, homeSide.TeamId, awaySide.TeamId);
        EnsureScoreboardState(matchId, gameClockMs, shotClockMs);
        RenderLocalState();
        _ = BroadcastAsync();
    }

    private bool TryGetCurrentLiveMatch(out Match match)
    {
        match = null!;
        if (_currentLiveMatchId is null)
        {
            MessageBox.Show("Seleziona prima una partita.", "Partita live", MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        var found = _db.Matches.FirstOrDefault(x => x.Id == _currentLiveMatchId.Value);
        if (found is null)
        {
            MessageBox.Show("La partita selezionata non è più disponibile.", "Partita live", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        match = found;
        return true;
    }

    private void SetOfficialMatchStatus(Match match, string status, bool setActualStart = false, bool setActualEnd = false, bool save = true)
    {
        match.Status = status;
        match.UpdatedAt = Now();
        _currentLiveMatchStatus = status;

        if (setActualStart && string.IsNullOrWhiteSpace(match.ActualStartAt))
        {
            match.ActualStartAt = Now();
        }

        if (setActualEnd)
        {
            match.ActualEndAt = Now();
        }

        if (save)
        {
            SaveChanges();
        }

        RefreshLiveMatchOptionLabel(match.Id);
    }

    private bool EnsureLiveInteractionAllowed(bool showMessage = true)
    {
        if (_isFreeLiveMode)
        {
            return true;
        }

        if (_currentLiveMatchId is null)
        {
            if (showMessage)
            {
                MessageBox.Show("Seleziona e prepara una partita prima di usare la console live.", "Partita live", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            return false;
        }

        if (_currentLiveMatchStatus == "Live")
        {
            return true;
        }

        if (showMessage)
        {
            MessageBox.Show("I controlli live sono disponibili solo dopo aver premuto Inizia partita.", "Partita live", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        return false;
    }

    private void RefreshLiveMatchOptionLabel(int matchId)
    {
        var selected = LiveMatchCombo.SelectedItem;
        LoadLiveMatchOptions();
        LiveMatchCombo.SelectedItem = _liveMatchOptions.FirstOrDefault(x => x.Id == matchId);
        if (LiveMatchCombo.SelectedItem is null)
        {
            LiveMatchCombo.SelectedItem = selected;
        }
    }

    private void LoadLiveScorers(int matchId, int homeTeamId, int awayTeamId)
    {
        var playersById = _db.Players.ToDictionary(x => x.Id);
        var matchPlayers = _db.MatchPlayers
            .Where(x => x.MatchId == matchId)
            .OrderBy(x => x.JerseyNumber)
            .ThenBy(x => x.PlayerId)
            .ToList();

        static LiveScorerOption ToOption(MatchPlayer matchPlayer, Dictionary<int, Player> playersById)
        {
            playersById.TryGetValue(matchPlayer.PlayerId, out var player);
            var playerName = player is null
                ? $"Giocatore #{matchPlayer.PlayerId}"
                : $"{player.LastName} {player.FirstName}".Trim();
            var number = matchPlayer.JerseyNumber is null ? "" : $"#{matchPlayer.JerseyNumber} ";
            return new LiveScorerOption(
                matchPlayer.Id,
                matchPlayer.TeamId,
                matchPlayer.PlayerId,
                matchPlayer.JerseyNumber,
                playerName,
                $"{number}{playerName}",
                matchPlayer.Points,
                matchPlayer.PersonalFouls);
        }

        _homeScorerOptions = new ObservableCollection<LiveScorerOption>(
            matchPlayers
                .Where(x => x.TeamId == homeTeamId)
                .Select(x => ToOption(x, playersById)));

        _awayScorerOptions = new ObservableCollection<LiveScorerOption>(
            matchPlayers
                .Where(x => x.TeamId == awayTeamId)
                .Select(x => ToOption(x, playersById)));

        HomeScorerCombo.ItemsSource = _homeScorerOptions;
        AwayScorerCombo.ItemsSource = _awayScorerOptions;
        HomeLivePlayersGrid.ItemsSource = _homeScorerOptions;
        AwayLivePlayersGrid.ItemsSource = _awayScorerOptions;
        HomeScorerCombo.SelectedItem = _homeScorerOptions.FirstOrDefault();
        AwayScorerCombo.SelectedItem = _awayScorerOptions.FirstOrDefault();
        HomeLivePlayersGrid.SelectedItem = _homeScorerOptions.FirstOrDefault();
        AwayLivePlayersGrid.SelectedItem = _awayScorerOptions.FirstOrDefault();
    }

    private void PersistScore(bool home, int points, LiveScorerOption? scorer)
    {
        if (_isFreeLiveMode || _currentLiveMatchId is null)
        {
            return;
        }

        var side = home ? "Home" : "Away";
        var teamId = home ? _currentHomeTeamId : _currentAwayTeamId;
        var matchTeam = _db.MatchTeams.FirstOrDefault(x => x.MatchId == _currentLiveMatchId.Value && x.Side == side);
        if (matchTeam is not null)
        {
            matchTeam.Score = home ? _state.HomeScore : _state.AwayScore;
        }

        MatchPlayer? matchPlayer = null;
        if (scorer is not null)
        {
            matchPlayer = _db.MatchPlayers.FirstOrDefault(x => x.Id == scorer.MatchPlayerId);
            if (matchPlayer is not null)
            {
                matchPlayer.Points = Math.Max(0, matchPlayer.Points + points);
                scorer.Points = matchPlayer.Points;
                _changedMatchPlayerIds.Add(matchPlayer.Id);
            }
        }

        _db.MatchEvents.Add(new MatchEvent
        {
            MatchId = _currentLiveMatchId.Value,
            Period = _state.Period,
            PeriodType = "Regular",
            GameClockMsRemaining = _gameClock.Update(),
            ShotClockMsRemaining = _shotClock.Update(),
            TeamId = teamId,
            PlayerId = matchPlayer?.PlayerId,
            EventType = "Score",
            Points = points,
            Description = matchPlayer is null
                ? $"{(home ? _state.HomeName : _state.AwayName)} +{points}"
                : $"{scorer?.DisplayName} +{points}",
            CreatedAt = Now()
        });

        PersistLiveState(saveChanges: false);
        SaveChanges();
        HomeLivePlayersGrid.Items.Refresh();
        AwayLivePlayersGrid.Items.Refresh();
    }

    private void PersistFoul(bool home, int delta, LiveScorerOption? scorer)
    {
        if (_isFreeLiveMode || _currentLiveMatchId is null)
        {
            return;
        }

        var side = home ? "Home" : "Away";
        var teamId = home ? _currentHomeTeamId : _currentAwayTeamId;
        var matchTeam = _db.MatchTeams.FirstOrDefault(x => x.MatchId == _currentLiveMatchId.Value && x.Side == side);
        if (matchTeam is not null)
        {
            matchTeam.FoulsCurrentPeriod = home ? _state.HomeFouls : _state.AwayFouls;
        }

        MatchPlayer? matchPlayer = null;
        if (scorer is not null)
        {
            matchPlayer = _db.MatchPlayers.FirstOrDefault(x => x.Id == scorer.MatchPlayerId);
            if (matchPlayer is not null)
            {
                matchPlayer.PersonalFouls = Math.Max(0, matchPlayer.PersonalFouls + delta);
                scorer.Fouls = matchPlayer.PersonalFouls;
                _changedMatchPlayerIds.Add(matchPlayer.Id);
            }
        }

        _db.MatchEvents.Add(new MatchEvent
        {
            MatchId = _currentLiveMatchId.Value,
            Period = _state.Period,
            PeriodType = "Regular",
            GameClockMsRemaining = _gameClock.Update(),
            ShotClockMsRemaining = _shotClock.Update(),
            TeamId = teamId,
            PlayerId = matchPlayer?.PlayerId,
            EventType = delta >= 0 ? "Foul" : "FoulCorrection",
            IsCorrection = delta < 0,
            Description = matchPlayer is null
                ? $"{(home ? _state.HomeName : _state.AwayName)} falli {(delta >= 0 ? "+1" : "-1")}"
                : $"{scorer?.DisplayName} fallo {(delta >= 0 ? "+1" : "-1")}",
            CreatedAt = Now()
        });

        PersistLiveState(saveChanges: false);
        SaveChanges();
        HomeLivePlayersGrid.Items.Refresh();
        AwayLivePlayersGrid.Items.Refresh();
    }

    private void PersistLiveState(bool saveChanges = true)
    {
        if (_isFreeLiveMode || _currentLiveMatchId is null)
        {
            return;
        }

        var matchId = _currentLiveMatchId.Value;
        var state = _db.ScoreboardStates.FirstOrDefault(x => x.MatchId == matchId);
        if (state is null)
        {
            state = new ScoreboardStateRecord { MatchId = matchId };
            _db.ScoreboardStates.Add(state);
        }

        state.CurrentPeriod = _state.Period;
        state.CurrentPeriodType = "Regular";
        state.GameClockMsRemaining = _gameClock.Update();
        state.ShotClockMsRemaining = _shotClock.Update();
        state.IsGameClockRunning = _gameClock.IsRunning;
        state.IsShotClockRunning = _shotClock.IsRunning;
        state.HomeScore = _state.HomeScore;
        state.AwayScore = _state.AwayScore;
        state.HomeFoulsCurrentPeriod = _state.HomeFouls;
        state.AwayFoulsCurrentPeriod = _state.AwayFouls;
        state.HomeTimeoutsUsedTotal = _state.HomeTimeouts;
        state.AwayTimeoutsUsedTotal = _state.AwayTimeouts;
        state.LastUpdatedAt = Now();

        var homeSide = _db.MatchTeams.FirstOrDefault(x => x.MatchId == matchId && x.Side == "Home");
        if (homeSide is not null)
        {
            homeSide.Score = _state.HomeScore;
            homeSide.FoulsCurrentPeriod = _state.HomeFouls;
            homeSide.TimeoutsUsedTotal = _state.HomeTimeouts;
        }

        var awaySide = _db.MatchTeams.FirstOrDefault(x => x.MatchId == matchId && x.Side == "Away");
        if (awaySide is not null)
        {
            awaySide.Score = _state.AwayScore;
            awaySide.FoulsCurrentPeriod = _state.AwayFouls;
            awaySide.TimeoutsUsedTotal = _state.AwayTimeouts;
        }

        if (saveChanges)
        {
            SaveChanges();
        }
    }

    private void EnsureScoreboardState(int matchId, int gameClockMs, int shotClockMs)
    {
        if (_db.ScoreboardStates.Any(x => x.MatchId == matchId))
        {
            return;
        }

        _db.ScoreboardStates.Add(new ScoreboardStateRecord
        {
            MatchId = matchId,
            CurrentPeriod = _state.Period,
            CurrentPeriodType = "Regular",
            GameClockMsRemaining = gameClockMs,
            ShotClockMsRemaining = shotClockMs,
            HomeScore = _state.HomeScore,
            AwayScore = _state.AwayScore,
            HomeFoulsCurrentPeriod = _state.HomeFouls,
            AwayFoulsCurrentPeriod = _state.AwayFouls,
            HomeTimeoutsUsedTotal = _state.HomeTimeouts,
            AwayTimeoutsUsedTotal = _state.AwayTimeouts,
            LastUpdatedAt = Now()
        });
        SaveChanges();
    }

    private void RosterTeamCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        LoadRosterForSelectedTeam();
    }

    private async void AddRosterPlayer_Click(object sender, RoutedEventArgs e)
    {
        if (RosterTeamCombo.SelectedItem is not Team team || AvailablePlayersGrid.SelectedItem is not Player player)
        {
            return;
        }

        if (team.Id == 0 || player.Id == 0)
        {
            MessageBox.Show(
                "Salva prima squadre e giocatori, poi aggiungili al roster.",
                "Roster",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var now = Now();
        var roster = new TeamRoster
        {
            TeamId = team.Id,
            PlayerId = player.Id,
            Role = "Player",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        if (_onlineEntities is not null)
        {
            try
            {
                roster = await _onlineEntities.CreateTeamRosterAsync(roster);
                UpsertLocalTeamRoster(roster);
                _db.SaveChanges();
                LoadRosterForSelectedTeam();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Roster online", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return;
        }

        _db.TeamRosters.Add(roster);
        _rosterRows.Add(new RosterRow(roster, player));
        _availableRosterPlayers.Remove(player);

        if (!SaveChanges())
        {
            LoadRosterForSelectedTeam();
        }
    }

    private async void RemoveRosterPlayer_Click(object sender, RoutedEventArgs e)
    {
        if (RosterGrid.SelectedItem is not RosterRow row)
        {
            return;
        }

        if (_onlineEntities is not null)
        {
            try
            {
                await _onlineEntities.DeleteTeamRosterAsync(row.Roster.Id);
                var local = _db.TeamRosters.FirstOrDefault(x => x.Id == row.Roster.Id);
                if (local is not null)
                {
                    _db.TeamRosters.Remove(local);
                    _db.SaveChanges();
                }

                LoadRosterForSelectedTeam();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Roster online", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return;
        }

        _db.TeamRosters.Remove(row.Roster);
        _rosterRows.Remove(row);

        var player = _players.FirstOrDefault(x => x.Id == row.Roster.PlayerId);
        if (player is not null)
        {
            _availableRosterPlayers.Add(player);
        }

        if (!SaveChanges())
        {
            LoadRosterForSelectedTeam();
        }
    }

    private void RosterGrid_CellEditEnding(object sender, System.Windows.Controls.DataGridCellEditEndingEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (SaveRosterChanges())
            {
                LoadRosterForSelectedTeam();
            }
        }), DispatcherPriority.Background);
    }

    private async void AddCompetitionEvent_Click(object sender, RoutedEventArgs e)
    {
        var editions = _editionRows.Select(row => row.Edition).ToList();
        if (editions.Count == 0)
        {
            MessageBox.Show("Crea prima almeno una edizione.", "3 Point Contest", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var evt = new CompetitionEvent
        {
            EditionId = _currentEditionId ?? editions[0].Id,
            EventType = "ThreePointContest",
            Name = "3 Point Contest",
            Status = "Scheduled"
        };

        var form = new CompetitionEventFormWindow(evt, editions, isNew: true) { Owner = this };
        if (form.ShowDialog() != true)
        {
            return;
        }

        if (_onlineEntities is not null)
        {
            try
            {
                evt = await _onlineEntities.CreateCompetitionEventAsync(evt);
                UpsertLocalCompetitionEvent(evt);
                _db.SaveChanges();
                LoadCrudData();
                CompetitionEventsGrid.SelectedItem = _competitionEventRows.FirstOrDefault(x => x.Event.Id == evt.Id);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Eventi online", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return;
        }

        _db.CompetitionEvents.Add(evt);
        if (SaveChanges()) LoadCrudData();
        CompetitionEventsGrid.SelectedItem = _competitionEventRows.FirstOrDefault(x => x.Event.Id == evt.Id);
    }

    private async void EditCompetitionEvent_Click(object sender, RoutedEventArgs e)
    {
        if (CompetitionEventsGrid.SelectedItem is not CompetitionEventRow row)
        {
            MessageBox.Show("Seleziona un evento da modificare.", "3 Point Contest", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var form = new CompetitionEventFormWindow(row.Event, _editionRows.Select(x => x.Edition).ToList(), isNew: false) { Owner = this };
        if (form.ShowDialog() != true)
        {
            return;
        }

        if (_onlineEntities is not null)
        {
            try
            {
                var evt = await _onlineEntities.UpdateCompetitionEventAsync(row.Event);
                UpsertLocalCompetitionEvent(evt);
                _db.SaveChanges();
                LoadCrudData();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Eventi online", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadCrudData();
            }

            return;
        }

        if (SaveChanges()) LoadCrudData();
    }

    private async void DeleteCompetitionEvent_Click(object sender, RoutedEventArgs e)
    {
        if (CompetitionEventsGrid.SelectedItem is not CompetitionEventRow row)
        {
            return;
        }

        if (HasBlockingLinks("evento", [new LinkCount("Partecipanti", CountLinks("three_point_contest_entries", "competition_event_id", row.Event.Id))]))
        {
            return;
        }

        var result = MessageBox.Show(
            $"Eliminare l'evento '{row.Event.Name}'?",
            "Conferma eliminazione",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        if (_onlineEntities is not null)
        {
            try
            {
                await _onlineEntities.DeleteCompetitionEventAsync(row.Event.Id);
                var local = _db.CompetitionEvents.FirstOrDefault(x => x.Id == row.Event.Id);
                if (local is not null)
                {
                    _db.CompetitionEvents.Remove(local);
                    _db.SaveChanges();
                }

                LoadCrudData();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Eventi online", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return;
        }

        _db.CompetitionEvents.Remove(row.Event);
        if (SaveChanges()) LoadCrudData();
    }

    private async void AddThreePointEntry_Click(object sender, RoutedEventArgs e)
    {
        var events = _db.CompetitionEvents.Where(x => x.EventType == "ThreePointContest").OrderBy(x => x.Name).ToList();
        if (events.Count == 0)
        {
            MessageBox.Show("Crea prima un evento 3 Point Contest.", "3 Point Contest", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var entry = new ThreePointContestEntry
        {
            CompetitionEventId = events[0].Id
        };

        var form = new ThreePointEntryFormWindow(entry, events, _db.Teams.ToList(), _db.TeamRosters.ToList(), _db.Players.ToList(), isNew: true) { Owner = this };
        if (form.ShowDialog() != true)
        {
            return;
        }

        if (ThreePointEntryAlreadyExists(entry))
        {
            MessageBox.Show("Per questo evento esiste gia un partecipante della stessa squadra o lo stesso giocatore.", "Dati non validi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_onlineEntities is not null)
        {
            try
            {
                entry = await _onlineEntities.CreateThreePointContestEntryAsync(entry);
                UpsertLocalThreePointContestEntry(entry);
                _db.SaveChanges();
                LoadCrudData();
                ThreePointEntriesGrid.SelectedItem = _threePointEntryRows.FirstOrDefault(x => x.Entry.Id == entry.Id);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "3 Point Contest online", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return;
        }

        _db.ThreePointContestEntries.Add(entry);
        if (SaveChanges()) LoadCrudData();
    }

    private async void EditThreePointEntry_Click(object sender, RoutedEventArgs e)
    {
        if (ThreePointEntriesGrid.SelectedItem is not ThreePointEntryRow row)
        {
            MessageBox.Show("Seleziona un partecipante da modificare.", "3 Point Contest", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var events = _db.CompetitionEvents.Where(x => x.EventType == "ThreePointContest").OrderBy(x => x.Name).ToList();
        var form = new ThreePointEntryFormWindow(row.Entry, events, _db.Teams.ToList(), _db.TeamRosters.ToList(), _db.Players.ToList(), isNew: false) { Owner = this };
        if (form.ShowDialog() != true)
        {
            return;
        }

        if (ThreePointEntryAlreadyExists(row.Entry))
        {
            MessageBox.Show("Per questo evento esiste gia un partecipante della stessa squadra o lo stesso giocatore.", "Dati non validi", MessageBoxButton.OK, MessageBoxImage.Warning);
            LoadCrudData();
            return;
        }

        if (_onlineEntities is not null)
        {
            try
            {
                var entry = await _onlineEntities.UpdateThreePointContestEntryAsync(row.Entry);
                UpsertLocalThreePointContestEntry(entry);
                _db.SaveChanges();
                LoadCrudData();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "3 Point Contest online", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadCrudData();
            }

            return;
        }

        if (SaveChanges()) LoadCrudData();
    }

    private async void DeleteThreePointEntry_Click(object sender, RoutedEventArgs e)
    {
        if (ThreePointEntriesGrid.SelectedItem is not ThreePointEntryRow row)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Eliminare il partecipante '{row.PlayerName}'?",
            "Conferma eliminazione",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        if (_onlineEntities is not null)
        {
            try
            {
                await _onlineEntities.DeleteThreePointContestEntryAsync(row.Entry.Id);
                var local = _db.ThreePointContestEntries.FirstOrDefault(x => x.Id == row.Entry.Id);
                if (local is not null)
                {
                    _db.ThreePointContestEntries.Remove(local);
                    _db.SaveChanges();
                }

                LoadCrudData();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "3 Point Contest online", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return;
        }

        _db.ThreePointContestEntries.Remove(row.Entry);
        if (SaveChanges()) LoadCrudData();
    }

    private async void AddThreePointRound_Click(object sender, RoutedEventArgs e)
    {
        var options = BuildThreePointEntryOptions();
        if (options.Count == 0)
        {
            MessageBox.Show("Crea prima almeno un partecipante.", "3 Point Contest", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var round = new ThreePointContestRound { EntryId = options[0].Id, RoundNumber = 1, RoundType = "Qualification" };
        var form = new ThreePointRoundFormWindow(round, options, isNew: true) { Owner = this };
        if (form.ShowDialog() != true) return;

        if (ThreePointRoundAlreadyExists(round))
        {
            MessageBox.Show("Esiste gia una prova con lo stesso numero e la stessa fase per questo partecipante.", "Dati non validi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_onlineEntities is not null)
        {
            try
            {
                round = await _onlineEntities.CreateThreePointContestRoundAsync(round);
                UpsertLocalThreePointContestRound(round);
                await PushThreePointEntryTotalOnlineAsync(round.EntryId);
                _db.SaveChanges();
                LoadCrudData();
                ThreePointRoundsGrid.SelectedItem = _threePointRoundRows.FirstOrDefault(x => x.Round.Id == round.Id);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "3 Point Contest online", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadCrudData();
            }

            return;
        }

        _db.ThreePointContestRounds.Add(round);
        UpdateThreePointEntryTotal(round.EntryId);
        if (SaveChanges()) LoadCrudData();
    }

    private async void EditThreePointRound_Click(object sender, RoutedEventArgs e)
    {
        if (ThreePointRoundsGrid.SelectedItem is not ThreePointRoundRow row)
        {
            MessageBox.Show("Seleziona un round da modificare.", "3 Point Contest", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var oldEntryId = row.Round.EntryId;
        var form = new ThreePointRoundFormWindow(row.Round, BuildThreePointEntryOptions(), isNew: false) { Owner = this };
        if (form.ShowDialog() != true) return;

        if (ThreePointRoundAlreadyExists(row.Round))
        {
            MessageBox.Show("Esiste gia una prova con lo stesso numero e la stessa fase per questo partecipante.", "Dati non validi", MessageBoxButton.OK, MessageBoxImage.Warning);
            LoadCrudData();
            return;
        }

        if (_onlineEntities is not null)
        {
            try
            {
                var round = await _onlineEntities.UpdateThreePointContestRoundAsync(row.Round);
                UpsertLocalThreePointContestRound(round);
                await PushThreePointEntryTotalOnlineAsync(oldEntryId, round.EntryId);
                _db.SaveChanges();
                LoadCrudData();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "3 Point Contest online", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadCrudData();
            }

            return;
        }

        UpdateThreePointEntryTotal(oldEntryId);
        UpdateThreePointEntryTotal(row.Round.EntryId);
        if (SaveChanges()) LoadCrudData();
    }

    private async void DeleteThreePointRound_Click(object sender, RoutedEventArgs e)
    {
        if (ThreePointRoundsGrid.SelectedItem is not ThreePointRoundRow row) return;
        var result = MessageBox.Show("Eliminare la prova selezionata?", "Conferma eliminazione", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        var entryId = row.Round.EntryId;
        if (_onlineEntities is not null)
        {
            try
            {
                await _onlineEntities.DeleteThreePointContestRoundAsync(row.Round.Id);
                var local = _db.ThreePointContestRounds.FirstOrDefault(x => x.Id == row.Round.Id);
                if (local is not null)
                {
                    _db.ThreePointContestRounds.Remove(local);
                    _db.SaveChanges();
                }

                await PushThreePointEntryTotalOnlineAsync(entryId);
                _db.SaveChanges();
                LoadCrudData();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "3 Point Contest online", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadCrudData();
            }

            return;
        }

        _db.ThreePointContestRounds.Remove(row.Round);
        if (SaveChanges())
        {
            UpdateThreePointEntryTotal(entryId);
            SaveChanges();
            LoadCrudData();
        }
    }

    private async void AddForfeit_Click(object sender, RoutedEventArgs e)
    {
        var options = BuildForfeitMatchOptions();
        if (options.Count == 0)
        {
            MessageBox.Show("Crea prima almeno una partita.", "Tavolino", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var forfeit = new ForfeitResult { MatchId = options[0].Id, HomeAssignedScore = 20, AwayAssignedScore = 0, Reason = "OrganizerDecision", CreatedAt = Now() };
        var form = new ForfeitFormWindow(forfeit, options, _db.MatchTeams.ToList(), _db.Teams.ToList(), isNew: true) { Owner = this };
        if (form.ShowDialog() != true) return;

        if (_db.ForfeitResults.Any(x => x.MatchId == forfeit.MatchId))
        {
            MessageBox.Show("Per questa partita esiste gia un risultato a tavolino.", "Dati non validi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_onlineEntities is not null)
        {
            try
            {
                ApplyForfeitToMatch(forfeit);
                var match = _db.Matches.Single(x => x.Id == forfeit.MatchId);
                var home = _db.MatchTeams.Single(x => x.MatchId == forfeit.MatchId && x.Side == "Home");
                var away = _db.MatchTeams.Single(x => x.MatchId == forfeit.MatchId && x.Side == "Away");
                var bundle = await _onlineEntities.SaveForfeitBundleAsync(forfeit, match, home, away);
                forfeit = bundle.Forfeit ?? forfeit;
                UpsertLocalForfeitResult(forfeit);
                UpsertLocalMatch(bundle.Match);
                UpsertLocalMatchTeam(bundle.Home);
                UpsertLocalMatchTeam(bundle.Away);
                _db.SaveChanges();
                LoadCrudData();
                ForfeitsGrid.SelectedItem = _forfeitRows.FirstOrDefault(x => x.Forfeit.Id == forfeit.Id);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Tavolino online", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadCrudData();
            }

            return;
        }

        _db.ForfeitResults.Add(forfeit);
        ApplyForfeitToMatch(forfeit);
        if (SaveChanges()) LoadCrudData();
    }

    private async void EditForfeit_Click(object sender, RoutedEventArgs e)
    {
        if (ForfeitsGrid.SelectedItem is not ForfeitRow row)
        {
            MessageBox.Show("Seleziona un risultato da modificare.", "Tavolino", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var form = new ForfeitFormWindow(row.Forfeit, BuildForfeitMatchOptions(), _db.MatchTeams.ToList(), _db.Teams.ToList(), isNew: false) { Owner = this };
        if (form.ShowDialog() != true) return;

        if (_onlineEntities is not null)
        {
            try
            {
                ApplyForfeitToMatch(row.Forfeit);
                var match = _db.Matches.Single(x => x.Id == row.Forfeit.MatchId);
                var home = _db.MatchTeams.Single(x => x.MatchId == row.Forfeit.MatchId && x.Side == "Home");
                var away = _db.MatchTeams.Single(x => x.MatchId == row.Forfeit.MatchId && x.Side == "Away");
                var bundle = await _onlineEntities.SaveForfeitBundleAsync(row.Forfeit, match, home, away);
                if (bundle.Forfeit is not null) UpsertLocalForfeitResult(bundle.Forfeit);
                UpsertLocalMatch(bundle.Match);
                UpsertLocalMatchTeam(bundle.Home);
                UpsertLocalMatchTeam(bundle.Away);
                _db.SaveChanges();
                LoadCrudData();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Tavolino online", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadCrudData();
            }

            return;
        }

        ApplyForfeitToMatch(row.Forfeit);
        if (SaveChanges()) LoadCrudData();
    }

    private async void DeleteForfeit_Click(object sender, RoutedEventArgs e)
    {
        if (ForfeitsGrid.SelectedItem is not ForfeitRow row) return;
        var result = MessageBox.Show("Eliminare il risultato a tavolino selezionato?", "Conferma eliminazione", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        if (_onlineEntities is not null)
        {
            try
            {
                var bundle = await _onlineEntities.DeleteForfeitBundleAsync(row.Forfeit.Id);
                var local = _db.ForfeitResults.FirstOrDefault(x => x.Id == row.Forfeit.Id);
                if (local is not null)
                {
                    _db.ForfeitResults.Remove(local);
                }

                UpsertLocalMatch(bundle.Match);
                UpsertLocalMatchTeam(bundle.Home);
                UpsertLocalMatchTeam(bundle.Away);
                _db.SaveChanges();

                LoadCrudData();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Tavolino online", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return;
        }

        _db.ForfeitResults.Remove(row.Forfeit);
        if (SaveChanges()) LoadCrudData();
    }

    private async void RecalculateStandings_Click(object sender, RoutedEventArgs e)
    {
        if (_onlineEntities is not null)
        {
            try
            {
                RecalculateStandings();
                var onlineStandings = await _onlineEntities.ReplaceStandingsAsync(_db.Standings.ToList());
                ReplaceLocalStandings(onlineStandings);
                LoadCrudData();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Classifica online", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadCrudData();
            }

            return;
        }

        RecalculateStandings();
        if (SaveChanges()) LoadCrudData();
    }

    private void LoadRosterForSelectedTeam()
    {
        if (RosterTeamCombo.SelectedItem is not Team team)
        {
            _availableRosterPlayers = [];
            _rosterRows = [];
            AvailablePlayersGrid.ItemsSource = _availableRosterPlayers;
            RosterGrid.ItemsSource = _rosterRows;
            return;
        }

        var rosterItems = LoadTeamRosters()
            .Where(roster => roster.TeamId == team.Id)
            .OrderBy(roster => roster.JerseyNumber)
            .ThenBy(roster => roster.Id)
            .ToList();

        var rosterPlayerIds = rosterItems.Select(roster => roster.PlayerId).ToHashSet();
        _availableRosterPlayers = new ObservableCollection<Player>(
            _players
                .Where(player => !rosterPlayerIds.Contains(player.Id))
                .OrderBy(player => player.LastName)
                .ThenBy(player => player.FirstName));

        _rosterRows = new ObservableCollection<RosterRow>(
            rosterItems
                .Select(roster =>
                {
                    var player = _players.FirstOrDefault(x => x.Id == roster.PlayerId);
                    return new RosterRow(roster, player);
                }));

        AvailablePlayersGrid.ItemsSource = _availableRosterPlayers;
        RosterGrid.ItemsSource = _rosterRows;
    }

    private async Task BroadcastAsync()
    {
        _state.GameClockMs = _gameClock.Update();
        _state.ShotClockMs = _shotClock.Update();
        _state.IsGameClockRunning = _gameClock.IsRunning;
        _state.IsShotClockRunning = _shotClock.IsRunning;
        _state.HasActiveMatch = IsOfficialLiveSessionLocked;
        _state.HomePlayers = _homeScorerOptions
            .Select(x => new PlayerStat(x.JerseyNumber, x.PlayerName, x.Points, x.Fouls))
            .ToList();
        _state.AwayPlayers = _awayScorerOptions
            .Select(x => new PlayerStat(x.JerseyNumber, x.PlayerName, x.Points, x.Fouls))
            .ToList();
        await _broadcaster.BroadcastAsync(_state);
    }

    private static string FormatGameClock(int ms)
    {
        var totalSeconds = (int)Math.Ceiling(Math.Max(0, ms) / 1000d);
        var minutes = totalSeconds / 60;
        var seconds = totalSeconds % 60;
        return $"{minutes:00}:{seconds:00}";
    }

    private static string Now() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

    protected override void OnClosed(EventArgs e)
    {
        _db.Dispose();
        base.OnClosed(e);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (IsOfficialLiveSessionLocked || _hasPendingLiveSync || _isSyncingLiveData)
        {
            var details = IsOfficialLiveSessionLocked
                ? "La partita è ancora in corso o in pausa."
                : "Esistono modifiche live non ancora confermate dal server.";
            var result = MessageBox.Show(
                $"{details}\n\nChiudendo ora i dati restano nel salvataggio locale e saranno ritentati al prossimo avvio. Vuoi chiudere comunque?",
                "Sessione live attiva",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                e.Cancel = true;
                return;
            }
        }

        base.OnClosing(e);
    }

    private sealed class RosterRow
    {
        public RosterRow(TeamRoster roster, Player? player)
        {
            Roster = roster;
            PlayerName = player is null
                ? $"Giocatore #{roster.PlayerId}"
                : $"{player.LastName} {player.FirstName}".Trim();
            JerseyNumber = roster.JerseyNumber;
            Role = roster.Role;
            IsCaptain = roster.IsCaptain;
            IsActive = roster.IsActive;
        }

        public TeamRoster Roster { get; }
        public string PlayerName { get; }
        public int? JerseyNumber { get; set; }
        public string? Role { get; set; }
        public bool IsCaptain { get; set; }
        public bool IsActive { get; set; }

        public void ApplyToEntity()
        {
            Roster.JerseyNumber = JerseyNumber;
            Roster.Role = Role;
            Roster.IsCaptain = IsCaptain;
            Roster.IsActive = IsActive;
        }
    }

    private sealed class EditionRow
    {
        public EditionRow(Edition edition, string tournamentName)
        {
            Edition = edition;
            TournamentName = tournamentName;
        }

        public Edition Edition { get; }
        public string TournamentName { get; }
    }

    private sealed class CourtRow
    {
        public CourtRow(Court court, string editionName)
        {
            Court = court;
            EditionName = editionName;
        }

        public Court Court { get; }
        public string EditionName { get; }
    }

    private sealed class GroupRow
    {
        public GroupRow(TournamentGroup group, string editionName)
        {
            Group = group;
            EditionName = editionName;
        }

        public TournamentGroup Group { get; }
        public string EditionName { get; }
    }

    private sealed class GroupTeamRow
    {
        public GroupTeamRow(GroupTeam groupTeam, string groupName, string teamName)
        {
            GroupTeam = groupTeam;
            GroupName = groupName;
            TeamName = teamName;
        }

        public GroupTeam GroupTeam { get; }
        public string GroupName { get; }
        public string TeamName { get; }
    }

    private sealed class MatchRow
    {
        public MatchRow(Match match, string editionName, string groupName, string courtName, string homeTeamName, string awayTeamName)
        {
            Match = match;
            EditionName = editionName;
            GroupName = groupName;
            CourtName = courtName;
            HomeTeamName = homeTeamName;
            AwayTeamName = awayTeamName;
        }

        public Match Match { get; }
        public string EditionName { get; }
        public string GroupName { get; }
        public string CourtName { get; }
        public string HomeTeamName { get; }
        public string AwayTeamName { get; }
    }

    private sealed class MatchOption
    {
        public MatchOption(int id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }

        public int Id { get; }
        public string DisplayName { get; }
    }

    private sealed class LiveScorerOption
    {
        public LiveScorerOption(int matchPlayerId, int teamId, int playerId, int? jerseyNumber, string playerName, string displayName, int points, int fouls)
        {
            MatchPlayerId = matchPlayerId;
            TeamId = teamId;
            PlayerId = playerId;
            JerseyNumber = jerseyNumber;
            PlayerName = playerName;
            DisplayName = displayName;
            Points = points;
            Fouls = fouls;
        }

        public int MatchPlayerId { get; }
        public int TeamId { get; }
        public int PlayerId { get; }
        public int? JerseyNumber { get; }
        public string PlayerName { get; }
        public string DisplayName { get; }
        public int Points { get; set; }
        public int Fouls { get; set; }
    }

    private sealed class ContestEntryOption
    {
        public ContestEntryOption(ThreePointContestEntry entry, string teamName, string playerName)
        {
            Entry = entry;
            TeamName = teamName;
            PlayerName = playerName;
        }

        public ThreePointContestEntry Entry { get; }
        public string TeamName { get; }
        public string PlayerName { get; }
        public string DisplayName => $"{PlayerName} - {TeamName}";
    }

    private sealed class ContestRoundOption
    {
        public ContestRoundOption(ThreePointContestRound round) => Round = round;
        public ThreePointContestRound Round { get; }
        public string DisplayName => $"{FormatRoundType(Round.RoundType)} - Prova {Round.RoundNumber}";

        private static string FormatRoundType(string roundType) => roundType switch
        {
            "Final" => "Finale",
            "TieBreak" => "Spareggio",
            _ => "Qualificazioni"
        };
    }

    private sealed class ContestRoundLiveState
    {
        public string Status { get; set; } = "Ready";
        public int Station { get; set; } = 1;
        public int ClockMs { get; set; } = 60000;
    }

    private sealed class CompetitionEventRow
    {
        public CompetitionEventRow(CompetitionEvent competitionEvent, string editionName)
        {
            Event = competitionEvent;
            EditionName = editionName;
        }

        public CompetitionEvent Event { get; }
        public string EditionName { get; }
    }

    private sealed class ThreePointEntryRow
    {
        public ThreePointEntryRow(ThreePointContestEntry entry, string eventName, string teamName, string playerName)
        {
            Entry = entry;
            EventName = eventName;
            TeamName = teamName;
            PlayerName = playerName;
        }

        public ThreePointContestEntry Entry { get; }
        public string EventName { get; }
        public string TeamName { get; }
        public string PlayerName { get; }
    }

    private sealed class ThreePointRoundRow
    {
        public ThreePointRoundRow(ThreePointContestRound round, string entryName)
        {
            Round = round;
            EntryName = entryName;
        }

        public ThreePointContestRound Round { get; }
        public string EntryName { get; }
        public string RoundTypeText => Round.RoundType switch
        {
            "Final" => "Finale",
            "TieBreak" => "Spareggio",
            _ => "Qualificazioni"
        };
    }

    private sealed class ForfeitRow
    {
        public ForfeitRow(ForfeitResult forfeit, string matchName, string winnerName, string scoreText)
        {
            Forfeit = forfeit;
            MatchName = matchName;
            WinnerName = winnerName;
            ScoreText = scoreText;
        }

        public ForfeitResult Forfeit { get; }
        public string MatchName { get; }
        public string WinnerName { get; }
        public string ScoreText { get; }
    }

    private sealed class StandingRow
    {
        public StandingRow(Standing standing, string groupName, string teamName)
        {
            Standing = standing;
            GroupName = groupName;
            TeamName = teamName;
        }

        public Standing Standing { get; }
        public string GroupName { get; }
        public string TeamName { get; }
    }

    private sealed record LinkCount(string Label, long Count);
}
