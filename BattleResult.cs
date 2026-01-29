        // Update the living file with the fates of characters
        public static void EditLivingFile(Dictionary<string, CharacterFate> characterFates, string playerID, string playerHeirID)
        {
            // Read all lines from Living.txt
            string[] lines = File.ReadAllLines(DataSearch.LivingFilePath);
            List<string> updatedLines = new List<string>();

            // Keep track of slain characters and their successors
            Dictionary<string, string> slainToSuccessor = new Dictionary<string, string>();
            
            // Map slain characters to their successors
            foreach (var fate in characterFates)
            {
                if (fate.Value == CharacterFate.Slain)
                {
                    string slainId = fate.Key;
                    // For player character, use playerHeirID
                    if (slainId == playerID)
                    {
                        slainToSuccessor[slainId] = playerHeirID;
                    }
                    // For other characters, we might need additional logic
                    // For now, we'll leave them out or handle them differently
                }
            }

            // Process each line
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                
                // Check if this line starts a character block
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    string charId = line.Substring(1, line.Length - 2);
                    
                    // If this character was slain, skip the entire block
                    if (characterFates.ContainsKey(charId) && 
                        characterFates[charId] == CharacterFate.Slain)
                    {
                        // Skip until we reach the end of this character block
                        while (i < lines.Length && !lines[i].Trim().Equals("[end]"))
                        {
                            i++;
                        }
                        // Skip the [end] line too
                        if (i < lines.Length) i++;
                        // Continue to next iteration without adding anything
                        continue;
                    }
                }
                
                // Check if this line is a court_data block with an employer field
                if (line.Trim().StartsWith("court_data="))
                {
                    // Parse the court_data line to find employer field
                    string updatedLine = line;
                    int employerIndex = line.IndexOf("employer=");
                    
                    if (employerIndex != -1)
                    {
                        // Extract the employer ID
                        int employerStart = employerIndex + 9; // Length of "employer="
                        int employerEnd = line.IndexOf(',', employerStart);
                        if (employerEnd == -1) employerEnd = line.IndexOf('}', employerStart);
                        if (employerEnd == -1) employerEnd = line.Length;
                        
                        string employerId = line.Substring(employerStart, employerEnd - employerStart).Trim();
                        
                        // If this employer was slain, update to their successor
                        if (slainToSuccessor.ContainsKey(employerId))
                        {
                            string successorId = slainToSuccessor[employerId];
                            updatedLine = line.Substring(0, employerStart) + successorId + line.Substring(employerEnd);
                        }
                    }
                    
                    updatedLines.Add(updatedLine);
                }
                else
                {
                    // Add the line to our updated content
                    updatedLines.Add(line);
                }
            }

            // Write the updated content back to the file
            File.WriteAllLines(DataSearch.LivingFilePath, updatedLines.ToArray());
        }
