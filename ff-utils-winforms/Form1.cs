﻿using ff_utils_winforms.GuiHelpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ff_utils_winforms
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Program.logTbox = logTbox;
            CheckForIllegalCrossThreadCalls = false;
            InitCombox(createMp4Enc, 0);
            InitCombox(createMp4Crf, 1);
            InitCombox(createMp4Fps, 2);
            InitCombox(loopTimesLossless, 0);
            InitCombox(encContainer, 0);
            InitCombox(encVidCodec, 1);
            InitCombox(encVidCrf, 1);
            InitCombox(encAudCodec, 1);
            InitCombox(encAudBitrate, 4);
            InitCombox(encAudioCh, 0);
            InitCombox(changeSpeedCombox, 0);
            InitCombox(comparisonLayout, 0);
            InitCombox(comparisonType, 0);
            InitCombox(comparisonCrf, 1);
            InitCombox(delayTrackCombox, 0);
        }

        void InitCombox(ComboBox cbox, int index)
        {
            cbox.SelectedIndex = index;
            cbox.Text = cbox.Items[index].ToString();
        }

        private void DragEnterHandler(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private async void DragDropHandler(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            await Task.Delay(100);
            if (mainTabControl.SelectedTab == extractFramesPage) await ExtractFrames(files);
            if (mainTabControl.SelectedTab == framesToVideoPage) await FramesToVideo(files);
            if (mainTabControl.SelectedTab == loopPage) await Loop(files);
            if (mainTabControl.SelectedTab == speedPage) await ChangeSpeed(files);
            if (mainTabControl.SelectedTab == comparisonPage) await CreateComparison(files);
            if (mainTabControl.SelectedTab == encPage) await Encode(files);
            if (mainTabControl.SelectedTab == delayPage) await Delay(files);
        }

        async Task ExtractFrames(string[] files)
        {
            if (extractFramesTabcontrol.SelectedIndex == 0)
            {
                foreach (string file in files)
                    await FFmpegCommands.VideoToFrames(file, tonemapHdrCbox2.Checked, extractAllDelSrcCbox.Checked);
            }
            if (extractFramesTabcontrol.SelectedIndex == 1)
            {
                int frameNum = frameNumTbox.GetInt();
                foreach (string file in files)
                    await FFmpegCommands.ExtractSingleFrame(file, frameNum, tonemapHdrCbox2.Checked, extractSingleDelSrcCbox.Checked);
            }
        }

        async Task FramesToVideo(string[] dirs)
        {
            foreach (string dir in dirs)
            {
                if (!IOUtils.IsPathDirectory(dir))
                {
                    Program.Print("Please drop a folder containing frames, not single files!");
                    continue;
                }

                string concatFile = Path.Combine(IOUtils.GetTempPath(), "concat-temp.ini");
                string[] paths = IOUtils.GetFilesSorted(dir);
                string concatFileContent = "";
                foreach (string path in paths)
                    concatFileContent += $"file '{path}'\n";
                File.WriteAllText(concatFile, concatFileContent);

                if (createVidTabControl.SelectedTab == framesToVidTab) // Create MP4
                {
                    bool h265 = createMp4Enc.SelectedIndex == 1;
                    int crf = createMp4Crf.GetInt();
                    float fps = createMp4Fps.GetFloat();
                    await FFmpegCommands.FramesToMp4Concat(concatFile, dir + ".mp4", h265, crf, fps);
                }
                if (createVidTabControl.SelectedTab == framesToGifTab) // Create GIF
                {
                    bool optimize = createGifOpti.Checked;
                    float fps = createGifFps.GetFloat();
                    await FFmpegCommands.FramesToGifConcat(concatFile, dir + ".gif", optimize, fps);
                }
                if (createVidTabControl.SelectedTab == framesToApngTab) // Create APNG
                {
                    bool optimize = createApngOpti.Checked;
                    float fps = createApngFps.GetFloat();
                    await FFmpegCommands.FramesToApngConcat(concatFile, dir + ".png", optimize, fps);
                }

                //await Task.Delay(10);
            }
        }

        async Task Loop(string[] files)
        {
            if (loopTabControl.SelectedIndex == 0) // Lossless
            {
                int times = loopTimesLossless.GetInt();
                foreach (string file in files)
                    await FFmpegCommands.LoopVideo(file, times, false);
            }
        }

        async Task Encode(string[] files)
        {
            foreach (string file in files)
            {
                if(encodeTabControl.SelectedTab == encVidTab)
                    EncodeTabHelper.Encode(file, encContainer, encVidCodec, encFpsBox, encAudCodec, encAudioCh, encVidCrf, encAudBitrate, encDelSrc);
                if (encodeTabControl.SelectedTab == encGifTab)
                    EncodeTabHelper.VideoToGif(file, vidToGifOptimize, vidToGifFps);
                if (encodeTabControl.SelectedTab == encApngTab)
                    EncodeTabHelper.VideoToApng(file, vidToApngOptimize, vidToApngFps);
            }

        }


        async Task ChangeSpeed(string[] files)
        {
            if (speedTabControl.SelectedIndex == 0) // Lossless
            {
                int times = changeSpeedCombox.GetInt();
                foreach (string file in files)
                    await FFmpegCommands.ChangeSpeed(file, times, changeSpeedAudio.Checked);
            }
        }

        async Task CreateComparison(string[] files)
        {
            if (files.Length < 2)
            {
                Program.Print("Please drop two video files!");
                return;
            }

            await ComparisonHelper.CreateComparison(files, comparisonLayout.SelectedIndex == 1, comparisonType.SelectedIndex == 1, comparisonCrf.GetInt());
        }

        async Task Delay(string[] files)
        {
            FFmpegCommands.Track track = (delayTrackCombox.SelectedIndex == 0) ? FFmpegCommands.Track.Audio : FFmpegCommands.Track.Video;
            foreach (string file in files)
                await FFmpegCommands.Delay(file, track, delayAmount.Text.GetFloat(), false);
        }
    }
}
