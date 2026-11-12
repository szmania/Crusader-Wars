using CrusaderWars.unit_mapper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows.Forms;
using System.Xml;

namespace CrusaderWars.mod_manager
{
    [SupportedOSPlatform("windows")]
    public partial class TwGameSelectionForm : Form
    {
        /// <summary>
        /// One row in the year-range matrix: a distinct time period and which games
        /// have a mapper folder covering it.
        /// </summary>
        public class YearRangeRow
        {
            public int StartYear { get; set; }
            public int EndYear { get; set; }
            public bool AttilaAvailable { get; set; }
            public bool Rome2Available { get; set; }
            public string AttilaFolder { get; set; } = string.Empty;
            public string Rome2Folder { get; set; } = string.Empty;
            public string ScreenName { get; set; } = string.Empty;
            public bool AttilaSelected { get; set; }
            public bool Rome2Selected { get; set; }
            public List<string> AttilaScreenNames { get; set; } = new List<string>();
            public List<string> Rome2ScreenNames { get; set; } = new List<string>();

            public string Label => StartYear == 0 && EndYear >= 9999 ? "Any Year" : $"{StartYear} AD \u2013 {EndYear} AD";
        }

        /// <summary>The user's selections, one per year-range row. Populated on OK.</summary>
        public List<YearRangeRow> Result { get; private set; } = new List<YearRangeRow>();

        private readonly List<YearRangeRow> _rows;
        private readonly string _playthroughTag;
        private bool _suppressCellEvents;

        /// <param name="rows">Pre-built year-range rows with availability flags and stored selections.</param>
        public TwGameSelectionForm(List<YearRangeRow> rows, string playthroughTag)
        {
            InitializeComponent();
            _rows = rows;
            _playthroughTag = playthroughTag;
        }

        private void TwGameSelectionForm_Load(object sender, EventArgs e)
        {
            _suppressCellEvents = true;
            try
            {
                dataGridViewGames.Rows.Clear();

                foreach (var row in _rows)
                {
                    int rowIndex = dataGridViewGames.Rows.Add();
                    var gridRow = dataGridViewGames.Rows[rowIndex];

                    gridRow.Cells["colYearRange"].Value = row.Label;

                    // Attila cell — locked on when it's the only option for this row
                    var attilaCell = (DataGridViewCheckBoxCell)gridRow.Cells["colAttila"];
                    bool attilaLocked = row.AttilaAvailable && !row.Rome2Available;
                    attilaCell.Value = row.AttilaAvailable ? row.AttilaSelected : false;
                    attilaCell.ReadOnly = !row.AttilaAvailable || attilaLocked;
                    attilaCell.Style.BackColor = row.AttilaAvailable ? System.Drawing.SystemColors.Window : System.Drawing.SystemColors.ControlDark;
                    attilaCell.Style.ForeColor = row.AttilaAvailable ? System.Drawing.SystemColors.ControlText : System.Drawing.SystemColors.GrayText;

                    // Rome 2 cell — locked on when it's the only option for this row
                    var rome2Cell = (DataGridViewCheckBoxCell)gridRow.Cells["colRome2"];
                    bool rome2Locked = row.Rome2Available && !row.AttilaAvailable;
                    rome2Cell.Value = row.Rome2Available ? row.Rome2Selected : false;
                    rome2Cell.ReadOnly = !row.Rome2Available || rome2Locked;
                    rome2Cell.Style.BackColor = row.Rome2Available ? System.Drawing.SystemColors.Window : System.Drawing.SystemColors.ControlDark;
                    rome2Cell.Style.ForeColor = row.Rome2Available ? System.Drawing.SystemColors.ControlText : System.Drawing.SystemColors.GrayText;
                }
            }
            finally
            {
                _suppressCellEvents = false;
            }
        }

        /// <summary>
        /// Enforce mutual exclusion: only one game per row may be checked. If the user
        /// checks a cell, uncheck the other cell in the same row. If a game is the only
        /// option for a row, its checkbox is locked on.
        /// </summary>
        private void dataGridViewGames_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_suppressCellEvents || e.RowIndex < 0) return;

            var row = _rows[e.RowIndex];
            var gridRow = dataGridViewGames.Rows[e.RowIndex];

            string colName = dataGridViewGames.Columns[e.ColumnIndex].Name;

            if (colName == "colAttila")
            {
                bool attilaChecked = gridRow.Cells["colAttila"].Value is true;
                if (attilaChecked && row.Rome2Available)
                {
                    _suppressCellEvents = true;
                    gridRow.Cells["colRome2"].Value = false;
                    _suppressCellEvents = false;
                }
            }
            else if (colName == "colRome2")
            {
                bool rome2Checked = gridRow.Cells["colRome2"].Value is true;
                if (rome2Checked && row.AttilaAvailable)
                {
                    _suppressCellEvents = true;
                    gridRow.Cells["colAttila"].Value = false;
                    _suppressCellEvents = false;
                }
            }
        }

        private void dataGridViewGames_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dataGridViewGames.IsCurrentCellDirty)
                dataGridViewGames.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void dataGridViewGames_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _rows.Count) return;

            var row = _rows[e.RowIndex];
            if (e.ColumnIndex == dataGridViewGames.Columns["colAttila"].Index)
                e.ToolTipText = $"Attila: {row.ScreenName}";
            else if (e.ColumnIndex == dataGridViewGames.Columns["colRome2"].Index)
                e.ToolTipText = $"Rome 2: {row.ScreenName}";
        }
        private void btnOk_Click(object sender, EventArgs e)
        {
            Result.Clear();
            for (int i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                var gridRow = dataGridViewGames.Rows[i];
                row.AttilaSelected = gridRow.Cells["colAttila"].Value is true;
                row.Rome2Selected = gridRow.Cells["colRome2"].Value is true;
                Result.Add(row);
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        // ── Static helpers: build the year-range rows from mapper folders ──

        /// <summary>
        /// Scans both game subdirectories for mapper folders matching the playthrough tag,
        /// reads each folder's TimePeriod.xml, and builds a list of distinct year ranges
        /// annotated with which games cover each range. Selections are initialized from
        /// MapperSelectionsStore (Attila is the default when both games cover a range).
        /// </summary>
        public static List<YearRangeRow> BuildRows(string playthroughTag)
        {
            string tagToFind = playthroughTag == "Custom" ? client.ModOptions.GetSelectedCustomMapper() : playthroughTag;
            if (string.IsNullOrEmpty(tagToFind)) return new List<YearRangeRow>();

            // Collect (game, folder, startYear, endYear, screenName) for every matching mapper folder.
            var entries = new List<(string Game, string Folder, int StartYear, int EndYear, string ScreenName)>();

            foreach (string game in MapperSelectionsStore.SupportedGames)
            {
                foreach (var dir in UnitMappers_BETA.GetMapperDirectories(game))
                {
                    string tagFile = Path.Combine(dir, "tag.txt");
                    if (!File.Exists(tagFile) ||
                        !File.ReadAllText(tagFile).Trim().Equals(tagToFind, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var (startYear, endYear) = ReadTimePeriod(dir);
                    entries.Add((game, Path.GetFileName(dir), startYear, endYear, GetModScreenName(dir)));
                }
            }

            if (!entries.Any()) return new List<YearRangeRow>();

            // Build distinct year-range boundaries from all entries.
            var boundaries = entries.Select(e => e.StartYear)
                .Concat(entries.Select(e => e.EndYear + 1))
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            // Create contiguous ranges from consecutive boundaries.
            var ranges = new List<(int Start, int End)>();
            for (int i = 0; i < boundaries.Count - 1; i++)
            {
                int start = boundaries[i];
                int end = boundaries[i + 1] - 1;
                if (start <= end) ranges.Add((start, end));
            }

            // For each range, determine which games have a mapper folder covering it.
            var rows = new List<YearRangeRow>();
            var storedSelections = MapperSelectionsStore.GetSelectionsForTag(playthroughTag);
            var storedAttilaFolders = storedSelections.Where(s => s.Game.Equals("Attila", StringComparison.OrdinalIgnoreCase)).Select(s => s.Folder).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var storedRome2Folders = storedSelections.Where(s => s.Game.Equals("Rome2", StringComparison.OrdinalIgnoreCase)).Select(s => s.Folder).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var (start, end) in ranges)
            {
                var attilaCovers = entries.Where(e => e.Game == "Attila" && e.StartYear <= start && e.EndYear >= end).ToList();
                var rome2Covers = entries.Where(e => e.Game == "Rome2" && e.StartYear <= start && e.EndYear >= end).ToList();

                // Skip ranges where neither game has coverage
                if (!attilaCovers.Any() && !rome2Covers.Any()) continue;

                var row = new YearRangeRow
                {
                    StartYear = start,
                    EndYear = end,
                    AttilaAvailable = attilaCovers.Any(),
                    Rome2Available = rome2Covers.Any(),
                    AttilaFolder = attilaCovers.FirstOrDefault().Folder ?? string.Empty,
                    Rome2Folder = rome2Covers.FirstOrDefault().Folder ?? string.Empty,
                    ScreenName = attilaCovers.FirstOrDefault().ScreenName ?? rome2Covers.FirstOrDefault().ScreenName ?? string.Empty,
                };

                // Default selection: Attila wins when both cover (D1). Stored selection overrides.
                bool attilaStored = attilaCovers.Any() && storedAttilaFolders.Contains(row.AttilaFolder);
                bool rome2Stored = rome2Covers.Any() && storedRome2Folders.Contains(row.Rome2Folder);

                if (attilaStored && !rome2Stored)
                {
                    row.AttilaSelected = true;
                    row.Rome2Selected = false;
                }
                else if (rome2Stored && !attilaStored)
                {
                    row.Rome2Selected = true;
                    row.AttilaSelected = false;
                }
                else if (attilaStored && rome2Stored)
                {
                    // Both stored — Attila wins (D1)
                    row.AttilaSelected = true;
                    row.Rome2Selected = false;
                }
                else
                {
                    // No stored selection — Attila default when both, else the only one
                    row.AttilaSelected = row.AttilaAvailable;
                    row.Rome2Selected = !row.AttilaAvailable && row.Rome2Available;
                }

                rows.Add(row);
            }

            return rows;
        }

        /// <summary>Reads StartDate/EndDate from Time Period.xml or TimePeriod.xml. Defaults to 0/9999.</summary>
        private static (int startYear, int endYear) ReadTimePeriod(string mapperPath)
        {
            string timePeriodPath = Path.Combine(mapperPath, "Time Period.xml");
            if (!File.Exists(timePeriodPath))
                timePeriodPath = Path.Combine(mapperPath, "TimePeriod.xml");
            if (!File.Exists(timePeriodPath)) return (0, 9999);

            try
            {
                var doc = new XmlDocument();
                doc.Load(timePeriodPath);
                string startStr = doc.SelectSingleNode("//StartDate")?.InnerText ?? "Default";
                string endStr = doc.SelectSingleNode("//EndDate")?.InnerText ?? "Default";
                int startYear = int.TryParse(startStr, out int s) ? s : 0;
                int endYear = int.TryParse(endStr, out int e) ? e : 9999;
                return (startYear, endYear);
            }
            catch
            {
                return (0, 9999);
            }
        }

        private static string GetModScreenName(string mapperPath)
        {
            string modsPath = Path.Combine(mapperPath, "Mods.xml");
            if (!File.Exists(modsPath)) return string.Empty;
            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(modsPath);
                if (xmlDoc.DocumentElement == null) return string.Empty;
                var screenNames = new List<string>();
                foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
                {
                    string? sn = node.Attributes?["screen_name"]?.Value;
                    if (!string.IsNullOrEmpty(sn)) screenNames.Add(sn);
                    foreach (XmlNode sub in node.ChildNodes)
                    {
                        string? subSn = sub.Attributes?["screen_name"]?.Value;
                        if (!string.IsNullOrEmpty(subSn)) screenNames.Add(subSn);
                    }
                }
                return string.Join(", ", screenNames.Distinct(StringComparer.OrdinalIgnoreCase));
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}