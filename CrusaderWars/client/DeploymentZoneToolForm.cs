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
        private readonly bool _isAttackerPlayer;

        // Dragging and Resizing State
        private enum DragHandle { None, Body, TopLeft, Top, TopRight, Right, BottomRight, Bottom, BottomLeft, Left }
        private DragHandle _activeHandle = DragHandle.None;
        private bool _isDragging = false;
        private Point _lastMousePosition;
        private RectangleF _dragStartZone;
        private bool _isAttackerZoneActive = false;
        private bool _ignoreNudChanges = false;


        public DeploymentZoneToolForm(DeploymentArea attackerArea, DeploymentArea defenderArea, float mapDimension, bool isAttackerPlayer)
        {
            InitializeComponent();

            _initialAttackerArea = attackerArea;
            _initialDefenderArea = defenderArea;
            _mapDimension = mapDimension;
            _isAttackerPlayer = isAttackerPlayer;

            // Calculate scale to fit map into panel
            _scale = MAP_PANEL_SIZE / _mapDimension;

            // Set labels based on player side
            if (_isAttackerPlayer)
            {
                attackerGroupBox.Text = "Player Alliance Zone";
                defenderGroupBox.Text = "Enemy Alliance Zone";
            }
            else
            {
                attackerGroupBox.Text = "Enemy Alliance Zone";
                defenderGroupBox.Text = "Player Alliance Zone";
            }


            // Populate controls
            PopulateControls(_initialAttackerArea, true);
            PopulateControls(_initialDefenderArea, false);

            // Set up event handlers
            mapPanel.Paint += MapPanel_Paint;
            mapPanel.MouseDown += MapPanel_MouseDown;
            mapPanel.MouseMove += MapPanel_MouseMove;
            mapPanel.MouseUp += MapPanel_MouseUp;
            mapPanel.MouseLeave += (s, e) => { this.Cursor = Cursors.Default; };
            AddValueChangedHandlers();

            // Initial draw
            UpdateZonesAndRedraw();
        }

        private void PopulateControls(DeploymentArea area, bool isAttacker)
        {
            _ignoreNudChanges = true;
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
            _ignoreNudChanges = false;
        }

        private void AddValueChangedHandlers()
        {
            nudAttackerX.ValueChanged += (s, e) => { if (!_ignoreNudChanges) UpdateZonesAndRedraw(); };
            nudAttackerY.ValueChanged += (s, e) => { if (!_ignoreNudChanges) UpdateZonesAndRedraw(); };
            nudAttackerWidth.ValueChanged += (s, e) => { if (!_ignoreNudChanges) UpdateZonesAndRedraw(); };
            nudAttackerHeight.ValueChanged += (s, e) => { if (!_ignoreNudChanges) UpdateZonesAndRedraw(); };
            nudDefenderX.ValueChanged += (s, e) => { if (!_ignoreNudChanges) UpdateZonesAndRedraw(); };
            nudDefenderY.ValueChanged += (s, e) => { if (!_ignoreNudChanges) UpdateZonesAndRedraw(); };
            nudDefenderWidth.ValueChanged += (s, e) => { if (!_ignoreNudChanges) UpdateZonesAndRedraw(); };
            nudDefenderHeight.ValueChanged += (s, e) => { if (!_ignoreNudChanges) UpdateZonesAndRedraw(); };

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


            // Draw attacker zone (Blue for player, Orange for enemy)
            Color attackerColor = _isAttackerPlayer ? Color.Blue : Color.OrangeRed;
            DrawZone(g, _attackerZone, attackerColor);


            // Draw defender zone (Red for enemy, Blue for player)
            Color defenderColor = _isAttackerPlayer ? Color.Red : Color.Blue;
            DrawZone(g, _defenderZone, defenderColor);
        }

        private void DrawZone(Graphics g, RectangleF zone, Color color)
        {
            RectangleF scaledRect = new RectangleF(
                zone.X * _scale,
                -zone.Y * _scale - (zone.Height * _scale), // Invert Y for drawing
                zone.Width * _scale,
                zone.Height * _scale
            );
            using (var brush = new SolidBrush(Color.FromArgb(128, color)))
            {
                g.FillRectangle(brush, scaledRect);
            }
            using (var pen = new Pen(color, 2))
            {
                g.DrawRectangle(pen, Rectangle.Round(scaledRect));
            }
        }


        private void MapPanel_MouseDown(object sender, MouseEventArgs e)
        {
            PointF mapPoint = PixelToMapCoords(e.Location);
            _activeHandle = GetHandleAtPoint(_attackerZone, mapPoint);
            if (_activeHandle != DragHandle.None)
            {
                _isDragging = true;
                _isAttackerZoneActive = true;
                _dragStartZone = _attackerZone;
                _lastMousePosition = e.Location;
                return;
            }

            _activeHandle = GetHandleAtPoint(_defenderZone, mapPoint);
            if (_activeHandle != DragHandle.None)
            {
                _isDragging = true;
                _isAttackerZoneActive = false;
                _dragStartZone = _defenderZone;
                _lastMousePosition = e.Location;
                return;
            }
        }

        private void MapPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging)
            {
                PointF mapPoint = PixelToMapCoords(e.Location);
                DragHandle handle = GetHandleAtPoint(_attackerZone, mapPoint);
                if (handle == DragHandle.None)
                {
                    handle = GetHandleAtPoint(_defenderZone, mapPoint);
                }
                UpdateCursor(handle);
                return;
            }

            float dx = (e.Location.X - _lastMousePosition.X) / _scale;
            float dy = -(e.Location.Y - _lastMousePosition.Y) / _scale; // Inverted Y
            _lastMousePosition = e.Location;

            RectangleF currentZone = _isAttackerZoneActive ? _attackerZone : _defenderZone;
            RectangleF newZone = currentZone;

            switch (_activeHandle)
            {
                case DragHandle.Body:
                    newZone.X += dx;
                    newZone.Y += dy;
                    break;
                case DragHandle.Left: newZone = new RectangleF(currentZone.X + dx, currentZone.Y, currentZone.Width - dx, currentZone.Height); break;
                case DragHandle.Right: newZone = new RectangleF(currentZone.X, currentZone.Y, currentZone.Width + dx, currentZone.Height); break;
                case DragHandle.Top: newZone = new RectangleF(currentZone.X, currentZone.Y, currentZone.Width, currentZone.Height + dy); break;
                case DragHandle.Bottom: newZone = new RectangleF(currentZone.X, currentZone.Y + dy, currentZone.Width, currentZone.Height - dy); break;
                case DragHandle.TopLeft: newZone = new RectangleF(currentZone.X + dx, currentZone.Y, currentZone.Width - dx, currentZone.Height + dy); break;
                case DragHandle.TopRight: newZone = new RectangleF(currentZone.X, currentZone.Y, currentZone.Width + dx, currentZone.Height + dy); break;
                case DragHandle.BottomLeft: newZone = new RectangleF(currentZone.X + dx, currentZone.Y + dy, currentZone.Width - dx, currentZone.Height - dy); break;
                case DragHandle.BottomRight: newZone = new RectangleF(currentZone.X, currentZone.Y + dy, currentZone.Width + dx, currentZone.Height - dy); break;
            }

            // Snap and Clamp
            newZone = SnapAndClampZone(newZone);


            if (_isAttackerZoneActive)
            {
                _attackerZone = newZone;
            }
            else
            {
                _defenderZone = newZone;
            }

            UpdateNudsFromZone(_isAttackerZoneActive);
            mapPanel.Invalidate();
        }

        private void MapPanel_MouseUp(object sender, MouseEventArgs e)
        {
            _isDragging = false;
            _activeHandle = DragHandle.None;
        }

        private PointF PixelToMapCoords(Point pixelPoint)
        {
            float mapX = (pixelPoint.X - MAP_PANEL_SIZE / 2f) / _scale;
            float mapY = -(pixelPoint.Y - MAP_PANEL_SIZE / 2f) / _scale;
            return new PointF(mapX, mapY);
        }

        private DragHandle GetHandleAtPoint(RectangleF zone, PointF mapPoint)
        {
            const int handleSize = 15; // in pixels
            float handleMapSize = handleSize / _scale;

            if (new RectangleF(zone.Left, zone.Top - handleMapSize, handleMapSize, handleMapSize).Contains(mapPoint)) return DragHandle.TopLeft;
            if (new RectangleF(zone.Right - handleMapSize, zone.Top - handleMapSize, handleMapSize, handleMapSize).Contains(mapPoint)) return DragHandle.TopRight;
            if (new RectangleF(zone.Left, zone.Bottom, handleMapSize, handleMapSize).Contains(mapPoint)) return DragHandle.BottomLeft;
            if (new RectangleF(zone.Right - handleMapSize, zone.Bottom, handleMapSize, handleMapSize).Contains(mapPoint)) return DragHandle.BottomRight;

            if (new RectangleF(zone.Left, zone.Top - handleMapSize, zone.Width, handleMapSize).Contains(mapPoint)) return DragHandle.Top;
            if (new RectangleF(zone.Left, zone.Bottom, zone.Width, handleMapSize).Contains(mapPoint)) return DragHandle.Bottom;
            if (new RectangleF(zone.Left, zone.Top - handleMapSize, handleMapSize, zone.Height).Contains(mapPoint)) return DragHandle.Left;
            if (new RectangleF(zone.Right - handleMapSize, zone.Top - handleMapSize, handleMapSize, zone.Height).Contains(mapPoint)) return DragHandle.Right;

            if (zone.Contains(mapPoint)) return DragHandle.Body;

            return DragHandle.None;
        }

        private void UpdateCursor(DragHandle handle)
        {
            switch (handle)
            {
                case DragHandle.Body:
                    this.Cursor = Cursors.SizeAll;
                    break;
                case DragHandle.Top:
                case DragHandle.Bottom:
                    this.Cursor = Cursors.SizeNS;
                    break;
                case DragHandle.Left:
                case DragHandle.Right:
                    this.Cursor = Cursors.SizeWE;
                    break;
                case DragHandle.TopLeft:
                case DragHandle.BottomRight:
                    this.Cursor = Cursors.SizeNWSE;
                    break;
                case DragHandle.TopRight:
                case DragHandle.BottomLeft:
                    this.Cursor = Cursors.SizeNESW;
                    break;
                default:
                    this.Cursor = Cursors.Default;
                    break;
            }
        }

        private RectangleF SnapAndClampZone(RectangleF zone)
        {
            // Snap position and size to increments of 50
            float x = (float)Math.Round(zone.X / 50) * 50;
            float y = (float)Math.Round(zone.Y / 50) * 50;
            float width = Math.Max(50, (float)Math.Round(zone.Width / 50) * 50);
            float height = Math.Max(50, (float)Math.Round(zone.Height / 50) * 50);

            // Clamp to map boundaries
            float boundary = _mapDimension / 2f;
            float edgeBuffer = 50f;

            // Clamp size first to prevent issues with position clamping
            width = Math.Min(width, 2 * boundary - 2 * edgeBuffer);
            height = Math.Min(height, 2 * boundary - 2 * edgeBuffer);

            // Clamp position
            float min_x = -boundary + edgeBuffer;
            float max_x = boundary - width - edgeBuffer;
            x = x < min_x ? min_x : (x > max_x ? max_x : x); // Safe clamp

            float min_y = -boundary + edgeBuffer;
            float max_y = boundary - height - edgeBuffer;
            y = y < min_y ? min_y : (y > max_y ? max_y : y); // Safe clamp

            return new RectangleF(x, y, width, height);
        }


        private void UpdateNudsFromZone(bool isAttacker)
        {
            _ignoreNudChanges = true;
            if (isAttacker)
            {
                nudAttackerX.Value = (decimal)(_attackerZone.X + _attackerZone.Width / 2);
                nudAttackerY.Value = (decimal)(_attackerZone.Y + _attackerZone.Height / 2);
                nudAttackerWidth.Value = (decimal)_attackerZone.Width;
                nudAttackerHeight.Value = (decimal)_attackerZone.Height;
            }
            else
            {
                nudDefenderX.Value = (decimal)(_defenderZone.X + _defenderZone.Width / 2);
                nudDefenderY.Value = (decimal)(_defenderZone.Y + _defenderZone.Height / 2);
                nudDefenderWidth.Value = (decimal)_defenderZone.Width;
                nudDefenderHeight.Value = (decimal)_defenderZone.Height;
            }
            _ignoreNudChanges = false;
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
