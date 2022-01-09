using RadiusSaveConvertor;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadiusSaveEditorGUI
{
    //sizes and maps needed
    public class MapSurveyInfo
    {
        public List<ITR_OBJ> levelItems = new List<ITR_OBJ>();
        public List<ITR_FNPCData> levelEnemies = new List<ITR_FNPCData>();
        public List<ITR_FAnomaly> levelAnomalies = new List<ITR_FAnomaly>();
        public List<ITR_FArtifact> levelArtifacts = new List<ITR_FArtifact>();
        public float scale = 1.0f;
        public bool showEnemies = true;
        public bool showItems = true;
        public bool showArtifacts = true;
        public bool showAnomalies = true;
        public Bitmap bitmap = new Bitmap(512, 512);
        public PointF offset;
        public PointF size;

        public MapSurveyInfo(float scale)
        {
            this.scale = scale;
        }

        public void Clear()
        {
            levelItems.Clear();
            levelEnemies.Clear();
            levelAnomalies.Clear();
            levelArtifacts.Clear();
            showEnemies = true;
            showItems = true;
            showArtifacts = true;
            showAnomalies = true;
        }
        public Bitmap GetMap(bool enemies, bool items, bool artifacts, bool anomalies)
        {
            if (showEnemies != enemies || showItems != items || showArtifacts != artifacts || showAnomalies != anomalies)
            {
                showEnemies = enemies;
                showItems = items;
                showAnomalies = anomalies;
                showArtifacts = artifacts;
                Render();
            }
            return bitmap;
        }
        public void CountBoundaries()
        {
            var minX = 100000000000.0f;
            var maxX = -100000000000.0f;
            var minY = 100000000000.0f;
            var maxY = -100000000000.0f;
            foreach (ITR_OBJ item in levelItems)
            {
                var transform = (ITR_FTransform)((ITR_STRUCT)item.GetProp("HolderRelativeTransform").value).value;
                PointF position = new PointF(transform.position.x, transform.position.y);
                minX = (float)Math.Min(minX, position.X);
                maxX = (float)Math.Max(maxX, position.X);
                minY = (float)Math.Min(minY, position.Y);
                maxY = (float)Math.Max(maxY, position.Y);
            }
            /*
            foreach (ITR_FNPCData npc in levelEnemies)
            {
                var transform = npc.transform;
                PointF position = new PointF(transform.position.x, transform.position.y);
                minX = (float)Math.Min(minX, position.X);
                maxX = (float)Math.Max(maxX, position.X);
                minY = (float)Math.Min(minY, position.Y);
                maxY = (float)Math.Max(maxY, position.Y);
            }
            foreach (ITR_FAnomaly anomaly in levelAnomalies)
            {
                var transform = anomaly.transform;
                PointF position = new PointF(transform.position.x, transform.position.y);
                minX = (float)Math.Min(minX, position.X);
                maxX = (float)Math.Max(maxX, position.X);
                minY = (float)Math.Min(minY, position.Y);
                maxY = (float)Math.Max(maxY, position.Y);
            }
            foreach (ITR_FArtifact artifact in levelArtifacts)
            {
                var transform = artifact.transform;
                PointF position = new PointF(transform.position.x, transform.position.y);
                minX = (float)Math.Min(minX, position.X);
                maxX = (float)Math.Max(maxX, position.X);
                minY = (float)Math.Min(minY, position.Y);
                maxY = (float)Math.Max(maxY, position.Y);
            }
            */
            offset.X = -minX;
            offset.Y = -minY;
            size.X = (maxX - minX);
            size.Y = (maxY - minY);
            size.X = Math.Max(size.X, size.Y);
            size.Y = size.X;
        }
        public void Render()
        {
            Graphics g = Graphics.FromImage(bitmap);
            g.Clear(Color.Gray);
            g.DrawEllipse(Pens.Red, 0, 0, 512, 512);
            if (showItems)
            {
                foreach (ITR_OBJ item in levelItems) 
                {
                    var transform = (ITR_FTransform)((ITR_STRUCT)item.GetProp("HolderRelativeTransform").value).value;
                    PointF position = new PointF(transform.position.x, transform.position.y);
                    position.X = (position.X + offset.X) / (size.X * scale);
                    position.Y = 1.0f - (position.Y + offset.Y) / (size.Y * scale);
                    position.X *= 512 - 4;
                    position.Y *= 512 - 4;
                    g.FillEllipse(Brushes.Green, new RectangleF(position, new SizeF(8,8)));
                }
            }
            /*
            if (showEnemies)
            {
                foreach (ITR_FNPCData npc in levelEnemies)
                {
                    var transform = npc.transform;
                    PointF position = new PointF(transform.position.x, transform.position.y);
                    position.X = (position.X + offset.X) / (size.X * scale);
                    position.Y = (position.Y + offset.Y) / (size.Y * scale);
                    position.X *= 512;
                    position.Y *= 512;
                    g.DrawEllipse(Pens.Red, new RectangleF(position, new SizeF(8, 8)));
                }
            }
            if (showAnomalies)
            {
                foreach (ITR_FAnomaly anomaly in levelAnomalies)
                {
                    var transform = anomaly.transform;
                    PointF position = new PointF(transform.position.x, transform.position.y);
                    position.X = (position.X + offset.X) / (size.X * scale);
                    position.Y = (position.Y + offset.Y) / (size.Y * scale);
                    position.X *= 512;
                    position.Y *= 512;
                    g.DrawEllipse(Pens.Purple, new RectangleF(position, new SizeF(1, 1)));
                }
            }
            if (showArtifacts)
            {
                foreach (ITR_FArtifact artifact in levelArtifacts)
                {
                    var transform = artifact.transform;
                    PointF position = new PointF(transform.position.x, transform.position.y);
                    position.X = (position.X + offset.X) / (size.X * scale);
                    position.Y = (position.Y + offset.Y) / (size.Y * scale);
                    position.X *= 512;
                    position.Y *= 512;
                    g.DrawEllipse(Pens.Orange, new RectangleF(position, new SizeF(1, 1)));
                }
            }
            */
        }
    }
}
