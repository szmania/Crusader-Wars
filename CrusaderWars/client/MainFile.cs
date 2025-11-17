                    Label messageLabel = new Label
                    {
                        Text = "River, Strait, and Coastal Battles!\n\n" +
                               "• Armies crossing rivers, straits, or fighting in coastal provinces will now battle on unique, immersive maps that reflect the terrain.\n" +
                               "----------------------------------------------------------\n\n" +
                               "Prisoners of War & Slain in Battle!\n\n" +
                               "• Characters can now be slain or taken prisoner on the battlefield, with outcomes influenced by their prowess, traits, and the battle's result.\n" +
                               "----------------------------------------------------------\n\n" +
                               "New Optional Sub-Mod for Medieval Playthroughs for the Far East!\n\n" +
                               "• Added support for 'Dahan China' for the Medieval playthroughs.\n" +
                               "• Download it here: https://steamcommunity.com/workshop/filedetails/?id=2826656101\n" +
                               "• And here: https://steamcommunity.com/sharedfiles/filedetails/?id=1559011232\n" +
                               "----------------------------------------------------------\n\n" +
                               "New Required Mods for AGOT Playthrough!\n\n" +
                               "For a more immersive and authentic AGOT experience, the following Total War: Attila mods are now required:\n" +
                               "• Medieval Kingdoms 1212 AD Models Pack 1.v2\n" +
                               "  Download it here: https://steamcommunity.com/workshop/filedetails/?id=1429140619\n" +
                               "• Medieval Kingdoms 1212 AD Models Pack 5\n" +
                               "  Download it here: https://steamcommunity.com/workshop/filedetails/?id=1592154821\n" +
                               "• Medieval Kingdoms 1212AD - Custom cities beta\n" +
                               "  Download it here: https://steamcommunity.com/workshop/filedetails/?id=3010246623\n" +
                               "----------------------------------------------------------\n\n" +
                               "New Optional Sub-Mod for The Fallen Eagle!\n\n" +
                               "• Added support for 'Fall of the Eagles' for the Late-Roman The Fallen Eagle playthrough.\n" +
                               "• Download it here: https://steamcommunity.com/workshop/filedetails/?id=434826744\n" +
                               "----------------------------------------------------------\n\n" +
                               "Support for Optional Sub-Mods!\n\n" +
                               "• Added support for 'Ice and Fire Total War: War for Westeros' for the AGOT Playthrough.\n" +
                               "• Download it here: https://www.moddb.com/mods/total-war-ice-fire-book-inspired-mod\n" +
                               "----------------------------------------------------------\n\n" +
                               "New Required Mod for Medieval Playthroughs!\n\n" +
                               "• The High, Late, and Renaissance medieval playthroughs now require the 'Medieval Kingdoms 1212AD - Custom cities beta' mod.\n" +
                               "• Download it here: https://steamcommunity.com/workshop/filedetails/?id=3010246623\n",
                        Font = new Font("Microsoft Sans Serif", 10f),
                        AutoSize = true,
                        MaximumSize = new Size(scrollPanel.ClientSize.Width - 25, 0),
                        Location = new Point(0, 0)
                    };
                    scrollPanel.Controls.Add(messageLabel);

                    LinkLabel appLink = new LinkLabel
