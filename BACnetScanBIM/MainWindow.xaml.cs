﻿using BACnetCommonLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BACnetScanBIM
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Logger.Log("Start");
            this.DataContext = MainVM.Instance;
            MainVM.Instance.Run();
        }

        private void btnReadSubNodesBatch_Click(object sender, RoutedEventArgs e)
        {
            Logger.Log("Begin Scan Device");
            var deviceList = MainVM.Instance.DevicesList;
            foreach (var device in deviceList)
            {
                var count = MainVM.Instance.GetDeviceArrayIndexCount(device);
                MainVM.Instance.ScanPointsBatch(device, count);
            }
            Logger.Log("Begin Scan Properties");
            foreach (var device in deviceList)
            {
                MainVM.Instance.ScanSubProperties(device);
            }
            Logger.Log("Scan Finished");
        }
        
        private void btnSaveExcel_Click(object sender, RoutedEventArgs e)
        {
            MainVM.Instance.Save2Excel();
            Logger.Log("Save2Excel Finished");
        }

        private void btnCombineExcel_Click(object sender, RoutedEventArgs e)
        {
            MainVM.Instance.CombineExcel();
            Logger.Log("CombineExcel Finished");
        }
    }
}