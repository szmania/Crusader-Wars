using CrusaderWars.unit_mapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace CrusaderWars.mod_manager
{
    public partial class SubmodSelectionForm : Form
    {
        private readonly List<Submod> _availableSubmods;
        private readonly List<string> _initiallyActiveSubmods;
        public List<string> SelectedSubmodTags { get; private set; }

        public SubmodSelectionForm(List<Submod> availableSubmods, List<string> activeSubmods)
        {
            InitializeComponent();
            _availableSubmods = availableSubmods;
            _initiallyActiveSubmods = activeSubmods;
            SelectedSubmodTags = new List<string>();
        }

        private void SubmodSelectionForm_Load(object sender, EventArgs e)
        {
            checkedListBoxSubmods.DisplayMember = "ScreenName";
            foreach (var submod in _availableSubmods)
            {
                bool isChecked = _initiallyActiveSubmods.Contains(submod.Tag);
                checkedListBoxSubmods.Items.Add(submod, isChecked);
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            SelectedSubmodTags = checkedListBoxSubmods.CheckedItems
                .OfType<Submod>()
                .Select(s => s.Tag)
                .ToList();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
