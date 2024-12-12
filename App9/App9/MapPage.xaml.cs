using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Maps;

namespace App9
{
    public partial class MapPage : ContentPage
    {
        private Map map;
        private List<CustomPin> customPins;
        private string filePath;
        private CustomPin startPin;
        private CustomPin endPin;
        private bool isSelectingStart = false;
        private bool isSelectingEnd = false;
        private Polyline routePolyline;

        public MapPage()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("MapPage constructor started.");
                InitializeComponent();
                customPins = new List<CustomPin>();
                filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "pins.txt");

                Device.BeginInvokeOnMainThread(() =>
                {
                    InitializeMap();
                    LoadPinsAndMessagesFromFile();
                });
                System.Diagnostics.Debug.WriteLine("MapPage constructor completed.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Initialization error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
        }

        private void InitializeMap()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("InitializeMap started.");
                map = new Map
                {
                    MapType = MapType.Street,
                    VerticalOptions = LayoutOptions.FillAndExpand,
                    HasZoomEnabled = true,
                    HasScrollEnabled = true
                };

                map.MapClicked += OnMapClicked;

                var stackLayout = new StackLayout();

                var searchBar = new SearchBar { Placeholder = "Adres ara..." };
                searchBar.SearchButtonPressed += OnSearchButtonPressed;
                stackLayout.Children.Add(searchBar);

                var buttonStack = new StackLayout
                {
                    Orientation = StackOrientation.Vertical,
                    HorizontalOptions = LayoutOptions.FillAndExpand
                };

                var startButton = new Button { Text = "Başlangıç Noktası Seç" };
                startButton.Clicked += OnStartButtonClicked;
                buttonStack.Children.Add(startButton);

                var endButton = new Button { Text = "Bitiş Noktası Seç" };
                endButton.Clicked += OnEndButtonClicked;
                buttonStack.Children.Add(endButton);

                var routeButton = new Button { Text = "Rota Göster" };
                routeButton.Clicked += OnRouteButtonClicked;
                buttonStack.Children.Add(routeButton);

                var listPinsButton = new Button { Text = "İşaretli Noktaları Göster" };
                listPinsButton.Clicked += OnListPinsButtonClicked;
                buttonStack.Children.Add(listPinsButton);

                stackLayout.Children.Add(buttonStack);
                stackLayout.Children.Add(map);

                Content = stackLayout;

                var position = new Position(41.0082, 28.9784); // Default to Istanbul
                map.MoveToRegion(MapSpan.FromCenterAndRadius(position, Distance.FromKilometers(10)));
                System.Diagnostics.Debug.WriteLine("InitializeMap completed.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Harita başlatma hatası: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"İçsel hata: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        private void OnListPinsButtonClicked(object sender, EventArgs e)
        {
            var pinsListPage = new PinsListPage(customPins, filePath, this);
            Navigation.PushAsync(pinsListPage);
        }

        public void RemovePinFromMap(CustomPin customPin)
        {
            map.Pins.Remove(customPin);
            customPins.Remove(customPin);
            SavePinsAndMessagesToFile();
        }

        private async void OnSearchButtonPressed(object sender, EventArgs e)
        {
            var searchBar = (SearchBar)sender;
            var address = searchBar.Text;

            try
            {
                var locations = await Xamarin.Essentials.Geocoding.GetLocationsAsync(address);
                var location = locations?.FirstOrDefault();
                if (location != null)
                {
                    var position = new Position(location.Latitude, location.Longitude);
                    map.MoveToRegion(MapSpan.FromCenterAndRadius(position, Distance.FromKilometers(1)));

                    var pin = new CustomPin
                    {
                        Position = position,
                        Label = address,
                        Address = address,
                        Type = PinType.Place,
                        UserName = "Kullanıcı Adı",
                    };

                    map.Pins.Add(pin);
                    customPins.Add(pin);
                    pin.Clicked += OnPinClicked;
                    SavePinsAndMessagesToFile();
                }
                else
                {
                    await DisplayAlert("Hata", "Adres bulunamadı", "Tamam");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", $"Adres arama hatası: {ex.Message}", "Tamam");
            }
        }

        private void OnStartButtonClicked(object sender, EventArgs e)
        {
            isSelectingStart = true;
            isSelectingEnd = false;
            DisplayAlert("Bilgi", "Haritada başlangıç noktasını seçin", "Tamam");
        }

        private async void OnEndButtonClicked(object sender, EventArgs e)
        {
            var pinList = customPins.ToList();
            if (pinList.Count == 0)
            {
                await DisplayAlert("Hata", "Bitiş noktası seçmek için en az bir pin eklenmiş olmalıdır.", "Tamam");
                return;
            }

            var endOptions = pinList.Select(p => p.Label).ToArray();
            var endResult = await DisplayActionSheet("Bitiş Noktası Seçin", "İptal", null, endOptions);

            if (endResult != "İptal")
            {
                endPin = pinList.FirstOrDefault(p => p.Label == endResult);
                await DisplayAlert("Bilgi", "Bitiş noktası seçildi.", "Tamam");
            }
        }

        private async void OnRouteButtonClicked(object sender, EventArgs e)
        {
            if (startPin == null || endPin == null)
            {
                await DisplayAlert("Hata", "Lütfen başlangıç ve bitiş noktalarını seçin", "Tamam");
                return;
            }
            DrawRouteBetweenPins(startPin, endPin);
        }

        private async void OnMapClicked(object sender, MapClickedEventArgs e)
        {
            var position = e.Position;

            if (isSelectingStart)
            {
                if (startPin != null)
                {
                    map.Pins.Remove(startPin);
                }
                startPin = new CustomPin
                {
                    Position = new Position(position.Latitude, position.Longitude),
                    Label = "Başlangıç",
                    Type = PinType.Generic,
                    PinColor = Color.Green
                };
                map.Pins.Add(startPin);
                startPin.Clicked += OnPinClicked;
                isSelectingStart = false;
                await DisplayAlert("Bilgi", "Başlangıç noktası seçildi", "Tamam");
            }
            else if (isSelectingEnd)
            {
                if (endPin != null)
                {
                    map.Pins.Remove(endPin);
                }
                endPin = new CustomPin
                {
                    Position = new Position(position.Latitude, position.Longitude),
                    Label = "Bitiş",
                    Type = PinType.Generic,
                    PinColor = Color.Red
                };
                map.Pins.Add(endPin);
                endPin.Clicked += OnPinClicked;
                isSelectingEnd = false;
                await DisplayAlert("Bilgi", "Bitiş noktası seçildi", "Tamam");
            }
            else
            {
                string pinLabel = await DisplayPromptAsync("Yeni İşaret", "İşaret için bir etiket girin:");
                if (!string.IsNullOrWhiteSpace(pinLabel))
                {
                    var customPin = new CustomPin
                    {
                        Position = new Position(position.Latitude, position.Longitude),
                        Label = pinLabel,
                        Address = "Özel Konum",
                        Type = PinType.Generic,
                        UserName = "Kullanıcı Adı"
                    };

                    map.Pins.Add(customPin);
                    customPins.Add(customPin);
                    customPin.Clicked += OnPinClicked;

                    SavePinsAndMessagesToFile();
                    await AddMessageToPin(customPin);
                }
            }
        }

        private async void OnPinClicked(object sender, EventArgs e)
        {
            var pin = (CustomPin)sender;
            string action = await DisplayActionSheet("Pin Seçenekleri", "İptal", null, "Mesajları Göster", "Mesaj Ekle");
            if (action == "Mesajları Göster")
            {
                if (pin.Messages.Count > 0)
                {
                    string messages = string.Join("\n", pin.Messages.Select(m => $"{m.UserName}: {m.Text}"));
                    await DisplayAlert("Pin Mesajları", messages, "Tamam");
                }
                else
                {
                    await DisplayAlert("Mesajlar", "Bu pin için mesaj yok.", "Tamam");
                }
            }
            else if (action == "Mesaj Ekle")
            {
                await AddMessageToPin(pin);
            }
        }

        private void LoadPinsAndMessagesFromFile()
        {
            if (!File.Exists(filePath))
            {
                System.Diagnostics.Debug.WriteLine("Pinler dosyası bulunamadı.");
                return;
            }

            try
            {
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    var parts = line.Split(',');
                    if (parts.Length < 2) continue;

                    if (parts[0] == "PIN" && parts.Length >= 5)
                    {
                        // Pin bilgileri
                        if (double.TryParse(parts[1], out double lat) && double.TryParse(parts[2], out double lng))
                        {
                            var pin = new CustomPin
                            {
                                Position = new Position(lat, lng),
                                Label = parts[3],
                                UserName = parts[4],
                                Type = PinType.Place
                            };
                            customPins.Add(pin);
                            map.Pins.Add(pin);
                            pin.Clicked += OnPinClicked;
                        }
                    }
                    else if (parts[0] == "MESSAGE" && parts.Length >= 5)
                    {
                        // Mesaj bilgileri
                        if (double.TryParse(parts[1], out double lat) && double.TryParse(parts[2], out double lng))
                        {
                            var pin = customPins.FirstOrDefault(p =>
                                Math.Abs(p.Position.Latitude - lat) < 0.0001 &&
                                Math.Abs(p.Position.Longitude - lng) < 0.0001);
                            if (pin != null)
                            {
                                pin.Messages.Add(new PinMessage
                                {
                                    UserName = parts[3],
                                    Text = parts[4]
                                });
                            }
                        }
                    }
                }
                System.Diagnostics.Debug.WriteLine($"Yüklendi {customPins.Count} pin ve mesajları.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Pinleri ve mesajları yükleme hatası: {ex.Message}");
            }
        }


        private void SavePinsAndMessagesToFile()
        {
            try
            {
                using (var writer = new StreamWriter(filePath, false))
                {
                    foreach (var pin in customPins)
                    {
                        // İlk satırda pin bilgileri
                        writer.WriteLine($"PIN,{pin.Position.Latitude},{pin.Position.Longitude},{pin.Label},{pin.UserName}");

                        // Mesajlar ayrı satırda tutuluyor
                        foreach (var message in pin.Messages)
                        {
                            writer.WriteLine($"MESSAGE,{pin.Position.Latitude},{pin.Position.Longitude},{message.UserName},{message.Text}");
                        }
                    }
                }
                System.Diagnostics.Debug.WriteLine("Pinler ve mesajlar kaydedildi.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Pin ve mesaj kaydetme hatası: {ex.Message}");
            }
        }


        private void DrawRouteBetweenPins(CustomPin start, CustomPin end)
        {
            if (routePolyline != null)
            {
                map.MapElements.Remove(routePolyline);
            }

            routePolyline = new Polyline
            {
                StrokeColor = Color.Blue,
                StrokeWidth = 5
            };

            routePolyline.Geopath.Add(start.Position);
            routePolyline.Geopath.Add(end.Position);
            map.MapElements.Add(routePolyline);
        }

        private async Task AddMessageToPin(CustomPin pin)
        {
            string messageText = await DisplayPromptAsync("Yeni Mesaj", "Mesajınızı girin:");
            if (!string.IsNullOrEmpty(messageText))
            {
                pin.Messages.Add(new PinMessage
                {
                    Text = messageText,
                    UserName = App.LoggedInUser // Kullanıcı adı otomatik ekleniyor
                });

                SavePinsAndMessagesToFile();
                await DisplayAlert("Bilgi", "Mesaj başarıyla eklendi.", "Tamam");
            }
        }


        public class CustomPin : Pin
        {
            public string UserName { get; set; }
            public List<PinMessage> Messages { get; set; } = new List<PinMessage>();
            public Color PinColor { get; set; }
        }

        public class PinMessage
        {
            public string UserName { get; set; }
            public string Text { get; set; }
        }
    }
}
