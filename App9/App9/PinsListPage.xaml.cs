using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xamarin.Forms;
using static App9.MapPage;

namespace App9
{
    public partial class PinsListPage : ContentPage
    {
        private List<CustomPin> customPins;
        private string filePath;
        private MapPage mapPage; // Reference to the MapPage

        public PinsListPage(List<CustomPin> pins, string filePath, MapPage mapPage)
        {
            InitializeComponent();
            customPins = pins;
            this.mapPage = mapPage; // Initialize the MapPage reference
            this.filePath = filePath; // Initialize the file path
            pinsListView.ItemsSource = customPins; // Bind the ListView to the list of pins
        }

        private async void OnDeletePinClicked(object sender, EventArgs e)
        {
            var button = (Button)sender;
            var pinToDelete = (CustomPin)button.CommandParameter;

            bool confirm = await DisplayAlert("Pin Sil", $"{pinToDelete.Label} isimli pini silmek istediğinize emin misiniz?", "Evet", "Hayır");
            if (confirm)
            {
                // Remove the pin from the list
                customPins.Remove(pinToDelete);

                // Remove the pin from the map view
                mapPage.RemovePinFromMap(pinToDelete);

                // Update the pins file
                SavePinsToFile();

                // Refresh the ListView
                pinsListView.ItemsSource = null;
                pinsListView.ItemsSource = customPins;
            }
        }

        private void SavePinsToFile()
        {
            var lines = customPins.Select(pin => $"{pin.Position.Latitude},{pin.Position.Longitude},{pin.Label}");
            File.WriteAllLines(filePath, lines);
        }
    }
}
