using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using CrusaderWars.Tests.TestHelpers;
using CrusaderWars.unit_mapper;

namespace CrusaderWars.Tests.UnitMappers
{
    public class UnitMappers_BETATests : IDisposable
    {
        private readonly TempTestEnvironment _env;

        public UnitMappers_BETATests()
        {
            _env = new TempTestEnvironment();
        }

        [Fact]
        public void GetLoadedUnitMapperString_ReturnsFireForgedEmpire_WhenBookmarksPlusSelected()
        {
            // Arrange
            _env.CreateUnitMappersXml(bookmarksPlus: true);
            var mapperDir = _env.CreateBookmarksPlusMapperDir();
            _env.CreateFireforgedEmpireLocDir();

            // Temporarily override the UnitMappers.xml path for testing
            var originalPath = Environment.GetEnvironmentVariable("CC_UNITMAPPERS_PATH");
            Environment.SetEnvironmentVariable("CC_UNITMAPPERS_PATH", _env.UnitMappersPath);

            try
            {
                // Act
                UnitMappers_BETA.ActivePlaythroughTag = "BookmarksPlus";
                var result = UnitMappers_BETA.GetLoadedUnitMapperString();

                // Assert
                Assert.Equal("FIRE FORGED EMPIRE", result);
            }
            finally
            {
                if (originalPath == null)
                    Environment.SetEnvironmentVariable("CC_UNITMAPPERS_PATH", null);
                else
                    Environment.SetEnvironmentVariable("CC_UNITMAPPERS_PATH", originalPath);
            }
        }

        [Fact]
        public void GetLoadedUnitMapperString_ReturnsEmptyString_WhenNoMapperSelected()
        {
            // Arrange
            _env.CreateUnitMappersXml(); // All false

            // Act
            UnitMappers_BETA.ActivePlaythroughTag = "";
            var result = UnitMappers_BETA.GetLoadedUnitMapperString();

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void GetUnitMappersModsCollectionFromTag_ReturnsRequiredMods_ForBookmarksPlus()
        {
            // Arrange
            _env.CreateUnitMappersXml(bookmarksPlus: true);
            var mapperDir = _env.CreateBookmarksPlusMapperDir();

            // Act
                var (requiredMods, submods, ck3ModFileNames) = UnitMappers_BETA.GetUnitMappersModsCollectionFromTag("BookmarksPlus");

            // Assert
            Assert.Single(requiredMods);
            Assert.Equal("ufc_2933252806.mod", requiredMods[0].FileName);
            Assert.Empty(submods);
        }

        [Fact]
        public void GetUnitMappersModsCollectionFromTag_ReturnsEmpty_WhenMapperNotFound()
        {
            // Arrange
            _env.CreateUnitMappersXml(); // All false

            // Act
                var (requiredMods, submods, ck3ModFileNames) = UnitMappers_BETA.GetUnitMappersModsCollectionFromTag("BookmarksPlus");

            // Assert
            Assert.Empty(requiredMods);
            Assert.Empty(submods);
        }

        [Fact]
        public void GetUnitMappersModsCollectionFromTag_ReturnsRequiredMods_ForCustomMapper()
        {
            // Arrange
            _env.CreateUnitMappersXml(custom: true);
            var customMapperDir = Path.Combine(_env.UnitMappersPath, "CustomMapper");
            Directory.CreateDirectory(customMapperDir);
            Directory.CreateDirectory(Path.Combine(customMapperDir, "Factions"));
            File.WriteAllText(Path.Combine(customMapperDir, "tag.txt"), "CustomMapper");
            File.WriteAllText(Path.Combine(customMapperDir, "Mods.xml"),
                "<?xml version=\"1.0\"?><Mods><Mod>custom_mod.mod</Mod></Mods>");

            // Act
                var (requiredMods, submods, ck3ModFileNames) = UnitMappers_BETA.GetUnitMappersModsCollectionFromTag("Custom");

            // Assert
            Assert.Single(requiredMods);
            Assert.Equal("custom_mod.mod", requiredMods[0].FileName);
        }

        [Fact]
        public void GetUnitMappersModsCollectionFromTag_ParsesCk3ModFileNames_WhenAttributePresent()
        {
            // Arrange
            _env.CreateUnitMappersXml(bookmarksPlus: true);
            var mapperDir = Path.Combine(_env.UnitMappersPath, "OfficialCC_BookmarksPlus_Test");
            Directory.CreateDirectory(mapperDir);
            File.WriteAllText(Path.Combine(mapperDir, "tag.txt"), "BookmarksPlus");
            File.WriteAllText(Path.Combine(mapperDir, "Mods.xml"),
                "<?xml version=\"1.0\"?><Mods ck3_mod_file_names=\"ugc_12345.mod, ck3_awesomemod.mod\"><Mod>ufc_2933252806.mod</Mod></Mods>");

            // Act
            var (requiredMods, submods, ck3ModFileNames) = UnitMappers_BETA.GetUnitMappersModsCollectionFromTag("BookmarksPlus");

            // Assert
            Assert.Equal(2, ck3ModFileNames.Count);
            Assert.Contains("ugc_12345.mod", ck3ModFileNames);
            Assert.Contains("ck3_awesomemod.mod", ck3ModFileNames);
        }

        [Fact]
        public void GetUnitMappersModsCollectionFromTag_ReturnsEmptyCk3ModFileNames_WhenAttributeAbsent()
        {
            // Arrange
            _env.CreateUnitMappersXml(bookmarksPlus: true);
            _env.CreateBookmarksPlusMapperDir();

            // Act
            var (requiredMods, submods, ck3ModFileNames) = UnitMappers_BETA.GetUnitMappersModsCollectionFromTag("BookmarksPlus");

            // Assert
            Assert.Empty(ck3ModFileNames);
        }

        [Fact]
        public void GetUnitMappersModsCollectionFromTag_ReturnsEmptyCk3ModFileNames_WhenAttributeEmpty()
        {
            // Arrange
            _env.CreateUnitMappersXml(bookmarksPlus: true);
            var mapperDir = Path.Combine(_env.UnitMappersPath, "OfficialCC_BookmarksPlus_Test");
            Directory.CreateDirectory(mapperDir);
            File.WriteAllText(Path.Combine(mapperDir, "tag.txt"), "BookmarksPlus");
            File.WriteAllText(Path.Combine(mapperDir, "Mods.xml"),
                "<?xml version=\"1.0\"?><Mods ck3_mod_file_names=\"\"><Mod>ufc_2933252806.mod</Mod></Mods>");

            // Act
            var (requiredMods, submods, ck3ModFileNames) = UnitMappers_BETA.GetUnitMappersModsCollectionFromTag("BookmarksPlus");

            // Assert
            Assert.Empty(ck3ModFileNames);
        }

        [Fact]
        public void GetUnitMappersModsCollectionFromTag_DedupesCk3ModFileNames_CaseInsensitive()
        {
            // Arrange
            _env.CreateUnitMappersXml(bookmarksPlus: true);
            var mapperDir1 = Path.Combine(_env.UnitMappersPath, "OfficialCC_BookmarksPlus_A");
            Directory.CreateDirectory(mapperDir1);
            File.WriteAllText(Path.Combine(mapperDir1, "tag.txt"), "BookmarksPlus");
            File.WriteAllText(Path.Combine(mapperDir1, "Mods.xml"),
                "<?xml version=\"1.0\"?><Mods ck3_mod_file_names=\"ugc_12345.mod\"><Mod>a.mod</Mod></Mods>");

            var mapperDir2 = Path.Combine(_env.UnitMappersPath, "OfficialCC_BookmarksPlus_B");
            Directory.CreateDirectory(mapperDir2);
            File.WriteAllText(Path.Combine(mapperDir2, "tag.txt"), "BookmarksPlus");
            File.WriteAllText(Path.Combine(mapperDir2, "Mods.xml"),
                "<?xml version=\"1.0\"?><Mods ck3_mod_file_names=\"UGC_12345.MOD,ck3_other.mod\"><Mod>b.mod</Mod></Mods>");

            // Act
            var (requiredMods, submods, ck3ModFileNames) = UnitMappers_BETA.GetUnitMappersModsCollectionFromTag("BookmarksPlus");

            // Assert
            Assert.Equal(2, ck3ModFileNames.Count);
            Assert.Contains("ugc_12345.mod", ck3ModFileNames);
            Assert.Contains("ck3_other.mod", ck3ModFileNames);
        }

        public void Dispose()
        {
            _env?.Dispose();
        }
    }
}