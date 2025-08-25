using System;
using System.Linq;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using System.Collections.Generic;
using System.Text.Json;
using System.IO.Compression;

namespace CrusaderWars.data.save_file
{
    internal static class SaveFile
    {

        internal static void Uncompress()
        {
            string lastSavePath = Properties.Settings.Default.VAR_dir_save.Replace(@"save games", "last_save.ck3");
            string myLastSavePath = @".\data\save_file_data\last_save.zip";
            string gamestatePath = @".\data\save_file_data\gamestate_file\";

            File.Copy(lastSavePath, myLastSavePath, true);

            ReadMetaData(lastSavePath);
            ExtractGamestate(lastSavePath, gamestatePath);
        }

        internal static void Compress()
        {
            
            string editedGamestatePath = @".\data\save_file_data\gamestate";
            string newSaveFilePath = @".\data\save_file_data\battle_results.zip";
            string metadataPath = @".\data\save_file_data\metadata.txt";
            DEFAULTCompressFileWithMetadata(editedGamestatePath, newSaveFilePath, metadataPath);
        }

        internal static void Finish()
        {
            string newSaveFilePath = @".\data\save_file_data\battle_results.zip";
            string lastSavePath = Properties.Settings.Default.VAR_dir_save.Replace("save games", "last_save.ck3");
            string normalSavePath = Properties.Settings.Default.VAR_dir_save + @"\battle_results.ck3";

            File.Copy(newSaveFilePath, lastSavePath, true);
            File.Copy(newSaveFilePath, normalSavePath, true);
            EditContinueGameJson();
            DeleteOlderFiles();
        }

        static void DEFAULTCompressFileWithMetadata(string fileToZip, string zipFilePath, string metadata)
        {
            // Temporary file for creating the zip content
            string tempZipPath = Path.GetTempFileName();

            // Step 1: Create the ZIP file normally
            using (FileStream zipToCreate = new FileStream(tempZipPath, FileMode.Create))
            {
                using (ZipArchive archive = new ZipArchive(zipToCreate, ZipArchiveMode.Create))
                {
                    // Add the actual file to the ZIP archive
                    string fileName = Path.GetFileName(fileToZip);
                    ZipArchiveEntry fileEntry = archive.CreateEntry(fileName);

                    using (Stream fileStream = new FileStream(fileToZip, FileMode.Open, FileAccess.Read))
                    using (Stream entryStream = fileEntry.Open())
                    {
                        fileStream.CopyTo(entryStream);
                    }
                }
            }

            // Step 2: Write metadata and then append the zip content
            using (FileStream finalZipStream = new FileStream(zipFilePath, FileMode.Create, FileAccess.Write))
            {
                // Write metadata
                byte[] metadataBytes = Encoding.UTF8.GetBytes(File.ReadAllText(metadata));
                finalZipStream.Write(metadataBytes, 0, metadataBytes.Length);

                // Append the ZIP data
                using (FileStream tempZipStream = new FileStream(tempZipPath, FileMode.Open, FileAccess.Read))
                {
                    tempZipStream.CopyTo(finalZipStream);
                }
            }

            // Clean up the temporary file
            File.Delete(tempZipPath);
        }

        static void EditContinueGameJson()
        {
            string jsonPath = Properties.Settings.Default.VAR_dir_save.Replace("save games", "continue_game.json");
            string keyToUpdate = "title";  // The key you want to update
            string newValue = "battle_results.ck3";    // The new value for the key

            UpdateJsonKey(jsonPath, keyToUpdate, newValue);
        }

        static void UpdateJsonKey(string jsonFilePath, string keyToUpdate, string newValue)
        {
            // Read the JSON file
            string jsonString = File.ReadAllText(jsonFilePath);

            // Parse JSON into a dictionary
            var jsonDocument = JsonDocument.Parse(jsonString);
            var root = jsonDocument.RootElement;

            // Convert root to a mutable dictionary
            var jsonDictionary = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(root.GetRawText());

            // Check if the key exists
            if (jsonDictionary.ContainsKey(keyToUpdate))
            {
                // Update the key's value
                jsonDictionary[keyToUpdate] = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(newValue));

                // Serialize the dictionary back to JSON
                string updatedJsonString = JsonSerializer.Serialize(jsonDictionary, new JsonSerializerOptions { WriteIndented = true });

                // Write the updated JSON back to the file
                File.WriteAllText(jsonFilePath, updatedJsonString);
            }
            else
            {
                Console.WriteLine($"Key '{keyToUpdate}' not found in JSON file.");
            }
        }

        static void DeleteOlderFiles()
        {
            string myLastSavePath = @".\data\save_file_data\last_save.zip";
            string editedGamestatePath = @".\data\save_file_data\gamestate";
            string gamestatePath = @".\data\save_file_data\gamestate_file\gamestate";
            string newSaveFilePath = @".\data\save_file_data\battle_results.zip";

            string[] files_to_delete = new string[]{myLastSavePath, editedGamestatePath, gamestatePath, newSaveFilePath};
            foreach(string file in files_to_delete)
            {
                if(File.Exists(file))
                    File.Delete(file);
            }

        }


        static void ExtractGamestate(string zipFilePath, string extractPath)
        {
            if (File.Exists(@".\data\save_file_data\gamestate_file\gamestate"))
                File.Delete(@".\data\save_file_data\gamestate_file\gamestate");
            if (File.Exists(@".\data\save_file_data\gamestate"))
                File.Delete(@".\data\save_file_data\gamestate");


                using (FileStream fs = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read))
            {
                long zipDataStart = FindStartOfZipData(fs);

                if (zipDataStart >= 0)
                {
                    using (FileStream zipStream = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read))
                    {
                        zipStream.Seek(zipDataStart, SeekOrigin.Begin);

                        using (ICSharpCode.SharpZipLib.Zip.ZipFile zipFile = new ICSharpCode.SharpZipLib.Zip.ZipFile(zipStream))
                        {
                            foreach (ZipEntry entry in zipFile)
                            {
                                
                                string entryFileName = Path.Combine(extractPath, entry.Name);

                                using (Stream zipStreamEntry = zipFile.GetInputStream(entry))
                                using (FileStream fileStream = File.Create(entryFileName))
                                {
                                    zipStreamEntry.CopyTo(fileStream);
                                }
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("ZIP file signature not found.");
                }
            }

        }


        static long FindStartOfZipData(FileStream fs)
        {
            const int bufferSize = 4096;
            byte[] buffer = new byte[bufferSize];
            int bytesRead;

            while ((bytesRead = fs.Read(buffer, 0, bufferSize)) > 0)
            {
                for (int i = 0; i < bytesRead - 1; i++)
                {
                    if (buffer[i] == 0x50 && buffer[i + 1] == 0x4B)  // Check for 'PK' signature
                    {
                        return fs.Position - (bytesRead - i);
                    }
                }
            }

            return -1;  // Indicate that the signature was not found
        }

        static void ReadMetaData(string myLastSavePath)
        {
            using (StreamReader streamReader = new StreamReader(myLastSavePath))
            using (StreamWriter streamWriter = new StreamWriter(@".\data\save_file_data\metadata.txt", false, Encoding.UTF8))
            {
                streamWriter.NewLine = "\n";
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    if (line == "}")
                    {
                        streamWriter.WriteLine(line);
                        break;
                    }


                    streamWriter.WriteLine(line);

                }
            }
        }


    }
}
