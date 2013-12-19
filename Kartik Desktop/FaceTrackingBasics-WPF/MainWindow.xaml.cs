// -----------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace FaceTrackingBasics
{
    using System;
    using System.Linq;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using Microsoft.Kinect.Toolkit;
    using Microsoft.Kinect.Toolkit.FaceTracking;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly int Bgr32BytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;
        private readonly KinectSensorChooser sensorChooser = new KinectSensorChooser();
        private WriteableBitmap colorImageWritableBitmap;
        private byte[] colorImageData;
        private ColorImageFormat currentColorImageFormat = ColorImageFormat.Undefined;

        FaceTracker faceTracker;
        private byte[] colorPixelData;
        private short[] depthPixelData;
        private Skeleton[] skeletonData;
        private int count = 0, timesNodded = 0, timesShook = 0, timesTilted = 0;
        private int isTilt = 0, isHeadDown = 0, isHeadAway = 0;
        private int[] guessState = new int[8];
        private int[] state = new int[9];

        private float midPitch = 0;
        private float midYaw = 0;
        private float midRoll = 0;

        private int startCaliberate = 0;
        // 0 attentive
        // 1 distracted
        // 2 bored
        // 3 sad
        // 4 tired/sleepy
        // 5 thinking
        // 6 nodding in approval
        // 7 shaking in disapproval

        public MainWindow()
        {
            InitializeComponent();

            var faceTrackingViewerBinding = new Binding("Kinect") { Source = sensorChooser };
            faceTrackingViewer.SetBinding(FaceTrackingViewer.KinectProperty, faceTrackingViewerBinding);

            sensorChooser.KinectChanged += SensorChooserOnKinectChanged;

            sensorChooser.Start();

            for (int i = 0; i < 8; i++)
            {
                guessState[i] = 0;
                state[i] = 0;
            }
            state[8] = 0;
        }

        private void SensorChooserOnKinectChanged(object sender, KinectChangedEventArgs kinectChangedEventArgs)
        {
            KinectSensor oldSensor = kinectChangedEventArgs.OldSensor;
            KinectSensor newSensor = kinectChangedEventArgs.NewSensor;

            if (oldSensor != null)
            {
                oldSensor.AllFramesReady -= KinectSensorOnAllFramesReady;
                oldSensor.ColorStream.Disable();
                oldSensor.DepthStream.Disable();
                oldSensor.DepthStream.Range = DepthRange.Default;
                oldSensor.SkeletonStream.Disable();
                oldSensor.SkeletonStream.EnableTrackingInNearRange = false;
                oldSensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
            }

            if (newSensor != null)
            {
                try
                {
                    newSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                    newSensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
                    try
                    {
                        // This will throw on non Kinect For Windows devices.
                        newSensor.DepthStream.Range = DepthRange.Near;
                        newSensor.SkeletonStream.EnableTrackingInNearRange = true;
                    }
                    catch (InvalidOperationException)
                    {
                        newSensor.DepthStream.Range = DepthRange.Default;
                        newSensor.SkeletonStream.EnableTrackingInNearRange = false;
                    }

                    newSensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                    newSensor.SkeletonStream.Enable();

                    colorPixelData = new byte[sensorChooser.Kinect.ColorStream.FramePixelDataLength];
                    depthPixelData = new short[sensorChooser.Kinect.DepthStream.FramePixelDataLength];
                    skeletonData = new Skeleton[6];

                    faceTracker = new FaceTracker(sensorChooser.Kinect);

                    newSensor.AllFramesReady += KinectSensorOnAllFramesReady;
                }
                catch (InvalidOperationException)
                {
                    // This exception can be thrown when we are trying to
                    // enable streams on a device that has gone away.  This
                    // can occur, say, in app shutdown scenarios when the sensor
                    // goes away between the time it changed status and the
                    // time we get the sensor changed notification.
                    //
                    // Behavior here is to just eat the exception and assume
                    // another notification will come along if a sensor
                    // comes back.
                }
            }
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            sensorChooser.Stop();
            faceTrackingViewer.Dispose();
        }

        private void KinectSensorOnAllFramesReady(object sender, AllFramesReadyEventArgs e)
        {

            if (startCaliberate == 1)
            {
                count++;
                if (count == 90)
                {

                    count = 0;
                    startCaliberate = 0;
                    FaceCaliberateOperation(sender, e);
                    midPitch /= 90;
                    midRoll /= 90;
                    midYaw /= 90;

                    MessageBox.Show("Calibrated Pitch: " + midPitch + " Calibrated Roll: " + midRoll + " Calibrated Yaw: " + midYaw);
                }
                else
                {
                    FaceCaliberateOperation(sender, e);
                }
            }
            else
            {
                //count++;
                //if (count == 60)
                //{
                   // displayNote1();
                   // count = 0;
                   // for (int i = 0; i < 9; i++)
                   // {
                    //    state[i] = 0;
                   // }
                //}
                FaceTrackOperation(sender, e);

                displayNote1();
                count = 0;
                for (int i = 0; i < 9; i++)
                {
                    state[i] = 0;
                }
                

                /*if (count == 180)
                {
                    count = 0;

                    if (timesNodded >= 2)
                    {
                        //textBox1.Text = "Nodding in Approval";
                        guessState[6] += 5;
                    }

                    if (timesShook >= 2)
                    {
                        //textBox1.Text = "Shaking in Approval";
                        guessState[7] += 5;
                    }

                    if (timesNodded < 2)
                    {
                        if (isHeadDown == 1)
                        {
                            guessState[1] += 1;
                            guessState[2] += 2;
                            guessState[5] += 2;
                        }
                        if (isHeadDown == -1)
                        {
                            guessState[1] += 1;
                            guessState[3] += 2;
                            guessState[4] += 2;

                        }
                        if (isHeadDown == 0)
                        {
                            guessState[0] += 2;
                        }
                    }

                    if (timesShook < 2)
                    {
                        if (isHeadAway == 1 || isHeadAway == -1)
                        {
                            guessState[1] += 3;
                            guessState[5] += 1;
                            guessState[3] += 2;
                        }
                        else
                        {
                            guessState[0] += 2;
                        }
                    }

                    if (isTilt == 0)
                    {
                        guessState[0] += 2;
                    }
                    if (isTilt == 1 || isTilt == -1)
                    {
                        guessState[0] += 1;
                        guessState[5] += 1;
                        guessState[4] += 1;
                        guessState[3] += 1;
                    }

                    displayNote();
                    isHeadAway = 0;
                    isHeadDown = 0;
                    isTilt = 0;
                    timesShook = 0;
                    timesNodded = 0;
                }*/
            }
        }

        private void displayNote1()
        {
            int maxOccur = 0, maxState = 0;

            for (int i = 0; i < 9; i++)
            {
                if (state[i] > maxOccur)
                {
                    maxOccur = state[i];
                    maxState = i;
                }
            }

            if (maxState == 1)
            {
                textBox1.Text = "Straight face";
                textBox2.Text = "Attentive";
                textBox3.Text = "Keep it up.";
            }
            if (maxState == 0)
            {
                textBox1.Text = "Face Upwards";
                textBox2.Text = "Thinking/contemplative.";
                textBox3.Text = "If you have any doubts please ask.";
            }
            if (maxState == 2)
            {
                textBox1.Text = "Face Downwards";
                textBox2.Text = "Distracted/disinterested";
                textBox3.Text = "Perhaps we will study another time.";
            }
            if (maxState == 3 || maxState == 5)
            {
                textBox1.Text = "Facing away";
                textBox2.Text = "Distracted";
                textBox3.Text = "Please pay attention.";
            }
            if (maxState == 6 || maxState == 8)
            {
                textBox1.Text = "Tilted";
                textBox2.Text = "May be tired/sleepy";
                textBox3.Text = "Take a coffee break.";
            }

        }

        private void displayNote()
        {
            if (guessState[0] == 6)
            {
                textBox1.Text = "Very Attentive";
            }

            if (guessState[0] == 5)
            {
                textBox1.Text = "Somewhat Attentive. May be tired.";
            }

            if (guessState[1] > 2)
            {
                textBox1.Text = "Distracted";
            }

            if (guessState[6] == 5)
            {
                textBox1.Text = "Nodding in Approval.";
            }

            if (guessState[7] == 5)
            {
                textBox1.Text = "Shaking in disapproval.";
            }

            if (guessState[2] >= 3)
            {
                textBox1.Text = "Bored";
            }

            if (guessState[3] >= 3)
            {
                textBox1.Text = "Sad";
            }

            if (guessState[4] >= 3)
            {
                textBox1.Text = "Tired/Sleepy";
            }

            if (guessState[5] >= 3)
            {
                textBox1.Text = "thinking";
            }

            textBox1.Text += " count: " + count.ToString();

        }

        private void FaceCaliberateOperation(object sender, AllFramesReadyEventArgs e)
        {
            using (var colorImageFrame = e.OpenColorImageFrame())
            {
                if (colorImageFrame == null)
                {
                    return;
                }

                // Make a copy of the color frame for displaying.
                var haveNewFormat = this.currentColorImageFormat != colorImageFrame.Format;
                if (haveNewFormat)
                {
                    this.currentColorImageFormat = colorImageFrame.Format;
                    this.colorImageData = new byte[colorImageFrame.PixelDataLength];
                    this.colorImageWritableBitmap = new WriteableBitmap(
                        colorImageFrame.Width, colorImageFrame.Height, 96, 96, PixelFormats.Bgr32, null);
                    ColorImage.Source = this.colorImageWritableBitmap;
                }

                colorImageFrame.CopyPixelDataTo(this.colorImageData);
                this.colorImageWritableBitmap.WritePixels(
                    new Int32Rect(0, 0, colorImageFrame.Width, colorImageFrame.Height),
                    this.colorImageData,
                    colorImageFrame.Width * Bgr32BytesPerPixel,
                    0);
            }

            using (ColorImageFrame colorImageFrame = e.OpenColorImageFrame())
            {
                if (colorImageFrame == null)
                    return;
                colorImageFrame.CopyPixelDataTo(colorPixelData);
            }

            using (DepthImageFrame depthImageFrame = e.OpenDepthImageFrame())
            {
                if (depthImageFrame == null)
                    return;
                depthImageFrame.CopyPixelDataTo(depthPixelData);
            }

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame == null)
                    return;
                skeletonFrame.CopySkeletonDataTo(skeletonData);
            }

            var skeleton = skeletonData.FirstOrDefault(s => s.TrackingState == SkeletonTrackingState.Tracked);
            if (skeleton == null)
                return;

            FaceTrackFrame faceFrame = faceTracker.Track(sensorChooser.Kinect.ColorStream.Format, colorPixelData,
                                            sensorChooser.Kinect.DepthStream.Format, depthPixelData,
                                            skeleton);

            // If a face is tracked, then we can use it.
            if (faceFrame.TrackSuccessful)
            {
                // Retrieve only the Animation Units coeffs.

                //CanvasTranslate.X = faceFrame.Translation.X;
                //CanvasTranslate.Y = faceFrame.Translation.Y;
                Vector3DF faceRotation = faceFrame.Rotation;

                var AUCoeff = faceFrame.GetAnimationUnitCoefficients();

                var jawLower = AUCoeff[AnimationUnit.JawLower];
                var BrowLower = AUCoeff[AnimationUnit.BrowLower];
                var BrowUpper = AUCoeff[AnimationUnit.BrowRaiser];
                var lcd = AUCoeff[AnimationUnit.LipCornerDepressor];
                var lipRaiser = AUCoeff[AnimationUnit.LipRaiser];
                var lipStrectch = AUCoeff[AnimationUnit.LipStretcher];
                var Pitch = faceRotation.X;
                var Yaw = faceRotation.Y;
                var Roll = faceRotation.Z;

                midPitch += Pitch;
                midRoll += Roll;
                midYaw += Yaw;
                
            }
        }

        private void FaceTrackOperation(object sender, AllFramesReadyEventArgs e)
        {
            using (var colorImageFrame = e.OpenColorImageFrame())
            {
                if (colorImageFrame == null)
                {
                    return;
                }

                // Make a copy of the color frame for displaying.
                var haveNewFormat = this.currentColorImageFormat != colorImageFrame.Format;
                if (haveNewFormat)
                {
                    this.currentColorImageFormat = colorImageFrame.Format;
                    this.colorImageData = new byte[colorImageFrame.PixelDataLength];
                    this.colorImageWritableBitmap = new WriteableBitmap(
                        colorImageFrame.Width, colorImageFrame.Height, 96, 96, PixelFormats.Bgr32, null);
                    ColorImage.Source = this.colorImageWritableBitmap;
                }

                colorImageFrame.CopyPixelDataTo(this.colorImageData);
                this.colorImageWritableBitmap.WritePixels(
                    new Int32Rect(0, 0, colorImageFrame.Width, colorImageFrame.Height),
                    this.colorImageData,
                    colorImageFrame.Width * Bgr32BytesPerPixel,
                    0);
            }

            using (ColorImageFrame colorImageFrame = e.OpenColorImageFrame())
            {
                if (colorImageFrame == null)
                    return;
                colorImageFrame.CopyPixelDataTo(colorPixelData);
            }

            using (DepthImageFrame depthImageFrame = e.OpenDepthImageFrame())
            {
                if (depthImageFrame == null)
                    return;
                depthImageFrame.CopyPixelDataTo(depthPixelData);
            }

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame == null)
                    return;
                skeletonFrame.CopySkeletonDataTo(skeletonData);
            }

            var skeleton = skeletonData.FirstOrDefault(s => s.TrackingState == SkeletonTrackingState.Tracked);
            if (skeleton == null)
                return;

            FaceTrackFrame faceFrame = faceTracker.Track(sensorChooser.Kinect.ColorStream.Format, colorPixelData,
                                            sensorChooser.Kinect.DepthStream.Format, depthPixelData,
                                            skeleton);

            // If a face is tracked, then we can use it.
            if (faceFrame.TrackSuccessful)
            {
                // Retrieve only the Animation Units coeffs.

                //CanvasTranslate.X = faceFrame.Translation.X;
                //CanvasTranslate.Y = faceFrame.Translation.Y;
                Vector3DF faceRotation = faceFrame.Rotation;

                var AUCoeff = faceFrame.GetAnimationUnitCoefficients();

                var jawLower = AUCoeff[AnimationUnit.JawLower];
                var BrowLower = AUCoeff[AnimationUnit.BrowLower];
                var BrowUpper = AUCoeff[AnimationUnit.BrowRaiser];
                var lcd = AUCoeff[AnimationUnit.LipCornerDepressor];
                var lipRaiser = AUCoeff[AnimationUnit.LipRaiser];
                var lipStrectch = AUCoeff[AnimationUnit.LipStretcher];
                var Pitch = faceRotation.X - midPitch;
                var Yaw = faceRotation.Y - midYaw;
                var Roll = faceRotation.Z - midRoll;

                if (Pitch < midPitch + 10 && Pitch > midPitch - 10)
                {
                    isHeadDown = 0;
                    state[1] += 3;
                }
                if (Roll < midRoll + 10 && Roll > midRoll - 10)
                {
                    isTilt = 0;
                    state[1]++;
                }
                if (Yaw < midYaw + 10 && Yaw > midYaw - 10)
                {
                    isHeadAway = 0;
                    state[1]++;
                }

                if (Pitch > midPitch+10)
                {
                    if (isHeadDown <= 0) timesNodded++;
                    isHeadDown = 1;
                    state[0]+=3;
                    
                }
                if (Pitch < midPitch-10)
                {
                    if (isHeadDown >= 0) timesNodded++;
                    isHeadDown = -1;
                    state[2]+=3;
                    //timesNodded++;
                }

                if (Yaw > midYaw+10 )
                {
                    if (isHeadAway <= 0) timesShook++;
                    isHeadAway = 1;
                    state[3]+=3;
                    //timesShook++;
                }
                if (Yaw < midYaw-10 )
                {
                    if (isHeadAway >= 0) timesShook++;
                    isHeadAway = -1;
                    //timesShook++;
                    state[5]+=3;
                }

                if (Roll > midRoll+10)
                {
                    state[6]+=3;
                    isTilt = 1;
                    //timesShook++;
                }
                if (Roll < midRoll-10)
                {
                    state[8]+=3;
                    isTilt = -1;
                    //timesShook++;
                }

                //textBox2.Text = "P: " + ((float)Pitch).ToString() + " Y: " + ((float)Yaw).ToString() + " R: " + ((float)Roll).ToString();
                //textBox2.Text = "JL: " + ((float)jawLower).ToString() + " BL: " + ((float)BrowLower).ToString() + " BU: " + ((float)BrowUpper).ToString();
                //dataToBeSent3 = "lcd: " + ((float)lcd).ToString() + " LR: " + ((float)lipRaiser).ToString() + " LS: " + ((float)lipStrectch).ToString();

                //pitch(nod) - +-15 roll(tilt) - +-20 yaw(shaking offs- -10) +-15
                //textBox2.Text += " isTilt " + isTilt + " isHeadAway " + isHeadAway + " isHeadDown " + isHeadDown;
            }

        }

        private void caliberate_Click(object sender, RoutedEventArgs e)
        {
            startCaliberate = 1;
            count = 0;
        }
    }
}
