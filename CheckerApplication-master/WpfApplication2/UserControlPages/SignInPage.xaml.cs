﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using RFIDeas_pcProxAPI;
using System.Media;
using System.Timers;

namespace WpfApplication2
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class SignInPage : UserControl
    {

        AttendanceWriter attendanceWriter = MainWindow.AppWindow.getAttendanceWriter();

        List<string> authorizedCheckerIDs;
        List<string> temporaryCheckersList;
        System.Timers.Timer aTimer;
        int counter = 0;
        string chapelCheckerId;
        private SoundPlayer happyPlayer = new SoundPlayer(@"../../Assets/blip.wav");
        private SoundPlayer failPlayer = new SoundPlayer(@"../../Assets/failure_beep.wav");
        string lastID = "";


        public SignInPage()
        {
            InitializeComponent();
        }

        private void buttonScan_Click(object sender, RoutedEventArgs e)
        {
            getTemporaryCheckers();

            authorizedCheckerIDs = new List<string>();

            authorizedCheckerIDs = attendanceWriter.getAuthorizedCheckersFromTextFile();
            
            

            //Comment out this to skip authorization of sign in
            
            aTimer = new System.Timers.Timer();
            aTimer.Elapsed += new ElapsedEventHandler(signInScan);
            aTimer.Interval = 500;
            aTimer.Enabled = true;
            
            //Uncomment this to skip authorization to sign in
            //MainWindow.AppWindow.GoToEventsPage();

            buttonScan.IsEnabled = false;
        }

        
        private void getTemporaryCheckers()
        {
            this.temporaryCheckersList = attendanceWriter.getTempCheckersFromTextFile();
        }
        

        private void successfulSignIn()
        {
            MainWindow.AppWindow.GoToEventsPage();
        }

        private void signInScan(object source, ElapsedEventArgs e)
        {
            counter++;
            Byte[] Id = new Byte[8];
            int nBits = pcProxDLLAPI.getActiveID(8);

            // MainWindow.AppWindow.textBox2.Text = nBits.ToString();

            if (nBits > 0)
            {

                //SystemSounds.Beep.Play();

                String s = nBits.ToString() + " Bit ID [0]..[7]: ";
                String proxID = "";
                string checkerID = "";
                string checkersName = "";
                
                for (short i = 1; i > -1; i--)
                {
                    Id[i] = pcProxDLLAPI.getActiveID_byte(i);
                    s = s + String.Format("{0:X2}.", Id[i]);
                    proxID += String.Format("{0:X2}", Id[i]);
                    checkerID = Int32.Parse(proxID, System.Globalization.NumberStyles.HexNumber).ToString();
                }


                if (!lastID.Equals(checkerID))
                {

                    if (authorizedCheckerIDs.Contains(checkerID))
                    {
                        lastID = checkerID;

                        Dispatcher.Invoke(() =>
                        {
                            labelID.Foreground = new SolidColorBrush(Colors.LightGreen);
                            checkersName = attendanceWriter.getAuthorizedCheckersName(checkerID);
                            labelID.Text = checkersName + " is an authorized Christian Life and Worship Credit Checker";
                            labelID_Counter.Text = "";
                            Panel.SetZIndex(buttonProceed, 1);
                        });

                        this.chapelCheckerId = checkerID;

                        aTimer.Stop();
                        Dispatcher.Invoke(() =>
                        {
                            attendanceWriter.setChapelCheckerID(checkerID);
                            //successfulSignIn();
                            
                        });
                        happyPlayer.Play();
                    }
                    else
                    {
                        lastID = checkerID;
                        Dispatcher.Invoke(() =>
                        {
                            checkersName = attendanceWriter.getStudentsName(checkerID);
                            labelID.Text = checkersName + " is not an authorized Christian Life and Worship Credit Checker";
                            labelID_Counter.Text = counter.ToString();
                        });
                        failPlayer.Play();
                    }
                }

                else
                {
                    Dispatcher.Invoke(() => {
                        labelID_Counter.Text = counter.ToString();
                    });
                }

            }

            else
            {
                Dispatcher.Invoke(() => {
                    labelID_Counter.Text = counter.ToString();
                });
            }


            if (counter > 98)
                this.counter = 0;

        }

        private void buttonUpdate_Click(object sender, RoutedEventArgs e)
        {
            SQLPuller sqlPuller = new SQLPuller();
            sqlPuller.pullEvents();
            sqlPuller.pullAuthorizedCheckers();
            sqlPuller.pullStudents();
            buttonUpdateStudentInfo.IsEnabled = false;
        }

        private void buttonProceed_Click(object sender, RoutedEventArgs e)
        {
            successfulSignIn();
        }
    }
}
