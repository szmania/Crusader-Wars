                            string found_heritage_faction = heritage.Attributes?["faction"]?.Value ?? string.Empty;
                            if (!string.IsNullOrEmpty(found_heritage_faction))
                            {
                                heritage_mapping = (found_heritage_faction, currentFile);
                                Program.Logger.Debug($"  - Found/Updated heritage mapping: {HeritageName} -> {heritage_mapping.faction}.");
                            }

                            foreach(XmlNode culture in heritage.ChildNodes)
                            {
