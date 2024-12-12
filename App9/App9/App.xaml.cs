using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace App9
{
    public partial class App : Application
    {
        public static string LoggedInUser { get; set; } = string.Empty;
        public App()
        {
            InitializeComponent();

            MainPage = new NavigationPage(new MainPage());
           
        }

        protected override void OnStart()
        {

        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
