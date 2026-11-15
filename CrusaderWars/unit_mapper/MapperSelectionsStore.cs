using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using CrusaderWars.client;
using CrusaderWars.mod_manager;

namespace CrusaderWars.unit_mapper
{
    /// <summary>
    /// Load/save for .\settings\MapperSelections.xml (design 5.15.2 / 5.15.10).
    /// In-memory model shape: tag -> [(Game, Folder), ...]. Threading: UI thread only —
    /// every caller (Options form, UC_UnitMapper, SetPlaythrough) is a UI-thread path,
    /// consistent with every other settings file in this app.
    /// </summary>
    internal static class MapperSelectionsStore
    {
        // Games recognized by the MapperSelection/Folder/@Game enum in the XSD. Kept
        // in one place so availability checks and the folder dialog enumerate the same set.
        public static readonly string[] SupportedGames = { "attila", "rome_2" };

        private static Dictionary<string, List<(string Game, string Folder)>>? _selections;
        private static bool _dirty;

        private static readonly string FilePath = @".\settings\MapperSelections.xml";
        private static readonly string XsdPath = @".\settings\schemas\MapperSelections.xsd";

        private static bool _isRetryingLoad;

        /// <summary>Load-or-create. Missing file -> empty default written; corrupt file ->
        /// ValidationHelper.HandleValidationFailure recreates the empty default (same recovery
        /// ladder as Options.xml / UnitMappers.xml). Never throws to callers.</summary>
        public static void EnsureLoaded()
        {
            if (_selections != null) return;

            try
            {
                if (!File.Exists(FilePath))
                {
                    Program.Logger.Debug("MapperSelections.xml not found. Creating empty default.");
                    CreateDefaultFile();
                }
                else
                {
                    var validationErrors = XmlValidator.Validate(FilePath, XsdPath);
                    if (validationErrors.Count > 0)
                    {
                        if (_isRetryingLoad)
                        {
                            MessageBox.Show("The default MapperSelections.xml file is also invalid. The application will now exit.",
                                "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            Application.Exit();
                            return;
                        }

                        bool reset = ValidationHelper.HandleValidationFailure(FilePath, "MapperSelections.xml",
                            validationErrors, CreateDefaultFile);
                        if (reset)
                        {
                            _isRetryingLoad = true;
                            EnsureLoaded(); // re-run against the fresh default
                            _isRetryingLoad = false;
                            return;
                        }
                        return; // user chose to exit
                    }
                }

                _selections = ReadModel();
                _dirty = false;
            }
            catch (Exception ex)
            {
                Program.Logger.Log(ex);
                // A read failure must never take the battle path down: fall back to an
                // empty model, which means the D1 Attila-wins default everywhere.
                _selections = new Dictionary<string, List<(string, string)>>(StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>Read path (battle-time resolution + dialog pre-checks). Returns an
        /// empty list when the tag is absent — callers apply the D1 Attila-wins default.</summary>
        public static List<(string Game, string Folder)> GetSelectionsForTag(string tag)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(tag) || _selections == null ||
                !_selections.TryGetValue(tag, out var entries))
                return new List<(string Game, string Folder)>();

            return entries;
        }

        /// <summary>Write path (folder-dialog OK, custom-dropdown change). Custom tag uses
        /// replace semantics per 5.15.2 rule 4: removes any existing Folder whose text
        /// equals the new folder, then appends (Game, Folder). Ordinary tags upsert per
        /// (Game, Folder); duplicate entries within a tag are impossible.</summary>
        public static void SetSelection(string tag, string game, string folder)
        {
            if (string.IsNullOrEmpty(tag) || string.IsNullOrEmpty(game) || string.IsNullOrEmpty(folder)) return;
            if (!IsValidGame(game))
            {
                Program.Logger.Debug($"SetSelection rejected: unknown game '{game}' for tag '{tag}'.");
                return;
            }
            if (!IsValidFolderName(folder))
            {
                Program.Logger.Debug($"SetSelection rejected: unsafe folder name '{folder}' for tag '{tag}'.");
                return;
            }

            EnsureLoaded();
            if (_selections == null) return;

            var key = NormalizeTagKey(tag);
            if (key == null) return;

            if (!_selections.TryGetValue(key, out var entries))
            {
                entries = new List<(string, string)>();
                _selections[key] = entries;
            }

            // Custom tags: replace semantics — a selected custom tag maps to exactly one
            // (Game, Folder) entry, so any existing entry naming the same folder is dropped.
            bool isCustom = key.Equals("Custom", StringComparison.OrdinalIgnoreCase);
            entries.RemoveAll(e => isCustom && string.Equals(e.Folder, folder, StringComparison.OrdinalIgnoreCase));

            if (entries.Any(e => string.Equals(e.Game, game, StringComparison.OrdinalIgnoreCase)
                              && string.Equals(e.Folder, folder, StringComparison.OrdinalIgnoreCase)))
                return; // already present — no dirty flag so Flush stays a no-op

            entries.Add((game, folder));
            _dirty = true;
        }

        /// <summary>Removes one (Game, Folder) entry (folder dialog unchecking a row).
        /// No-op if absent.</summary>
        public static void RemoveSelection(string tag, string game, string folder)
        {
            if (string.IsNullOrEmpty(tag) || string.IsNullOrEmpty(game) || string.IsNullOrEmpty(folder)) return;

            EnsureLoaded();
            if (_selections == null) return;

            var key = NormalizeTagKey(tag);
            if (key == null || !_selections.TryGetValue(key, out var entries)) return;

            int removed = entries.RemoveAll(e => string.Equals(e.Game, game, StringComparison.OrdinalIgnoreCase)
                                             && string.Equals(e.Folder, folder, StringComparison.OrdinalIgnoreCase));
            if (removed > 0)
            {
                if (!entries.Any()) _selections.Remove(key); // keep the file minimal
                _dirty = true;
            }
        }

        /// <summary>Writes to disk only when dirty; clears the flag. Called by
        /// Options.FormClosing (alongside WriteUnitMappersOptions) and after every
        /// immediate-persist write.</summary>
        public static void Flush()
        {
            if (!_dirty || _selections == null) return;

            try
            {
                // Ensure the settings directory exists before writing
                string? dir = Path.GetDirectoryName(Path.GetFullPath(FilePath));
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                WriteModelAtomic(_selections);
                _dirty = false;
                Program.Logger.Debug("MapperSelections.xml flushed.");
            }
            catch (Exception ex)
            {
                Program.Logger.Log(ex);
                MessageBox.Show($"Error saving mapper folder selections: {ex.Message}",
                    "Crusader Conflicts: Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        internal static void ResetForTests()
        {
            _selections = null;
            _dirty = false;
            _isRetryingLoad = false;
        }

        private static string? NormalizeTagKey(string tag)
        {
            // Tags are persisted verbatim; only a blank tag is rejected. Case-insensitive
            // lookup is done at read time, so the stored casing stays as the mapper author wrote it.
            return string.IsNullOrWhiteSpace(tag) ? null : tag;
        }

        private static bool IsValidGame(string game) =>
            SupportedGames.Contains(game, StringComparer.OrdinalIgnoreCase);

        /// <summary>Folder values must be bare folder names (design 5.15.10 rule 4).
        /// Rejects path separators, traversal, and absolute-looking values.</summary>
        private static bool IsValidFolderName(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder)) return false;
            return !folder.Contains("/") && !folder.Contains("\\") && !folder.Contains("..") &&
                   !Path.IsPathRooted(folder) && folder.IndexOfAny(Path.GetInvalidPathChars()) < 0;
        }

        private static Dictionary<string, List<(string Game, string Folder)>> ReadModel()
        {
            var model = new Dictionary<string, List<(string, string)>>(StringComparer.OrdinalIgnoreCase);

            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            };

            using (var reader = XmlReader.Create(FilePath, settings))
            {
                while (reader.Read())
                {
                    if (reader.NodeType != XmlNodeType.Element) continue;

                    if (reader.Name == "MapperSelection")
                    {
                        string tag = reader.GetAttribute("Tag") ?? string.Empty;
                        while (reader.Read())
                        {
                            if (reader.NodeType != XmlNodeType.Element) continue;
                            if (reader.Name == "Folder")
                            {
                                string game = reader.GetAttribute("Game") ?? string.Empty;
                                string folder = reader.ReadElementContentAsString() ?? string.Empty;

                                if (string.IsNullOrEmpty(tag) || !IsValidGame(game) || !IsValidFolderName(folder))
                                {
                                    Program.Logger.Debug(
                                        $"Dropping unsafe MapperSelections entry: Tag='{tag}', Game='{game}', Folder='{folder}'.");
                                    continue;
                                }

                                if (!model.TryGetValue(tag, out var entries))
                                {
                                    entries = new List<(string, string)>();
                                    model[tag] = entries;
                                }
                                if (!entries.Any(e => string.Equals(e.Item1, game, StringComparison.OrdinalIgnoreCase)
                                                   && string.Equals(e.Item2, folder, StringComparison.OrdinalIgnoreCase)))
                                    entries.Add((game, folder));
                            }
                            else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "MapperSelection")
                            {
                                break; // next MapperSelection (or EOF)
                            }
                        }
                    }
                }
            }

            return model;
        }

        /// <summary>Serializes the model to a temp file then File.Replace's it into place,
        /// so a crash mid-write can never corrupt the previous good file.</summary>
        private static void WriteModelAtomic(Dictionary<string, List<(string Game, string Folder)>> model)
        {
            string dir = Path.GetDirectoryName(Path.GetFullPath(FilePath))!;
            Directory.CreateDirectory(dir);
            string tmpPath = FilePath + ".tmp";

            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                Encoding = new System.Text.UTF8Encoding(false)
            };

            using (var writer = XmlWriter.Create(tmpPath, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("MapperSelections");

                foreach (var (tag, entries) in model.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
                {
                    writer.WriteStartElement("MapperSelection");
                    writer.WriteAttributeString("Tag", tag);
                    foreach (var (game, folder) in entries.OrderBy(e => e.Game, StringComparer.OrdinalIgnoreCase))
                    {
                        writer.WriteStartElement("Folder");
                        writer.WriteAttributeString("Game", game);
                        writer.WriteString(folder);
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }

            string fullDestPath = Path.GetFullPath(FilePath);
            if (File.Exists(fullDestPath))
                File.Replace(tmpPath, fullDestPath, null);
            else
                File.Move(tmpPath, fullDestPath);
        }

        private static void CreateDefaultFile()
        {
            WriteModelAtomic(new Dictionary<string, List<(string, string)>>(StringComparer.OrdinalIgnoreCase));
            Program.Logger.Debug("Default MapperSelections.xml created (empty).");
        }
    }
}
