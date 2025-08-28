using CrusaderWars.client;
using CrusaderWars.client.WarningMessage;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Media;
using Control = System.Windows.Forms.Control;
using CrusaderWars.mod_manager;
using CrusaderWars.unit_mapper;

namespace CrusaderWars
{
    public partial class Options : Form
    {
        private string CK3_Path { get; set; }
        private string Attila_Path { get; set; }

        public Options()
        {
            InitializeComponent();
            this.Icon = Properties.Resources.logo;

        }

        private void CloseBtn_Click(object sender, EventArgs e)
        {
            Program.Logger.Debug("Options form closing...");
            SaveValuesToOptionsFile();
            ReadOptionsFile();
            ModOptions.StoreOptionsValues(optionsValuesCollection);
            WriteUnitMappersOptions();

            if (!string.IsNullOrEmpty(Properties.Settings.Default.VAR_attila_path))
            {
                Program.Logger.Debug("Saving active mods...");
                AttilaModManager.SaveActiveMods();
            }

            this.Dispose();
            this.Close();
        }


        private void Options_Load(object sender, EventArgs e)
        {
            Program.Logger.Debug("Options form loading...");
            General_Tab = new UC_GeneralOptions();
            Units_Tab = new UC_UnitsOptions();
            BattleScale_Tab = new UC_BattleScaleOptions();

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
        }

        /*##############################################
         *####              MOD OPTIONS             #### 
         *####--------------------------------------####
         *####          Mod options section         ####
         *##############################################
         */
        UserControl General_Tab;
        UserControl Units_Tab;
        UserControl BattleScale_Tab;
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

        private void Btn_BattleScaleTab_Click(object sender, EventArgs e)
        {
            if (OptionsPanel.Controls.Count > 0 && OptionsPanel.Controls[0] != BattleScale_Tab)
                ChangeOptionsTab(BattleScale_Tab);
        }

        void ChangeOptionsTab(Control control)
        {
            control.Dock = DockStyle.Fill;
            OptionsPanel.Controls.Clear();
            OptionsPanel.Controls.Add(control);
            control.BringToFront();

            // Define colors
            Color inactiveColor = System.Drawing.Color.FromArgb(64, 64, 64);
            Color activeColor = System.Drawing.Color.FromArgb(100, 100, 100);

            // Reset all buttons
            Btn_GeneralTab.BackgroundImage = null;
            Btn_UnitsTab.BackgroundImage = null;
            Btn_BattleScaleTab.BackgroundImage = null;
            Btn_GeneralTab.BackColor = inactiveColor;
            Btn_UnitsTab.BackColor = inactiveColor;
            Btn_BattleScaleTab.BackColor = inactiveColor;
            Btn_GeneralTab.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            Btn_UnitsTab.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            Btn_BattleScaleTab.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            Btn_GeneralTab.FlatAppearance.BorderSize = 1;
            Btn_UnitsTab.FlatAppearance.BorderSize = 1;
            Btn_BattleScaleTab.FlatAppearance.BorderSize = 1;

            // Highlight active button
            Button activeButton = null;
            if (control == General_Tab) activeButton = Btn_GeneralTab;
            else if (control == Units_Tab) activeButton = Btn_UnitsTab;
            else if (control == BattleScale_Tab) activeButton = Btn_BattleScaleTab;

            if (activeButton != null)
            {
                activeButton.BackColor = activeColor;
            }
        }

        //this is to read the options values on the .xml file
        public static List<(string option, string value)> optionsValuesCollection { get; private set; }

        private static string GetOptionValue(XmlDocument doc, string optionName, string defaultValue)
        {
            XmlNode node = doc.SelectSingleNode($"//Option [@name='{optionName}']");
            if (node != null)
            {
                return node.InnerText;
            }
            else
            {
                Program.Logger.Debug($"Option '{optionName}' not found in Options.xml. Creating with default value '{defaultValue}'.");
                XmlElement newOption = doc.CreateElement("Option");
                newOption.SetAttribute("name", optionName);
                newOption.InnerText = defaultValue;
                doc.DocumentElement.AppendChild(newOption);
                return defaultValue;
            }
        }
        public static void ReadOptionsFile()
        {
            Program.Logger.Debug("Reading options file...");
            try
            {
                string file = @".\Settings\Options.xml";
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(file);
                Program.Logger.Debug("XML options file loaded.");

                optionsValuesCollection = new List<(string option, string value)>();
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

                xmlDoc.Save(file);
                Program.Logger.Debug("All options read from XML.");

                optionsValuesCollection.AddRange(new List<(string, string)>
                {
                    ("CloseCK3", CloseCK3_Value),
                    ("CloseAttila", CloseAttila_Value),
                    ("FullArmies", FullArmies_Value),
                    ("TimeLimit", TimeLimit_Value),
                    ("BattleMapsSize", BattleMapsSize_Value) ,
                    ("DefensiveDeployables", DefensiveDeployables_Value),
                    ("UnitCards", UnitCards_Value),
                    ("SeparateArmies", SeparateArmies_Value),
                    ("LeviesMax", LeviesMax_Value),
                    ("RangedMax", RangedMax_Value),
                    ("InfantryMax", InfantryMax_Value),
                    ("CavalryMax", CavalryMax_Value),
                    ("BattleScale", BattleScale_Value),
                    ("AutoScaleUnits", AutoScaleUnits_Value),

                });
                Program.Logger.Debug("Options collection populated.");


            }
            catch (Exception ex)
            {
                Program.Logger.Log(ex);
                MessageBox.Show("Error reading game options. Restart the mod and try again", "Crusader Wars: Data Error",
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

                var LeviesMax_ComboBox = Units_Tab.Controls[0].Controls.Find("OptionSelection_LeviesMax", true).FirstOrDefault() as ComboBox;
                var RangedMax_ComboBox = Units_Tab.Controls[0].Controls.Find("OptionSelection_RangedMax", true).FirstOrDefault() as ComboBox;
                var InfantryMax_ComboBox = Units_Tab.Controls[0].Controls.Find("OptionSelection_InfantryMax", true).FirstOrDefault() as ComboBox;
                var CavalryMax_ComboBox = Units_Tab.Controls[0].Controls.Find("OptionSelection_CavalryMax", true).FirstOrDefault() as ComboBox;

                var BattleScale_ComboBox = BattleScale_Tab.Controls[0].Controls.Find("OptionSelection_BattleSizeScale", true).FirstOrDefault() as ComboBox;
                var AutoScaleUnits_ComboBox = BattleScale_Tab.Controls[0].Controls.Find("OptionSelection_AutoScale", true).FirstOrDefault() as ComboBox;


                CloseCK3_ComboBox.SelectedItem = optionsValuesCollection.FirstOrDefault(x => x.option == "CloseCK3").value;
                CloseAttila_ComboBox.SelectedItem = optionsValuesCollection.FirstOrDefault(x => x.option == "CloseAttila").value;
                FullArmies_ComboBox.SelectedItem = optionsValuesCollection.FirstOrDefault(x => x.option == "FullArmies").value;
                TimeLimit_ComboBox.SelectedItem = optionsValuesCollection.FirstOrDefault(x => x.option == "TimeLimit").value;
                BattleMapsSize_ComboBox.SelectedItem = optionsValuesCollection.FirstOrDefault(x => x.option == "BattleMapsSize").value;
                DefensiveDeployables_ComboBox.SelectedItem = optionsValuesCollection.FirstOrDefault(x => x.option == "DefensiveDeployables").value;
                UnitCards_ComboBox.SelectedItem = optionsValuesCollection.FirstOrDefault(x => x.option == "UnitCards").value;
                SeparateArmies_ComboBox.SelectedItem = optionsValuesCollection.FirstOrDefault(x => x.option == "SeparateArmies").value;

                LeviesMax_ComboBox.SelectedItem = optionsValuesCollection.FirstOrDefault(x => x.option == "LeviesMax").value;
                RangedMax_ComboBox.SelectedItem = optionsValuesCollection.FirstOrDefault(x => x.option == "RangedMax").value;
                InfantryMax_ComboBox.SelectedItem = optionsValuesCollection.FirstOrDefault(x => x.option == "InfantryMax").value;
                CavalryMax_ComboBox.SelectedItem = optionsValuesCollection.FirstOrDefault(x => x.option == "CavalryMax").value;

                BattleScale_ComboBox.SelectedItem = optionsValuesCollection.FirstOrDefault(x => x.option == "BattleScale").value;
                AutoScaleUnits_ComboBox.SelectedItem = optionsValuesCollection.FirstOrDefault(x => x.option == "AutoScaleUnits").value;


                Program.Logger.Debug("Options UI data set.");
                ChangeOptionsTab(General_Tab);
            }
            catch (Exception ex)
            {
                Program.Logger.Log(ex);
                MessageBox.Show("Error setting options UI. Some options may not display correctly.", "Crusader Wars: UI Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            }
        }


        void SaveValuesToOptionsFile()
        {
            Program.Logger.Debug("Saving options to file...");
            try
            {
                string file = @".\Settings\Options.xml";
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(file);

                var CloseCK3_ComboBox = General_Tab.Controls.Find("OptionSelection_CloseCK3", true)[0] as ComboBox;
                var CloseAttila_ComboBox = General_Tab.Controls.Find("OptionSelection_CloseAttila", true)[0] as ComboBox;
                var FullArmies_ComboBox = General_Tab.Controls.Find("OptionSelection_FullArmies", true)[0] as ComboBox;
                var TimeLimit_ComboBox = General_Tab.Controls.Find("OptionSelection_TimeLimit", true)[0] as ComboBox;
                var BattleMapsSize_ComboBox = General_Tab.Controls.Find("OptionSelection_BattleMapsSize", true)[0] as ComboBox;
                var DefensiveDeployables_ComboBox = General_Tab.Controls.Find("OptionSelection_DefensiveDeployables", true)[0] as ComboBox;
                var UnitCards_ComboBox = General_Tab.Controls.Find("OptionSelection_UnitCards", true)[0] as ComboBox;
                var SeparateArmies_ComboBox = General_Tab.Controls.Find("OptionSelection_SeparateArmies", true)[0] as ComboBox;

                var LeviesMax_ComboBox = Units_Tab.Controls.Find("OptionSelection_LeviesMax", true)[0] as ComboBox;
                var RangedMax_ComboBox = Units_Tab.Controls.Find("OptionSelection_RangedMax", true)[0] as ComboBox;
                var InfantryMax_ComboBox = Units_Tab.Controls.Find("OptionSelection_InfantryMax", true)[0] as ComboBox;
                var CavalryMax_ComboBox = Units_Tab.Controls.Find("OptionSelection_CavalryMax", true)[0] as ComboBox;

                var BattleScale_ComboBox = BattleScale_Tab.Controls.Find("OptionSelection_BattleSizeScale", true)[0] as ComboBox;
                var AutoScaleUnits_ComboBox = BattleScale_Tab.Controls.Find("OptionSelection_AutoScale", true)[0] as ComboBox;
                



                var CloseCK3_Node = xmlDoc.SelectSingleNode("//Option [@name='CloseCK3']");
                CloseCK3_Node.InnerText = CloseCK3_ComboBox.Text;
                var CloseAttila_Node = xmlDoc.SelectSingleNode("//Option [@name='CloseAttila']");
                CloseAttila_Node.InnerText = CloseAttila_ComboBox.Text;
                var FullArmies_Node = xmlDoc.SelectSingleNode("//Option [@name='FullArmies']");
                FullArmies_Node.InnerText = FullArmies_ComboBox.Text;
                var TimeLimit_Node = xmlDoc.SelectSingleNode("//Option [@name='TimeLimit']");
                TimeLimit_Node.InnerText = TimeLimit_ComboBox.Text;
                var BattleMapsSize_Node = xmlDoc.SelectSingleNode("//Option [@name='BattleMapsSize']");
                BattleMapsSize_Node.InnerText = BattleMapsSize_ComboBox.Text;
                var DefensiveDeployables_Node = xmlDoc.SelectSingleNode("//Option [@name='DefensiveDeployables']");
                DefensiveDeployables_Node.InnerText = DefensiveDeployables_ComboBox.Text;
                var UnitCards_Node = xmlDoc.SelectSingleNode("//Option [@name='UnitCards']");
                UnitCards_Node.InnerText = UnitCards_ComboBox.Text;
                var SeparateArmies_Node = xmlDoc.SelectSingleNode("//Option [@name='SeparateArmies']");
                SeparateArmies_Node.InnerText = SeparateArmies_ComboBox.Text;
                var CulturalPreciseness_Node = xmlDoc.SelectSingleNode("//Option [@name='CulturalPreciseness']");

                var LeviesMax_Node = xmlDoc.SelectSingleNode("//Option [@name='LeviesMax']");
                LeviesMax_Node.InnerText = LeviesMax_ComboBox.Text;
                var RangedMax_Node = xmlDoc.SelectSingleNode("//Option [@name='RangedMax']");
                RangedMax_Node.InnerText = RangedMax_ComboBox.Text;
                var InfantryMax_Node = xmlDoc.SelectSingleNode("//Option [@name='InfantryMax']");
                InfantryMax_Node.InnerText = InfantryMax_ComboBox.Text;
                var CavalryMax_Node = xmlDoc.SelectSingleNode("//Option [@name='CavalryMax']");
                CavalryMax_Node.InnerText = CavalryMax_ComboBox.Text;

                var BattleScale_Node = xmlDoc.SelectSingleNode("//Option [@name='BattleScale']");
                BattleScale_Node.InnerText = BattleScale_ComboBox.Text;
                var AutoScaleUnits_Node = xmlDoc.SelectSingleNode("//Option [@name='AutoScaleUnits']");
                AutoScaleUnits_Node.InnerText = AutoScaleUnits_ComboBox.Text;


                xmlDoc.Save(file);
                Program.Logger.Debug("Options saved to file.");
            }
            catch (Exception ex)
            {
                Program.Logger.Log(ex);
                MessageBox.Show("Error saving game options. Changes may not be saved.", "Crusader Wars: Save Error",
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
            ToolTip_Attila.ToolTipTitle = "Crusader Kings 3 Path";
            ToolTip_Attila.SetToolTip(ck3Btn, Properties.Settings.Default.VAR_ck3_path);
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
            try
            {
                string file = @".\Settings\Paths.xml";
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(file);

                XmlNode node = xmlDoc.SelectSingleNode($"Paths/{game}");
                node.Attributes["path"].Value = new_path;
                xmlDoc.Save(file);
            }
            catch 
            {
                MessageBox.Show("Error setting game paths. Restart the mod and try again", "Crusader Wars: Data Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                Application.Exit();
            }


        }

        /*
         * To read after each update, so that the user
         * doesnt need to always set the game paths
         */
        public static void ReadGamePaths()
        {
            try
            {
                string file = @".\Settings\Paths.xml";
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(file);

                //Read Attila Path
                XmlNode attila_node = xmlDoc.SelectSingleNode("Paths/TotalWarAttila");
                Properties.Settings.Default.VAR_attila_path = attila_node.Attributes["path"].Value;
                Properties.Settings.Default.Save();

                //Read CK3 Path
                XmlNode ck3_node = xmlDoc.SelectSingleNode("Paths/CrusaderKings");
                Properties.Settings.Default.VAR_ck3_path = ck3_node.Attributes["path"].Value;
                Properties.Settings.Default.Save();
            }
            catch 
            {
                MessageBox.Show("Error reading game paths. Restart the mod and try again", "Crusader Wars: Data Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                Application.Exit();
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


        /*##############################################
         *####             UNIT MAPPERS             #### 
         *####--------------------------------------####
         *####         Unit Mappers Section         ####
         *##############################################
         */

        UC_UnitMapper CrusaderKings_Tab;
        UC_UnitMapper TheFallenEagle_Tab;
        UC_UnitMapper RealmsInExile_Tab;
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



        void ChangeUnitMappersTab(Control control)
        {
            control.Dock = DockStyle.Fill;
            UMpanel.Controls.Clear();
            UMpanel.Controls.Add(control);
            control.BringToFront();

            // Define colors
            Color inactiveColor = System.Drawing.Color.FromArgb(64, 64, 64);
            Color activeColor = System.Drawing.Color.FromArgb(100, 100, 100);

            // Reset all buttons
            Btn_CK3Tab.BackgroundImage = null;
            Btn_TFETab.BackgroundImage = null;
            Btn_LOTRTab.BackgroundImage = null;
            Btn_CK3Tab.BackColor = inactiveColor;
            Btn_TFETab.BackColor = inactiveColor;
            Btn_LOTRTab.BackColor = inactiveColor;
            Btn_CK3Tab.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            Btn_TFETab.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            Btn_LOTRTab.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            Btn_CK3Tab.FlatAppearance.BorderSize = 1;
            Btn_TFETab.FlatAppearance.BorderSize = 1;
            Btn_LOTRTab.FlatAppearance.BorderSize = 1;

            // Highlight active button
            Button activeButton = null;
            if (control == CrusaderKings_Tab) activeButton = Btn_CK3Tab;
            else if (control == TheFallenEagle_Tab) activeButton = Btn_TFETab;
            else if (control == RealmsInExile_Tab) activeButton = Btn_LOTRTab;

            if (activeButton != null)
            {
                activeButton.BackColor = activeColor;
            }
        }

        List<string> GetUnitMappersModsCollectionFromTag(string tag)
        {
            var unit_mappers_folder = Directory.GetDirectories(@".\unit mappers");
            List<string> requiredMods = new List<string>();

            foreach(var mapper in unit_mappers_folder)
            {
                string mapperName = Path.GetDirectoryName(mapper);
                var files = Directory.GetFiles(mapper);
                foreach(var file in files)
                {
                    string fileName = Path.GetFileName(file);
                    if(fileName == "tag.txt")
                    {
                        string fileTag = File.ReadAllText(file);
                        if(tag == fileTag)
                        {
                            string modsPath = mapper + @"\Mods.xml";
                            if(File.Exists(modsPath))
                            {
                                XmlDocument xmlDocument = new XmlDocument();
                                xmlDocument.Load(modsPath);
                                foreach (XmlNode node in xmlDocument.DocumentElement.ChildNodes)
                                {
                                    if (node is XmlComment) continue;
                                    if (node.Name == "Mod")
                                    {
                                        requiredMods.Add(node.InnerText);
                                    }
                                }
                            }
                            else
                            {
                                MessageBox.Show($"Mods.xml was not found in {mapper}", "Crusader Wars: Crusader Wars: Unit Mappers Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                            }
                            break;
                        }
                    }
                }
            }

            return requiredMods;
        }

        void ReadUnitMappersOptions()
        {
            string file = @".\Settings\UnitMappers.xml";
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(file);

            var ck3ToggleStateStr = xmlDoc.SelectSingleNode("//UnitMappers [@name='DefaultCK3']").InnerText;
            var tfeToggleStateStr = xmlDoc.SelectSingleNode("//UnitMappers [@name='TheFallenEagle']").InnerText;
            var lotrToggleStateStr = xmlDoc.SelectSingleNode("//UnitMappers [@name='RealmsInExile']").InnerText;

            bool ck3ToggleState = false; bool tfeToggleState = false; bool lotrToggleState = false;
            if (ck3ToggleStateStr == "True") ck3ToggleState = true; else ck3ToggleState = false;
            if (tfeToggleStateStr == "True") tfeToggleState = true; else tfeToggleState = false;
            if (lotrToggleStateStr == "True") lotrToggleState = true; else lotrToggleState = false;

            CrusaderKings_Tab = new UC_UnitMapper(Properties.Resources._default, "https://steamcommunity.com/sharedfiles/filedetails/?id=3301634851", GetUnitMappersModsCollectionFromTag("DefaultCK3"),ck3ToggleState);
            TheFallenEagle_Tab = new UC_UnitMapper(Properties.Resources.tfe, "https://steamcommunity.com/sharedfiles/filedetails/?id=3301639735", GetUnitMappersModsCollectionFromTag("TheFallenEagle"), tfeToggleState);
            RealmsInExile_Tab = new UC_UnitMapper(Properties.Resources.LOTR, "https://steamcommunity.com/sharedfiles/filedetails/?id=3211765434", GetUnitMappersModsCollectionFromTag("RealmsInExile"), lotrToggleState);

            CrusaderKings_Tab.SetOtherControlsReferences(new UC_UnitMapper[] { TheFallenEagle_Tab, RealmsInExile_Tab });
            TheFallenEagle_Tab.SetOtherControlsReferences(new UC_UnitMapper[] { CrusaderKings_Tab, RealmsInExile_Tab });
            RealmsInExile_Tab.SetOtherControlsReferences(new UC_UnitMapper[] { CrusaderKings_Tab, TheFallenEagle_Tab });

            ChangeUnitMappersTab(CrusaderKings_Tab);
        }

        void WriteUnitMappersOptions()
        {
            string file = @".\Settings\UnitMappers.xml";
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(file);

            var CrusaderKings_Node = xmlDoc.SelectSingleNode("//UnitMappers [@name='DefaultCK3']");
            CrusaderKings_Node.InnerText = CrusaderKings_Tab.GetState().ToString();
            var TheFallenEagle_Node = xmlDoc.SelectSingleNode("//UnitMappers [@name='TheFallenEagle']");
            TheFallenEagle_Node.InnerText = TheFallenEagle_Tab.GetState().ToString();
            var RealmsInExile_Node = xmlDoc.SelectSingleNode("//UnitMappers [@name='RealmsInExile']");
            RealmsInExile_Node.InnerText = RealmsInExile_Tab.GetState().ToString();
            xmlDoc.Save(file);
        }

    }
}
