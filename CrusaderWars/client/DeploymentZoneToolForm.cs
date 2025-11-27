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
        private RectangleF _unsnappedAttackerZone;
        private RectangleF _unsnappedDefenderZone;
        private readonly float _scale;
        private readonly float _mapDimension;
        private readonly bool _isAttackerPlayer;
        private readonly bool _isSiegeBattle;

        // Dragging and Resizing State
        private enum DragHandle { None, Body, TopLeft, Top, TopRight, Right, BottomRight, Bottom, BottomLeft, Left }
        private DragHandle _activeHandle = DragHandle.None;
        private bool _isDragging = false;
        private Point _lastMousePosition;
        private bool _isAttackerZoneActive = false;
        private bool _ignoreNudChanges = false;


        public DeploymentZoneToolForm(DeploymentArea attackerArea, DeploymentArea defenderArea, float mapDimension, bool isAttackerPlayer, bool isSiegeBattle, string battleDate, string battleType, string provinceName, string mapX, string mapY)
        {
            InitializeComponent();

            _initialAttackerArea = attackerArea;
            _initialDefenderArea = defenderArea;
            _mapDimension = mapDimension;
            _isAttackerPlayer = isAttackerPlayer;
            _isSiegeBattle = isSiegeBattle;

            // Set battle details
            lblBattleDate.Text = $"Date: {battleDate}";
            lblBattleType.Text = $"Type: {battleType}";
            lblProvinceName.Text = $"Location: {provinceName}";
            lblCoordinates.Text = $"Coords: ({mapX}, {mapY})";

            // Calculate scale to fit map into panel
            _scale = (float)mapPanel.ClientRectangle.Width / _mapDimension;

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
            // Create proposed new zones from controls
            float ax = (float)nudAttackerX.Value;
            float ay = (float)nudAttackerY.Value;
            float aWidth = (float)nudAttackerWidth.Value;
            float aHeight = (float)nudAttackerHeight.Value;
            RectangleF newAttackerZone = new RectangleF(ax - aWidth / 2, ay - aHeight / 2, aWidth, aHeight);

            float dx = (float)nudDefenderX.Value;
            float dy = (float)nudDefenderY.Value;
            float dWidth = (float)nudDefenderWidth.Value;
            float dHeight = (float)nudDefenderHeight.Value;
            RectangleF newDefenderZone = new RectangleF(dx - dWidth / 2, dy - dHeight / 2, dWidth, dHeight);

            // Check for collision
            RectangleF collisionCheckZone = newDefenderZone;
            collisionCheckZone.Inflate(50, 50); // Add 50 margin on all sides
            if (newAttackerZone.IntersectsWith(collisionCheckZone))
            {
                // Collision detected. Revert the NUDs that caused it.
                // The one that doesn't match the current state is the one that changed.
                if (newAttackerZone != _attackerZone)
                {
                    UpdateNudsFromZone(true); // Revert attacker NUDs to match _attackerZone
                }
                if (newDefenderZone != _defenderZone)
                {
                    UpdateNudsFromZone(false); // Revert defender NUDs to match _defenderZone
                }
                return; // Stop processing
            }

            // No collision, so update the state
            _attackerZone = newAttackerZone;
            _defenderZone = newDefenderZone;

            mapPanel.Invalidate(); // Trigger repaint
        }

        private void MapPanel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Color.DarkSeaGreen); // Map background

            // Translate origin to center of panel
            g.TranslateTransform(mapPanel.ClientRectangle.Width / 2f, mapPanel.ClientRectangle.Height / 2f);

            // Draw map boundary
            float boundary = _mapDimension / 2f * _scale;
            g.DrawRectangle(Pens.Black, -boundary, -boundary, boundary * 2, boundary * 2);

            // Draw settlement indicator if it's a siege battle
            if (_isSiegeBattle)
            {
                float settlementSize = 80; // 80 pixels
                RectangleF settlementRect = new RectangleF(-settlementSize / 2, -settlementSize / 2, settlementSize, settlementSize);

                using (var settlementBrush = new SolidBrush(Color.FromArgb(150, Color.SaddleBrown)))
                {
                    g.FillRectangle(settlementBrush, settlementRect);
                }
                using (var settlementPen = new Pen(Color.Black, 2))
                {
                    g.DrawRectangle(settlementPen, Rectangle.Round(settlementRect));
                }
                // Draw label
                using (var font = new Font("Arial", 8, FontStyle.Bold))
                using (var stringFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (var textBrush = new SolidBrush(Color.White))
                {
                    g.DrawString("Settlement", font, textBrush, settlementRect, stringFormat);
                }
            }


            // Draw attacker zone (Blue for player, Orange for enemy)
            Color attackerColor = _isAttackerPlayer ? Color.Blue : Color.OrangeRed;
            string attackerLabel = _isAttackerPlayer ? "Player" : "Enemy";
            DrawZone(g, _attackerZone, attackerColor, attackerLabel);


            // Draw defender zone (Red for enemy, Blue for player)
            Color defenderColor = _isAttackerPlayer ? Color.Red : Color.Blue;
            string defenderLabel = _isAttackerPlayer ? "Enemy" : "Player";
            DrawZone(g, _defenderZone, defenderColor, defenderLabel);
        }

        private void DrawZone(Graphics g, RectangleF zone, Color color, string label)
        {
            // In our map system, Y is the bottom-left corner. For drawing, we need top-left.
            RectangleF scaledRect = new RectangleF(
                zone.X * _scale,
                -(zone.Y + zone.Height) * _scale, // Invert Y and shift by height
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
            // Draw label
            using (var font = new Font("Arial", 10, FontStyle.Bold))
            using (var stringFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            using (var textBrush = new SolidBrush(Color.White))
            {
                g.DrawString(label, font, textBrush, scaledRect, stringFormat);
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
                _lastMousePosition = e.Location;
                _unsnappedAttackerZone = _attackerZone; // Sync for dragging
                return;
            }

            _activeHandle = GetHandleAtPoint(_defenderZone, mapPoint);
            if (_activeHandle != DragHandle.None)
            {
                _isDragging = true;
                _isAttackerZoneActive = false;
                _lastMousePosition = e.Location;
                _unsnappedDefenderZone = _defenderZone; // Sync for dragging
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

            RectangleF currentZone = _isAttackerZoneActive ? _unsnappedAttackerZone : _unsnappedDefenderZone;
            RectangleF newZone = currentZone;

            // In our Y-up system, RectangleF's (X, Y) is the bottom-left corner.
            switch (_activeHandle)
            {
                case DragHandle.Body:
                    newZone.X += dx;
                    newZone.Y += dy;
                    break;
                // Edges
                case DragHandle.Left: newZone.X += dx; newZone.Width -= dx; break;
                case DragHandle.Right: newZone.Width += dx; break;
                case DragHandle.Bottom: newZone.Y += dy; newZone.Height -= dy; break;
                case DragHandle.Top: newZone.Height += dy; break;
                // Corners
                case DragHandle.TopLeft: newZone.X += dx; newZone.Width -= dx; newZone.Height += dy; break;
                case DragHandle.TopRight: newZone.Width += dx; newZone.Height += dy; break;
                case DragHandle.BottomLeft: newZone.X += dx; newZone.Width -= dx; newZone.Y += dy; newZone.Height -= dy; break;
                case DragHandle.BottomRight: newZone.Width += dx; newZone.Y += dy; newZone.Height -= dy; break;
            }

            // --- Collision Detection ---
            RectangleF otherZone = _isAttackerZoneActive ? _defenderZone : _attackerZone;
            RectangleF collisionZone = otherZone;
            collisionZone.Inflate(50, 50); // Add 50 margin on all sides

            if (newZone.IntersectsWith(collisionZone))
            {
                return; // Don't allow move/resize that causes overlap
            }
            // --- End Collision Detection ---

            _lastMousePosition = e.Location; // Update last mouse position only on valid move

            if (_isAttackerZoneActive)
            {
                _unsnappedAttackerZone = newZone;
                _attackerZone = SnapAndClampZone(newZone);
            }
            else
            {
                _unsnappedDefenderZone = newZone;
                _defenderZone = SnapAndClampZone(newZone);
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
            float mapX = (pixelPoint.X - mapPanel.ClientRectangle.Width / 2f) / _scale;
            float mapY = -(pixelPoint.Y - mapPanel.ClientRectangle.Height / 2f) / _scale;
            return new PointF(mapX, mapY);
        }

        private DragHandle GetHandleAtPoint(RectangleF zone, PointF mapPoint)
        {
            const int handleSize = 15; // in pixels
            float handleMapSize = handleSize / _scale;

            float left = zone.X;
            float right = zone.X + zone.Width;
            float bottom = zone.Y;
            float top = zone.Y + zone.Height;

            // Corners
            if (mapPoint.X >= left && mapPoint.X <= left + handleMapSize && mapPoint.Y <= top && mapPoint.Y >= top - handleMapSize) return DragHandle.TopLeft;
            if (mapPoint.X <= right && mapPoint.X >= right - handleMapSize && mapPoint.Y <= top && mapPoint.Y >= top - handleMapSize) return DragHandle.TopRight;
            if (mapPoint.X >= left && mapPoint.X <= left + handleMapSize && mapPoint.Y >= bottom && mapPoint.Y <= bottom + handleMapSize) return DragHandle.BottomLeft;
            if (mapPoint.X <= right && mapPoint.X >= right - handleMapSize && mapPoint.Y >= bottom && mapPoint.Y <= bottom + handleMapSize) return DragHandle.BottomRight;

            // Edges
            if (mapPoint.X > left + handleMapSize && mapPoint.X < right - handleMapSize && mapPoint.Y <= top && mapPoint.Y >= top - handleMapSize) return DragHandle.Top;
            if (mapPoint.X > left + handleMapSize && mapPoint.X < right - handleMapSize && mapPoint.Y >= bottom && mapPoint.Y <= bottom + handleMapSize) return DragHandle.Bottom;
            if (mapPoint.Y > bottom + handleMapSize && mapPoint.Y < top - handleMapSize && mapPoint.X >= left && mapPoint.X <= left + handleMapSize) return DragHandle.Left;
            if (mapPoint.Y > bottom + handleMapSize && mapPoint.Y < top - handleMapSize && mapPoint.X <= right && mapPoint.X >= right - handleMapSize) return DragHandle.Right;

            // Body
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
            // Prevent flipping and enforce minimum size
            if (zone.Width < 50)
            {
                if (_activeHandle == DragHandle.Left || _activeHandle == DragHandle.TopLeft || _activeHandle == DragHandle.BottomLeft)
                    zone.X = zone.Right - 50;
                zone.Width = 50;
            }
            if (zone.Height < 50)
            {
                if (_activeHandle == DragHandle.Bottom || _activeHandle == DragHandle.BottomLeft || _activeHandle == DragHandle.BottomRight)
                    zone.Y = zone.Top - 50;
                zone.Height = 50;
            }

            // Snap position and size to increments of 50
            float x = (float)Math.Round(zone.X / 50) * 50;
            float y = (float)Math.Round(zone.Y / 50) * 50;
            float width = (float)Math.Round(zone.Width / 50) * 50;
            float height = (float)Math.Round(zone.Height / 50) * 50;

            // Ensure minimum size after snapping
            if (width < 50) width = 50;
            if (height < 50) height = 50;

            // Clamp to map boundaries
            float boundary = _mapDimension / 2f;
            float edgeBuffer = 50f;

            // Clamp position
            x = Math.Max(-boundary + edgeBuffer, x);
            y = Math.Max(-boundary + edgeBuffer, y);

            // Clamp size based on new position
            if (x + width > boundary - edgeBuffer)
            {
                width = boundary - edgeBuffer - x;
            }
            if (y + height > boundary - edgeBuffer)
            {
                height = boundary - edgeBuffer - y;
            }

            return new RectangleF(x, y, width, height);
        }


        private void UpdateNudsFromZone(bool isAttacker)
        {
            _ignoreNudChanges = true;
            RectangleF zone = isAttacker ? _attackerZone : _defenderZone;
            NumericUpDown nudX = isAttacker ? nudAttackerX : nudDefenderX;
            NumericUpDown nudY = isAttacker ? nudAttackerY : nudDefenderY;
            NumericUpDown nudW = isAttacker ? nudAttackerWidth : nudDefenderWidth;
            NumericUpDown nudH = isAttacker ? nudAttackerHeight : nudDefenderHeight;

            nudX.Value = (decimal)(zone.X + zone.Width / 2);
            nudY.Value = (decimal)(zone.Y + zone.Height / 2);
            nudW.Value = (decimal)zone.Width;
            nudH.Value = (decimal)zone.Height;
            
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
