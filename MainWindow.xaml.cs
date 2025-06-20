﻿using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System;
using System.IO;
using Microsoft.Win32;
using System.Windows.Media.Media3D;
using static System.Net.Mime.MediaTypeNames;

namespace WpfApp1
{
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window
   {
      private BitmapImage? _bitmapImage;


      public MainWindow()
      {
         InitializeComponent();
         _bitmapImage = null;
      }

      private static readonly int STAGE_HEADER_COLOR = 0x88;
      private static readonly int STAGE_BACK_COLOR = 0xEE;
      private static readonly int MAX_STAGE_HEIGHT = 275;

      private string PickImageFile()
      {
         OpenFileDialog openFileDialog = new OpenFileDialog();
         //openFileDialog.Filter = "Image Files|*.png;*.jpg;*.jpeg|All files|*.*";
         openFileDialog.Filter = "JPEG Files|*.jpg;*.jpeg";
         if (openFileDialog.ShowDialog() == true)
         {
            System.Diagnostics.Debug.WriteLine($"File picked: {openFileDialog.FileName}");
            return openFileDialog.FileName;
         }

         return null;
      }

      private BitmapImage LoadImageFromFile(string filename)
      {
         try
         {
            BitmapImage ret = new BitmapImage(new Uri(filename));
            return ret;
         }
         catch (Exception ex)
         {
            MessageBox.Show(ex.Message);
         }

         return null;
      }

      private void SaveStageImageToFile(BitmapSource outputBitmap, string outputFilename)
      {
         using (FileStream stream = new FileStream(outputFilename, FileMode.Create))
         {
            PngBitmapEncoder encoder5 = new PngBitmapEncoder();
            encoder5.Frames.Add(BitmapFrame.Create(outputBitmap));
            encoder5.Save(stream);
         }
      }

      /// <summary>
      /// Crop out the stage info areas and save each one as a separate png file.
      /// </summary>
      /// <param name="bitmapSource"></param>
      /// <param name="outputFolder"></param>
      /// <param name="startStage"></param>
      /// <param name="playerName"></param>
      private void CropAndSave(BitmapSource bitmapSource, string outputFolder, int startStage, string playerName)
      {
         var writeableBitmap = new WriteableBitmap(bitmapSource);
         System.Diagnostics.Debug.WriteLine($"wb: {writeableBitmap.PixelWidth} x {writeableBitmap.PixelHeight} : {bitmapSource.Format} : {bitmapSource.Format.BitsPerPixel}");
         
         var stride = bitmapSource.PixelWidth * (bitmapSource.Format.BitsPerPixel + 7) / 8;
         System.Diagnostics.Debug.WriteLine($"stride: {stride}");

         // Gain access to the pixel data
         var bitmapData = new byte[writeableBitmap.PixelHeight * stride];
         bitmapSource.CopyPixels(bitmapData, stride, 0);

         int stage = startStage;

         // Search the pixel data for the areas that hold the stage information
         for (int row = 0; row < writeableBitmap.PixelHeight; row++)
         {
            if ((bitmapData[row * stride] == STAGE_HEADER_COLOR) && (bitmapData[row * stride + 1] == STAGE_HEADER_COLOR)) // Found start of stage header
            {
               int firstRow = row;
               int stageHeight = 0;
               while ((bitmapData[row * stride] == STAGE_HEADER_COLOR) && (bitmapData[row * stride + 1] == STAGE_HEADER_COLOR)) // While inside stage header, keep going
               {
                  row++;
               }

               while ((bitmapData[row * stride] == STAGE_BACK_COLOR) && (bitmapData[row * stride + 1] == STAGE_BACK_COLOR)) // Now we have found the stage info area, keep going until we reach next stage header
               {
                  row++;
                  stageHeight = row - firstRow;
                  if (stageHeight > MAX_STAGE_HEIGHT)
                     break;
               }

               int lastRow = row-1;
               System.Diagnostics.Debug.WriteLine($"Found stage {stage} at {firstRow} to {lastRow} with height = {stageHeight}");

               // Copy the stage data into a new bitmap
               var stageData = new byte[stageHeight * stride];
               Int32Rect rect = new Int32Rect(0, firstRow, writeableBitmap.PixelWidth, stageHeight);
               bitmapSource.CopyPixels(rect, stageData, stride, 0);

               // Save it out
               string outputFilename = System.IO.Path.Combine(outputFolder, $"{playerName}Stage{stage}.png");
               BitmapSource outputBitmap = BitmapImage.Create(bitmapSource.PixelWidth, stageHeight, bitmapSource.DpiX, bitmapSource.DpiY, bitmapSource.Format, null, stageData, stride);
               SaveStageImageToFile(outputBitmap, outputFilename);

               stage++;
            }
         }
      }

      private void PickImage_Click(object sender, RoutedEventArgs e)
      {
         string filename = PickImageFile();
         if(String.IsNullOrEmpty(filename)) { return; }

         _bitmapImage = LoadImageFromFile(filename);
         if (_bitmapImage == null) { return; }

         ScreenshotImage.Source = _bitmapImage;
         string outputFolder = System.IO.Path.GetDirectoryName(filename);
         CropAndSave((BitmapSource)ScreenshotImage.Source, outputFolder, int.Parse(StartStage.Text), PlayerName.Text);

         MessageBox.Show($"Images saved to {outputFolder}.");

         System.Diagnostics.Debug.WriteLine($"goodbye");
      }
   }
}