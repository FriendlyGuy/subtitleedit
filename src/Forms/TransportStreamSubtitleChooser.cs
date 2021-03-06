﻿using Nikse.SubtitleEdit.Core;
using Nikse.SubtitleEdit.Core.SubtitleFormats;
using Nikse.SubtitleEdit.Core.TransportStream;
using Nikse.SubtitleEdit.Forms.Ocr;
using Nikse.SubtitleEdit.Logic;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Nikse.SubtitleEdit.Forms
{
    public partial class TransportStreamSubtitleChooser : PositionAndSizeForm
    {
        private TransportStreamParser _tsParser;
        private string _fileName;
        private ProgramMapTableParser _programMapTableParser;

        public TransportStreamSubtitleChooser()
        {
            UiUtil.PreInitialize(this);
            InitializeComponent();
            UiUtil.FixFonts(this);
            labelChoose.Text = Configuration.Settings.Language.MatroskaSubtitleChooser.PleaseChoose;
            buttonOK.Text = Configuration.Settings.Language.General.Ok;
            buttonCancel.Text = Configuration.Settings.Language.General.Cancel;
            toolStripMenuItemExport.Text = Configuration.Settings.Language.Main.Menu.File.Export;
            vobSubToolStripMenuItem.Text = Configuration.Settings.Language.Main.Menu.File.ExportVobSub;
            bDNXMLToolStripMenuItem.Text = Configuration.Settings.Language.Main.Menu.File.ExportBdnXml;
            bluraySupToolStripMenuItem.Text = Configuration.Settings.Language.Main.Menu.File.ExportBluRaySup;
            saveAllImagesWithHtmlIndexViewToolStripMenuItem.Text = Configuration.Settings.Language.VobSubOcr.SaveAllSubtitleImagesWithHtml;
            UiUtil.FixLargeFonts(this, buttonOK);
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void TransportStreamSubtitleChooser_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
            }
        }

        internal void Initialize(TransportStreamParser tsParser, string fileName)
        {
            _programMapTableParser = new ProgramMapTableParser();
            _programMapTableParser.Parse(fileName); // get languages
            _tsParser = tsParser;
            _fileName = fileName;
            Text = string.Format(Configuration.Settings.Language.TransportStreamSubtitleChooser.Title, fileName);

            foreach (int id in tsParser.SubtitlePacketIds)
            {
                var language = _programMapTableParser.GetSubtitleLanguage(id);
                if (!string.IsNullOrEmpty(language))
                {
                    listBoxTracks.Items.Add(string.Format(Configuration.Settings.Language.TransportStreamSubtitleChooser.PidLine, id + ", " + language, tsParser.GetDvbSubtitles(id).Count));
                }
                else
                {
                    listBoxTracks.Items.Add(string.Format(Configuration.Settings.Language.TransportStreamSubtitleChooser.PidLine, id, tsParser.GetDvbSubtitles(id).Count));
                }
            }
            listBoxTracks.SelectedIndex = 0;
        }

        public int SelectedIndex => listBoxTracks.SelectedIndex;

        private void listBoxTracks_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idx = listBoxTracks.SelectedIndex;
            if (idx < 0)
            {
                return;
            }

            listBoxSubtitles.Items.Clear();
            int pid = _tsParser.SubtitlePacketIds[idx];
            var list = _tsParser.GetDvbSubtitles(pid);
            int i = 0;
            foreach (var sub in list)
            {
                i++;
                var start = new TimeCode(sub.StartMilliseconds);
                var end = new TimeCode(sub.EndMilliseconds);
                listBoxSubtitles.Items.Add(string.Format(Configuration.Settings.Language.TransportStreamSubtitleChooser.SubLine, i, start, end, sub.NumberOfImages));
            }
            if (list.Count > 0)
            {
                listBoxSubtitles.SelectedIndex = 0;
            }
        }

        private void listBoxSubtitles_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idx = listBoxSubtitles.SelectedIndex;
            if (idx < 0)
            {
                return;
            }

            int pid = _tsParser.SubtitlePacketIds[listBoxTracks.SelectedIndex];
            var list = _tsParser.GetDvbSubtitles(pid);

            var dvbBmp = list[idx].GetBitmap();
            var nDvbBmp = new NikseBitmap(dvbBmp);
            nDvbBmp.CropTopTransparent(2);
            nDvbBmp.CropTransparentSidesAndBottom(2, true);
            dvbBmp.Dispose();
            var oldImage = pictureBox1.Image;
            pictureBox1.Image = nDvbBmp.GetBitmap();
            oldImage?.Dispose();
        }

        private List<TransportStreamSubtitle> GetSelectedSubtitles()
        {
            int idx = listBoxSubtitles.SelectedIndex;
            if (idx < 0)
            {
                return null;
            }

            int pid = _tsParser.SubtitlePacketIds[listBoxTracks.SelectedIndex];
            return _tsParser.GetDvbSubtitles(pid);
        }

        private string GetSelectedLanguage()
        {
            int idx = listBoxSubtitles.SelectedIndex;
            if (idx < 0)
            {
                return null;
            }

            int pid = _tsParser.SubtitlePacketIds[listBoxTracks.SelectedIndex];
            return _programMapTableParser.GetSubtitleLanguage(pid);
        }

        private void BluraySupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportTo(ExportPngXml.ExportFormats.BluraySup);
        }

        private void BDNXMLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportTo(ExportPngXml.ExportFormats.BdnXml);
        }

        private void VobSubToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportTo(ExportPngXml.ExportFormats.VobSub);
        }

        private void DOSTToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportTo(ExportPngXml.ExportFormats.Dost);
        }

        private Point GetResolution()
        {
            int idx = listBoxTracks.SelectedIndex;
            if (idx < 0)
            {
                return new Point(DvbSubPes.DefaultScreenWidth, DvbSubPes.DefaultScreenHeight);
            }

            int pid = _tsParser.SubtitlePacketIds[idx];
            var list = _tsParser.GetDvbSubtitles(pid);
            if (list.Count > 0)
            {
                using (var bmp = list[0].GetBitmap())
                { 
                    return new Point(bmp.Width, bmp.Height);
                }
            }
            return new Point(DvbSubPes.DefaultScreenWidth, DvbSubPes.DefaultScreenHeight);
        }

        private void ExportTo(string exportType)
        {
            var subtitles = GetSelectedSubtitles();
            if (subtitles == null)
            {
                return;
            }

            using (var formSubOcr = new VobSubOcr())
            {
                formSubOcr.Initialize(subtitles, Configuration.Settings.VobSubOcr, _fileName, GetSelectedLanguage());
                using (var exportBdnXmlPng = new ExportPngXml())
                {
                    exportBdnXmlPng.InitializeFromVobSubOcr(formSubOcr.SubtitleFromOcr, new SubRip(), exportType, _fileName, formSubOcr, GetSelectedLanguage());
                    exportBdnXmlPng.SetResolution(GetResolution());
                    exportBdnXmlPng.ShowDialog(this);
                }
            }

        }

        private void SaveAllImagesWithHtmlIndexViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var subtitles = GetSelectedSubtitles();
            if (subtitles == null)
            {
                return;
            }

            using (var formSubOcr = new VobSubOcr())
            {
                formSubOcr.Initialize(subtitles, Configuration.Settings.VobSubOcr, _fileName, GetSelectedLanguage());
                formSubOcr.SaveAllImagesWithHtmlIndexViewToolStripMenuItem_Click(sender, e);
            }
        }
    }
}
