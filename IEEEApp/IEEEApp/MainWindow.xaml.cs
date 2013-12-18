using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace IEEEApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
       static  int  count = 0;
        const int skeletonCount = 6;
        Skeleton[] allSkeletons = new Skeleton[skeletonCount];
        Skeleton first;
        SkeletonPoint KneeCentre;
        //Joint MidKnee;
        double rightAngle, leftAngle, midAngle, centralAngle, bendAngle = 80, sitAngle = 110, standAngle = 160, thresh1 = 20.0f, thresh2 = 15.0f, straightAngle = 180, thresh3 = 10.0f;
        double leftElbowElevation, rightElbowElevation, leftHandElevation, rightHandElevation;
        int boredomCount = 0,;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("hello");
            kinectSensorChooser1.KinectSensorChanged += new DependencyPropertyChangedEventHandler(kinectSensorChooser1_KinectSensorChanged);
        }

        private void kinectSensorChooser1_KinectSensorChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            KinectSensor old = (KinectSensor)e.OldValue;
            StopKinect(old);
            KinectSensor sensor = (KinectSensor)e.NewValue;
            sensor.DepthStream.Enable();
            sensor.ColorStream.Enable();
            sensor.SkeletonStream.Enable();
            sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(sensor_AllFramesReady);
            try
            {
                sensor.Start();
            }
            catch
            {
                kinectSensorChooser1.AppConflictOccurred();
            }

        }

        void sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
           // count += 1;
          //  if (count == 120)
          //  {

                ca3.Text = count.ToString();
                count = 0;
                first = GetFirstSkeleton(e);

                if (first == null)
                {
                    return;
                }
                count = 0;
                callMain();
           // }
        }

        private void StopKinect(KinectSensor sensor)
        {
            if (sensor != null)
            {
                if (sensor.IsRunning)
                {
                    //stop sensor 
                    sensor.Stop();

                    //stop audio if not null
                    if (sensor.AudioSource != null)
                    {
                        sensor.AudioSource.Stop();
                    }

                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            kinectSensorChooser1.Kinect.DepthStream.Disable();
            kinectSensorChooser1.Kinect.ColorStream.Disable();
            kinectSensorChooser1.Kinect.SkeletonStream.Disable();
            StopKinect(kinectSensorChooser1.Kinect);
        }

        void getMidKnee()
        {
            KneeCentre.X = (first.Joints[JointType.KneeLeft].Position.X + first.Joints[JointType.KneeRight].Position.X)/2;
            KneeCentre.Y = (first.Joints[JointType.KneeLeft].Position.Y + first.Joints[JointType.KneeRight].Position.Y) / 2;
            KneeCentre.Z = (first.Joints[JointType.KneeLeft].Position.Z + first.Joints[JointType.KneeRight].Position.Z) / 2;
        }

        public static double vectorNorm(double x, double y, double z)
        {
            return Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2) + Math.Pow(z, 2));
        }

        public static double vectorNorm( double y, double z)
        {
            return Math.Sqrt(Math.Pow(y, 2) + Math.Pow(z, 2));
        }

        double CalculateAngle3D(Joint shoulder, Joint hip, SkeletonPoint knee)
        {
            double angle = 0;
            double value = CalcDiff();
            value = 0.38 * value;
            double shrhX = shoulder.Position.X - hip.Position.X;
            double shrhY = shoulder.Position.Y - (hip.Position.Y - value);
            double shrhZ = shoulder.Position.Z - hip.Position.Z;
            double hsl = vectorNorm(shrhX, shrhY, shrhZ);
            double unrhX = knee.X - hip.Position.X;
            double unrhY = knee.Y - (hip.Position.Y - value);
            double unrhZ = knee.Z - hip.Position.Z;
            double hul = vectorNorm(unrhX, unrhY, unrhZ);
            double mhshu = shrhX * unrhX + shrhY * unrhY + shrhZ * unrhZ;

            double x = mhshu / (hul * hsl);
            if (x != Double.NaN)
            {
                if (-1 <= x && x <= 1)
                {
                    double angleRad = Math.Acos(x);
                    angle = angleRad * (180.0 / 3.1416);
                }
                else
                    angle = 0;
            }
            else
                angle = 0;

            return angle;
        }

        double CalcDiff()
        {
            double value = first.Joints[JointType.Head].Position.Y - first.Joints[JointType.HipCenter].Position.Y;
            return value;
        }
      
        double CalculateAngle2D_YZ(Joint shoulder, Joint hip, Joint knee)
        {
            double angle = 0;
            double value = CalcDiff();
            value = 0.4 * value;
            double shrhY = shoulder.Position.Y - (hip.Position.Y-value);
            double shrhZ = shoulder.Position.Z - hip.Position.Z;
            double hsl = vectorNorm( shrhY, shrhZ);
            double unrhY = knee.Position.Y - (hip.Position.Y-value);
            double unrhZ = knee.Position.Z - hip.Position.Z;
            double hul = vectorNorm( unrhY, unrhZ);
            double mhshu = shrhY * unrhY + shrhZ * unrhZ;

            double x = mhshu / (hul * hsl);
            if (x != Double.NaN)
            {
                if (-1 <= x && x <= 1)
                {
                    double angleRad = Math.Acos(x);
                    angle = angleRad * (180.0 / 3.1416);
                }
                else
                    angle = 0;
            }
            else
                angle = 0;

            return angle;
        }



     /*   double CalculateAngle2D_YZ(Joint shoulder, Joint hip, SkeletonPoint knee)
        {
            double angle = 0;
            double value = CalcDiff();
            value = 0.32 * value;
            double shrhY = shoulder.Position.Y - (hip.Position.Y - value);
            double shrhZ = shoulder.Position.Z - hip.Position.Z;
            double hsl = vectorNorm(shrhY, shrhZ);
            double unrhY = knee.Y - (hip.Position.Y - value);
            double unrhZ = knee.Z - hip.Position.Z;
            double hul = vectorNorm(unrhY, unrhZ);
            double mhshu = shrhY * unrhY + shrhZ * unrhZ;

            double x = mhshu / (hul * hsl);
            if (x != Double.NaN)
            {
                if (-1 <= x && x <= 1)
                {
                    double angleRad = Math.Acos(x);
                    angle = angleRad * (180.0 / 3.1416);
                }
                else
                    angle = 0;
            }
            else
                angle = 0;

            return angle;
        }
*/

        double CalculateAngle2D_XY(Joint head, Joint ShoulderCenter, Joint spine)
        {
            double angle = 0;
            double shrhX = head.Position.X - ShoulderCenter.Position.X;
            double shrhY = head.Position.Y - (ShoulderCenter.Position.Y);
            double hsl = vectorNorm(shrhX, shrhY);
            double unrhX = spine.Position.X - ShoulderCenter.Position.X;
            double unrhY = spine.Position.Y - (ShoulderCenter.Position.Y);
            double hul = vectorNorm(unrhX, unrhY);
            double mhshu = shrhX * unrhX + shrhY * unrhY;

            double x = mhshu / (hul * hsl);
            if (x != Double.NaN)
            {
                if (-1 <= x && x <= 1)
                {
                    double angleRad = Math.Acos(x);
                    angle = angleRad * (180.0 / 3.1416);
                }
                else
                    angle = 0;
            }
            else
                angle = 0;

            return angle;
        }

        double CalculateAngle3D(Joint shoulder, Joint hip, Joint knee)
        {
            double angle = 0;
            double value = CalcDiff();
            value = 0.32 * value;
            double shrhX = shoulder.Position.X - hip.Position.X;
            double shrhY = shoulder.Position.Y - (hip.Position.Y - value);
            double shrhZ = shoulder.Position.Z - hip.Position.Z;
            double hsl = vectorNorm(shrhX, shrhY, shrhZ);
            double unrhX = knee.Position.X - hip.Position.X;
            double unrhY = knee.Position.Y -  (hip.Position.Y - value);
            double unrhZ = knee.Position.Z - hip.Position.Z;
            double hul = vectorNorm(unrhX, unrhY, unrhZ);
            double mhshu = shrhX * unrhX + shrhY * unrhY + shrhZ * unrhZ;

            double x = mhshu / (hul * hsl);
            if (x != Double.NaN)
            {
                if (-1 <= x && x <= 1)
                {
                    double angleRad = Math.Acos(x);
                    angle = angleRad * (180.0 / 3.1416);
                }
                else
                    angle = 0;
            }
            else
                angle = 0;

            return angle;
        }

      

        Skeleton GetFirstSkeleton(AllFramesReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrameData = e.OpenSkeletonFrame())
            {
                if (skeletonFrameData == null)
                {
                    return null;
                }


                skeletonFrameData.CopySkeletonDataTo(allSkeletons);

                //get the first tracked skeleton
                Skeleton first = (from s in allSkeletons
                                  where s.TrackingState == SkeletonTrackingState.Tracked
                                  select s).FirstOrDefault();

                return first;

            }
        }

        void checkUser()
        {
            rightAngle = CalculateAngle2D_YZ(first.Joints[JointType.ShoulderLeft], first.Joints[JointType.HipLeft], first.Joints[JointType.KneeLeft]);
            leftAngle = CalculateAngle2D_YZ(first.Joints[JointType.ShoulderRight], first.Joints[JointType.HipRight], first.Joints[JointType.KneeRight]);
            centralAngle = CalculateAngle2D_XY(first.Joints[JointType.Spine], first.Joints[JointType.ShoulderCenter], first.Joints[JointType.Head]);
            // double midAngle = CalculateAngle2D(first.Joints[JointType.ShoulderCenter], first.Joints[JointType.HipCenter], KneeCentre);
            midAngle = (rightAngle + leftAngle) / 2;
            leftElbowElevation = first.Joints[JointType.ElbowLeft].Position.Y - first.Joints[JointType.ShoulderCenter].Position.Y;
            rightElbowElevation = first.Joints[JointType.ElbowRight].Position.Y - first.Joints[JointType.ShoulderCenter].Position.Y;
            leftHandElevation = first.Joints[JointType.HandLeft].Position.Y - first.Joints[JointType.ShoulderCenter].Position.Y;
            rightHandElevation = first.Joints[JointType.HandRight].Position.Y - first.Joints[JointType.ShoulderCenter].Position.Y;
            if (midAngle <= sitAngle + thresh1 && midAngle >= sitAngle - thresh1)
            {
                if (centralAngle <= straightAngle + thresh3 && centralAngle >= straightAngle - thresh3)
                {
                    if (leftElbowElevation >= -0.1 && rightElbowElevation >= -0.1 && leftHandElevation >= -0.1 && rightHandElevation >= -0.1)
                    {
                        UserPresence.Text = "The user is Sitting " + midAngle;
                        P_N.Text = " Negative (both hand above";
                        Emotion.Text = " Bored ";
                    }
                    else if ((leftHandElevation >= -0.1 || rightHandElevation >= -0.1) && leftElbowElevation < 0 && rightElbowElevation < 0)
                    {
                        UserPresence.Text = "The user is Sitting " + midAngle;
                        P_N.Text = " Negative (hand on chin ";
                        Emotion.Text = " Uncertainity/Puzzled/Anxiety";
                    }
                    else
                    {
                        UserPresence.Text = "The user is Sitting " + midAngle;
                        P_N.Text = " Positive (sitting";
                        Emotion.Text = " Neutral ";
                    }
                }
                else
                {
                    UserPresence.Text = "The user is Sitting " + midAngle;
                    P_N.Text = " Negative (head on side";
                    Emotion.Text = " Uncertainity/Puzzled/Anxiety";

                }
                
            }
            else if (midAngle <= bendAngle + thresh2 && midAngle >= bendAngle - thresh2)
            {
                UserPresence.Text = "The user is Sitting " + midAngle;
                P_N.Text = " Positive (bendin";
                Emotion.Text = " Enthusiastic ";
            }
            else if (midAngle <= standAngle + thresh1 && midAngle >= standAngle - thresh1)
            {
                UserPresence.Text = "The user is Standing " + midAngle;
                P_N.Text = " ";
                Emotion.Text = " ";
            }
            else
            {
                UserPresence.Text = "Please adjust the Kinect and retry " + midAngle;
                P_N.Text = " ";
                Emotion.Text = " ";
            }

                
        }

       

        void callMain()
        {
           // getMidKnee();
            checkUser();
           
        }

       
    }
}
