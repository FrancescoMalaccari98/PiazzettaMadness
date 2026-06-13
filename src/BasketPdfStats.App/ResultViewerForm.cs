using BasketPdfStats.App.ViewModels;
using BasketPdfStats.Core.Models;
using Forms = System.Windows.Forms;

namespace BasketPdfStats.App;

public sealed class ResultViewerForm : Forms.Form
{
    private static readonly (string Property, string Header, int Width)[] PlayerColumns =
    [
        ("Number", "N.", 42), ("Name", "Nome", 170), ("Minutes", "Min.", 58),
        ("FieldGoals", "FG", 58), ("FieldGoalsPercentage", "FG%", 52),
        ("TwoPoints", "2P", 58), ("TwoPointsPercentage", "2P%", 52),
        ("ThreePoints", "3P", 58), ("ThreePointsPercentage", "3P%", 52),
        ("FreeThrows", "FT", 58), ("FreeThrowsPercentage", "FT%", 52),
        ("ReboundsOffensive", "RO", 42), ("ReboundsDefensive", "RD", 42), ("Rebounds", "RT", 42),
        ("Assists", "AS", 42), ("Turnovers", "PP", 42), ("Steals", "PR", 42), ("Blocks", "SD", 42),
        ("Fouls", "FF", 42), ("FoulsDrawn", "FS", 42), ("PlusMinus", "+/-", 48),
        ("Evaluation", "Val.", 48), ("Points", "PTl", 48), ("Provider", "Provider", 130), ("Warnings", "Warnings", 220)
    ];

    private readonly ProcessingResultViewModel _viewModel;

    public ResultViewerForm(ProcessingResult result)
    {
        _viewModel = new ProcessingResultViewModel(result);
        Text = $"Risultato elaborazione - {_viewModel.FileName}";
        StartPosition = Forms.FormStartPosition.CenterScreen;
        Width = 1320;
        Height = 860;
        MinimumSize = new System.Drawing.Size(1040, 680);
        BackColor = System.Drawing.Color.FromArgb(246, 248, 250);
        Font = new System.Drawing.Font("Segoe UI", 9F);
        Controls.Add(BuildLayout());
    }

    private Forms.Control BuildLayout()
    {
        var layout = new Forms.TableLayoutPanel
        {
            Dock = Forms.DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Forms.Padding(12)
        };
        layout.RowStyles.Add(new Forms.RowStyle(Forms.SizeType.AutoSize));
        layout.RowStyles.Add(new Forms.RowStyle(Forms.SizeType.AutoSize));
        layout.RowStyles.Add(new Forms.RowStyle(Forms.SizeType.Percent, 100F));
        layout.Controls.Add(BuildHeader(), 0, 0);
        layout.Controls.Add(BuildScoreboard(), 0, 1);
        layout.Controls.Add(BuildTabs(), 0, 2);
        return layout;
    }

    private Forms.Control BuildHeader()
    {
        var panel = new Forms.TableLayoutPanel
        {
            Dock = Forms.DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            Padding = new Forms.Padding(12),
            BackColor = System.Drawing.Color.FromArgb(36, 41, 47)
        };
        panel.Controls.Add(Label(_viewModel.FileName, 15F, bold: true, color: System.Drawing.Color.White));
        panel.Controls.Add(Label($"{_viewModel.Competition}  {_viewModel.DateAndTime}", color: System.Drawing.Color.Gainsboro));
        panel.Controls.Add(Label($"Provider: {_viewModel.Providers}  |  Reconciliation: {_viewModel.ReconciliationEnabled}", color: System.Drawing.Color.Gainsboro));
        panel.Controls.Add(Label($"Teams: {_viewModel.TeamCount}  |  Players: {_viewModel.PlayerCount}  |  Stats: {_viewModel.StatCount}  |  Warning: {_viewModel.WarningCount}  |  Failed rules: {_viewModel.FailedRuleCount}", color: System.Drawing.Color.Gainsboro));
        return panel;
    }

    private Forms.Control BuildScoreboard()
    {
        var panel = new Forms.TableLayoutPanel
        {
            Dock = Forms.DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            Padding = new Forms.Padding(8, 12, 8, 8),
            BackColor = System.Drawing.Color.White
        };
        panel.Controls.Add(Label($"{_viewModel.HomeTeam}   {_viewModel.HomeScore} - {_viewModel.AwayScore}   {_viewModel.AwayTeam}", 20F, bold: true, align: System.Drawing.ContentAlignment.MiddleCenter));
        var periods = string.Join("    ", _viewModel.Periods.Select(period => $"{period.Period}: {period.Home}-{period.Away}"));
        panel.Controls.Add(Label(string.IsNullOrWhiteSpace(periods) ? "Parziali non disponibili" : periods, align: System.Drawing.ContentAlignment.MiddleCenter));
        return panel;
    }

    private Forms.Control BuildTabs()
    {
        var tabs = new Forms.TabControl { Dock = Forms.DockStyle.Fill };
        tabs.TabPages.Add(Tab("Team Totals", BuildTeamTotals()));
        tabs.TabPages.Add(Tab("Home Players", Grid(_viewModel.HomePlayers, PlayerColumns)));
        tabs.TabPages.Add(Tab("Away Players", Grid(_viewModel.AwayPlayers, PlayerColumns)));
        tabs.TabPages.Add(Tab("Comparative Stats", Grid(_viewModel.ComparativeStats,
            ("Statistic", "Statistica", 320), ("Home", "Home", 130), ("Away", "Away", 130), ("Value", "Valore comune", 150))));
        tabs.TabPages.Add(Tab("Validation / Warnings", Grid(_viewModel.WarningRows,
            ("Severity", "Severity", 90), ("RuleId", "Rule ID", 230), ("FieldId", "Field ID", 260), ("Message", "Messaggio", 520))));
        return tabs;
    }

    private Forms.Control BuildTeamTotals()
    {
        var split = new Forms.SplitContainer
        {
            Dock = Forms.DockStyle.Fill,
            Orientation = Forms.Orientation.Vertical,
            SplitterDistance = 630
        };
        split.Panel1.Controls.Add(Group(_viewModel.HomeTeam, Grid(_viewModel.HomeTeamTotals,
            ("Statistic", "Statistica", 260), ("Value", "Valore", 100), ("Confidence", "Confidence", 90), ("Status", "Stato", 120))));
        split.Panel2.Controls.Add(Group(_viewModel.AwayTeam, Grid(_viewModel.AwayTeamTotals,
            ("Statistic", "Statistica", 260), ("Value", "Valore", 100), ("Confidence", "Confidence", 90), ("Status", "Stato", 120))));
        return split;
    }

    private static Forms.TabPage Tab(string title, Forms.Control content)
    {
        var page = new Forms.TabPage(title);
        page.Controls.Add(content);
        return page;
    }

    private static Forms.GroupBox Group(string title, Forms.Control content)
    {
        var group = new Forms.GroupBox { Text = title, Dock = Forms.DockStyle.Fill, Padding = new Forms.Padding(8) };
        group.Controls.Add(content);
        return group;
    }

    private static Forms.DataGridView Grid<T>(IReadOnlyList<T> rows, params (string Property, string Header, int Width)[] columns)
    {
        var grid = new Forms.DataGridView
        {
            Dock = Forms.DockStyle.Fill,
            ReadOnly = true,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToOrderColumns = false,
            RowHeadersVisible = false,
            SelectionMode = Forms.DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = System.Drawing.Color.White,
            DataSource = rows.ToList()
        };
        foreach (var column in columns)
        {
            grid.Columns.Add(new Forms.DataGridViewTextBoxColumn
            {
                DataPropertyName = column.Property,
                HeaderText = column.Header,
                Width = column.Width
            });
        }

        return grid;
    }

    private static Forms.Label Label(
        string text,
        float fontSize = 9F,
        bool bold = false,
        System.Drawing.Color? color = null,
        System.Drawing.ContentAlignment align = System.Drawing.ContentAlignment.MiddleLeft) =>
        new()
        {
            Text = text,
            AutoSize = true,
            Dock = Forms.DockStyle.Fill,
            Font = new System.Drawing.Font("Segoe UI", fontSize, bold ? System.Drawing.FontStyle.Bold : System.Drawing.FontStyle.Regular),
            ForeColor = color ?? System.Drawing.Color.FromArgb(36, 41, 47),
            TextAlign = align,
            Padding = new Forms.Padding(0, 2, 0, 2)
        };
}
