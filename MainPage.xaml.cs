/*
    Copyright (c) Microsoft Corporation All rights reserved.  
 
    MIT License: 
 
    Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
    documentation files (the  "Software"), to deal in the Software without restriction, including without limitation
    the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software,
    and to permit persons to whom the Software is furnished to do so, subject to the following conditions: 
 
    The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. 
 
    THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
    TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
    THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
    TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using Microsoft.Band;
using Microsoft.Band.Sensors;
using Microsoft.Band.Notifications;
using Microsoft.Band.Tiles;
using System;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace Sensors
{
    public sealed partial class MainPage : Page
    {

        private const string tileId = "{87281FA7-4576-4533-A3E9-8D7BEF7CEEE5}";
        private IBandClient bandClientCopy;
        private int tilesRemaining;

        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        /// <summary>
        /// Connect to Microsoft Band and read Accelerometer data.
        /// </summary>
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the list of Microsoft Bands paired to the phone.
                IBandInfo[] pairedBands = await BandClientManager.Instance.GetBandsAsync();
                if (pairedBands.Length < 1)
                {
                    this.textBlock.Text = "This sample app requires a Microsoft Band paired to your phone. Also make sure that you have the latest firmware installed on your Band, as provided by the latest Microsoft Health app.";
                    return;
                }

                using (IBandClient bandClient = await BandClientManager.Instance.ConnectAsync(pairedBands[0]))
                {
                    // normally wouldn't do it this way obviously
                    bandClientCopy = bandClient;

                    tilesRemaining = await bandClient.TileManager.GetRemainingTileCapacityAsync();

                    if (tilesRemaining > 0)
                    {

                        Guid myTileId = new Guid(tileId);
                        BandTile myTile = new BandTile(myTileId)
                        {
                            Name = "My Tile",
                            IsBadgingEnabled = true,
                            TileIcon = await LoadIcon("ms-appx:///Assets/SampleTileIconLarge.png"),
                            SmallIcon = await LoadIcon("ms-appx:///Assets/SampleTileIconSmall.png")
                        };
                        await bandClient.TileManager.AddTileAsync(myTile);
                    }

                    bandClient.SensorManager.SkinTemperature.ReadingChanged += SkinTemperature_ReadingChanged;
                    await bandClient.SensorManager.SkinTemperature.StartReadingsAsync();
                    await Task.Delay(TimeSpan.FromMinutes(1));
                    await bandClient.SensorManager.Accelerometer.StopReadingsAsync();
                }

            }
            catch (Exception ex)
            {
                this.textBlock.Text = ex.ToString();
            }
        }

        private async void SkinTemperature_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandSkinTemperatureReading> e)
        {
            IBandSkinTemperatureReading temperatureReading = e.SensorReading;
            string text = string.Format("Temperature: {0}C", temperatureReading.Temperature);
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { this.textBlock.Text = text; }).AsTask();

            if (tilesRemaining > 0)
                await bandClientCopy.NotificationManager.SendMessageAsync(Guid.Parse(tileId), text, "Your temperature reading has been taken.", DateTimeOffset.Now, MessageFlags.ShowDialog);

        }

        private async void Accelerometer_ReadingChanged(object sender, BandSensorReadingEventArgs<IBandAccelerometerReading> e)
        {
            IBandAccelerometerReading accel = e.SensorReading;
            string text = string.Format("X = {0}\nY = {1}\nZ = {2}", accel.AccelerationX, accel.AccelerationY, accel.AccelerationZ);
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { this.textBlock.Text = text; }).AsTask();
        }

        private async void RemoveTileButton_Click(object sender, RoutedEventArgs e)
        {
                await bandClientCopy.TileManager.RemoveTileAsync(Guid.Parse(tileId));
        }

        private async Task<BandIcon> LoadIcon(string uri)
        {
            StorageFile imageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri(uri));

            using (IRandomAccessStream fileStream = await imageFile.OpenAsync(FileAccessMode.Read))
            {
                WriteableBitmap bitmap = new WriteableBitmap(1, 1);
                await bitmap.SetSourceAsync(fileStream);
                return bitmap.ToBandIcon();
            }
        }
    }
}
