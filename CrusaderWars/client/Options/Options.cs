using CrusaderWars.client;
using CrusaderWars.client.Options;
using CrusaderWars.client.WarningMessage;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.Media;
using Control = System.Windows.Forms.Control;
using CrusaderWars.mod_manager;
using CrusaderWars.unit_mapper;
using Timer = System.Windows.Forms.Timer;

namespace CrusaderWars
{
    public partial class Options : Form
    {
        private Timer _pulseTimer;
        private bool _isPulsing = false;
        private bool _pulseState = false;
        private string CK3_Path { get; set; } = string.Empty;
        private string Attila_Path { get; set; } = string.Empty;
        private bool _isModManagerExpanded = true; // Default to expanded
        private bool _unitMappersXmlChanged = false; // Added: To track if UnitMappers.xml was modified

        public Options()
        {
            InitializeComponent();
            this.Icon = Properties.Resources.logo;

            _pulseTimer = new Timer();
            _pulseTimer.Interval = 500;
            _pulseTimer.Tick += PulseTimer_Tick;
        }

        private void PulseTimer_Tick(object? sender, EventArgs e)
        {
            _pulseState = !_pulseState;
            var activePlaythrough = GetActivePlaythrough();

            // Reset all buttons to default non-pulsing state
            Btn_CK3Tab.FlatAppearance.BorderSize = 1;
            Btn_TFETab.FlatAppearance.BorderSize = 1;
            Btn_LOTRTab.FlatAppearance.BorderSize = 1;
            Btn_AGOTTab.FlatAppearance.BorderSize = 1;
            Btn_CustomTab.FlatAppearance.BorderSize = 1;
            Btn_CK3Tab.FlatAppearance.BorderColor = Color.Black;
            Btn_TFETab.FlatAppearance.BorderColor = Color.Black;
            Btn_LOTRTab.FlatAppearance.BorderColor = Color.Black;
            Btn_AGOTTab.FlatAppearance.BorderColor = Color.Black;
            Btn_CustomTab.FlatAppearance.BorderColor = Color.Black;

            // Reset submod button borders on all playthrough tabs
            foreach (var playthrough in new[] { CrusaderKings_Tab, TheFallenEagle_Tab, RealmsInExile_Tab, AGOT_Tab })
            {
                if (playthrough != null)
                {
                    var submodButton = playthrough.Controls.Find("BtnSubmods", true).FirstOrDefault() as Button;
                    if (submodButton != null)
                    {
                        submodButton.FlatAppearance.BorderSize = 0;
                    }
                }
            }


            if (activePlaythrough != null)
            {
                Button? activeButton = null;
                if (activePlaythrough == CrusaderKings_Tab) activeButton = Btn_CK3Tab;
                else if (activePlaythrough == TheFallenEagle_Tab) activeButton = Btn_TFETab;
                else if (activePlaythrough == RealmsInExile_Tab) activeButton = Btn_LOTRTab;
                else if (activePlaythrough == AGOT_Tab) activeButton = Btn_AGOTTab; // Added AGOT tab

                if (activeButton != null)
                {
                    activeButton.FlatAppearance.BorderColor = _pulseState ? Color.FromArgb(255, 215, 0) : Color.FromArgb(255, 165, 0); // Gold/Orange pulse
                    activeButton.FlatAppearance.BorderSize = 2;
                }

                // New logic for submod button pulsation
                var activeSubmods = SubmodManager.GetActiveSubmodsForPlaythrough(activePlaythrough.GetPlaythroughTag());
                if (activeSubmods.Any())
                {
                    var submodButton = activePlaythrough.Controls.Find("BtnSubmods", true).FirstOrDefault() as Button;
                    if (submodButton != null && submodButton.Visible)
                    {
                        submodButton.FlatAppearance.BorderColor = _pulseState ? Color.FromArgb(255, 215, 0) : Color.FromArgb(255, 165, 0); // Gold/Orange pulse
                        submodButton.FlatAppearance.BorderSize = 2;
                    }
                }
            }
            else
            {
                TableLayoutPlaythroughs.Invalidate();
            }
        }

        private UC_UnitMapper? GetActivePlaythrough()
        {
            if (CrusaderKings_Tab != null && CrusaderKings_Tab.GetState())
            {
                return CrusaderKings_Tab;
            }
            if (TheFallenEagle_Tab != null && TheFallenEagle_Tab.GetState())
            {
                return TheFallenEagle_Tab;
            }
            if (RealmsInExile_Tab != null && RealmsInExile_Tab.GetState())
            {
                return RealmsInExile_Tab;
            }
            if (AGOT_Tab != null && AGOT_Tab.GetState()) // Added AGOT tab
            {
                return AGOT_Tab;
            }
            return null;
        }

        private void CloseBtn_Click(object sender, EventArgs e)
        {
            Program.Logger.Debug("Close button clicked, attempting to close Options form.");
            this.Close(); // This will trigger the FormClosing event
        }


        private void Options_Load(object sender, EventArgs e)
        {
            Program.Logger.Debug("Options form loading...");
            General_Tab = new UC_GeneralOptions();
            Units_Tab = new UC_UnitsOptions();
            CandK_Tab = new UC_CommandersAndKnightsOptions();

            SubmodManager.LoadActiveSubmods();
            ReadUnitMappersOptions();
            ReadOptionsFile();
            SetOptionsUIData();
            Status_Refresh();

            if(!string.IsNullOrEmpty(Properties.Settings.Default.VAR_attila_path))
            {
                Program.Logger.Debug("Attila path found. Initializing mod manager...");
                AttilaModManager.SetControlReference(ModManager);
                AttilaModManager.ReadInstalledModsAndPopulateModManager();
            }
            Program.Logger.Debug("Options form loaded.");

            // Set initial state for Mod Manager (expanded by default)
            _isModManagerExpanded = true; // Explicitly set to true for default expanded state
            panel1.Visible = _isModManagerExpanded;
            toggleModManagerButton.Text = "Mod Manager [▲]";
            ToolTip_Options.SetToolTip(toggleModManagerButton, "Click to collapse the Mod Manager. This section shows optional mods.");
        }

        /*##############################################
         *####              MOD OPTIONS             #### 
         *####--------------------------------------####
         *####          Mod options section         ####
         *##############################################
         */
        UserControl General_Tab = null!;
        UserControl Units_Tab = null!;
        UC_CommandersAndKnightsOptions CandK_Tab = null!; // Changed type to UC_CommandersAndKnightsOptions
        private void Btn_GeneralTab_Click(object sender, EventArgs e)
        {
            if (OptionsPanel.Controls.Count > 0 && OptionsPanel.Controls[0] != General_Tab)
                ChangeOptionsTab(General_Tab);
        }

        private void Btn_UnitsTab_Click(object sender, EventArgs e)
        {
            if (OptionsPanel.Controls.Count > 0 && OptionsPanel.Controls[0] != Units_Tab)
                ChangeOptionsTab(Units_Tab);
        }

        private void Btn_CandKTab_Click(object sender, EventArgs e)
        {
            if (OptionsPanel.Controls.Count > 0 && OptionsPanel.Controls[0] != CandK_Tab)
                ChangeOptionsTab(CandK_Tab);
        }

        void ChangeOptionsTab(Control control)
        {
            control.Dock = DockStyle.Fill;
            OptionsPanel.Controls.Clear();
            OptionsPanel.Controls.Add(control);
            control.BringToFront();

            // Define colors
            Color inactiveColor = System.Drawing.Color.FromArgb(128, 53, 0);
            Color activeColor = System.Drawing.Color.FromArgb(140, 87, 63);

            // Reset all buttons
            Btn_GeneralTab.BackgroundImage = null;
            Btn_UnitsTab.BackgroundImage = null;
            Btn_CandKTab.BackgroundImage = null;
            Btn_GeneralTab.BackColor = inactiveColor;
            Btn_UnitsTab.BackColor = inactiveColor;
            Btn_CandKTab.BackColor = inactiveColor;
            Btn_GeneralTab.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            Btn_UnitsTab.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            Btn_CandKTab.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            Btn_GeneralTab.FlatAppearance.BorderSize = 1;
            Btn_UnitsTab.FlatAppearance.BorderSize = 1;
            Btn_CandKTab.FlatAppearance.BorderSize = 1;

            // Highlight active button
            Button? activeButton = null;
            if (control == General_Tab) activeButton = Btn_GeneralTab;
            else if (control == Units_Tab) activeButton = Btn_UnitsTab;
            else if (control == CandK_Tab) activeButton = Btn_CandKTab;

            if (activeButton != null)
            {
                activeButton.BackColor = activeColor;
                activeButton.FlatAppearance.BorderSize = 2;
            }
        }

        //this is to read the options values on the .xml file
        
        private static string GetOptionValue(XmlDocument doc, string optionName, string defaultValue)
        {
            XmlNode? node = doc.SelectSingleNode($"//Option [@name='{optionName}']");
            if (node != null)
            {
                return node.InnerText;
            }
            else
            {
                Program.Logger.Debug($"Option '{optionName}' not found in Options.xml. Creating with default value '{defaultValue}'.");
                XmlElement newOption = doc.CreateElement("Option");
                newOption.SetAttribute("name", optionName);
                newOption.InnerText = defaultValue; // This line sets the InnerText
                doc.DocumentElement?.AppendChild(newOption);
                return defaultValue;
            }
        }
        public static void ReadOptionsFile()
        {
            Program.Logger.Debug("Reading options file...");
            try
            {
                string file = @".\settings\Options.xml";
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(file);
                Program.Logger.Debug("XML options file loaded.");

                ModOptions.optionsValuesCollection.Clear();
                var CloseCK3_Value = GetOptionValue(xmlDoc, "CloseCK3", "Enabled");
                var CloseAttila_Value = GetOptionValue(xmlDoc, "CloseAttila", "Enabled");
                var FullArmies_Value = GetOptionValue(xmlDoc, "FullArmies", "Disabled");
                var TimeLimit_Value = GetOptionValue(xmlDoc, "TimeLimit", "Enabled");
                var BattleMapsSize_Value = GetOptionValue(xmlDoc, "BattleMapsSize", "Dynamic");
                var DefensiveDeployables_Value = GetOptionValue(xmlDoc, "DefensiveDeployables", "Enabled");
                var UnitCards_Value = GetOptionValue(xmlDoc, "UnitCards", "Enabled");
                var LeviesMax_Value = GetOptionValue(xmlDoc, "LeviesMax", "10");
                var RangedMax_Value = GetOptionValue(xmlDoc, "RangedMax", "4");
                var InfantryMax_Value = GetOptionValue(xmlDoc, "InfantryMax", "8");
                var CavalryMax_Value = GetOptionValue(xmlDoc, "CavalryMax", "4");
                var BattleScale_Value = GetOptionValue(xmlDoc, "BattleScale", "100%");
                var AutoScaleUnits_Value = GetOptionValue(xmlDoc, "AutoScaleUnits", "Enabled");
                var SeparateArmies_Value = GetOptionValue(xmlDoc, "SeparateArmies", "Friendly Only");
                var SiegeEnginesInFieldBattles_Value = GetOptionValue(xmlDoc, "SiegeEnginesInFieldBattles", "Enabled");

                // New Commander and Knight wound chances
                var CommanderWoundedChance_Value = GetOptionValue(xmlDoc, "CommanderWoundedChance", "65");
                var CommanderSeverelyInjuredChance_Value = GetOptionValue(xmlDoc, "CommanderSeverelyInjuredChance", "10");
                var CommanderBrutallyMauledChance_Value = GetOptionValue(xmlDoc, "CommanderBrutallyMauledChance", "5");
                var CommanderMaimedChance_Value = GetOptionValue(xmlDoc, "CommanderMaimedChance", "5");
                var CommanderOneLeggedChance_Value = GetOptionValue(xmlDoc, "CommanderOneLeggedChance", "2");
                var CommanderOneEyedChance_Value = GetOptionValue(xmlDoc, "CommanderOneEyedChance", "3");
                var CommanderDisfiguredChance_Value = GetOptionValue(xmlDoc, "CommanderDisfiguredChance", "2");
                var CommanderSlainChance_Value = GetOptionValue(xmlDoc, "CommanderSlainChance", "8");
                var CommanderPrisonerChance_Value = GetOptionValue(xmlDoc, "CommanderPrisonerChance", "60");

                var KnightWoundedChance_Value = GetOptionValue(xmlDoc, "KnightWoundedChance", "65");
                var KnightSeverelyInjuredChance_Value = GetOptionValue(xmlDoc, "KnightSeverelyInjuredChance", "10");
                var KnightBrutallyMauledChance_Value = GetOptionValue(xmlDoc, "KnightBrutallyMauledChance", "5");
                var KnightMaimedChance_Value = GetOptionValue(xmlDoc, "KnightMaimedChance", "5");
                var KnightOneLeggedChance_Value = GetOptionValue(xmlDoc, "KnightOneLeggedChance", "2");
                var KnightOneEyedChance_Value = GetOptionValue(xmlDoc, "KnightOneEyedChance", "3");
                var KnightDisfiguredChance_Value = GetOptionValue(xmlDoc, "KnightDisfiguredChance", "2");
                var KnightSlainChance_Value = GetOptionValue(xmlDoc, "KnightSlainChance", "8");
                var KnightPrisonerChance_Value = GetOptionValue(xmlDoc, "KnightPrisonerChance", "60");

                // Add new OptInPreReleases option
                var OptInPreReleases_Value = GetOptionValue(xmlDoc, "OptInPreReleases", "False");


                xmlDoc.Save(file);
                Program.Logger.Debug("All options read from XML.");

                ModOptions.optionsValuesCollection.Add("CloseCK3", CloseCK3_Value);
                ModOptions.optionsValuesCollection.Add("CloseAttila", CloseAttila_Value);
                ModOptions.optionsValuesCollection.Add("FullArmies", FullArmies_Value);
                ModOptions.optionsValuesCollection.Add("TimeLimit", TimeLimit_Value);
                ModOptions.optionsValuesCollection.Add("BattleMapsSize", BattleMapsSize_Value);
                ModOptions.optionsValuesCollection.Add("DefensiveDeployables", DefensiveDeployables_Value);
                ModOptions.optionsValuesCollection.Add("UnitCards", UnitCards_Value);
                ModOptions.optionsValuesCollection.Add("SeparateArmies", SeparateArmies_Value);
                ModOptions.optionsValuesCollection.Add("LeviesMax", LeviesMax_Value);
                ModOptions.optionsValuesCollection.Add("RangedMax", RangedMax_Value);
                ModOptions.optionsValuesCollection.Add("InfantryMax", InfantryMax_Value);
                ModOptions.optionsValuesCollection.Add("CavalryMax", CavalryMax_Value);
                ModOptions.optionsValuesCollection.Add("BattleScale", BattleScale_Value);
                ModOptions.optionsValuesCollection.Add("AutoScaleUnits", AutoScaleUnits_Value);
                ModOptions.optionsValuesCollection.Add("SiegeEnginesInFieldBattles", SiegeEnginesInFieldBattles_Value);
                ModOptions.optionsValuesCollection.Add("CommanderWoundedChance", CommanderWoundedChance_Value);
                ModOptions.optionsValuesCollection.Add("CommanderSeverelyInjuredChance", CommanderSeverelyInjuredChance_Value);
                ModOptions.optionsValuesCollection.Add("CommanderBrutallyMauledChance", CommanderBrutallyMauledChance_Value);
                ModOptions.optionsValuesCollection.Add("CommanderMaimedChance", CommanderMaimedChance_Value);
                ModOptions.optionsValuesCollection.Add("CommanderOneLeggedChance", CommanderOneLeggedChance_Value);
                ModOptions.optionsValuesCollection.Add("CommanderOneEyedChance", CommanderOneEyedChance_Value);
                ModOptions.optionsValuesCollection.Add("CommanderDisfiguredChance", CommanderDisfiguredChance_Value);
                ModOptions.optionsValuesCollection.Add("CommanderSlainChance", CommanderSlainChance_Value);
                ModOptions.optionsValuesCollection.Add("CommanderPrisonerChance", CommanderPrisonerChance_Value);
                ModOptions.optionsValuesCollection.Add("KnightWoundedChance", KnightWoundedChance_Value);
                ModOptions.optionsValuesCollection.Add("KnightSeverelyInjuredChance", KnightSeverelyInjuredChance_Value);
                ModOptions.optionsValuesCollection.Add("KnightBrutallyMauledChance", KnightBrutallyMauledChance_Value);
                ModOptions.optionsValuesCollection.Add("KnightMaimedChance", KnightMaimedChance_Value);
                ModOptions.optionsValuesCollection.Add("KnightOneLeggedChance", KnightOneLeggedChance_Value);
                ModOptions.optionsValuesCollection.Add("KnightOneEyedChance", KnightOneEyedChance_Value);
                ModOptions.optionsValuesCollection.Add("KnightDisfiguredChance", KnightDisfiguredChance_Value);
                ModOptions.optionsValuesCollection.Add("KnightSlainChance", KnightSlainChance_Value);
                ModOptions.optionsValuesCollection.Add("KnightPrisonerChance", KnightPrisonerChance_Value);
                ModOptions.optionsValuesCollection.Add("OptInPreReleases", OptInPreReleases_Value); // Add new option
                Program.Logger.Debug("Options collection populated.");


            }
            catch (Exception ex)
            {
                Program.Logger.Log(ex);
                MessageBox.Show("Error reading game options. Restart the mod and try again", "Crusader Conflicts: Data Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                Application.Exit();
            }
        }

        void SetOptionsUIData()
        {
            Program.Logger.Debug("Setting options UI data...");
            try
            {
                var CloseCK3_ComboBox = General_Tab.Controls[0].Controls.Find("OptionSelection_CloseCK3", true).FirstOrDefault() as ComboBox;
                var CloseAttila_ComboBox = General_Tab.Controls[0].Controls.Find("OptionSelection_CloseAttila", true).FirstOrDefault() as ComboBox;
                var FullArmies_ComboBox = General_Tab.Controls[0].Controls.Find("OptionSelection_FullArmies", true).FirstOrDefault() as ComboBox;
                var TimeLimit_ComboBox = General_Tab.Controls[0].Controls.Find("OptionSelection_TimeLimit", true).FirstOrDefault() as ComboBox;
                var BattleMapsSize_ComboBox = General_Tab.Controls[0].Controls.Find("OptionSelection_BattleMapsSize", true).FirstOrDefault() as ComboBox;
                var DefensiveDeployables_ComboBox = General_Tab.Controls[0].Controls.Find("OptionSelection_DefensiveDeployables", true).FirstOrDefault() as ComboBox;
                var UnitCards_ComboBox = General_Tab.Controls[0].Controls.Find("OptionSelection_UnitCards", true).FirstOrDefault() as ComboBox;
                var SeparateArmies_ComboBox = General_Tab.Controls[0].Controls.Find("OptionSelection_SeparateArmies", true).FirstOrDefault() as ComboBox;
                var SiegeEnginesInFieldBattles_ComboBox = General_Tab.Controls[0].Controls.Find("OptionSelection_SiegeEngines", true).FirstOrDefault() as ComboBox;

                var LeviesMax_ComboBox = Units_Tab.Controls[0].Controls.Find("OptionSelection_LeviesMax", true).FirstOrDefault() as ComboBox;
                var RangedMax_ComboBox = Units_Tab.Controls[0].Controls.Find("OptionSelection_RangedMax", true).FirstOrDefault() as ComboBox;
                var InfantryMax_ComboBox = Units_Tab.Controls[0].Controls.Find("OptionSelection_InfantryMax", true).FirstOrDefault() as ComboBox;
                var CavalryMax_ComboBox = Units_Tab.Controls[0].Controls.Find("OptionSelection_CavalryMax", true).FirstOrDefault() as ComboBox;

                var BattleScale_ComboBox = Units_Tab.Controls[0].Controls.Find("OptionSelection_BattleSizeScale", true).FirstOrDefault() as ComboBox;
                var AutoScaleUnits_ComboBox = Units_Tab.Controls[0].Controls.Find("OptionSelection_AutoScale", true).FirstOrDefault() as ComboBox;

                // Commander NumericUpDowns
                var numCommanderWounded = CandK_Tab.numCommanderWounded;
                var numCommanderSeverelyInjured = CandK_Tab.numCommanderSeverelyInjured;
                var numCommanderBrutallyMauled = CandK_Tab.numCommanderBrutallyMauled;
                var numCommanderMaimed = CandK_Tab.numCommanderMaimed;
                var numCommanderOneLegged = CandK_Tab.numCommanderOneLegged;
                var numCommanderOneEyed = CandK_Tab.numCommanderOneEyed;
                var numCommanderDisfigured = CandK_Tab.numCommanderDisfigured;
                var numCommanderSlain = CandK_Tab.numCommanderSlain;
                var numCommanderPrisoner = CandK_Tab.numCommanderPrisoner;

                // Knight NumericUpDowns
                var numKnightWounded = CandK_Tab.numKnightWounded;
                var numKnightSeverelyInjured = CandK_Tab.numKnightSeverelyInjured;
                var numKnightBrutallyMauled = CandK_Tab.numKnightBrutallyMauled;
                var numKnightMaimed = CandK_Tab.numKnightMaimed;
                var numKnightOneLegged = CandK_Tab.numKnightOneLegged;
                var numKnightOneEyed = CandK_Tab.numKnightOneEyed;
                var numKnightDisfigured = CandK_Tab.numKnightDisfigured;
                var numKnightSlain = CandK_Tab.numKnightSlain;
                var numKnightPrisoner = CandK_Tab.numKnightPrisoner;


                CloseCK3_ComboBox!.SelectedItem = ModOptions.optionsValuesCollection["CloseCK3"];
                CloseAttila_ComboBox!.SelectedItem = ModOptions.optionsValuesCollection["CloseAttila"];
                FullArmies_ComboBox!.SelectedItem = ModOptions.optionsValuesCollection["FullArmies"];
                TimeLimit_ComboBox!.SelectedItem = ModOptions.optionsValuesCollection["TimeLimit"];
                BattleMapsSize_ComboBox!.SelectedItem = ModOptions.optionsValuesCollection["BattleMapsSize"];
                DefensiveDeployables_ComboBox!.SelectedItem = ModOptions.optionsValuesCollection["DefensiveDeployables"];
                UnitCards_ComboBox!.SelectedItem = ModOptions.optionsValuesCollection["UnitCards"];
                SeparateArmies_ComboBox!.SelectedItem = ModOptions.optionsValuesCollection["SeparateArmies"];
                SiegeEnginesInFieldBattles_ComboBox!.SelectedItem = ModOptions.optionsValuesCollection["SiegeEnginesInFieldBattles"];

                LeviesMax_ComboBox!.SelectedItem = ModOptions.optionsValuesCollection["LeviesMax"];
                RangedMax_ComboBox!.SelectedItem = ModOptions.optionsValuesCollection["RangedMax"];
                InfantryMax_ComboBox!.SelectedItem = ModOptions.optionsValuesCollection["InfantryMax"];
                CavalryMax_ComboBox!.SelectedItem = ModOptions.optionsValuesCollection["CavalryMax"];

                BattleScale_ComboBox!.SelectedItem = ModOptions.optionsValuesCollection["BattleScale"];
                AutoScaleUnits_ComboBox!.SelectedItem = ModOptions.optionsValuesCollection["AutoScaleUnits"];

                // Temporarily disable event handlers in CandK_Tab to prevent validation logic from firing
                if (CandK_Tab is UC_CommandersAndKnightsOptions candKOptionsForEvents)
                {
                    candKOptionsForEvents.UnsubscribeEventHandlers();
                }

                // Set Commander NumericUpDown values with proper validation
                if (numCommanderWounded != null) 
                {
                    int val = Int32.Parse(ModOptions.optionsValuesCollection["CommanderWoundedChance"]);
                    numCommanderWounded.Value = Math.Max(numCommanderWounded.Minimum, Math.Min(numCommanderWounded.Maximum, val));
                }
                if (numCommanderSeverelyInjured != null) 
                {
                    int val = Int32.Parse(ModOptions.optionsValuesCollection["CommanderSeverelyInjuredChance"]);
                    numCommanderSeverelyInjured.Value = Math.Max(numCommanderSeverelyInjured.Minimum, Math.Min(numCommanderSeverelyInjured.Maximum, val));
                }
                if (numCommanderBrutallyMauled != null) 
                {
                    int val = Int32.Parse(ModOptions.optionsValuesCollection["CommanderBrutallyMauledChance"]);
                    numCommanderBrutallyMauled.Value = Math.Max(numCommanderBrutallyMauled.Minimum, Math.Min(numCommanderBrutallyMauled.Maximum, val));
                }
                if (numCommanderMaimed != null) 
                {
                    int val = Int32.Parse(ModOptions.optionsValuesCollection["CommanderMaimedChance"]);
                    numCommanderMaimed.Value = Math.Max(numCommanderMaimed.Minimum, Math.Min(numCommanderMaimed.Maximum, val));
                }
                if (numCommanderOneLegged != null) 
                {
                    int val = Int32.Parse(ModOptions.optionsValuesCollection["CommanderOneLeggedChance"]);
                    numCommanderOneLegged.Value = Math.Max(numCommanderOneLegged.Minimum, Math.Min(numCommanderOneLegged.Maximum, val));
                }
                if (numCommanderOneEyed != null) 
                {
                    int val = Int32.Parse(ModOptions.optionsValuesCollection["CommanderOneEyedChance"]);
                    numCommanderOneEyed.Value = Math.Max(numCommanderOneEyed.Minimum, Math.Min(numCommanderOneEyed.Maximum, val));
                }
                if (numCommanderDisfigured != null) 
                {
                    int val = Int32.Parse(ModOptions.optionsValuesCollection["CommanderDisfiguredChance"]);
                    numCommanderDisfigured.Value = Math.Max(numCommanderDisfigured.Minimum, Math.Min(numCommanderDisfigured.Maximum, val));
                }
                if (numCommanderSlain != null)
                {
                    int val = Int32.Parse(ModOptions.optionsValuesCollection["CommanderSlainChance"]);
                    numCommanderSlain.Value = Math.Max(numCommanderSlain.Minimum, Math.Min(numCommanderSlain.Maximum, val));
                }

                // Set Knight NumericUpDown values with proper validation
                if (numKnightWounded != null) 
                {
                    int val = Int32.Parse(ModOptions.optionsValuesCollection["KnightWoundedChance"]);
                    numKnightWounded.Value = Math.Max(numKnightWounded.Minimum, Math.Min(numKnightWounded.Maximum, val));
                }
                if (numKnightSeverelyInjured != null) 
                {
                    int val = Int32.Parse(ModOptions.optionsValuesCollection["KnightSeverelyInjuredChance"]);
                    numKnightSeverelyInjured.Value = Math.Max(numKnightSeverelyInjured.Minimum, Math.Min(numKnightSeverelyInjured.Maximum, val));
                }
                if (numKnightBrutallyMauled != null) 
                {
                    int val = Int32.Parse(ModOptions.optionsValuesCollection["KnightBrutallyMauledChance"]);
                    numKnightBrutallyMauled.Value = Math.Max(numKnightBrutallyMauled.Minimum, Math.Min(numKnightBrutallyMauled.Maximum, val));
                }
                if (numKnightMaimed != null) 
                {
                    int val = Int32.Parse(ModOptions.optionsValuesCollection["KnightMaimedChance"]);
                    numKnightMaimed.Value = Math.Max(numKnightMaimed.Minimum, Math.Min(numKnightMaimed.Maximum, val));
                }
                if (numKnightOneLegged != null) 
                {
                    int val = Int32.Parse(ModOptions.optionsValuesCollection["KnightOneLeggedChance"]);
                    numKnightOneLegged.Value = Math.Max(numKnightOneLegged.Minimum, Math.Min(numKnightOneLegged.Maximum, val));
                }
                if (numKnightOneEyed != null) 
                {
                    int val = Int32.Parse(ModOptions.optionsValuesCollection["KnightOneEyedChance"]);
                    numKnightOneEyed.Value = Math.Max(numKnightOneEyed.Minimum, Math.Min(numKnightOneEyed.Maximum, val));
                }
                if (numKnightDisfigured != null) 
                {
                    int val = Int32.Parse(ModOptions.optionsValuesCollection["KnightDisfiguredChance"]);
                    numKnightDisfigured.Value = Math.Max(numKnightDisfigured.Minimum, Math.Min(numKnightDisfigured.Maximum, val));
                }
                if (numKnightSlain != null)
                {
                    int val = Int32.Parse(ModOptions.optionsValuesCollection["KnightSlainChance"]);
                    numKnightSlain.Value = Math.Max(numKnightSlain.Minimum, Math.Min(numKnightSlain.Maximum, val));
                }

                // Set Prisoner NumericUpDown values with proper validation
                if (numCommanderPrisoner != null)
                {
                    int val = Int32.Parse(ModOptions.optionsValuesCollection["CommanderPrisonerChance"]);
                    numCommanderPrisoner.Value = Math.Max(numCommanderPrisoner.Minimum, Math.Min(numCommanderPrisoner.Maximum, val));
                }
                if (numKnightPrisoner != null)
                {
                    int val = Int32.Parse(ModOptions.optionsValuesCollection["KnightPrisonerChance"]);
                    numKnightPrisoner.Value = Math.Max(numKnightPrisoner.Minimum, Math.Min(numKnightPrisoner.Maximum, val));
                }

                // Manually trigger UpdateTotal for CandK_Tab after setting values
                if (CandK_Tab is UC_CommandersAndKnightsOptions candKOptions)
                {
                    // Re-enable event handlers before updating totals
                    candKOptions.SubscribeEventHandlers();
                    // Update totals after setting values
                    candKOptions.UpdateCommanderTotal();
                    candKOptions.UpdateKnightTotal();
                }

                Program.Logger.Debug("Options UI data set.");
                ChangeOptionsTab(General_Tab);
            }
            catch (Exception ex)
            {
                Program.Logger.Log(ex);
                MessageBox.Show("Error setting options UI. Some options may not display correctly.", "Crusader Conflicts: UI Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            }
        }


        void SaveValuesToOptionsFile()
        {
            Program.Logger.Debug("Saving options to file...");
            try
            {
                string file = @".\settings\Options.xml";
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(file);

                var CloseCK3_ComboBox = General_Tab.Controls.Find("OptionSelection_CloseCK3", true).FirstOrDefault() as ComboBox;
                var CloseAttila_ComboBox = General_Tab.Controls.Find("OptionSelection_CloseAttila", true).FirstOrDefault() as ComboBox;
                var FullArmies_ComboBox = General_Tab.Controls.Find("OptionSelection_FullArmies", true).FirstOrDefault() as ComboBox;
                var TimeLimit_ComboBox = General_Tab.Controls.Find("OptionSelection_TimeLimit", true).FirstOrDefault() as ComboBox;
                var BattleMapsSize_ComboBox = General_Tab.Controls.Find("OptionSelection_BattleMapsSize", true).FirstOrDefault() as ComboBox;
                var DefensiveDeployables_ComboBox = General_Tab.Controls.Find("OptionSelection_DefensiveDeployables", true).FirstOrDefault() as ComboBox;
                var UnitCards_ComboBox = General_Tab.Controls[0].Controls.Find("OptionSelection_UnitCards", true).FirstOrDefault() as ComboBox;
                var SeparateArmies_ComboBox = General_Tab.Controls.Find("OptionSelection_SeparateArmies", true).FirstOrDefault() as ComboBox;
                var SiegeEnginesInFieldBattles_ComboBox = General_Tab.Controls.Find("OptionSelection_SiegeEngines", true).FirstOrDefault() as ComboBox;

                var LeviesMax_ComboBox = Units_Tab.Controls.Find("OptionSelection_LeviesMax", true).FirstOrDefault() as ComboBox;
                var RangedMax_ComboBox = Units_Tab.Controls.Find("OptionSelection_RangedMax", true).FirstOrDefault() as ComboBox;
                var InfantryMax_ComboBox = Units_Tab.Controls.Find("OptionSelection_InfantryMax", true).FirstOrDefault() as ComboBox;
                var CavalryMax_ComboBox = Units_Tab.Controls.Find("OptionSelection_CavalryMax", true).FirstOrDefault() as ComboBox;

                var BattleScale_ComboBox = Units_Tab.Controls.Find("OptionSelection_BattleSizeScale", true).FirstOrDefault() as ComboBox;
                var AutoScaleUnits_ComboBox = Units_Tab.Controls.Find("OptionSelection_AutoScale", true).FirstOrDefault() as ComboBox;

                // Commander NumericUpDowns
                var numCommanderWounded = CandK_Tab.numCommanderWounded;
                var numCommanderSeverelyInjured = CandK_Tab.numCommanderSeverelyInjured;
                var numCommanderBrutallyMauled = CandK_Tab.numCommanderBrutallyMauled;
                var numCommanderMaimed = CandK_Tab.numCommanderMaimed;
                var numCommanderOneLegged = CandK_Tab.numCommanderOneLegged;
                var numCommanderOneEyed = CandK_Tab.numCommanderOneEyed;
                var numCommanderDisfigured = CandK_Tab.numCommanderDisfigured;
                var numCommanderSlain = CandK_Tab.numCommanderSlain;
                var numCommanderPrisoner = CandK_Tab.numCommanderPrisoner;

                // Knight NumericUpDowns
                var numKnightWounded = CandK_Tab.numKnightWounded;
                var numKnightSeverelyInjured = CandK_Tab.numKnightSeverelyInjured;
                var numKnightBrutallyMauled = CandK_Tab.numKnightBrutallyMauled;
                var numKnightMaimed = CandK_Tab.numKnightMaimed;
                var numKnightOneLegged = CandK_Tab.numKnightOneLegged;
                var numKnightOneEyed = CandK_Tab.numKnightOneEyed;
                var numKnightDisfigured = CandK_Tab.numKnightDisfigured;
                var numKnightSlain = CandK_Tab.numKnightSlain;
                var numKnightPrisoner = CandK_Tab.numKnightPrisoner;


                var CloseCK3_Node = xmlDoc.SelectSingleNode("//Option [@name='CloseCK3']");
                if (CloseCK3_Node != null && CloseCK3_ComboBox != null) CloseCK3_Node.InnerText = CloseCK3_ComboBox.Text;
                var CloseAttila_Node = xmlDoc.SelectSingleNode("//Option [@name='CloseAttila']");
                if (CloseAttila_Node != null && CloseAttila_ComboBox != null) CloseAttila_Node.InnerText = CloseAttila_ComboBox.Text;
                var FullArmies_Node = xmlDoc.SelectSingleNode("//Option [@name='FullArmies']");
                if (FullArmies_Node != null && FullArmies_ComboBox != null) FullArmies_Node.InnerText = FullArmies_ComboBox.Text;
                var TimeLimit_Node = xmlDoc.SelectSingleNode("//Option [@name='TimeLimit']");
                if (TimeLimit_Node != null && TimeLimit_ComboBox != null) TimeLimit_Node.InnerText = TimeLimit_ComboBox.Text;
                var BattleMapsSize_Node = xmlDoc.SelectSingleNode("//Option [@name='BattleMapsSize']");
                if (BattleMapsSize_Node != null && BattleMapsSize_ComboBox != null) BattleMapsSize_Node.InnerText = BattleMapsSize_ComboBox.Text;
                var DefensiveDeployables_Node = xmlDoc.SelectSingleNode("//Option [@name='DefensiveDeployables']");
                if (DefensiveDeployables_Node != null && DefensiveDeployables_ComboBox != null) DefensiveDeployables_Node.InnerText = DefensiveDeployables_ComboBox.Text;
                var UnitCards_Node = xmlDoc.SelectSingleNode("//Option [@name='UnitCards']");
                if (UnitCards_Node != null && UnitCards_ComboBox != null) UnitCards_Node.InnerText = UnitCards_ComboBox.Text;
                var SeparateArmies_Node = xmlDoc.SelectSingleNode("//Option [@name='SeparateArmies']");
                if (SeparateArmies_Node != null && SeparateArmies_ComboBox != null) SeparateArmies_Node.InnerText = SeparateArmies_ComboBox.Text;
                var SiegeEnginesInFieldBattles_Node = xmlDoc.SelectSingleNode("//Option [@name='SiegeEnginesInFieldBattles']");
                if (SiegeEnginesInFieldBattles_Node != null && SiegeEnginesInFieldBattles_ComboBox != null) SiegeEnginesInFieldBattles_Node.InnerText = SiegeEnginesInFieldBattles_ComboBox.Text;

                var LeviesMax_Node = xmlDoc.SelectSingleNode("//Option [@name='LeviesMax']");
                if (LeviesMax_Node != null && LeviesMax_ComboBox != null) LeviesMax_Node.InnerText = LeviesMax_ComboBox.Text;
                var RangedMax_Node = xmlDoc.SelectSingleNode("//Option [@name='RangedMax']");
                if (RangedMax_Node != null && RangedMax_ComboBox != null) RangedMax_Node.InnerText = RangedMax_ComboBox.Text;
                var InfantryMax_Node = xmlDoc.SelectSingleNode("//Option [@name='InfantryMax']");
                if (InfantryMax_Node != null && InfantryMax_ComboBox != null) InfantryMax_Node.InnerText = InfantryMax_ComboBox.Text;
                var CavalryMax_Node = xmlDoc.SelectSingleNode("//Option [@name='CavalryMax']");
                if (CavalryMax_Node != null && CavalryMax_ComboBox != null) CavalryMax_Node.InnerText = CavalryMax_ComboBox.Text;

                var BattleScale_Node = xmlDoc.SelectSingleNode("//Option [@name='BattleScale']");
                if (BattleScale_Node != null && BattleScale_ComboBox != null) BattleScale_Node.InnerText = BattleScale_ComboBox.Text;
                var AutoScaleUnits_Node = xmlDoc.SelectSingleNode("//Option [@name='AutoScaleUnits']");
                if (AutoScaleUnits_Node != null && AutoScaleUnits_ComboBox != null) AutoScaleUnits_Node.InnerText = AutoScaleUnits_ComboBox.Text;

                // Save Commander NumericUpDown values
                var CommanderWoundedChance_Node = xmlDoc.SelectSingleNode("//Option [@name='CommanderWoundedChance']");
                if (CommanderWoundedChance_Node != null && numCommanderWounded != null) CommanderWoundedChance_Node.InnerText = numCommanderWounded.Value.ToString();
                var CommanderSeverelyInjuredChance_Node = xmlDoc.SelectSingleNode("//Option [@name='CommanderSeverelyInjuredChance']");
                if (CommanderSeverelyInjuredChance_Node != null && numCommanderSeverelyInjured != null) CommanderSeverelyInjuredChance_Node.InnerText = numCommanderSeverelyInjured.Value.ToString();
                var CommanderBrutallyMauledChance_Node = xmlDoc.SelectSingleNode("//Option [@name='CommanderBrutallyMauledChance']");
                if (CommanderBrutallyMauledChance_Node != null && numCommanderBrutallyMauled != null) CommanderBrutallyMauledChance_Node.InnerText = numCommanderBrutallyMauled.Value.ToString();
                var CommanderMaimedChance_Node = xmlDoc.SelectSingleNode("//Option [@name='CommanderMaimedChance']");
                if (CommanderMaimedChance_Node != null && numCommanderMaimed != null) CommanderMaimedChance_Node.InnerText = numCommanderMaimed.Value.ToString();
                var CommanderOneLeggedChance_Node = xmlDoc.SelectSingleNode("//Option [@name='CommanderOneLeggedChance']");
                if (CommanderOneLeggedChance_Node != null && numCommanderOneLegged != null) CommanderOneLeggedChance_Node.InnerText = numCommanderOneLegged.Value.ToString();
                var CommanderOneEyedChance_Node = xmlDoc.SelectSingleNode("//Option [@name='CommanderOneEyedChance']");
                if (CommanderOneEyedChance_Node != null && numCommanderOneEyed != null) CommanderOneEyedChance_Node.InnerText = numCommanderOneEyed.Value.ToString();
                var CommanderDisfiguredChance_Node = xmlDoc.SelectSingleNode("//Option [@name='CommanderDisfiguredChance']");
                if (CommanderDisfiguredChance_Node != null && numCommanderDisfigured != null) CommanderDisfiguredChance_Node.InnerText = numCommanderDisfigured.Value.ToString();
                var CommanderSlainChance_Node = xmlDoc.SelectSingleNode("//Option [@name='CommanderSlainChance']");
                if (CommanderSlainChance_Node != null && numCommanderSlain != null) CommanderSlainChance_Node.InnerText = numCommanderSlain.Value.ToString();
                var CommanderPrisonerChance_Node = xmlDoc.SelectSingleNode("//Option [@name='CommanderPrisonerChance']");
                if (CommanderPrisonerChance_Node != null && numCommanderPrisoner != null) CommanderPrisonerChance_Node.InnerText = numCommanderPrisoner.Value.ToString();

                // Save Knight NumericUpDown values
                var KnightWoundedChance_Node = xmlDoc.SelectSingleNode("//Option [@name='KnightWoundedChance']");
                if (KnightWoundedChance_Node != null && numKnightWounded != null) KnightWoundedChance_Node.InnerText = numKnightWounded.Value.ToString();
                var KnightSeverelyInjuredChance_Node = xmlDoc.SelectSingleNode("//Option [@name='KnightSeverelyInjuredChance']");
                if (KnightSeverelyInjuredChance_Node != null && numKnightSeverelyInjured != null) KnightSeverelyInjuredChance_Node.InnerText = numKnightSeverelyInjured.Value.ToString();
                var KnightBrutallyMauledChance_Node = xmlDoc.SelectSingleNode("//Option [@name='KnightBrutallyMauledChance']");
                if (KnightBrutallyMauledChance_Node != null && numKnightBrutallyMauled != null) KnightBrutallyMauledChance_Node.InnerText = numKnightBrutallyMauled.Value.ToString();
                var KnightMaimedChance_Node = xmlDoc.SelectSingleNode("//Option [@name='KnightMaimedChance']");
                if (KnightMaimedChance_Node != null && numKnightMaimed != null) KnightMaimedChance_Node.InnerText = numKnightMaimed.Value.ToString();
                var KnightOneLeggedChance_Node = xmlDoc.SelectSingleNode("//Option [@name='KnightOneLeggedChance']");
                if (KnightOneLeggedChance_Node != null && numKnightOneLegged != null) KnightOneLeggedChance_Node.InnerText = numKnightOneLegged.Value.ToString();
                var KnightOneEyedChance_Node = xmlDoc.SelectSingleNode("//Option [@name='KnightOneEyedChance']");
                if (KnightOneEyedChance_Node != null && numKnightOneEyed != null) KnightOneEyedChance_Node.InnerText = numKnightOneEyed.Value.ToString();
                var KnightDisfiguredChance_Node = xmlDoc.SelectSingleNode("//Option [@name='KnightDisfiguredChance']");
                if (KnightDisfiguredChance_Node != null && numKnightDisfigured != null) KnightDisfiguredChance_Node.InnerText = numKnightDisfigured.Value.ToString();
                var KnightSlainChance_Node = xmlDoc.SelectSingleNode("//Option [@name='KnightSlainChance']");
                if (KnightSlainChance_Node != null && numKnightSlain != null) KnightSlainChance_Node.InnerText = numKnightSlain.Value.ToString();
                var KnightPrisonerChance_Node = xmlDoc.SelectSingleNode("//Option [@name='KnightPrisonerChance']");
                if (KnightPrisonerChance_Node != null && numKnightPrisoner != null) KnightPrisonerChance_Node.InnerText = numKnightPrisoner.Value.ToString();


                xmlDoc.Save(file);
                Program.Logger.Debug("Options saved to file.");
            }
            catch (Exception ex)
            {
                Program.Logger.Log(ex);
                MessageBox.Show("Error saving game options. Changes may not be saved.", "Crusader Conflicts: Save Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            }
        }

        /*##############################################
         *####              GAMES PATHS             #### 
         *####--------------------------------------####
         *####          Game paths section          ####
         *##############################################
         */

        private void AttilaBtn_MouseHover(object sender, EventArgs e)
        {
            ToolTip_Attila.ToolTipTitle = "Attila Path";
            ToolTip_Attila.SetToolTip(AttilaBtn, Properties.Settings.Default.VAR_attila_path);
        }

        private void ck3Btn_MouseHover(object sender, EventArgs e)
        {
            var tooltip = ToolTip_Attila;
            if (tooltip != null)
            {
                tooltip.ToolTipTitle = "Crusader Kings 3 Path";
                tooltip.SetToolTip(ck3Btn, Properties.Settings.Default.VAR_ck3_path);
            }
        }

        private void Status_Refresh()
        {
            //Path Status
            //Ck3
            if (Properties.Settings.Default.VAR_ck3_path.Contains("ck3.exe"))
            {
                Status_Ck3_Icon.BackgroundImage = CrusaderWars.Properties.Resources.correct;
            }
            else
            {
                Status_Ck3_Icon.BackgroundImage = CrusaderWars.Properties.Resources.warning__1_;
            }

            //Attila
            if (Properties.Settings.Default.VAR_attila_path.Contains("Attila.exe"))
            {
                Status_Attila_Icon.BackgroundImage = CrusaderWars.Properties.Resources.correct;
            }
            else
            {
                Status_Attila_Icon.BackgroundImage = CrusaderWars.Properties.Resources.warning__1_;
            }
        }


        private void ck3Btn_Click(object sender, EventArgs e)
        {
            string game_node = "CrusaderKings";

            // Open the file explorer dialog
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Title = "Select 'ck3.exe' from the installation folder";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                
                CK3_Path = openFileDialog1.FileName; // Get the selected file path
                Properties.Settings.Default.VAR_ck3_path = CK3_Path;
                ChangePathSettings(game_node, CK3_Path);
                Properties.Settings.Default.Save();
            }

            Status_Refresh();

        }

        private void AttilaBtn_Click(object sender, EventArgs e)
        {
            string game_node = "TotalWarAttila";

            // Open the file explorer dialog
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Title = "Select 'Attila.exe' from the installation folder";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                
                Attila_Path = openFileDialog1.FileName; // Get the selected file path
                Properties.Settings.Default.VAR_attila_path = Attila_Path;
                if (Attila_Path.Contains("Attila.exe"))
                {
                    Properties.Settings.Default.VAR_log_attila = Attila_Path.Substring(0, Attila_Path.IndexOf("Attila.exe")) + "data\\BattleResults_log.txt";
                }
                ChangePathSettings(game_node, Attila_Path);
                Properties.Settings.Default.Save();
            }

            if (!string.IsNullOrEmpty(Properties.Settings.Default.VAR_attila_path))
            {
                AttilaModManager.SetControlReference(ModManager);
                AttilaModManager.ReadInstalledModsAndPopulateModManager();
            }

            Status_Refresh();

        }

        private void ChangePathSettings(string game, string new_path)
        {
            Program.Logger.Debug($"Attempting to change path setting for {game} to {new_path}");
            try
            {
                string file = @".\settings\Paths.xml";
                XmlDocument xmlDoc = new XmlDocument();

                // Ensure the file exists, if not, create a basic structure
                if (!File.Exists(file))
                {
                    Program.Logger.Debug("Paths.xml not found, creating default structure.");
                    XmlElement rootElement = xmlDoc.CreateElement("Paths");
                    xmlDoc.AppendChild(rootElement);

                    XmlElement attilaElement = xmlDoc.CreateElement("TotalWarAttila");
                    attilaElement.SetAttribute("path", "");
                    rootElement.AppendChild(attilaElement);

                    XmlElement ck3Element = xmlDoc.CreateElement("CrusaderKings");
                    ck3Element.SetAttribute("path", "");
                    rootElement.AppendChild(ck3Element);

                    xmlDoc.Save(file);
                }
                else
                {
                    xmlDoc.Load(file);
                }

                XmlNode? root = xmlDoc.DocumentElement;
                if (root == null)
                {
                    Program.Logger.Debug("Paths.xml has no root element, recreating.");
                    root = xmlDoc.CreateElement("Paths");
                    xmlDoc.AppendChild(root);
                }

                XmlNode? node = root.SelectSingleNode(game);
                if (node == null)
                {
                    Program.Logger.Debug($"Node '{game}' not found in Paths.xml, creating.");
                    node = xmlDoc.CreateElement(game);
                    root.AppendChild(node);
                }

                XmlAttribute? pathAttribute = node.Attributes?["path"];
                if (pathAttribute == null)
                {
                    Program.Logger.Debug($"'path' attribute not found for node '{game}', creating.");
                    pathAttribute = xmlDoc.CreateAttribute("path");
                    node.Attributes?.Append(pathAttribute);
                }

                pathAttribute.Value = new_path;
                xmlDoc.Save(file);
                Program.Logger.Debug($"Path setting for {game} updated successfully.");
            }
            catch (Exception ex)
            {
                Program.Logger.Log(ex);
                MessageBox.Show($"Error setting game path for {game}: {ex.Message}", "Crusader Conflicts: Data Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                Application.Exit();
            }
        }

        /*
         * To read after each update, so that the user
         * doesnt need to always set the game paths
         *
         */
        public static void ReadGamePaths()
        {
            Program.Logger.Debug("Reading game paths from Paths.xml...");
            try
            {
                string file = @".\settings\Paths.xml";
                XmlDocument xmlDoc = new XmlDocument();
                bool fileModified = false;

                // 1. Handle missing file: Create default structure if file doesn't exist
                if (!File.Exists(file))
                {
                    Program.Logger.Debug("Paths.xml not found. Creating default structure.");
                    XmlElement rootElement = xmlDoc.CreateElement("Paths");
                    xmlDoc.AppendChild(rootElement);

                    XmlElement attilaElement = xmlDoc.CreateElement("TotalWarAttila");
                    attilaElement.SetAttribute("path", "");
                    rootElement.AppendChild(attilaElement);

                    XmlElement ck3Element = xmlDoc.CreateElement("CrusaderKings");
                    ck3Element.SetAttribute("path", "");
                    rootElement.AppendChild(ck3Element);

                    fileModified = true;
                }
                else
                {
                    xmlDoc.Load(file);
                }

                XmlNode? root = xmlDoc.DocumentElement;
                if (root == null)
                {
                    Program.Logger.Debug("Paths.xml has no root element. Recreating root.");
                    root = xmlDoc.CreateElement("Paths");
                    xmlDoc.AppendChild(root);
                    fileModified = true;
                }

                // 2. Handle missing TotalWarAttila node or path attribute
                XmlNode? attila_node = root.SelectSingleNode("TotalWarAttila");
                if (attila_node == null)
                {
                    Program.Logger.Debug("TotalWarAttila node not found. Creating.");
                    attila_node = xmlDoc.CreateElement("TotalWarAttila");
                    root.AppendChild(attila_node);
                    fileModified = true;
                }
                if (attila_node.Attributes?["path"] == null)
                {
                    Program.Logger.Debug("'path' attribute not found for TotalWarAttila. Creating.");
                    ((XmlElement)attila_node).SetAttribute("path", "");
                    fileModified = true;
                }

                // 3. Handle missing CrusaderKings node or path attribute
                XmlNode? ck3_node = root.SelectSingleNode("CrusaderKings");
                if (ck3_node == null)
                {
                Program.Logger.Debug("CrusaderKings node not found. Creating.");
                    ck3_node = xmlDoc.CreateElement("CrusaderKings");
                    root.AppendChild(ck3_node);
                    fileModified = true;
                }
                if (ck3_node.Attributes?["path"] == null)
                {
                    Program.Logger.Debug("'path' attribute not found for CrusaderKings. Creating.");
                    ((XmlElement)ck3_node).SetAttribute("path", "");
                    fileModified = true;
                }

                // Save if any modifications were made to the XML structure
                if (fileModified)
                {
                    xmlDoc.Save(file);
                    Program.Logger.Debug("Paths.xml created or repaired with default structure.");
                }

                // 4. Load values into application settings
                Properties.Settings.Default.VAR_attila_path = attila_node.Attributes?["path"]?.Value ?? string.Empty;
                Properties.Settings.Default.VAR_ck3_path = ck3_node.Attributes?["path"]?.Value ?? string.Empty;
                Properties.Settings.Default.Save();
                Program.Logger.Debug("Game paths loaded successfully.");
            }
            catch (Exception ex)
            {
                Program.Logger.Log(ex);
                MessageBox.Show($"Error reading or creating game paths file (Paths.xml): {ex.Message}", "Crusader Conflicts: Data Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                Application.Exit();
            }
        }

        public static void SetArmiesControl(ModOptions.ArmiesSetup value)
        {
            Program.Logger.Debug($"Setting 'Armies Control' to {value}.");
            string valueString = "";
            switch (value)
            {
                case ModOptions.ArmiesSetup.All_Controled:
                    valueString = "All Controled";
                    break;
                case ModOptions.ArmiesSetup.Friendly_Only:
                    valueString = "Friendly Only";
                    break;
                case ModOptions.ArmiesSetup.All_Separate:
                    valueString = "All Separate";
                    break;
            }

            try
            {
                string file = @".\settings\Options.xml";
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(file);

                var separateArmiesNode = xmlDoc.SelectSingleNode("//Option [@name='SeparateArmies']");
                if (separateArmiesNode != null)
                {
                    separateArmiesNode.InnerText = valueString;
                    xmlDoc.Save(file);
                    Program.Logger.Debug("'SeparateArmies' option updated in Options.xml.");

                    ModOptions.optionsValuesCollection["SeparateArmies"] = valueString;
                    Program.Logger.Debug("In-memory options collections updated.");
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error in SetArmiesControl: {ex.Message}");
            }
        }

        public static void SetOptInPreReleases(bool enabled)
        {
            Program.Logger.Debug($"Setting 'OptInPreReleases' to {enabled}.");
            string valueString = enabled ? "True" : "False";

            try
            {
                string file = @".\settings\Options.xml";
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(file);

                // Corrected the XPath query string to ensure it matches the attribute name used for creation
                var optInPreReleasesNode = xmlDoc.SelectSingleNode("//Option [@name='OptInPreReleases']");
                if (optInPreReleasesNode != null)
                {
                    optInPreReleasesNode.InnerText = valueString;
                    xmlDoc.Save(file);
                    Program.Logger.Debug("'OptInPreReleases' option updated in Options.xml.");

                    ModOptions.optionsValuesCollection["OptInPreReleases"] = valueString;
                    Program.Logger.Debug("In-memory options collections updated.");
                }
                else
                {
                    // If the node doesn't exist, create it (should be handled by ReadOptionsFile, but as a fallback)
                    XmlElement newOption = xmlDoc.CreateElement("Option");
                    newOption.SetAttribute("name", "OptInPreReleases");
                    newOption.InnerText = valueString;
                    xmlDoc.DocumentElement?.AppendChild(newOption);
                    xmlDoc.Save(file);
                    Program.Logger.Debug("Created and set 'OptInPreReleases' option in Options.xml.");

                    ModOptions.optionsValuesCollection["OptInPreReleases"] = valueString;
                    Program.Logger.Debug("In-memory options collections updated after creation.");
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Debug($"Error in SetOptInPreReleases: {ex.Message}");
            }
        }

        /*##############################################
         *####         OPTIONS FORM MOVEMENT        #### 
         *####--------------------------------------####
         *####--------------------------------------####
         *##############################################
         */


        Point mouseOffset;
        private void Options_MouseDown(object sender, MouseEventArgs e)
        {
            mouseOffset = new Point(-e.X, -e.Y);
        }

        private void Options_MouseMove(object sender, MouseEventArgs e)
        {
            // Move the form when the left mouse button is down
            if (e.Button == MouseButtons.Left)
            {
                Point mousePos = Control.MousePosition;
                mousePos.Offset(mouseOffset.X, mouseOffset.Y);
                Location = mousePos;
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;
                return cp;
            }
        }





        /*##############################################
         *####              MOD MANAGER             #### 
         *####--------------------------------------####
         *####         Mod Manager Section          ####
         *##############################################
         */

        private void ModManager_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == ModManager.Columns[0].Index && e.RowIndex != -1)
            {
                int rowIndex = e.RowIndex;
                AttilaModManager.ChangeEnabledState(ModManager.Rows[rowIndex]);
            }
        }
        private void ModManager_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            // End of edition on each click on column of checkbox
            if (e.ColumnIndex == ModManager.Columns[0].Index && e.RowIndex != -1)
            {
                ModManager.EndEdit();
            }
        }

        private void ModManager_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            // End of edition on each click on column of checkbox
            if (e.ColumnIndex == ModManager.Columns[0].Index && e.RowIndex != -1)
            {
                ModManager.EndEdit();
            }
        }

        private void toggleModManagerButton_Click(object sender, EventArgs e)
        {
            this.SuspendLayout();
            var originalAnchor = this.TableLayoutModManager.Anchor;
            this.TableLayoutModManager.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            int panelHeight = panel1.Height;
            _isModManagerExpanded = !_isModManagerExpanded;
            panel1.Visible = _isModManagerExpanded;

            if (_isModManagerExpanded)
            {
                // Expanding
                this.Height += panelHeight;
                this.TableLayoutModManager.Height += panelHeight;
                toggleModManagerButton.Text = "Mod Manager [▲]";
                ToolTip_Options.SetToolTip(toggleModManagerButton, "Click to collapse the Mod Manager. This section shows optional mods.");
            }
            else
            {
                // Collapsing
                this.Height -= panelHeight;
                this.TableLayoutModManager.Height -= panelHeight;
                toggleModManagerButton.Text = "Mod Manager [▼]";
                ToolTip_Options.SetToolTip(toggleModManagerButton, "Click to expand the Mod Manager. This section shows optional mods.");
            }

            this.TableLayoutModManager.Anchor = originalAnchor;
            this.ResumeLayout(true);
        }


        /*##############################################
         *####             UNIT MAPPERS             #### 
         *####--------------------------------------####
         *####         Unit Mappers Section         ####
         *##############################################
         */

        UC_UnitMapper CrusaderKings_Tab = null!;
        UC_UnitMapper TheFallenEagle_Tab = null!;
        UC_UnitMapper RealmsInExile_Tab = null!;
        UC_UnitMapper AGOT_Tab = null!; // Added AGOT tab

        private void Btn_CK3Tab_Click(object sender, EventArgs e)
        {
            if (UMpanel.Controls.Count > 0 && UMpanel.Controls[0] != CrusaderKings_Tab)
                ChangeUnitMappersTab(CrusaderKings_Tab);
        }

        private void Btn_TFETab_Click(object sender, EventArgs e)
        {
            if (UMpanel.Controls.Count > 0 && UMpanel.Controls[0] != TheFallenEagle_Tab)
                ChangeUnitMappersTab(TheFallenEagle_Tab);
        }

        private void Btn_LOTRTab_Click(object sender, EventArgs e)
        {
            if (UMpanel.Controls.Count > 0 && UMpanel.Controls[0] != RealmsInExile_Tab)
                ChangeUnitMappersTab(RealmsInExile_Tab);
        }

        private void Btn_AGOTTab_Click(object sender, EventArgs e)
        {
            if (UMpanel.Controls.Count > 0 && UMpanel.Controls[0] != AGOT_Tab)
                ChangeUnitMappersTab(AGOT_Tab);
        }


        void ChangeUnitMappersTab(Control control)
        {
            UMpanel.Controls.Clear();
            UMpanel.Controls.Add(control);
            control.Location = new System.Drawing.Point((UMpanel.Width - control.Width) / 2, 0); // Centered horizontally
            control.BringToFront();

            // Define colors
            Color inactiveColor = System.Drawing.Color.FromArgb(128, 53, 0);
            Color activeColor = System.Drawing.Color.FromArgb(140, 87, 63);

            // Reset all buttons
            Btn_CK3Tab.BackgroundImage = null;
            Btn_TFETab.BackgroundImage = null;
            Btn_LOTRTab.BackgroundImage = null;
            Btn_AGOTTab.BackgroundImage = null; // Added AGOT tab
            Btn_CK3Tab.BackColor = inactiveColor;
            Btn_TFETab.BackColor = inactiveColor;
            Btn_LOTRTab.BackColor = inactiveColor;
            Btn_AGOTTab.BackColor = inactiveColor; // Added AGOT tab
            Btn_CK3Tab.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            Btn_TFETab.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            Btn_LOTRTab.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            Btn_AGOTTab.FlatAppearance.BorderColor = System.Drawing.Color.Black; // Added AGOT tab
            Btn_CK3Tab.FlatAppearance.BorderSize = 1;
            Btn_TFETab.FlatAppearance.BorderSize = 1;
            Btn_LOTRTab.FlatAppearance.BorderSize = 1;
            Btn_AGOTTab.FlatAppearance.BorderSize = 1; // Added AGOT tab

            // Highlight active button
            Button? activeButton = null;
            if (control == CrusaderKings_Tab) activeButton = Btn_CK3Tab;
            else if (control == TheFallenEagle_Tab) activeButton = Btn_TFETab;
            else if (control == RealmsInExile_Tab) activeButton = Btn_LOTRTab;
            else if (control == AGOT_Tab) activeButton = Btn_AGOTTab; // Added AGOT tab

            if (activeButton != null)
            {
                activeButton.BackColor = activeColor;
            }
        }

        // NEW HELPER METHOD: GetOrCreateUnitMapperOption
        private string GetOrCreateUnitMapperOption(XmlDocument doc, string mapperName)
        {
            XmlNode? node = doc.SelectSingleNode($"//UnitMappers[@name='{mapperName}']");
            if (node != null)
            {
                return node.InnerText;
            }
            else
            {
                Program.Logger.Debug($"Unit mapper '{mapperName}' not found in UnitMappers.xml. Creating with default value 'False'.");
                XmlElement newMapper = doc.CreateElement("UnitMappers");
                newMapper.SetAttribute("name", mapperName);
                newMapper.InnerText = "False";
                doc.DocumentElement?.AppendChild(newMapper);
                _unitMappersXmlChanged = true; // Mark the document as changed
                return "False";
            }
        }

        void ReadUnitMappersOptions()
        {
            string file = @".\settings\UnitMappers.xml";
            XmlDocument xmlDoc = new XmlDocument();

            // Check if the file exists. If not, create it with default values.
            if (!File.Exists(file))
            {
                Program.Logger.Debug("UnitMappers.xml not found. Creating with default values.");
                XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
                xmlDoc.AppendChild(xmlDeclaration);

                XmlElement rootElement = xmlDoc.CreateElement("UMOptions");
                xmlDoc.AppendChild(rootElement);

                // Helper to create elements
                void createMapper(string name)
                {
                    XmlElement mapperElement = xmlDoc.CreateElement("UnitMappers");
                    mapperElement.SetAttribute("name", name);
                    mapperElement.InnerText = "False";
                    rootElement.AppendChild(mapperElement);
                };

                createMapper("DefaultCK3");
                createMapper("TheFallenEagle");
                createMapper("RealmsInExile");
                createMapper("AGOT");

                xmlDoc.Save(file);
                Program.Logger.Debug("UnitMappers.xml created successfully.");
            }

            xmlDoc.Load(file);

            _unitMappersXmlChanged = false; // Reset the flag at the beginning of the method

            var ck3ToggleStateStr = GetOrCreateUnitMapperOption(xmlDoc, "DefaultCK3");
            var tfeToggleStateStr = GetOrCreateUnitMapperOption(xmlDoc, "TheFallenEagle");
            var lotrToggleStateStr = GetOrCreateUnitMapperOption(xmlDoc, "RealmsInExile");
            var agotToggleStateStr = GetOrCreateUnitMapperOption(xmlDoc, "AGOT");

            if (_unitMappersXmlChanged)
            {
                xmlDoc.Save(file);
                Program.Logger.Debug("UnitMappers.xml updated with new entries.");
            }

            bool ck3ToggleState = false; bool tfeToggleState = false; bool lotrToggleState = false; bool agotToggleState = false; // Added agotToggleState
            if (ck3ToggleStateStr == "True") ck3ToggleState = true; else ck3ToggleState = false;
            if (tfeToggleStateStr == "True") tfeToggleState = true; else tfeToggleState = false;
            if (lotrToggleStateStr == "True") lotrToggleState = true; else lotrToggleState = false;
            if (agotToggleStateStr == "True") agotToggleState = true; else agotToggleState = false; // Added AGOT tab

            // NOTE: The constructor for UC_UnitMapper will need to be updated to accept the list of submods.
            // This change is commented out because the UC_UnitMapper.cs file was not provided.
            // You will need to modify its constructor to accept `List<Submod> submods` as a new parameter.
            var ck3Mods = CrusaderWars.unit_mapper.UnitMappers_BETA.GetUnitMappersModsCollectionFromTag("DefaultCK3");
            var tfeMods = CrusaderWars.unit_mapper.UnitMappers_BETA.GetUnitMappersModsCollectionFromTag("TheFallenEagle");
            var lotrMods = CrusaderWars.unit_mapper.UnitMappers_BETA.GetUnitMappersModsCollectionFromTag("RealmsInExile");
            var agotMods = CrusaderWars.unit_mapper.UnitMappers_BETA.GetUnitMappersModsCollectionFromTag("AGOT");

            CrusaderKings_Tab = new UC_UnitMapper(Properties.Resources._default, "https://steamcommunity.com/sharedfiles/filedetails/?id=3301634851", ck3Mods.requiredMods.Select(m => (m.FileName, m.Sha256, m.ScreenName, m.Url)).ToList(), ck3ToggleState, "DefaultCK3", ck3Mods.submods.GroupBy(s => s.Tag).Select(g => g.First()).ToList());
            TheFallenEagle_Tab = new UC_UnitMapper(Properties.Resources.tfe, string.Empty, tfeMods.requiredMods.Select(m => (m.FileName, m.Sha256, m.ScreenName, m.Url)).ToList(), tfeToggleState, "TheFallenEagle", tfeMods.submods.GroupBy(s => s.Tag).Select(g => g.First()).ToList());
            TheFallenEagle_Tab.SetSteamLinkButtonTooltip("Now requires TW:Attila mod 'Age of Justinian 555 2.0'.");
            RealmsInExile_Tab = new UC_UnitMapper(Properties.Resources.LOTR, "https://steamcommunity.com/sharedfiles/filedetails/?id=3211765434", lotrMods.requiredMods.Select(m => (m.FileName, m.Sha256, m.ScreenName, m.Url)).ToList(), lotrToggleState, "RealmsInExile", lotrMods.submods.GroupBy(s => s.Tag).Select(g => g.First()).ToList());
            AGOT_Tab = new UC_UnitMapper(Properties.Resources.playthrough_agot, string.Empty, agotMods.requiredMods.Select(m => (m.FileName, m.Sha256, m.ScreenName, m.Url)).ToList(), agotToggleState, "AGOT", agotMods.submods.GroupBy(s => s.Tag).Select(g => g.First()).ToList()); // Changed to use playthrough_agot

            CrusaderKings_Tab.ToggleClicked += PlaythroughToggle_Clicked;
            TheFallenEagle_Tab.ToggleClicked += PlaythroughToggle_Clicked;
            RealmsInExile_Tab.ToggleClicked += PlaythroughToggle_Clicked;
            AGOT_Tab.ToggleClicked += PlaythroughToggle_Clicked; // Added AGOT tab

            CrusaderKings_Tab.SetOtherControlsReferences(new UC_UnitMapper[] { TheFallenEagle_Tab, RealmsInExile_Tab, AGOT_Tab }); // Added AGOT tab
            TheFallenEagle_Tab.SetOtherControlsReferences(new UC_UnitMapper[] { CrusaderKings_Tab, RealmsInExile_Tab, AGOT_Tab }); // Added AGOT tab
            RealmsInExile_Tab.SetOtherControlsReferences(new UC_UnitMapper[] { CrusaderKings_Tab, TheFallenEagle_Tab, AGOT_Tab }); // Added AGOT tab
            AGOT_Tab.SetOtherControlsReferences(new UC_UnitMapper[] { CrusaderKings_Tab, TheFallenEagle_Tab, RealmsInExile_Tab }); // Added AGOT tab

            ChangeUnitMappersTab(CrusaderKings_Tab);
            CheckPlaythroughSelection();
        }

        void WriteUnitMappersOptions()
        {
            string file = @".\settings\UnitMappers.xml";
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(file);

            var CrusaderKings_Node = xmlDoc.SelectSingleNode("//UnitMappers [@name='DefaultCK3']");
            if (CrusaderKings_Node != null) CrusaderKings_Node.InnerText = CrusaderKings_Tab.GetState().ToString();
            var TheFallenEagle_Node = xmlDoc.SelectSingleNode("//UnitMappers [@name='TheFallenEagle']");
            if (TheFallenEagle_Node != null) TheFallenEagle_Node.InnerText = TheFallenEagle_Tab.GetState().ToString();
            var RealmsInExile_Node = xmlDoc.SelectSingleNode("//UnitMappers [@name='RealmsInExile']");
            if (RealmsInExile_Node != null) RealmsInExile_Node.InnerText = RealmsInExile_Tab.GetState().ToString();
            var AGOT_Node = xmlDoc.SelectSingleNode("//UnitMappers [@name='AGOT']"); // Added AGOT tab
            if (AGOT_Node != null && AGOT_Tab != null) AGOT_Node.InnerText = AGOT_Tab.GetState().ToString(); // Added AGOT tab
            xmlDoc.Save(file);
        }

        private void TableLayoutPlaythroughs_Paint(object sender, PaintEventArgs e)
        {
            if (_isPulsing)
            {
                Control control = (Control)sender;
                Color pulseColor = _pulseState ? Color.FromArgb(255, 80, 80) : Color.FromArgb(180, 30, 30);
                using (Pen pen = new Pen(pulseColor, 3))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, control.ClientSize.Width - 1, control.ClientSize.Height - 1);
                }
            }
        }

        private void CheckPlaythroughSelection()
        {
            var activePlaythrough = GetActivePlaythrough();

            // Stop pulsing on all tabs first
            CrusaderKings_Tab?.SetPulsing(false);
            TheFallenEagle_Tab?.SetPulsing(false);
            RealmsInExile_Tab?.SetPulsing(false);
            AGOT_Tab?.SetPulsing(false); // Added AGOT tab

            if (activePlaythrough == null)
            {
                // No playthrough selected, pulse the container
                _isPulsing = true;
                if (!_pulseTimer.Enabled)
                {
                    _pulseTimer.Start();
                }
            }
            else
            {
                // A playthrough is selected, stop pulsing the container and pulse the active tab
                _isPulsing = false;
                TableLayoutPlaythroughs.Invalidate(); // Redraw to remove border if it was there
                activePlaythrough.SetPulsing(true);
                if (!_pulseTimer.Enabled)
                {
                    _pulseTimer.Start();
                }
            }
        }

        private void PlaythroughToggle_Clicked(object? sender, EventArgs e)
        {
            CheckPlaythroughSelection();
        }

        private void Options_FormClosing(object sender, FormClosingEventArgs e)
        {
            Program.Logger.Debug("Options form closing event triggered.");

            // Perform validation for Commander and Knight wound chances
            if (CandK_Tab is UC_CommandersAndKnightsOptions candKOptions)
            {
                bool commanderValid = candKOptions.IsCommanderTotalValid();
                bool knightValid = candKOptions.IsKnightTotalValid();

                if (!commanderValid || !knightValid)
                {
                    string errorMessage = "Wound chance percentages must total 100% for both Commanders and Knights.\n\n";
                    if (!commanderValid)
                    {
                        errorMessage += $"Commander Total: {candKOptions.GetCommanderTotal()}%\n";
                    }
                    if (!knightValid)
                    {
                        errorMessage += $"Knight Total: {candKOptions.GetKnightTotal()}%\n";
                    }
                    errorMessage += "\nPlease adjust the values in the 'Cmdr/Knights' tab.";

                    MessageBox.Show(errorMessage, "Crusader Conflicts: Configuration Error",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);

                    // Switch to the Cmdr/Knights tab to show the user where the error is
                    ChangeOptionsTab(CandK_Tab);
                    e.Cancel = true; // Prevent the form from closing
                    return;
                }
            }

            // If validation passes, proceed with saving and cleanup
            SaveValuesToOptionsFile();
            WriteUnitMappersOptions();
            SubmodManager.SaveActiveSubmods(); // Save active submods

            if (!string.IsNullOrEmpty(Properties.Settings.Default.VAR_attila_path))
            {
                Program.Logger.Debug("Saving active mods...");
                AttilaModManager.SaveActiveMods();
            }

            this.Dispose(); // Dispose resources
        }
    }
}
