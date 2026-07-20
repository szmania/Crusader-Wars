using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using CrusaderWars.client;
using CrusaderWars.data;
using Xunit;

namespace CrusaderWars.Tests;

public class SecurityTests
{
    [Fact]
    public void XmlValidator_RejectsXXE()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var maliciousXmlPath = Path.Combine(tempDir, "malicious.xml");
        var xsdPath = Path.Combine(Directory.GetCurrentDirectory(), "unit mappers", "schemas", "mods.xsd");

        // Create a malicious XML with an external entity
        var maliciousXmlContent = @"<?xml version=""1.0""?>
<!DOCTYPE foo [
  <!ENTITY xxe SYSTEM ""file:///c:/windows/win.ini"">
]>
<Mods>
  <Mod name=""&xxe;""/>
</Mods>";
        File.WriteAllText(maliciousXmlPath, maliciousXmlContent);

        // Act
        var errors = XmlValidator.Validate(maliciousXmlPath, xsdPath);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains("prohibited in this XML", errors[0], StringComparison.OrdinalIgnoreCase);

        // Cleanup
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void Updater_ApplyUpdate_RejectsZipPathTraversal()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "updater_test");
        var zipPath = Path.Combine(tempDir, "malicious.zip");
        var extractPath = Path.Combine(tempDir, "extract");
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(extractPath);

        // Create a malicious zip file with a path traversal entry
        using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            var entry = archive.CreateEntry(@"..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..\\..-pwned.txt");
            using (var stream = entry.Open())
            using (var writer = new StreamWriter(stream))
            {
                writer.Write("pwned");
            }
        }

        var updaterForm = new CWUpdater.AutoUpdater();

        // Act & Assert
        var ex = Assert.ThrowsAsync<IOException>(() => updaterForm.ApplyUpdate(zipPath, extractPath));
        Assert.Contains("traversing outside the target directory", ex.Result.Message);


        // Cleanup
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void Updater_CreateShortcut_EscapesBatchFileArguments()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var shortcutPath = Path.Combine(tempDir, "test.lnk");
        var targetPath = Path.Combine(tempDir, "game.exe");
        var workingDir = tempDir;
        var arguments = "used_mods.txt & echo pwned > pwned.txt";
        var description = "test";

        var mainFile = new HomePage();

        // Act
        // This will create a .bat file as a fallback
        mainFile.GetType().GetMethod("CreateShortcut", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(mainFile, new object[] { shortcutPath, targetPath, workingDir, arguments, description });

        var batchPath = Path.Combine(tempDir, "test.bat");
        var batchContent = File.ReadAllText(batchPath);

        // Assert
        Assert.DoesNotContain("echo pwned", batchContent);
        Assert.Contains("\"used_mods.txt & echo pwned > pwned.txt\"", batchContent);

        // Cleanup
        Directory.Delete(tempDir, true);
    }
}