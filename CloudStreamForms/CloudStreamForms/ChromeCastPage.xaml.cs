using CloudStreamForms.Models;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Xaml;
using XamEffects;
using static CloudStreamForms.App;
using static CloudStreamForms.CloudStreamCore;
using static CloudStreamForms.MainChrome;

namespace CloudStreamForms
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ChromeCastPage : ContentPage
    {
        public EpisodeResult episodeResult;
        public Movie chromeMovieResult;

        public string TitleName { set { NameLabel.Text = value; } }
        public string DescriptName { set { EpsodeName.Text = value; } }
        public string EpisodeTitleName { set { EpTitleLabel.Text = value; } }
        public string EpisodePosterUrl { set {/* EpisodePoster.Source = value; */} }
        public string EpisodeDescription { set { EpTitleDescript.Text = value; /* EpisodePoster.Source = value; */} }

        public string PosterUrl { set { Poster.Source = value; } }
        public int IconSize { set; get; } = 48;
        public int BigIconSize { set; get; } = 60;

        public int FastForwardTime {
            get { return Settings.LoadingChromeSec; }
        }
        public int BackForwardTime {
            get { return Settings.LoadingChromeSec; }
        }

        public float ScaleAll { set; get; } = 1.4f;
        public float ScaleAllBig { set; get; } = 2f;

        public static int currentSelected = 0;
        public static double changeTime = -1;

        object subtitleMutex = new object();
        public List<Subtitle> subtitles = new List<Subtitle>();
        public int subtitleIndex = -1;
        public bool HasSubtitlesOn = false;
        public int subtitleDelay = 0;

        public async void SelectSubtitleOption()
        {
            List<string> options = new List<string>();
            if (subtitles.Count == 0) {
                options.Add("Download Subtitles (English)");
            }
            else {
                // if (subtitleIndex != -1) {
                // options.Add($"Turn {(HasSubtitlesOn ? "Off" : $"On ({subtitles[subtitleIndex].name})")}");
                //}
                if (HasSubtitlesOn) {
                    if (subtitleIndex != -1) {
                        options.Add($"Turn off");
                    }
                    options.Add($"Change Delay ({subtitleDelay} ms)");
                }
                options.Add("Select Subtitle");
            }
            options.Add("Download Subtitles");

            async Task UpdateSubtitles()
            {
                ActionPopup.StartIndeterminateLoadinbar("Loading Subtitles...");
                if (subtitleIndex != -1) {
                    await ChangeSubtitles(subtitles[subtitleIndex].data, subtitles[subtitleIndex].name, subtitleDelay);
                }
                else {
                    await ChangeSubtitles("", "", 0);
                }
                await ActionPopup.StopIndeterminateLoadinbar();
            }

            string action = await ActionPopup.DisplayActionSheet("Subtitles", options.ToArray());
            if (action == "Download Subtitles") {
                string subAction = await ActionPopup.DisplayActionSheet("Download Subtitles", subtitleNames);
                if (subAction != "Cancel") {
                    int index = subtitleNames.IndexOf(subAction);
                    PopulateSubtitle(subtitleShortNames[index], subAction);
                }
            }
            else if (action == "Select Subtitle") {
                string subAction = await ActionPopup.DisplayActionSheet("Select Subtitle", subtitles.Select(t => t.name).ToArray());

                if (subAction != "Cancel") {
                    for (int i = 0; i < subtitles.Count; i++) {
                        if (subtitles[i].name == subAction) {
                            subtitleIndex = i;
                            HasSubtitlesOn = true;
                            await Task.Delay(100);
                            await UpdateSubtitles();
                        }
                    }
                }
            }
            else if (action == "Download Subtitles (English)") {
                PopulateSubtitle();
            }
            else if (action.StartsWith("Turn off")) {
                subtitleIndex = -1;
                await UpdateSubtitles();
                // await MainChrome.ToggleSubtitles(!HasSubtitlesOn);
                HasSubtitlesOn = !HasSubtitlesOn;
            }
            else if (action.StartsWith("Change Delay")) {
                int del = await ActionPopup.DisplayIntEntry("ms", "Subtitle Delay", 50, false, subtitleDelay.ToString(), "Set Delay");
                if (del != -1) {
                    subtitleDelay = del;
                    await Task.Delay(100);
                    await UpdateSubtitles();
                }
            }
        }

        Dictionary<string, bool> searchingForLang = new Dictionary<string, bool>();

        public void PopulateSubtitle(string lang = "eng", string name = "English")
        {
            bool ContainsLang()
            {
                lock (subtitleMutex) {
                    return subtitles.Where(t => t.name == name).Count() != 0;
                }
            }

            if (ContainsLang()) {
                App.ShowToast("Subtitle already downloaded"); // THIS SHOULD NEVER HAPPEND
                return;
            }

            if (searchingForLang.ContainsKey(lang)) {
                App.ShowToast("Searching for subtitles");
                return;
            }
            searchingForLang[lang] = true;

            var thread = mainCore.CreateThread(6);
            mainCore.StartThread("PopulateSubtitles", () => {
                try {
                    string data = mainCore.DownloadSubtitle(chromeMovieResult.title.id, lang, false, true);
                    if (data.IsClean()) {
                        if (!ContainsLang()) {
                            lock (subtitleMutex) {
                                Subtitle s = new Subtitle();
                                s.name = name;
                                s.data = data;
                                subtitles.Add(s);
                            }
                            App.ShowToast(name + " subtitles added");
                        }
                    }
                    else {
                        App.ShowToast("Error Downloading Subtitles");
                    }
                }
                finally {
                    if (searchingForLang.ContainsKey(lang)) {
                        searchingForLang.Remove(lang);
                    }
                }
            });
        }

        async void SelectMirror()
        {
            bool succ = false;
            currentSelected--;
            while (!succ) {
                currentSelected++;

                if (currentSelected >= episodeResult.Mirros.Count) {
                    succ = true;
                }
                else {
                    try {
                        DescriptName = episodeResult.Mirros[currentSelected];
                    }
                    catch (Exception) {

                    }

                    string _sub = "";
                    if (chromeMovieResult.subtitles != null) {
                        if (chromeMovieResult.subtitles.Count > 0) {
                            _sub = chromeMovieResult.subtitles[0].data;
                        }
                    }

                    if (MainChrome.CurrentTime > 120) {
                        changeTime = MainChrome.CurrentTime;
                        print("CHANGE TIME TO " + changeTime);
                    }

                    string subTxt = "";
                    if (subtitleIndex != -1 && HasSubtitlesOn) {
                        subTxt = subtitles[subtitleIndex].data;
                    }
                    succ = await MainChrome.CastVideo(episodeResult.mirrosUrls[currentSelected], episodeResult.Mirros[currentSelected], subtitleUrl: subTxt, posterUrl: chromeMovieResult.title.hdPosterUrl, movieTitle: chromeMovieResult.title.name, setTime: changeTime, subtitleDelay: subtitleDelay);

                }
            }
            try {
                DescriptName = episodeResult.Mirros[currentSelected];
            }
            catch (Exception) {

            }

            // CastVideo(episodeResult.mirrosUrls[currentSelected], episodeResult.Mirros[currentSelected], CurrentTime);
        }

        void OnStop()
        {
            if (isActive) {
                Navigation.PopModalAsync();
            }
            isActive = false;
        }

        protected override bool OnBackButtonPressed()
        {
            isActive = false;
            return base.OnBackButtonPressed();
        }

        public static bool isActive = false;


        public void OnDisconnectedHandle(object sender, EventArgs e)
        {
            OnStop();
        }
        public void OnPauseChangedHandle(object sender, bool e)
        {
            SetPause(e);
        }
        public void OnForceUpdateTimeHandle(object sender, double e)
        {
            UpdateTxt();
        }

        static string lastId = "";

        public ChromeCastPage()
        {
            isActive = true;
            episodeResult = MovieResult.chromeResult;
            chromeMovieResult = MovieResult.chromeMovieResult;

            InitializeComponent();

            if (lastId != chromeMovieResult.title.id) {
                lastId = chromeMovieResult.title.id;
                if (globalSubtitlesEnabled) {
                    PopulateSubtitle();
                }
            }

            Subbutton.Source = App.GetImageSource("outline_subtitles_white_48dp.png");
            BindingContext = this;
            TitleName = chromeMovieResult.title.name;
            EpisodeTitleName = episodeResult.Title;
            PosterUrl = CloudStreamCore.ConvertIMDbImagesToHD(chromeMovieResult.title.hdPosterUrl, 150, 225);
            EpisodePosterUrl = episodeResult.PosterUrl;
            EpisodeDescription = episodeResult.Description;
            BackgroundColor = Settings.BlackRBGColor;
            //  CloudStreamForms.MainPage.mainPage.BarBackgroundColor = Color.Transparent;
            ChromeLabel.Text = "Connected to " + MainChrome.chromeRecivever.FriendlyName;

            try {
                DescriptName = episodeResult.Mirros[currentSelected];
            }
            catch (Exception _ex) {
                print("ERROR LOADING MIRROR " + _ex);
            }



            //https://material.io/resources/icons/?style=baseline
            VideoSlider.DragStarted += (o, e) => {
                draging = true;
            };


            VideoSlider.DragCompleted += (o, e) => {
                MainChrome.SetChromeTime(VideoSlider.Value * CurrentCastingDuration);
                draging = false;
                UpdateTxt();
            };

            const bool rotateAllWay = false;
            const int rotate = 90;
            const int time = 100;

            Commands.SetTap(FastForwardBtt, new Command(async () => {
                SeekMedia(FastForwardTime);
                FastForward.Rotation = 0;
                if (rotateAllWay) {
                    await FastForward.RotateTo(360, 200, Easing.SinOut);
                }
                else {
                    FastForward.ScaleTo(0.9, time, Easing.SinOut);
                    await FastForward.RotateTo(rotate, time, Easing.SinOut);
                    FastForward.ScaleTo(1, time, Easing.SinOut);
                    await FastForward.RotateTo(0, time, Easing.SinOut);
                }
            }));

            Commands.SetTap(BackForwardBtt, new Command(async () => {
                SeekMedia(-BackForwardTime);
                BackForward.Rotation = 0;
                if (rotateAllWay) {
                    await BackForward.RotateTo(-360, 200, Easing.SinOut);
                }
                else {
                    BackForward.ScaleTo(0.9, time, Easing.SinOut);
                    await BackForward.RotateTo(-rotate, time, Easing.SinOut);
                    BackForward.ScaleTo(1, time, Easing.SinOut);
                    await BackForward.RotateTo(0, time, Easing.SinOut);
                }
            }));

            StopAll.Clicked += (o, e) => {
                //  MainChrome.StopCast();
                JustStopVideo();
                OnStop();
            };

            SkipForward.Clicked += async (o, e) => {
                currentSelected++;
                if (currentSelected > episodeResult.Mirros.Count) { currentSelected = 0; }
                SelectMirror();
                await SkipForward.TranslateTo(6, 0, 50, Easing.SinOut);
                await SkipForward.TranslateTo(0, 0, 50, Easing.SinOut);
            };

            SkipBack.Clicked += async (o, e) => {
                currentSelected--;
                if (currentSelected < 0) { currentSelected = episodeResult.Mirros.Count - 1; }
                SelectMirror();
                await SkipBack.TranslateTo(-6, 0, 50, Easing.SinOut);
                await SkipBack.TranslateTo(0, 0, 50, Easing.SinOut);
            };

            PlayList.Clicked += async (o, e) => {
                //ListScale();
                string a = await ActionPopup.DisplayActionSheet("Select Mirror", episodeResult.Mirros.ToArray()); //await DisplayActionSheet("Select Mirror", "Cancel", null, episodeResult.Mirros.ToArray());
                                                                                                                  //ListScale();

                for (int i = 0; i < episodeResult.Mirros.Count; i++) {
                    if (a == episodeResult.Mirros[i]) {
                        currentSelected = i;
                        SelectMirror();
                        return;
                    }
                }
            };
            ConstUpdate();

            MainChrome.Volume = (MainChrome.Volume);

            /*
            LowVol.Source = GetImageSource("round_volume_down_white_48dp.png");
            MaxVol.Source = GetImageSource("round_volume_up_white_48dp.png");*/

            //   UserDialogs.Instance.TimePrompt(new TimePromptConfig() { CancelText = "Cancel", Title = "da", Use24HourClock = false, OkText = "OK", IsCancellable = true });

        }

        bool draging = false;
        public async void ConstUpdate()
        {
            while (true) {
                await Task.Delay(1000);
                UpdateTxt();
            }
        }

        public void UpdateTxt()
        {
            StartTxt.Text = ConvertTimeToString(CurrentTime);
            EndTxt.Text = ConvertTimeToString(CurrentCastingDuration - CurrentTime);
            if (CurrentCastingDuration - CurrentTime < -1) {
                OnStop();
            }
            if (!draging) {
                VideoSlider.Value = CurrentTime / CurrentCastingDuration;
            }
        }

        const bool IsRounded = false;
        public static string RoundedPrefix { get { return IsRounded ? "round" : "baseline"; } }


        void SetPause(bool paused)
        {
            Pause.Source = paused ? "netflixPlay128v2.png" : "netflixPause128.png";//GetImageSource(paused ? "netflixPlay.png" : "netflixPause.png");//GetImageSource(RoundedPrefix + "_play_arrow_white_48dp.png") : GetImageSource(RoundedPrefix + "_pause_white_48dp.png");
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            App.UpdateToTransparentBg();
            BackFaded.Source = GetImageSource("faded.png");
            PlayList.Source = GetImageSource(RoundedPrefix + "_playlist_play_white_48dp.png");
            StopAll.Source = GetImageSource(RoundedPrefix + "_stop_white_48dp.png");
            BackForward.Source = GetImageSource("netflixSkipBack.png");//GetImageSource(RoundedPrefix + "_replay_white_48dp.png");
            FastForward.Source = GetImageSource("netflixSkipForward.png"); // GetImageSource(RoundedPrefix + "_replay_white_48dp_mirror.png");
            SkipBack.Source = GetImageSource(RoundedPrefix + "_skip_previous_white_48dp.png");
            SkipForward.Source = GetImageSource(RoundedPrefix + "_skip_next_white_48dp.png");
            Audio.Source = GetImageSource(RoundedPrefix + "_volume_up_white_48dp.png");
            SetPause(IsPaused);
            UpdateTxt();

            OnDisconnected += OnDisconnectedHandle;
            OnPauseChanged += OnPauseChangedHandle;
            OnForceUpdateTime += OnForceUpdateTimeHandle;
        }


        protected override void OnDisappearing()
        {
            string lastId = MovieResult.GetId(episodeResult, chromeMovieResult);

            print("SETTITME::: : " + lastId + "|" + (long)(MainChrome.CurrentTime * 1000) + "|" + (long)(MainChrome.CurrentCastingDuration * 1000));
            App.SetViewPos(lastId, (long)(MainChrome.CurrentTime * 1000));
            App.SetViewDur(lastId, (long)(MainChrome.CurrentCastingDuration * 1000));
            App.ForceUpdateVideo?.Invoke(null, EventArgs.Empty);

            App.UpdateBackground();
            OnDisconnected -= OnDisconnectedHandle;
            OnPauseChanged -= OnPauseChangedHandle;
            OnForceUpdateTime -= OnForceUpdateTimeHandle;
            base.OnDisappearing();
        }

        private void AudioClicked(object sender, EventArgs e)
        {
            PopupNavigation.Instance.PushAsync(new CloudStreamForms.MyPopupPage());
            ScaleAudio();
        }

        private void SubClicked(object sender, EventArgs e)
        {
            ScaleSub();
            SelectSubtitleOption();
        }

        async void ScaleSub()
        {
            Subbutton.AbortAnimation("ScaleTo");
            await Subbutton.ScaleTo(1.3, 100, Easing.SinOut);
            await Subbutton.ScaleTo(1.2, 100, Easing.SinOut);
        }

        async void ScaleAudio()
        {
            Audio.AbortAnimation("ScaleTo");
            await Audio.ScaleTo(1.4, 100, Easing.SinOut);
            await Audio.ScaleTo(1.3, 100, Easing.SinOut);
        }

        private void Pause_Clicked(object sender, EventArgs e)
        {
            SetPause(!IsPaused);
            PauseAndPlay(!IsPaused);
            PauseScale();
        }

        async void PauseScale()
        {
            Pause.Scale = 1.7;
            await Pause.ScaleTo(1.6, 50, Easing.SinOut);
            await Pause.ScaleTo(1.7, 50, Easing.SinOut);
        }

        async void ListScale()
        {
            PlayList.Scale = 1.4;
            await PlayList.ScaleTo(2, 50, Easing.SinOut);
            await PlayList.ScaleTo(1.4, 50, Easing.SinOut);
        }
    }
}