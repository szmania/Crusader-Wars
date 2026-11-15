using System;
using System.IO;
using System.Linq;
using Xunit;
using CrusaderWars.data;

namespace CrusaderWars.Tests.Migration
{
    /// <summary>
    /// Section 18 Phase D (D3): tests for MigrationService.MigrateUnitsCardsNamesDirectory().
    ///
    /// The production method operates on the CWD-relative path ".\data\units_cards_names" and
    /// logs via Program.Logger (also CWD-relative), so each test swaps the process CWD into a
    /// disposable temp root, runs the REAL migration code, and restores the CWD afterwards.
    ///
    /// These tests exercise the actual production migration, not a simulation.
    ///
    /// NOTE: xUnit may run test classes in parallel. Because these tests mutate the process-wide
    /// CWD, the class is placed in a dedicated collection with parallelism disabled.
    /// </summary>
    [CollectionDefinition(nameof(UnitsCardsNamesMigrationCollection), DisableParallelization = true)]
    public class UnitsCardsNamesMigrationCollection { }

    [Collection(nameof(UnitsCardsNamesMigrationCollection))]
    public class UnitsCardsNamesMigrationTests : IDisposable
    {
        private readonly string _root;
        private readonly string _originalCwd;

        public UnitsCardsNamesMigrationTests()
        {
            _originalCwd = Directory.GetCurrentDirectory();
            _root = Path.Combine(Path.GetTempPath(), $"CC_MigrationTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_root);
            Directory.SetCurrentDirectory(_root);
        }

        public void Dispose()
        {
            Directory.SetCurrentDirectory(_originalCwd);
            try
            {
                if (Directory.Exists(_root))
                    Directory.Delete(_root, recursive: true);
            }
            catch
            {
                // Best-effort cleanup.
            }
        }

        private string UnitsCardsNamesRoot => Path.Combine(_root, "data", "units_cards_names");
        private string AttilaDir => Path.Combine(UnitsCardsNamesRoot, "attila");
        private string Rome2Dir => Path.Combine(UnitsCardsNamesRoot, "rome_2");

        private static void CreateLocFolder(string parent, string name, string fileMarker)
        {
            var dir = Path.Combine(parent, name);
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "unit_cards.loc"), fileMarker);
        }

        [Fact]
        public void Migrate_MovesLegacyFlatFolders_IntoAttila()
        {
            // Arrange: legacy flat layout (no attila/ dir yet).
            CreateLocFolder(UnitsCardsNamesRoot, "mk1212", "mk1212 content");
            CreateLocFolder(UnitsCardsNamesRoot, "fireforged empire", "fireforged content");
            CreateLocFolder(UnitsCardsNamesRoot, "Custom_MyMapper", "custom content");

            // Act
            MigrationService.MigrateUnitsCardsNamesDirectory();

            // Assert: every legacy folder moved under attila\ with contents intact.
            Assert.True(Directory.Exists(AttilaDir), "attila/ should exist after migration");
            Assert.True(Directory.Exists(Rome2Dir), "rome_2/ should exist after migration");

            Assert.True(Directory.Exists(Path.Combine(AttilaDir, "mk1212")));
            Assert.True(Directory.Exists(Path.Combine(AttilaDir, "fireforged empire")));
            Assert.True(Directory.Exists(Path.Combine(AttilaDir, "Custom_MyMapper")));

            Assert.Equal("mk1212 content", File.ReadAllText(Path.Combine(AttilaDir, "mk1212", "unit_cards.loc")));
            Assert.Equal("fireforged content", File.ReadAllText(Path.Combine(AttilaDir, "fireforged empire", "unit_cards.loc")));
            Assert.Equal("custom content", File.ReadAllText(Path.Combine(AttilaDir, "Custom_MyMapper", "unit_cards.loc")));

            // Sources no longer exist at the flat root.
            Assert.False(Directory.Exists(Path.Combine(UnitsCardsNamesRoot, "mk1212")));
            Assert.False(Directory.Exists(Path.Combine(UnitsCardsNamesRoot, "fireforged empire")));
            Assert.False(Directory.Exists(Path.Combine(UnitsCardsNamesRoot, "Custom_MyMapper")));
        }

        [Fact]
        public void Migrate_NeverMovesAttilaOrRome2Folders()
        {
            // Arrange: attila/ and rome_2/ already exist with content; a legacy folder too.
            CreateLocFolder(AttilaDir, "already_migrated", "attila content");
            CreateLocFolder(Rome2Dir, "rome_mapper", "rome content");
            CreateLocFolder(UnitsCardsNamesRoot, "legacy_flat", "legacy content");

            // Act
            MigrationService.MigrateUnitsCardsNamesDirectory();

            // Assert: attila/rome_2 content untouched; only the legacy folder moved.
            Assert.Equal("attila content", File.ReadAllText(Path.Combine(AttilaDir, "already_migrated", "unit_cards.loc")));
            Assert.Equal("rome content", File.ReadAllText(Path.Combine(Rome2Dir, "rome_mapper", "unit_cards.loc")));

            Assert.True(Directory.Exists(Path.Combine(AttilaDir, "legacy_flat")), "legacy folder should move into attila/");
            Assert.False(Directory.Exists(Path.Combine(UnitsCardsNamesRoot, "legacy_flat")));
        }

        [Fact]
        public void Migrate_IsIdempotent_SecondRunIsNoOp()
        {
            // Arrange
            CreateLocFolder(UnitsCardsNamesRoot, "mk1212", "mk1212 content");

            // Act: run migration twice.
            MigrationService.MigrateUnitsCardsNamesDirectory();
            MigrationService.MigrateUnitsCardsNamesDirectory();

            // Assert: folder still in attila\ exactly once, contents intact, no duplicates.
            var attilaSubdirs = Directory.GetDirectories(AttilaDir)
                .Select(Path.GetFileName)
                .ToList();

            Assert.Single(attilaSubdirs.Where(n => n == "mk1212"));
            Assert.Equal("mk1212 content", File.ReadAllText(Path.Combine(AttilaDir, "mk1212", "unit_cards.loc")));
            Assert.False(Directory.Exists(Path.Combine(UnitsCardsNamesRoot, "mk1212")));
        }

        [Fact]
        public void Migrate_SkipsDestination_WhenFolderAlreadyExistsThere()
        {
            // Arrange: legacy folder at root AND a same-named folder already inside attila/.
            CreateLocFolder(UnitsCardsNamesRoot, "conflict_mapper", "legacy version");
            CreateLocFolder(AttilaDir, "conflict_mapper", "attila version");

            // Act
            MigrationService.MigrateUnitsCardsNamesDirectory();

            // Assert: destination content wins (skip, no data loss); legacy source remains.
            Assert.Equal("attila version", File.ReadAllText(Path.Combine(AttilaDir, "conflict_mapper", "unit_cards.loc")));
            Assert.True(Directory.Exists(Path.Combine(UnitsCardsNamesRoot, "conflict_mapper")),
                "legacy source should remain when the destination already exists");
            Assert.Equal("legacy version", File.ReadAllText(Path.Combine(UnitsCardsNamesRoot, "conflict_mapper", "unit_cards.loc")));
        }

        [Fact]
        public void Migrate_CreatesGameDirs_WhenNoLegacyFoldersExist()
        {
            // Arrange: empty units_cards_names root (fresh install shape).
            // Act
            MigrationService.MigrateUnitsCardsNamesDirectory();

            // Assert: both game dirs created, nothing else.
            Assert.True(Directory.Exists(AttilaDir));
            Assert.True(Directory.Exists(Rome2Dir));
            Assert.Empty(Directory.GetDirectories(AttilaDir));
            Assert.Empty(Directory.GetDirectories(Rome2Dir));
        }
    }
}
