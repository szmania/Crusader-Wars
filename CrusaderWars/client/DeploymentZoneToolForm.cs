using CrusaderWars.terrain;
using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace CrusaderWars.client
{
    public partial class DeploymentZoneToolForm : Form
    {
        private readonly DeploymentArea _initialAttackerArea;
        private readonly DeploymentArea _initialDefenderArea;
        private RectangleF _attackerZone;
        private RectangleF _defenderZone;
        private readonly float _scale;
        private readonly float _mapDimension;
        private const int MAP_PANEL_SIZE = 500;

        public DeploymentZoneToolForm(DeploymentArea attackerArea, DeploymentArea defenderArea, float mapDimension)
        {
            InitializeComponent();

            _initialAttackerArea = attackerArea;
            _initialDefenderArea = defenderArea;
            _mapDimension = mapDimension;

            // Calculate scale to fit map into panel
            _scale = MAP_PANEL_SIZE / _mapDimension;

            // Populate controls
            PopulateControls(_initialAttackerArea, true);
            PopulateControls(_initialDefenderArea, false);

            // Set up event handlers
            mapPanel.Paint += MapPanel_Paint;
            AddValueChangedHandlers();

            // Initial draw
            UpdateZonesAndRedraw();
        }

        private void PopulateControls(DeploymentArea area, bool isAttacker)
        {
            if (isAttacker)
            {
                nudAttackerX.Value = decimal.Parse(area.X, CultureInfo.InvariantCulture);
                nudAttackerY.Value = decimal.Parse(area.Y, CultureInfo.InvariantCulture);
                nudAttackerWidth.Value = decimal.Parse(area.Width, CultureInfo.InvariantCulture);
                nudAttackerHeight.Value = decimal.Parse(area.Height, CultureInfo.InvariantCulture);
            }
            else
            {
                nudDefenderX.Value = decimal.Parse(area.X, CultureInfo.InvariantCulture);
                nudDefenderY.Value = decimal.Parse(area.Y, CultureInfo.InvariantCulture);
                nudDefenderWidth.Value = decimal.Parse(area.Width, CultureInfo.InvariantCulture);
                nudDefenderHeight.Value = decimal.Parse(area.Height, CultureInfo.InvariantCulture);
            }
        }

        private void AddValueChangedHandlers()
        {
            nudAttackerX.ValueChanged += (s, e) => UpdateZonesAndRedraw();
            nudAttackerY.ValueChanged += (s, e) => UpdateZonesAndRedraw();
            nudAttackerWidth.ValueChanged += (s, e) => UpdateZonesAndRedraw();
            nudAttackerHeight.ValueChanged += (s, e) => UpdateZonesAndRedraw();
            nudDefenderX.ValueChanged += (s, e) => UpdateZonesAndRedraw();
            nudDefenderY.ValueChanged += (s, e) => UpdateZonesAndRedraw();
            nudDefenderWidth.ValueChanged += (s, e) => UpdateZonesAndRedraw();
            nudDefenderHeight.ValueChanged += (s, e) => UpdateZonesAndRedraw();

            btnSave.Click += BtnSave_Click;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
        }

        private void UpdateZonesAndRedraw()
        {
            // Update attacker zone from controls
            float ax = (float)nudAttackerX.Value;
            float ay = (float)nudAttackerY.Value;
            float aWidth = (float)nudAttackerWidth.Value;
            float aHeight = (float)nudAttackerHeight.Value;
            _attackerZone = new RectangleF(ax - aWidth / 2, ay - aHeight / 2, aWidth, aHeight);

            // Update defender zone from controls
            float dx = (float)nudDefenderX.Value;
            float dy = (float)nudDefenderY.Value;
            float dWidth = (float)nudDefenderWidth.Value;
            float dHeight = (float)nudDefenderHeight.Value;
            _defenderZone = new RectangleF(dx - dWidth / 2, dy - dHeight / 2, dWidth, dHeight);

            mapPanel.Invalidate(); // Trigger repaint
        }

        private void MapPanel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Color.DarkSeaGreen); // Map background

            // Translate origin to center of panel
            g.TranslateTransform(MAP_PANEL_SIZE / 2f, MAP_PANEL_SIZE / 2f);

            // Draw map boundary
            float boundary = _mapDimension / 2f * _scale;
            g.DrawRectangle(Pens.Black, -boundary, -boundary, boundary * 2, boundary * 2);


            // Draw attacker zone (Blue)
            RectangleF scaledAttackerRect = new RectangleF(
                _attackerZone.X * _scale,
                -_attackerZone.Y * _scale - (_attackerZone.Height * _scale), // Invert Y for drawing
                _attackerZone.Width * _scale,
                _attackerZone.Height * _scale
            );
            using (var brush = new SolidBrush(Color.FromArgb(128, 0, 0, 255)))
            {
                g.FillRectangle(brush, scaledAttackerRect);
            }
            g.DrawRectangle(Pens.Blue, Rectangle.Round(scaledAttackerRect));


            // Draw defender zone (Red)
            RectangleF scaledDefenderRect = new RectangleF(
                _defenderZone.X * _scale,
                -_defenderZone.Y * _scale - (_defenderZone.Height * _scale), // Invert Y for drawing
                _defenderZone.Width * _scale,
                _defenderZone.Height * _scale
            );
            using (var brush = new SolidBrush(Color.FromArgb(128, 255, 0, 0)))
            {
                g.FillRectangle(brush, scaledDefenderRect);
            }
            g.DrawRectangle(Pens.Red, Rectangle.Round(scaledDefenderRect));
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        public class DeploymentZoneValues
        {
            public decimal CenterX { get; set; }
            public decimal CenterY { get; set; }
            public decimal Width { get; set; }
            public decimal Height { get; set; }
        }

        public DeploymentZoneValues GetAttackerValues()
        {
            return new DeploymentZoneValues
            {
                CenterX = nudAttackerX.Value,
                CenterY = nudAttackerY.Value,
                Width = nudAttackerWidth.Value,
                Height = nudAttackerHeight.Value
            };
        }

        public DeploymentZoneValues GetDefenderValues()
        {
            return new DeploymentZoneValues
            {
                CenterX = nudDefenderX.Value,
                CenterY = nudDefenderY.Value,
                Width = nudDefenderWidth.Value,
                Height = nudDefenderHeight.Value
            };
        }
    }
}
