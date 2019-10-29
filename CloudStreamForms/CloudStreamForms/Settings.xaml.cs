﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Reflection;



namespace CloudStreamForms
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Settings : ContentPage
    {
        public const string errorEpisodeToast = "No Links Found";
        public static int LoadingMiliSec
        {
            set {
                App.SetKey("Settings", nameof(LoadingMiliSec), value);
            }
            get {
                return App.GetKey("Settings", nameof(LoadingMiliSec), 5000);
            }
        }

        public static bool DefaultDub
        {
            set {
                App.SetKey("Settings", nameof(DefaultDub), value);
            }
            get {
                return App.GetKey("Settings", nameof(DefaultDub), true);
            }
        }

        public static bool SubtitlesEnabled
        {
            set {
                App.SetKey("Settings", nameof(SubtitlesEnabled), value);
            }
            get {
                return App.GetKey("Settings", nameof(SubtitlesEnabled), true);
            }
        }


        public static bool BlackBg
        {
            set {
                App.SetKey("Settings", nameof(BlackBg), value);
            }
            get {
                return App.GetKey("Settings", nameof(BlackBg), false);
            }
        }

        public static bool ViewHistory
        {
            set {
                App.SetKey("Settings", nameof(ViewHistory), value);
            }
            get {
                return App.GetKey("Settings", nameof(ViewHistory), true);
            }
        }

        public static bool EpDecEnabled
        {
            set {
                App.SetKey("Settings", nameof(EpDecEnabled), value);
            }
            get {
                return App.GetKey("Settings", nameof(EpDecEnabled), true);
            }
        }
        public static bool MovieDecEnabled
        {
            set {
                App.SetKey("Settings", nameof(MovieDecEnabled), value);
            }
            get {
                return App.GetKey("Settings", nameof(MovieDecEnabled), true);
            }
        }
        public static bool SearchEveryCharEnabled
        {
            set {
                App.SetKey("Settings", nameof(SearchEveryCharEnabled), value);
            }
            get {
                return App.GetKey("Settings", nameof(SearchEveryCharEnabled), true);
            }
        }


        public string MainColor { get { return Device.RuntimePlatform == Device.UWP ? "#303F9F" : "#ffffff"; } }

        public static string MainBackgroundColor
        {
            get {
                if (BlackBg) {
                    return "#000000";
                }
                string color = "#111111";
                if (Device.RuntimePlatform == Device.UWP) {
                    color = "#000811";
                }

                return color;
            }
        }


        public Settings()
        {
            InitializeComponent();

            //Main.print("COLOR: "+ BlackBgToggle.OnColor);
            //  if (Device.RuntimePlatform == Device.UWP) {
            BindingContext = this;
            // }
            StarMe.Clicked += (o, e) => {
                App.OpenBrowser("https://github.com/LagradOst/CloudStream-2");
            };
            BuildNumber.Text = "Build Version: " + App.GetBuildNumber();

            ViewHistoryToggle.OnChanged += (o, e) => {
                ViewHistory = e.Value;
            };

            DubToggle.OnChanged += (o, e) => {
                DefaultDub = e.Value;
            };

            BlackBgToggle.OnChanged += (o, e) => {
                BlackBg = e.Value;
            };

            SubtitesToggle.OnChanged += (o, e) => {
                SubtitlesEnabled = e.Value;
            };
            EpsDecToggle.OnChanged += (o, e) => {
                EpDecEnabled = e.Value;
            };
            DecToggle.OnChanged += (o, e) => {
                MovieDecEnabled = e.Value;
            };
            SearchToggle.OnChanged += (o, e) => {
                SearchEveryCharEnabled = e.Value;
            };
            LoadingTime.Text = "Loading Time: " + LoadingMiliSec + "ms";
            LoadingSlider.Value = ((LoadingMiliSec - 1000.0) / 9000.0);

        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            SubtitesToggle.On = SubtitlesEnabled;
            BlackBgToggle.On = BlackBg;
            DubToggle.On = DefaultDub;
            ViewHistoryToggle.On = ViewHistory;
            DecToggle.On = MovieDecEnabled;
            EpsDecToggle.On = EpDecEnabled;
            SearchToggle.On = SearchEveryCharEnabled;
            BackgroundColor = Color.FromHex(Settings.MainBackgroundColor);

        }

        private void Slider_DragCompleted(object sender, EventArgs e)
        {
            LoadingMiliSec = (int)Math.Round(((Slider)sender).Value * 9000) + 1000;
            LoadingTime.Text = "Loading Time: " + LoadingMiliSec + "ms";
        }
    }
}