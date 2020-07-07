using CloudStreamForms.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamEffects;
using static CloudStreamForms.App;
using static CloudStreamForms.CloudStreamCore;
using static CloudStreamForms.MainPage;
using static CloudStreamForms.Settings;

namespace CloudStreamForms
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MovieResult : ContentPage
    {
        const uint FATE_TIME_MS = 500;

        public int SmallFontSize { get; set; } = 11;
        public int WithSize { get; set; } = 50;
        List<Episode> CurrentEpisodes { set { currentMovie.episodes = value; } get { return currentMovie.episodes; } }

        public MovieResultMainEpisodeView epView;

        LabelList SeasonPicker;
        LabelList DubPicker;
        LabelList FromToPicker;


        public Poster mainPoster;
        public string trailerUrl = "";
        List<Button> recBtts = new List<Button>();

        public static List<Movie> lastMovie;
        List<Poster> RecomendedPosters { set { currentMovie.title.recomended = value; } get { return currentMovie.title.recomended; } }  //= new List<Poster>();

        bool loadedTitle = false;
        int currentSeason = 0;
        //ListView episodeView;
        public const int heightRequestPerEpisode = 120;
        public const int heightRequestAddEpisode = 40;
        public const int heightRequestAddEpisodeAndroid = 0;

        bool isMovie = false;
        Movie currentMovie = new Movie();
        bool isDub = true;
        bool RunningWindows { get { return DeviceInfo.Platform == DevicePlatform.UWP; } }
        string CurrentMalLink {
            get {

                try {
                    string s = currentMovie.title.MALData.seasonData[currentSeason].malUrl;
                    if (s != "https://myanimelist.net") {
                        return s;
                    }
                    else {
                        return "";
                    }
                }
                catch (Exception) {
                    return "";
                }
            }
        }

        protected override bool OnBackButtonPressed()
        {
            Search.mainPoster = new Poster();
            if (lastMovie != null) {
                if (lastMovie.Count > 1) {
                    mainCore.activeMovie = lastMovie[lastMovie.Count - 1];
                    lastMovie.RemoveAt(lastMovie.Count - 1);
                }
            }
            if (setKey) {
                App.RemoveKey("BookmarkData", currentMovie.title.id);
            }
            return base.OnBackButtonPressed();

            //     Navigation.PopModalAsync(false);
            //     return true;
        }



        bool setKey = false;
        void SetKey()
        {
            App.SetKey("BookmarkData", currentMovie.title.id, "Name=" + currentMovie.title.name + "|||PosterUrl=" + currentMovie.title.hdPosterUrl + "|||Id=" + currentMovie.title.id + "|||TypeId=" + ((int)currentMovie.title.movieType) + "|||ShortEpView=" + currentMovie.title.shortEpView + "|||=EndAll");
            setKey = false;
        }

        private void StarBttClicked(object sender, EventArgs e)
        {
            bool keyExists = App.KeyExists("BookmarkData", currentMovie.title.id);
            if (keyExists) {
                App.RemoveKey("BookmarkData", currentMovie.title.id);
            }
            else {
                if (currentMovie.title.name == null) {
                    App.SetKey("BookmarkData", currentMovie.title.id, "Name=" + currentMovie.title.name + "|||Id=" + currentMovie.title.id + "|||");

                    setKey = true;
                }
                else {
                    SetKey();
                }
            }
            ChangeStar(!keyExists);
        }

        private void SubtitleBttClicked(object sender, EventArgs e)
        {
            Settings.SubtitlesEnabled = !Settings.SubtitlesEnabled;
            ChangeSubtitle(SubtitlesEnabled);
        }

        private void ShareBttClicked(object sender, EventArgs e)
        {
            if (currentMovie.title.id != "" && currentMovie.title.name != "") {
                Share();
            }
        }

        async void Share()
        {
            List<string> actions = new List<string>() { "Everything", "CloudStream Link", "IMDb Link", "Title", "Title and Description" };
            if (CurrentMalLink != "") {
                actions.Insert(3, "MAL Link");
            }
            if (trailerUrl != "") {
                actions.Insert(actions.Count - 2, "Trailer Link");
            }
            string a = await ActionPopup.DisplayActionSheet("Copy", actions.ToArray());//await DisplayActionSheet("Copy", "Cancel", null, actions.ToArray());
            string copyTxt = "";
            if (a == "CloudStream Link") {
                ActionPopup.StartIndeterminateLoadinbar("Loading...");
                string _s = CloudStreamCore.ShareMovieCode(currentMovie.title.id + "Name=" + currentMovie.title.name + "=EndAll");
                await ActionPopup.StopIndeterminateLoadinbar();
                if (_s != "") {
                    copyTxt = _s;
                }
            }
            else if (a == "IMDb Link") {
                copyTxt = "https://www.imdb.com/title/" + currentMovie.title.id;
            }
            else if (a == "Title") {
                copyTxt = currentMovie.title.name;
            }
            else if (a == "MAL Link") {
                copyTxt = CurrentMalLink;
            }
            else if (a == "Title and Description") {
                copyTxt = currentMovie.title.name + "\n" + currentMovie.title.description;
            }
            else if (a == "Trailer Link") {
                copyTxt = trailerUrl;
            }
            else if (a == "Everything") {
                copyTxt = currentMovie.title.name + " | " + RatingLabel.Text + "\n" + currentMovie.title.description;
                ActionPopup.StartIndeterminateLoadinbar("Loading...");
                string _s = CloudStreamCore.ShareMovieCode(currentMovie.title.id + "Name=" + currentMovie.title.name + "=EndAll");
                await ActionPopup.StopIndeterminateLoadinbar();

                if (_s != "") {
                    copyTxt = copyTxt + "\nCloudStream: " + _s;
                }
                copyTxt = copyTxt + "\nIMDb: " + "https://www.imdb.com/title/" + currentMovie.title.id;
                if (CurrentMalLink != "") {
                    copyTxt = copyTxt + "\nMAL: " + CurrentMalLink;
                }
                if (trailerUrl != "") {
                    copyTxt = copyTxt + "\nTrailer: " + trailerUrl;
                }
            }
            if (a != "Cancel" && copyTxt != "") {
                await Clipboard.SetTextAsync(copyTxt);
                App.ShowToast("Copied " + a + " to Clipboard");
            }

        }

        void ChangeStar(bool? overrideBool = null, string key = null)
        {
            bool keyExists = false;
            if (key == null) {
                key = currentMovie.title.id;
            }
            if (overrideBool == null) {
                keyExists = App.KeyExists("BookmarkData", key);
                print("KEYEXISTS:" + keyExists + "|" + currentMovie.title.id);
            }
            else {
                keyExists = (bool)overrideBool;
            }
            StarBtt.Source = GetImageSource((keyExists ? "bookmarkedBtt.png" : "notBookmarkedBtt.png"));
            Device.BeginInvokeOnMainThread(() => {
                StarBtt.Transformations = new List<FFImageLoading.Work.ITransformation>() { (new FFImageLoading.Transformations.TintTransformation(keyExists ? DARK_BLUE_COLOR : LIGHT_LIGHT_BLACK_COLOR)) };
            });
        }

        void ChangeSubtitle(bool? overrideBool = null)
        {
            bool res = false;
            if (overrideBool == null) {

                res = SubtitlesEnabled;
            }
            else {
                res = (bool)overrideBool;
                //SubtitlesEnabled = res;
            }

            Device.BeginInvokeOnMainThread(() => {
                SubtitleBtt.Transformations = new List<FFImageLoading.Work.ITransformation>() { (new FFImageLoading.Transformations.TintTransformation(res ? DARK_BLUE_COLOR : LIGHT_LIGHT_BLACK_COLOR)) };
            });
        }

        public void SetChromeCast(bool enabled)
        {
            ChromeCastBtt.IsVisible = enabled;
            ChromeCastBtt.IsEnabled = enabled;
            ImgChromeCastBtt.IsVisible = enabled;
            ImgChromeCastBtt.IsEnabled = enabled;
            if (enabled) {
                ImgChromeCastBtt.Source = GetImageSource(MainChrome.CurrentImageSource);
            }
            NameLabel.Margin = new Thickness((enabled ? 50 : 15), 10, 10, 10);
        }


        bool hasAppeared = false;
        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (!hasAppeared) {
                hasAppeared = true;
                ForceUpdateVideo += ForceUpdateAppearing;
            }

            print("APPEARING;::");
            //FadeAppear();
            SetChromeCast(MainChrome.IsChromeDevicesOnNetwork);

        }

        protected override void OnDisappearing()
        {
            //  ForceUpdateVideo -= ForceUpdateAppearing; // CANT REMOVE IT BECAUSE VIDEOPAGE TRIGGERS ONDIS
            base.OnDisappearing();
        }

        public void ForceUpdateAppearing(object s, EventArgs e)
        {
            ForceUpdate();
        }

        private void ChromeCastBtt_Clicked(object sender, EventArgs e)
        {
            WaitChangeChromeCast();
        }

        public static void OpenChrome(bool validate = true)
        {
            if (!ChromeCastPage.isActive) {
                Page p = new ChromeCastPage() { episodeResult = chromeResult, chromeMovieResult = chromeMovieResult };
                MainPage.mainPage.Navigation.PushModalAsync(p, false);
            }
        }

        private void OpenChromecastView(object sender, EventArgs e)
        {
            if (sender != null) {
                ChromeCastPage.isActive = false;
            }
            if (!ChromeCastPage.isActive) {
                OpenChrome(false);
                //      Page p = new ChromeCastPage() { episodeResult = chromeResult, chromeMovieResult = chromeMovieResult };
                //    Navigation.PushModalAsync(p, false);
            }
        }

        public static EpisodeResult chromeResult;
        public static Movie chromeMovieResult;
        async void WaitChangeChromeCast()
        {
            if (MainChrome.IsCastingVideo) {
                Device.BeginInvokeOnMainThread(() => {
                    OpenChromecastView(1, EventArgs.Empty);
                });
            }
            else {
                List<string> names = MainChrome.GetChromeDevicesNames();
                if (MainChrome.IsConnectedToChromeDevice) { names.Add("Disconnect"); }
                string a = await ActionPopup.DisplayActionSheet("Cast to", names.ToArray());//await DisplayActionSheet("Cast to", "Cancel", MainChrome.IsConnectedToChromeDevice ? "Disconnect" : null, names.ToArray());
                if (a != "Cancel") {
                    MainChrome.ConnectToChromeDevice(a);
                }
            }
        }

        public static ImageSource GetGradient()
        {
            return GetImageSource("gradient" + Settings.BlackColor + ".png");//BlackBg ? "gradient.png" : "gradientGray.png");
        }

        async void FadeAppear()
        {
            NormalStack.Opacity = 0;
            NormalStack.Scale = 0.7;
            await Task.Delay(100);
            NormalStack.FadeTo(1);
            NormalStack.ScaleTo(1);
        }

        public MovieResult()
        {
            InitializeComponent();

            mainPoster = Search.mainPoster;

            Gradient.Source = GetGradient();
            IMDbBtt.Source = GetImageSource("IMDbWhite.png");//"imdbIcon.png");
            MALBtt.Source = GetImageSource("MALWhite.png");//"MALIcon.png");
            ShareBtt.Source = GetImageSource("baseline_share_white_48dp.png");//GetImageSource("round_reply_white_48dp_inverted.png");
            StarBtt.Source = GetImageSource("notBookmarkedBtt.png");
            SubtitleBtt.Source = GetImageSource("outline_subtitles_white_48dp.png"); //GetImageSource("round_subtitles_white_48dp.png");
                                                                                     //  IMDbBlue.Source = GetImageSource("IMDbBlue.png"); //GetImageSource("round_subtitles_white_48dp.png");

            ReviewLabel.Clicked += (o, e) => {
                if (!ReviewPage.isOpen) {
                    Page _p = new ReviewPage(currentMovie.title.id, mainPoster.name);
                    MainPage.mainPage.Navigation.PushModalAsync(_p);
                }
            };

            SeasonPicker = new LabelList(SeasonBtt, new List<string>());
            DubPicker = new LabelList(DubBtt, new List<string>());
            // FromToPicker = new LabelList(FromToBtt, new List<string>());

            // -------------- CHROMECASTING THINGS --------------

            if (Device.RuntimePlatform == Device.UWP) {
                ImgChromeCastBtt.TranslationX = 0;
                ImgChromeCastBtt.TranslationY = 0;
                OffBar.IsVisible = false;
                OffBar.IsEnabled = false;
            }
            else {
                OffBar.Source = App.GetImageSource("gradient.png"); OffBar.HeightRequest = 3; OffBar.HorizontalOptions = LayoutOptions.Fill; OffBar.ScaleX = 100; OffBar.Opacity = 0.3; OffBar.TranslationY = 9;
            }

            MainChrome.OnChromeImageChanged += (o, e) => {
                ImgChromeCastBtt.Source = GetImageSource(e);
                ImgChromeCastBtt.Transformations.Clear();
                if (MainChrome.IsConnectedToChromeDevice) {
                    // ImgChromeCastBtt.Transformations = new List<FFImageLoading.Work.ITransformation>() { (new FFImageLoading.Transformations.TintTransformation("#303F9F")) };
                }
            };

            MainChrome.OnChromeDevicesFound += (o, e) => {
                SetChromeCast(MainChrome.IsChromeDevicesOnNetwork);
            };

            MainChrome.OnVideoCastingChanged += (o, e) => {
                if (e) {
                    OpenChromecastView(null, null);
                }
            };

            if (!MainChrome.IsConnectedToChromeDevice) {
                MainChrome.GetAllChromeDevices();
            }

            Recommendations.SizeChanged += (o, e) => {
                SetRecs();
            };


            //ViewToggle.Source = GetImageSource("viewOnState.png");
            ChangeViewToggle();
            ChangeSubtitle();

            //NameLabel.Text = activeMovie.title.name;
            NameLabel.Text = mainPoster.name;
            RatingLabel.Text = mainPoster.year;

            mainCore.titleLoaded += MovieResult_titleLoaded;
            mainCore.trailerLoaded += MovieResult_trailerLoaded;
            mainCore.episodeLoaded += MovieResult_epsiodesLoaded;


            // TrailerBtt.Clicked += TrailerBtt_Clicked;
            Gradient.Clicked += TrailerBtt_Clicked;
            mainCore.linkAdded += MovieResult_linkAdded;

            mainCore.fishingDone += MovieFishingDone;

            mainCore.moeDone += MovieResult_moeDone;

            BackgroundColor = Settings.BlackRBGColor;
            BgColorSet.BackgroundColor = Settings.BlackRBGColor;

            MALB.IsVisible = false;
            MALB.IsEnabled = false;
            MALA.IsVisible = false;
            MALA.IsEnabled = false;
            Grid.SetColumn(MALB, 0);
            Grid.SetColumn(MALA, 0);

            DubBtt.IsVisible = false;
            SeasonBtt.IsVisible = false;

            epView = new MovieResultMainEpisodeView();
            SetHeight();

            BindingContext = epView;
            episodeView.VerticalScrollBarVisibility = Settings.ScrollBarVisibility;
            //  RecStack.HorizontalScrollBarVisibility = Settings.ScrollBarVisibility; // REPLACE REC

            ReloadAllBtt.Clicked += (o, e) => {
                App.RemoveKey("CacheImdb", currentMovie.title.id);
                App.RemoveKey("CacheMAL", currentMovie.title.id);
                Navigation.PopModalAsync(false);
                Search.mainPoster = new Poster();
                PushPageFromUrlAndName(currentMovie.title.id, currentMovie.title.name);
            };
            ReloadAllBtt.Source = GetImageSource("round_refresh_white_48dp.png");

            mainCore.GetImdbTitle(mainPoster);
            currentMovie.title.id = mainPoster.url.Replace("https://imdb.com/title/", "");
            ChangeStar();

            ChangedRecState(0, true);


            Commands.SetTap(NotificationBtt, new Command(() => {
                ToggleNotify();
            }));

            SkipAnimeBtt.Clicked += (o, e) => {
                // Grid.SetColumn(SkipAnimeBtt, 0);
                Device.BeginInvokeOnMainThread(() => {
                    shouldSkipAnimeLoading = true;
                    SkipAnimeBtt.IsVisible = false;
                    SkipAnimeBtt.IsEnabled = false;
                    hasSkipedLoading = true;
                });
            };

            fishProgressLoaded += (o, e) => {
                if (!SameAsActiveMovie()) return;

                Device.InvokeOnMainThreadAsync(async () => {
                    SkipAnimeBtt.Text = $"Skip - {e.currentProgress} of {e.maxProgress}"; // {(int)(e.progressProcentage * 100)}%

                    if (e.progressProcentage > 0) {
                        if (!SkipAnimeBtt.IsVisible && !hasSkipedLoading) {
                            SkipAnimeBtt.Opacity = 0;
                            // Grid.SetColumn(SkipAnimeBtt, 1);
                            SkipAnimeBtt.IsVisible = true;
                            SkipAnimeBtt.IsEnabled = true;
                            SkipAnimeBtt.FadeTo(1);
                        }
                    }
                    if (e.progressProcentage >= 1) {
                        hasSkipedLoading = true;
                        //  Grid.SetColumn(SkipAnimeBtt, 0);
                        SkipAnimeBtt.IsVisible = false;
                        SkipAnimeBtt.IsEnabled = false;
                    }

                    /*
                   if (e.progress >= 1 && (!FishProgress.IsVisible || FishProgress.Progress >= 1)) return;
                   FishProgress.IsVisible = true;
                   FishProgress.IsEnabled = true;
                   FishProgressTxt.IsVisible = true;
                   FishProgressTxt.IsEnabled = true;
                   if (FishProgress.Opacity == 0) {
                       FishProgress.FadeTo(1);
                   }

                   // FishProgressTxt.Text = e.name;
                   await FishProgress.ProgressTo(e.progress, 250, Easing.SinIn);
                   if (e.progress >= 1) {

                       FishProgressTxt.IsVisible = false;
                       FishProgressTxt.IsEnabled = false;

                       await FishProgress.FadeTo(0);
                       FishProgress.IsVisible = false;
                       FishProgress.IsEnabled = false;
                   }*/
                });

            };

        }

        bool hasSkipedLoading = false;

        void CancelNotifications()
        {
            var keys = App.GetKey<List<int>>("NotificationsIds", currentMovie.title.id, new List<int>());
            for (int i = 0; i < keys.Count; i++) {
                App.CancelNotifaction(keys[i]);
            }
        }

        void AddNotifications()
        {
            List<int> keys = new List<int>();

            for (int i = 0; i < setNotificationsTimes.Count; i++) {
                // GENERATE UNIQUE ID
                int _id = 1337 + setNotificationsTimes[i].number * 100000000 + int.Parse(currentMovie.title.id.Replace("tt", ""));// int.Parse(setNotificationsTimes[i].number + currentMovie.title.id.Replace("tt", ""));
                keys.Add(_id);
                print("BIGICON:::" + currentMovie.title.hdPosterUrl + "|" + currentMovie.title.posterUrl);//setNotificationsTimes[i].timeOfRelease);//
                App.ShowNotIntent("NEW EPISODE - " + currentMovie.title.name, setNotificationsTimes[i].episodeName, _id, currentMovie.title.id, currentMovie.title.name, bigIconUrl: currentMovie.title.hdPosterUrl, time: setNotificationsTimes[i].timeOfRelease);// DateTime.UtcNow.AddSeconds(10));//ShowNotification("NEW EPISODE - " + currentMovie.title.name, setNotificationsTimes[i].episodeName, _id, i * 10);
            }
            App.SetKey("NotificationsIds", currentMovie.title.id, keys);
        }

        void ToggleNotify()
        {
            bool hasNot = App.GetKey<bool>("Notifications", currentMovie.title.id, false);
            App.SetKey("Notifications", currentMovie.title.id, !hasNot);
            UpdateNotification(!hasNot);

            if (!hasNot) {
                AddNotifications();
            }
            else {
                CancelNotifications();
            }
        }

        void UpdateNotification(bool? overrideNot = null)
        {
            bool hasNot = overrideNot ?? App.GetKey<bool>("Notifications", currentMovie.title.id, false);
            NotificationImg.Source = App.GetImageSource(hasNot ? "baseline_notifications_active_white_48dp.png" : "baseline_notifications_none_white_48dp.png");
            NotificationImg.Transformations = new List<FFImageLoading.Work.ITransformation>() { (new FFImageLoading.Transformations.TintTransformation(hasNot ? DARK_BLUE_COLOR : LIGHT_LIGHT_BLACK_COLOR)) };
            NotificationTime.TextColor = hasNot ? Color.FromHex(DARK_BLUE_COLOR) : Color.Gray;
        }

        List<MoeEpisode> setNotificationsTimes = new List<MoeEpisode>();

        private void MovieResult_moeDone(object sender, List<MoeEpisode> e)
        {
            if (e == null) return;
            print("MOE DONE:::: + " + e.Count);
            for (int i = 0; i < e.Count; i++) {
                print("MOE______ " + e[i].episodeName);
            }
            void FadeIn()
            {
                NotificationTime.Opacity = 0;
                NotificationTime.FadeTo(1, FATE_TIME_MS);
            }

            if (e.Count <= 0) {
                Device.BeginInvokeOnMainThread(() => {
                    NotificationTime.Text = "Completed";
                    FadeIn();
                });
                return;
            };

            setNotificationsTimes = e;


            if (!SameAsActiveMovie()) return;
            Device.BeginInvokeOnMainThread(() => {
                try {
                    AddNotifications(); // UPDATE NOTIFICATIONS
                }
                catch (Exception _ex) {
                    print("NOTIFICATIONS ADD ERROR: " + _ex);
                }
                try {
                    NotificationTime.Text = "Completed";
                    FadeIn();
                    for (int i = e.Count - 1; i >= 0; i--) {
                        var diff = e[i].DiffTime;
                        print("DIFFTIME:::::" + e[i].DiffTime);
                        if (diff.TotalSeconds > 0) {
                            NotificationTime.Text = "Next Epiode: " + (diff.Days == 0 ? "" : (diff.Days + "d ")) + (diff.Hours == 0 ? "" : (diff.Hours + "h ")) + diff.Minutes + "m";
                            UpdateNotification();
                            NotificationBtt.IsEnabled = true;
                            return;
                        }
                    }
                }
                catch (Exception _ex) {
                    print("EXKKK::" + _ex);
                }
            });
        }


        public void SetColor(EpisodeResult episodeResult)
        {
            string id = GetId(episodeResult);
            if (id != "") {
                List<string> hexColors = new List<string>() { "#ffffff", LIGHT_BLUE_COLOR, "#e5e598" };
                List<string> darkHexColors = new List<string>() { "#909090", DARK_BLUE_COLOR, "#d3c450" };
                int color = 0;
                if (App.KeyExists("ViewHistory", id)) {
                    color = 1;
                }

                DownloadState state = App.GetDstate(GetCorrectId(episodeResult));
                switch (state) {
                    case DownloadState.Downloading:
                        episodeResult.downloadState = 2;
                        break;
                    case DownloadState.Downloaded:
                        episodeResult.downloadState = 1;
                        break;
                    case DownloadState.NotDownloaded:
                        episodeResult.downloadState = 0;
                        break;
                    case DownloadState.Paused:
                        episodeResult.downloadState = 3;
                        break;
                    default:
                        break;
                }
                print("SETCOLOR::: " + state);

                /*
                if (App.KeyExists("dlength", "id" + GetCorrectId(episodeResult))) {
                    try {
                        DownloadState state = App.GetDownloadInfo(GetCorrectId(episodeResult)).state.state;
                        switch (state) {
                            case DownloadState.Downloading:
                                episodeResult.downloadState = 2;
                                break;
                            case DownloadState.Downloaded:
                                episodeResult.downloadState = 1;
                                break;
                            case DownloadState.NotDownloaded:
                                episodeResult.downloadState = 1;
                                break;
                            case DownloadState.Paused:
                                episodeResult.downloadState = 3;
                                break;
                            default:
                                break;
                        }
                        print("STATE:::: " + episodeResult.downloadState + "|" + state);
                      //  color = 2;
                    }
                    catch (Exception _ex) {
                        print("EX:::" + _ex);
                    }

                } else {
                    episodeResult.downloadState = 0;
                }*/

                episodeResult.MainTextColor = hexColors[color];
                episodeResult.MainDarkTextColor = darkHexColors[color];
            }
        }


        async void SetEpisodeFromTo(int segment, int max = -1)
        {
            epView.MyEpisodeResultCollection.Clear();

            int start = MovieResultMainEpisodeView.MAX_EPS_PER * segment;
            if (max == -1) {
                max = epView.AllEpisodes.Length;
            }
            else {
                max = Math.Min(max, epView.AllEpisodes.Length);
            }

            int end = Math.Min(MovieResultMainEpisodeView.MAX_EPS_PER * (segment + 1), max);
            SetHeight(null, end - start);
            RecomendationLoaded.IsVisible = false;

            FadeEpisodes.Opacity = 0;
            for (int i = start; i < end; i++) {
                // await Task.Delay(30);
                epView.MyEpisodeResultCollection.Add(epView.AllEpisodes[i]);
            }
            await Task.Delay(100);
            await FadeEpisodes.FadeTo(1);

        }

        int maxEpisodes = 1;
        public void AddEpisode(EpisodeResult episodeResult, int index)
        {
            var _episode = ChangeEpisode(episodeResult);
            epView.AllEpisodes[index] = _episode;
        }

        EpisodeResult UpdateLoad(EpisodeResult episodeResult)
        {
            long pos;
            long len;
            print("POST PRO ON: " + GetId(episodeResult));
            string realId = GetId(episodeResult);
            print("ID::::::: ON " + realId + "|" + App.GetKey(VIEW_TIME_POS, realId, -1L));
            if ((pos = App.GetViewPos(realId)) > 0) {
                if ((len = App.GetViewDur(realId)) > 0) {
                    episodeResult.Progress = (double)pos / (double)len;
                    episodeResult.ProgressState = pos;
                    print("MAIN DRI:: " + pos + "|" + len + "|" + episodeResult.Progress);
                }//tt8993804 // tt0772328
            }
            return episodeResult;
        }

        EpisodeResult ChangeEpisode(EpisodeResult episodeResult)
        {
            episodeResult.OgTitle = episodeResult.Title;
            SetColor(episodeResult);
            /*if (episodeResult.Rating != "") {
                episodeResult.Title += " | ★ " + episodeResult.Rating;
            }*/
            if (episodeResult.Rating == "") {
                episodeResult.Rating = currentMovie.title.rating;
            }

            if (!isMovie) {
                episodeResult.Title = episodeResult.Episode + ". " + episodeResult.Title;
                print("ADDMOVIE:___" + episodeResult.Episode + "|" + episodeResult.Title);
            }

            if (episodeResult.PosterUrl == "") {
                if (mainCore.activeMovie.title.posterUrl != "") {
                    string posterUrl = "";
                    try {
                        if (mainCore.activeMovie.title.trailers.Count > 0) {
                            if (mainCore.activeMovie.title.trailers[0].PosterUrl != null) {
                                posterUrl = mainCore.activeMovie.title.trailers[0].PosterUrl;
                            }
                        }
                    }
                    catch (Exception) {

                    }
                    episodeResult.PosterUrl = posterUrl;
                }
            }
            if (episodeResult.PosterUrl == "") {
                episodeResult.PosterUrl = CloudStreamCore.VIDEO_IMDB_IMAGE_NOT_FOUND;
            }
            else {
                episodeResult.PosterUrl = CloudStreamCore.ConvertIMDbImagesToHD(episodeResult.PosterUrl, 224, 126); //episodeResult.PosterUrl.Replace(",126,224_AL", "," + pwidth + "," + pheight + "_AL").Replace("UY126", "UY" + pheight).Replace("UX224", "UX" + pwidth);
            }

            episodeResult.Progress = 0;

            UpdateLoad(episodeResult);

            int GetRealIdFromId()
            {
                for (int i = 0; i < epView.MyEpisodeResultCollection.Count; i++) {
                    if (epView.MyEpisodeResultCollection[i].Id == episodeResult.Id) {
                        return i;
                    }
                }
                return -1;
            }

            episodeResult.TapComTwo = new Command(async (s) => {
                int _id = GetRealIdFromId();
                if (_id == -1) return;
                var epRes = epView.MyEpisodeResultCollection[_id];
                if (epRes.downloadState == 1) {
                    PlayDownloadedEp(epRes);
                }
                else {
                    await LoadLinksForEpisode(epRes);
                }
            });

            episodeResult.TapCom = new Command(async (s) => {
                int _id = GetRealIdFromId();
                if (_id == -1) return;

                var epRes = epView.MyEpisodeResultCollection[_id];
                if (epRes.downloadState == 4) return;

                void DeleteData()
                {
                    string downloadKeyData = App.GetDownloadInfo(GetCorrectId(epRes), false).info.fileUrl;//.GetKey("Download", GetId(episodeResult), "");
                    DeleteFile(downloadKeyData, epRes);
                }
                /*
                if (epRes.IsDownloading) { // REMOVE
                    bool action = await DisplayAlert("Delete file", "Do you want to delete " + epRes.OgTitle, "Delete", "Cancel");
                    if (action) {
                        DeleteData();
                    }
                }
                else*/
                if (epRes.IsDownloaded || epRes.IsDownloading) {
                    string action = await ActionPopup.DisplayActionSheet(epRes.OgTitle, "Play", "Delete File"); //await DisplayActionSheet(epRes.OgTitle, "Cancel", null, "Play", "Delete File");
                    if (action == "Delete File") {
                        DeleteData();
                    }
                    else if (action == "Play") {
                        PlayDownloadedEp(epRes);
                    }
                }
                else { // DOWNLOAD
                    epView.MyEpisodeResultCollection[_id].downloadState = 4; // SET IS SEARCHING
                    ForceUpdate();
                    currentDownloadSearchesHappening++;
                    mainCore.GetEpisodeLink(isMovie ? -1 : (episodeResult.Id + 1), currentSeason, isDub: isDub, purgeCurrentLinkThread: currentDownloadSearchesHappening > 0);
                    print("!!___" + _id);
                    await Task.Delay(10000); // WAIT 10 Sec
                    try {
                        if (!SameAsActiveMovie()) return;

                        currentMovie = mainCore.activeMovie;
                        print("!!___" + _id);
                        epRes = epView.MyEpisodeResultCollection[_id];

                        bool hasMirrors = false;
                        epView.MyEpisodeResultCollection[_id].downloadState = 0;
                        if (epRes.mirrosUrls != null) {
                            if (epRes.mirrosUrls.Count > 0) {
                                hasMirrors = true;
                                epView.MyEpisodeResultCollection[_id].downloadState = 2; // SET IS DOWNLOADING
                                ForceUpdate();

                                // App.ShowToast("Yeet" + epRes.mirrosUrls.Count);

                                var l = SortToHdMirrors(epRes.mirrosUrls, epRes.Mirros);
                                for (int i = 0; i < l.Length; i++) {
                                    print(l[i].name + "<<<<<<<LD");
                                }

                                List<string> mirrorUrls = new List<string>();
                                List<string> mirrorNames = new List<string>();

                                for (int i = 0; i < l.Length; i++) {
                                    mirrorNames.Add(l[i].name);
                                    mirrorUrls.Add(l[i].url);
                                }

                                App.UpdateDownload(GetCorrectId(episodeResult), -1);
                                string dpath = App.RequestDownload(GetCorrectId(episodeResult), episodeResult.OgTitle, episodeResult.Description, episodeResult.Episode, currentSeason, mirrorUrls, mirrorNames, episodeResult.GetDownloadTitle(currentSeason, episodeResult.Episode) + ".mp4", episodeResult.PosterUrl, currentMovie.title);
                                print("SETCOLOR:::");
                                //  SetColor(epView.MyEpisodeResultCollection[_id]);

                            }
                            print("SET COLOOOROOROROR" + epRes.OgTitle);
                            //await Task.Delay(1000);
                            ForceUpdate();

                        }
                        if (!hasMirrors) {
                            App.ShowToast("Download Failed");
                        }
                    }
                    catch (Exception _ex) {
                        print("EX DLOAD::: " + _ex);
                    }
                    currentDownloadSearchesHappening--;
                }
            });

            return episodeResult;
        }



        public void ClearEpisodes()
        {
            episodeView.ItemsSource = null;
            epView.MyEpisodeResultCollection.Clear();
            RecomendationLoaded.IsVisible = true;
            episodeView.ItemsSource = epView.MyEpisodeResultCollection;
            SetHeight();
        }


        void SetHeight(bool? setNull = null, int? overrideCount = null)
        {
            episodeView.RowHeight = Settings.EpDecEnabled ? 170 : 100;
            Device.BeginInvokeOnMainThread(() => episodeView.HeightRequest = ((setNull ?? showState != 0) ? 0 : ((overrideCount ?? epView.MyEpisodeResultCollection.Count) * (episodeView.RowHeight) + 40)));
        }

        private void MovieFishingDone(object sender, Movie e)
        {
            if (!SameAsActiveMovie()) return;
            currentMovie = e;
        }

        bool SameAsActiveMovie()
        {
            return currentMovie.title.id == mainCore.activeMovie.title.id;
        }

        private void MovieResult_linkAdded(object sender, Link e)
        {
            if (!SameAsActiveMovie()) return;
            if (currentMovie.episodes[0].name + currentMovie.episodes[0].description == mainCore.activeMovie.episodes[0].name + mainCore.activeMovie.episodes[0].description && epView.MyEpisodeResultCollection.Count > 0) {
            }
            else {
                return;
            }

            MainThread.BeginInvokeOnMainThread(() => {
                currentMovie = mainCore.activeMovie;
                if (CurrentEpisodes != null) {
                    for (int i = 0; i < CurrentEpisodes.Count; i++) {
                        if (CurrentEpisodes[i].links != null) {
                            if (CurrentEpisodes[i].links.Count > 0) {
                                try {
                                    List<Link> links = CurrentEpisodes[i].links;
                                    try {
                                        links = links.OrderBy(l => -l.priority).ToList();
                                    }
                                    catch (Exception) { }
                                    //  if (links.Count != 0) {
                                    // print("LINK ADDED" + links.Count + "|" + links[links.Count - 1].name);
                                    //  }
                                    int realFrom = i % MovieResultMainEpisodeView.MAX_EPS_PER;
                                    epView.MyEpisodeResultCollection[realFrom].epVis = true;
                                    List<string> mirrors = new List<string>();
                                    List<string> mirrorsUrls = new List<string>();
                                    int mirrorCounter = 0;
                                    // myEpisodeResultCollection[i].Mirros.Clear();
                                    for (int f = 0; f < links.Count; f++) {
                                        try {
                                            Link link = links[f];
                                            if (CheckIfURLIsValid(link.url)) {
                                                string name = link.name;
                                                if (name.Contains("[MIRRORCOUNTER]")) {
                                                    mirrorCounter++;
                                                    name = name.Replace("[MIRRORCOUNTER]", mirrorCounter.ToString());
                                                }
                                                mirrors.Add(name);
                                                mirrorsUrls.Add(link.url);
                                            }
                                        }
                                        catch (Exception) { }
                                    }

                                    if (mirrors.Count > epView.MyEpisodeResultCollection[realFrom].Mirros.Count) {
                                        //EpisodeResult epRes = epView.MyEpisodeResultCollection[i];
                                        epView.MyEpisodeResultCollection[realFrom].mirrosUrls = mirrorsUrls;
                                        epView.MyEpisodeResultCollection[realFrom].epVis = mirrors.Count > 0;
                                        epView.MyEpisodeResultCollection[realFrom].Mirros = mirrors;// = new EpisodeResult() { mirros = mirrors, Description = epRes.Description, epVis = mirrors.Count > 0, Id = epRes.Id, mirrosUrls = mirrorsUrls, PosterUrl = epRes.PosterUrl, progress = epRes.progress, Rating = epRes.Rating, subtitles = epRes.subtitles, Title = epRes.Title };
                                    }
                                }
                                catch (Exception _ex) {
                                    print("THIS SHOULD NEVER EVER HAPPEND");
                                    print("VERY FATAL EX IN " + _ex);
                                }
                            }
                        }
                    }
                }
            });
            //print(e + "|" + activeMovie.episodes[1].maxProgress);
        }


        private void TrailerBtt_Clicked(object sender, EventArgs e)
        {
            if (trailerUrl != null) {
                if (trailerUrl != "") {
                    App.RequestVlc(trailerUrl, currentMovie.title.name + " - Trailer");
                    //  App.PlayVLCWithSingleUrl(trailerUrl, currentMovie.title.name + " - Trailer");
                }
            }
        }



        async void FadeTitles(bool fadeSeason)
        {
            print("FAFSAFAFAFA:::");
            DescriptionLabel.Opacity = 0;
            RatingLabel.Opacity = 0;
            RatingLabelRating.Opacity = 0;
            SeasonBtt.Opacity = 0;
            Rectangle bounds = DescriptionLabel.Bounds;
            // DescriptionLabel.LayoutTo(new Rectangle(bounds.X, bounds.Y, bounds.Width, 0), FATE_TIME_MS);
            await RatingLabelRating.FadeTo(1, FATE_TIME_MS);
            ReviewLabel.FadeTo(1, FATE_TIME_MS);

            await RatingLabel.FadeTo(1, FATE_TIME_MS);
            await DescriptionLabel.FadeTo(1, FATE_TIME_MS);
            //    await DescriptionLabel.LayoutTo(bounds, FATE_TIME_MS);
            if (fadeSeason) {
                await SeasonBtt.FadeTo(1, FATE_TIME_MS);
            }


        }

        private void MovieResult_titleLoaded(object sender, Movie e)
        {
            if (loadedTitle) return;
            if (e.title.name != mainPoster.name) return;
            if (!SameAsActiveMovie()) return;

            loadedTitle = true;
            isMovie = (e.title.movieType == MovieType.Movie || e.title.movieType == MovieType.AnimeMovie);

            currentMovie = e;
            if (setKey) {
                SetKey();
            }
            MainThread.BeginInvokeOnMainThread(() => {
                //App.ShowNotIntent("NEW EPISODE - " + currentMovie.title.name, currentMovie.title.name, 1337, currentMovie.title.id, currentMovie.title.name, bigIconUrl: currentMovie.title.hdPosterUrl, time: DateTime.UtcNow.AddSeconds(1));//ShowNotification("NEW EPISODE - " + currentMovie.title.name, setNotificationsTimes[i].episodeName, _id, i * 10);

                EPISODES.Text = isMovie ? "MOVIE" : "EPISODES";

                try {
                    string souceUrl = e.title.trailers.First().PosterUrl;
                    if (CheckIfURLIsValid(souceUrl)) {
                        TrailerBtt.Source = souceUrl;
                    }
                    else {
                        TrailerBtt.Source = ImageSource.FromResource("CloudStreamForms.Resource.gradient.png", Assembly.GetExecutingAssembly());
                    }
                }
                catch (Exception) {
                    TrailerBtt.Source = ImageSource.FromResource("CloudStreamForms.Resource.gradient.png", Assembly.GetExecutingAssembly());
                }

                ChangeStar();

                string extra = "";
                bool haveSeasons = e.title.seasons != 0;

                if (haveSeasons) {
                    extra = e.title.seasons + " Season" + (e.title.seasons == 1 ? "" : "s") + " | ";
                }

                string rYear = mainPoster.year;
                if (rYear == null || rYear == "") {
                    rYear = e.title.year;
                }
                RatingLabel.Text = ((rYear + " | " + e.title.runtime).Replace("|  |", "|")).Replace("|", "  "); //+ " | " + extra + "★ " + e.title.rating).Replace("|  |", "|")).Replace("|", "  ");
                RatingLabelRating.Text = "Rated: " + e.title.rating;
                DescriptionLabel.Text = Settings.MovieDecEnabled ? CloudStreamCore.RemoveHtmlChars(e.title.description) : "";
                if (e.title.description == "") {
                    DescriptionLabel.HeightRequest = 0;
                }



                // ---------------------------- SEASONS ----------------------------


                SeasonPicker.IsVisible = haveSeasons;
                FadeTitles(haveSeasons);

                DubPicker.SelectedIndexChanged += DubPicker_SelectedIndexChanged;
                if (haveSeasons) {
                    List<string> season = new List<string>();
                    for (int i = 1; i <= e.title.seasons; i++) {
                        season.Add("Season " + i);
                    }
                    SeasonPicker.ItemsSource = season;

                    int selIndex = App.GetKey<int>("SeasonIndex", mainCore.activeMovie.title.id, 0);
                    try {
                        SeasonPicker.SelectedIndex = Math.Min(selIndex, SeasonPicker.ItemsSource.Count - 1);
                    }
                    catch (Exception) {
                        SeasonPicker.SelectedIndex = 0; // JUST IN CASE
                    }

                    currentSeason = SeasonPicker.SelectedIndex + 1;


                    print("GetImdbEpisodes>>>>>>>>>>>>>>><<");
                    mainCore.GetImdbEpisodes(currentSeason);
                }
                else {
                    currentSeason = 0; // MOVIES
                    mainCore.GetImdbEpisodes();
                }
                SeasonPicker.SelectedIndexChanged += SeasonPicker_SelectedIndexChanged;

                // ---------------------------- RECOMMENDATIONS ----------------------------

                foreach (var item in Recommendations.Children) { // SETUP
                    Grid.SetColumn(item, 0);
                    Grid.SetRow(item, 0);
                }
                Recommendations.Children.Clear();

                for (int i = 0; i < RecomendedPosters.Count; i++) {
                    Poster p = e.title.recomended[i];
                    string posterURL = ConvertIMDbImagesToHD(p.posterUrl, 76, 113, 1.75); //.Replace(",76,113_AL", "," + pwidth + "," + pheight + "_AL").Replace("UY113", "UY" + pheight).Replace("UX76", "UX" + pwidth);
                    if (CheckIfURLIsValid(posterURL)) {
                        Grid stackLayout = new Grid() { VerticalOptions = LayoutOptions.Start };
                        Button imageButton = new Button() { HeightRequest = RecPosterHeight, WidthRequest = RecPosterWith, BackgroundColor = Color.Transparent, VerticalOptions = LayoutOptions.Start };
                        var ff = new FFImageLoading.Forms.CachedImage {
                            Source = posterURL,
                            HeightRequest = RecPosterHeight,
                            WidthRequest = RecPosterWith,
                            BackgroundColor = Color.Transparent,
                            VerticalOptions = LayoutOptions.Start,
                            Transformations = {
                            //  new FFImageLoading.Transformations.RoundedTransformation(10,1,1.5,10,"#303F9F")
                            new FFImageLoading.Transformations.RoundedTransformation(1, 1, 1.5, 0, "#303F9F")
                        },
                            InputTransparent = true,
                        };

                        // ================================================================ RECOMMENDATIONS CLICKED ================================================================
                        stackLayout.SetValue(XamEffects.TouchEffect.ColorProperty, Color.White);
                        Commands.SetTap(stackLayout, new Command((o) => {
                            int z = (int)o;
                            if (Search.mainPoster.url != RecomendedPosters[z].url) {
                                if (lastMovie == null) {
                                    lastMovie = new List<Movie>();
                                }
                                lastMovie.Add(mainCore.activeMovie);
                                Search.mainPoster = RecomendedPosters[z];
                                Page _p = new MovieResult();// { mainPoster = mainPoster };
                                Navigation.PushModalAsync(_p);
                            }
                            //do something
                        }));
                        Commands.SetTapParameter(stackLayout, i);
                        recBtts.Add(imageButton);

                        stackLayout.Children.Add(ff);
                        stackLayout.Children.Add(imageButton);

                        Recommendations.Children.Add(stackLayout);
                    }
                }

                SetRecs();

            });



        }

        const double _RecPosterMulit = 1.75;
        const int _RecPosterHeight = 100;
        const int _RecPosterWith = 65;
        int RecPosterHeight { get { return (int)Math.Round(_RecPosterHeight * _RecPosterMulit); } }
        int RecPosterWith { get { return (int)Math.Round(_RecPosterWith * _RecPosterMulit); } }

        void SetRecs()
        {
            Device.BeginInvokeOnMainThread(() => {
                const int total = 12;
                int perCol = (Application.Current.MainPage.Width < Application.Current.MainPage.Height) ? 3 : 6;

                for (int i = 0; i < Recommendations.Children.Count; i++) { // GRID
                    Grid.SetColumn(Recommendations.Children[i], i % perCol);
                    Grid.SetRow(Recommendations.Children[i], (int)Math.Floor(i / (double)perCol));
                }
                // Recommendations.HeightRequest = (RecPosterHeight + Recommendations.RowSpacing) * (total / perCol);
                Recommendations.HeightRequest = (RecPosterHeight + Recommendations.RowSpacing) * (total / perCol);
            });
        }

        private void DubPicker_SelectedIndexChanged(object sender, int e)
        {
            print("DUBCHANGED::");
            try {
                isDub = "Dub" == DubPicker.ItemsSource[DubPicker.SelectedIndex];
                SetDubExist();
            }
            catch (Exception _ex) {
                print("EXPECTION:" + _ex);
            }
        }

        private void SeasonPicker_SelectedIndexChanged(object sender, int e)
        {
            ClearEpisodes();
            //  epView.MyEpisodeResultCollection.Clear();

            DubPicker.button.FadeTo(0, FATE_TIME_MS);
            currentSeason = SeasonPicker.SelectedIndex + 1;
            App.SetKey("SeasonIndex", mainCore.activeMovie.title.id, SeasonPicker.SelectedIndex);

            mainCore.GetImdbEpisodes(currentSeason);
        }

        void SetChangeTo(int maxEp = -1)
        {
            Device.BeginInvokeOnMainThread(() => {
                if (maxEp == -1) {
                    maxEp = maxEpisodes;
                }
                var source = new List<string>();

                int times = (int)Math.Ceiling((decimal)epView.AllEpisodes.Length / (decimal)MovieResultMainEpisodeView.MAX_EPS_PER);

                for (int i = 0; i < times; i++) {
                    int fromTo = maxEp - i * MovieResultMainEpisodeView.MAX_EPS_PER;
                    string f = (i * MovieResultMainEpisodeView.MAX_EPS_PER + 1) + "-" + ((i) * MovieResultMainEpisodeView.MAX_EPS_PER + Math.Min(fromTo, MovieResultMainEpisodeView.MAX_EPS_PER));
                    source.Add(f);
                }

                FromToPicker = new LabelList(FromToBtt, source, "Select Episode");//.IsVisible = FromToPicker.ItemsSource.Count > 1;                
                FromToPicker.SelectedIndex = 0;//.IsVisible = FromToPicker.ItemsSource.Count > 1;           
                FromToPicker.IsVisible = FromToPicker.ItemsSource.Count > 1;
                FromToPicker.button.IsEnabled = FromToPicker.ItemsSource.Count > 1;
                FromToPicker.SelectedIndexChanged += (o, e) => {
                    SetEpisodeFromTo(e, maxEpisodes);
                };
            });

        }

        private void MovieResult_epsiodesLoaded(object sender, List<Episode> e)
        {
            Device.BeginInvokeOnMainThread(() => {
                print("GOT RESULTS; LETS GO");
                if (!SameAsActiveMovie()) return;

                if (e == null || e.Count == 0) {
                    RecomendationLoaded.IsVisible = false;
                    return;
                };
                print("episodes loaded");

                currentMovie = mainCore.activeMovie;

                currentMovie.episodes = e;
                CurrentEpisodes = e;
                ClearEpisodes();
                //bool isLocalMovie = false;
                bool isAnime = currentMovie.title.movieType == MovieType.Anime;

                if (currentMovie.title.movieType != MovieType.Movie && currentMovie.title.movieType != MovieType.AnimeMovie) { // SEASON ECT
                    print("MAXEPS:::" + CurrentEpisodes.Count);
                    epView.AllEpisodes = new EpisodeResult[CurrentEpisodes.Count];
                    maxEpisodes = epView.AllEpisodes.Length;
                    for (int i = 0; i < CurrentEpisodes.Count; i++) {
                        AddEpisode(new EpisodeResult() { Episode = i + 1, Title = CurrentEpisodes[i].name, Id = i, Description = CurrentEpisodes[i].description.Replace("\n", "").Replace("  ", ""), PosterUrl = CurrentEpisodes[i].posterUrl, Rating = CurrentEpisodes[i].rating, Progress = 0, epVis = false, subtitles = new List<string>() { "None" }, Mirros = new List<string>() }, i);
                    }
                    if (!isAnime) {
                        SetEpisodeFromTo(0);
                        SetChangeTo();
                    }
                }
                else { // MOVE
                    maxEpisodes = 1;
                    epView.AllEpisodes = new EpisodeResult[1];
                    AddEpisode(new EpisodeResult() { Title = currentMovie.title.name, Description = currentMovie.title.description, Id = 0, PosterUrl = "", Progress = 0, Rating = "", epVis = false, subtitles = new List<string>() { "None" }, Mirros = new List<string>() }, 0);
                    SetEpisodeFromTo(0);
                }

                DubPicker.ItemsSource.Clear();

                // SET DUB SUB
                if (isAnime) { 
                    mainCore.GetSubDub(currentSeason, out bool subExists, out bool dubExists);

                    isDub = dubExists;

                    if (Settings.DefaultDub) {
                        if (dubExists) {
                            DubPicker.ItemsSource.Add("Dub");
                        }
                    }
                    if (subExists) {
                        DubPicker.ItemsSource.Add("Sub");
                    }
                    if (!Settings.DefaultDub) {
                        if (dubExists) {
                            DubPicker.ItemsSource.Add("Dub");
                        }
                    }

                    if (DubPicker.ItemsSource.Count > 0) {
                        DubPicker.SelectedIndex = 0;
                    }
                    DubPicker.OnUpdateList();
                    SetDubExist();
                }




                bool enabled = currentMovie.title.movieType == MovieType.Anime; //CurrentMalLink != "";
                print("SETACTIVE::: " + enabled);

                MALB.IsVisible = enabled;
                MALB.IsEnabled = enabled;
                MALA.IsVisible = enabled;
                MALA.IsEnabled = enabled;

                Grid.SetColumn(MALB, enabled ? 5 : 0);
                Grid.SetColumn(MALA, enabled ? 5 : 0);

                DubPicker.button.Opacity = 0;
                DubPicker.IsVisible = DubPicker.ItemsSource.Count > 0;
                DubPicker.button.FadeTo(DubPicker.IsVisible ? 1 : 0, FATE_TIME_MS);
            });

        }


        void SetDubExist()
        {
            print("SETDUB:::");
            if (!SameAsActiveMovie()) return;
            print("SETDUB:::SET");

            TempThread tempThred = mainCore.CreateThread(6);
            mainCore.StartThread("Set SUB/DUB", () => {
                try {
                    int max = mainCore.GetMaxEpisodesInAnimeSeason(currentSeason, isDub, tempThred);
                    if (max > 0) {
                        print("CLEAR AND ADD");
                        MainThread.BeginInvokeOnMainThread(() => { 
                            // CLEAR EPISODES SO SWITCHING SUB DUB 
                            try {
                                for (int i = 0; i < epView.MyEpisodeResultCollection.Count; i++) {
                                    if (epView.MyEpisodeResultCollection[i].mirrosUrls != null) {
                                        epView.MyEpisodeResultCollection[i].mirrosUrls = new List<string>();
                                        epView.MyEpisodeResultCollection[i].Mirros = new List<string>();
                                        //epView.MyEpisodeResultCollection[i].epVis = false;
                                    }
                                }
                            }
                            catch (Exception _ex) {
                                print("MAIN ERROR IN CLEAR: " + _ex);
                            }

                            maxEpisodes = max;
                            print("MAXUSsssss" + maxEpisodes + "|" + max + "|" + (int)Math.Ceiling((double)max / (double)MovieResultMainEpisodeView.MAX_EPS_PER));

                            SetEpisodeFromTo(0, max);
                            SetChangeTo(max);
                        });
                    }
                    else {
                        Device.BeginInvokeOnMainThread(() => {
                            RecomendationLoaded.IsVisible = false;
                        });
                    }
                }
                finally {
                    mainCore.JoinThred(tempThred);
                }
            });
        }


        private void MovieResult_trailerLoaded(object sender, List<Trailer> e)
        {
            if (!SameAsActiveMovie()) return;
            if (e == null) return;
            currentMovie.title.trailers = e;
            epView.CurrentTrailers.Clear();
            for (int i = 0; i < e.Count; i++) {
                epView.CurrentTrailers.Add(e[i]);
            }

            if (e.Count > 4) return; // MAX 4 TRAILERS

            if (trailerUrl == "") {
                trailerUrl = e[0].Url;
            }

            Device.BeginInvokeOnMainThread(() => {
                TRAILERSTAB.IsVisible = true;
                TRAILERSTAB.IsEnabled = true;
                trailerView.Children.Clear();
                trailerView.HeightRequest = e.Count * 240 + 200;
                if (PlayBttGradient.Source == null) {
                    PlayBttGradient.Source = GetImageSource("nexflixPlayBtt.png");
                    PlayBttGradient.Opacity = 0;
                    PlayBttGradient.FadeTo(1, FATE_TIME_MS);
                }

                for (int i = 0; i < e.Count; i++) {
                    string p = e[i].PosterUrl;
                    if (CheckIfURLIsValid(p)) {
                        Grid stackLayout = new Grid();
                        Label textLb = new Label() { Text = e[i].Name, TextColor = Color.FromHex("#e7e7e7"), FontAttributes = FontAttributes.Bold, FontSize = 15, TranslationX = 10 };
                        Image playBtt = new Image() { Source = GetImageSource("nexflixPlayBtt.png"), VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center, Scale = 0.5, InputTransparent = true };
                        var ff = new FFImageLoading.Forms.CachedImage {
                            Source = p,
                            BackgroundColor = Color.Transparent,
                            VerticalOptions = LayoutOptions.Fill,
                            Aspect = Aspect.AspectFill,
                            HorizontalOptions = LayoutOptions.Fill,
                            Transformations = {
                            new FFImageLoading.Transformations.RoundedTransformation(1, 1.7, 1, 0, "#303F9F")

                        },
                            InputTransparent = true,
                        };

                        int _sel = int.Parse(i.ToString());
                        stackLayout.Children.Add(ff);
                        stackLayout.Children.Add(playBtt);
                        trailerView.Children.Add(stackLayout);
                        trailerView.Children.Add(textLb);

                        stackLayout.SetValue(XamEffects.TouchEffect.ColorProperty, new Color(1, 1, 1, 0.3));
                        Commands.SetTap(stackLayout, new Command((o) => {
                            int z = (int)o;
                            var _t = epView.CurrentTrailers[z];
                            RequestVlc(_t.Url, _t.Name);
                            //PlayVLCWithSingleUrl(_t.Url, _t.Name);
                        }));
                        Commands.SetTapParameter(stackLayout, _sel);
                        Grid.SetRow(stackLayout, (i + 1) * 2 - 2);
                        Grid.SetRow(textLb, (i + 1) * 2 - 1);

                    }
                }
            });

            //  trailerView.Children.Add(new )
            /*
            MainThread.BeginInvokeOnMainThread(() => {
                Trailer trailer = activeMovie.title.trailers.First();
                trailerUrl = trailer.url;
                print(trailer.posterUrl);
                TrailerBtt.Source = trailer.posterUrl;//ImageSource.FromUri(new System.Uri(trailer.posterUrl));

            });*/

        }
        private void IMDb_Clicked(object sender, EventArgs e)
        {
            // if (!SameAsActiveMovie()) return;
            App.OpenBrowser("https://www.imdb.com/title/" + mainPoster.url);
        }
        private void MAL_Clicked(object sender, EventArgs e)
        {
            //   if (!SameAsActiveMovie()) return;
            App.OpenBrowser(CurrentMalLink);
        }

        void PlayDownloadedEp(EpisodeResult episodeResult, string data = null)
        {
            var downloadKeyData = data ?? App.GetDownloadInfo(GetCorrectId(episodeResult), false).info.fileUrl;
            SetEpisode(episodeResult);
            Download.PlayVLCFile(downloadKeyData, episodeResult.Title, GetCorrectId(episodeResult).ToString());
        }

        /*
        void PlayEpisodeRes(EpisodeResult episodeResult)
        {
            string hasDownloadedFile = App.GetKey("Download", GetId(episodeResult), "");
            if (hasDownloadedFile != "") {
                Download.PlayFile(hasDownloadedFile, episodeResult.Title);
            }
            else {
                LoadLinksForEpisode(episodeResult);
            }
        }*/

        /*
    private void ImageButton_Clicked(object sender, EventArgs e) // LOAD
    {
        if (!SameAsActiveMovie()) return;
        EpisodeResult episodeResult = ((EpisodeResult)((ImageButton)sender).BindingContext);
        PlayEpisodeRes(episodeResult);

        episodeView.SelectedItem = null;
    }*/

        bool loadingLinks = false;

        static int currentDownloadSearchesHappening = 0;

        async Task<EpisodeResult> LoadLinksForEpisode(EpisodeResult episodeResult, bool autoPlay = true, bool overrideLoaded = false)
        {
            if (loadingLinks) return episodeResult;

            if (episodeResult.LoadedLinks && !overrideLoaded) {
                print("OPEN : " + episodeResult.Title);
                if (episodeResult.mirrosUrls.Count > 0) {

                    if (autoPlay) { PlayEpisode(episodeResult); }
                }
                else {
                    episodeView.SelectedItem = null;
                    App.ShowToast(errorEpisodeToast);
                }
            }
            else {
                mainCore.GetEpisodeLink(isMovie ? -1 : (episodeResult.Id + 1), currentSeason, isDub: isDub, purgeCurrentLinkThread: currentDownloadSearchesHappening > 0);

                await Device.InvokeOnMainThreadAsync(async () => {
                    // NormalStack.IsEnabled = false;
                    loadingLinks = true;

                    await ActionPopup.DisplayLoadingBar(LoadingMiliSec, "Loading Links...");

                    /*
                    UserDialogs.Instance.ShowLoading("Loading links...", MaskType.Gradient);
                    await Task.Delay(LoadingMiliSec);
                    UserDialogs.Instance.HideLoading();*/
                    int errorCount = 0;
                    const int maxErrorcount = 1;
                    bool gotError = false;
                    loadingLinks = false;

                    if (SameAsActiveMovie()) {
                        currentMovie = mainCore.activeMovie;
                    }
                //NormalStack.IsEnabled = true;
                // NormalStack.Opacity = 1f;
                checkerror:;
                    if (episodeResult == null) {
                        gotError = true;
                    }
                    else {
                        if (episodeResult.mirrosUrls == null) {
                            gotError = true;
                        }
                        else {
                            if (episodeResult.mirrosUrls.Count > 0) {
                                if (autoPlay) { PlayEpisode(episodeResult); }
                                //episodeResult.LoadedLinks = true;
                            }
                            else {
                                gotError = true;
                            }
                        }
                    }
                    if (gotError) {
                        if (errorCount < maxErrorcount) {
                            errorCount++;
                            await ActionPopup.DisplayLoadingBar(2000, "Loading More Links...");
                            goto checkerror;
                        }
                        else {
                            episodeView.SelectedItem = null;
                            App.ShowToast(errorEpisodeToast);
                        }
                    }
                });
            }

            return episodeResult;
        }


        // ============================== PLAY VIDEO ==============================
        void PlayEpisode(EpisodeResult episodeResult, bool? overrideSelectVideo = null)
        {
            string id = GetId(episodeResult);
            if (id != "") {
                if (ViewHistory) {
                    App.SetKey("ViewHistory", id, true);
                    SetColor(episodeResult);
                    // FORCE UPDATE
                    ForceUpdate(episodeResult.Id);
                }
            }

            /*
            string _sub = "";
            if (currentMovie.subtitles != null) {
                if (currentMovie.subtitles.Count > 0) {
                    _sub = currentMovie.subtitles[0].data;
                }
            }*/
            App.RequestVlc(episodeResult.mirrosUrls, episodeResult.Mirros, episodeResult.OgTitle, GetId(episodeResult), episode: episodeResult.Episode, season: currentSeason, subtitleFull: currentMovie.subtitles.Select(t => t.data).FirstOrDefault(), descript: episodeResult.Description, overrideSelectVideo: overrideSelectVideo, startId: (int)episodeResult.ProgressState);// startId: FROM_PROGRESS); //  (int)episodeResult.ProgressState																																																																													  //App.PlayVLCWithSingleUrl(episodeResult.mirrosUrls, episodeResult.Mirros, currentMovie.subtitles.Select(t => t.data).ToList(), currentMovie.subtitles.Select(t => t.name).ToList(), currentMovie.title.name, episodeResult.Episode, currentSeason, overrideSelectVideo);
        }

        // ============================== FORCE UPDATE ==============================
        void ForceUpdate(int? item = null)
        {
            //return;
            print("FORCE UPDATING");
            var _e = epView.MyEpisodeResultCollection.ToList();
            Device.BeginInvokeOnMainThread(() => {

                // if(item == null) {
                epView.MyEpisodeResultCollection.Clear();
                for (int i = 0; i < _e.Count; i++) {
                    // print("Main::" + _e[i].MainTextColor);
                    epView.MyEpisodeResultCollection.Add(UpdateLoad((EpisodeResult)_e[i].Clone()));
                }
                /*  }
                  else {

                      EpisodeResult episodeResult = epView.MyEpisodeResultCollection[(int)item];
                      epView.MyEpisodeResultCollection.RemoveAt((int)item);
                      epView.MyEpisodeResultCollection.Insert((int)item, episodeResult);
                  }*/


            });
        }

        // public string ChromeColor { set; get; }//{ get { return (MainChrome.IsPendingConnection || MainChrome.IsCastingVideo) ? "#303F9F" : "#ffffff"; } set { } }

        async void EpisodeSettings(EpisodeResult episodeResult)
        {
            print("EPDATA:::" + episodeResult.OgTitle + "|" + episodeResult.Episode);
            if (!episodeResult.LoadedLinks) {
                try {
                    await LoadLinksForEpisode(episodeResult, false);
                }
                catch (Exception) { }
            }
            /*
            if (loadingLinks) {
                await Task.Delay(LoadingMiliSec + 40);
            }*/

            if (!episodeResult.LoadedLinks) {
                //   App.ShowToast(errorEpisodeToast); episodeView.SelectedItem = null;
                return;
            }


            // ============================== GET ACTION ==============================
            string action = "";

            bool hasDownloadedFile = App.KeyExists("dlength", "id" + GetCorrectId(episodeResult));
            string downloadKeyData = "";

            List<string> actions = new List<string>() { "Play in App", "Play External App", "Play in Browser", "Download", "Download Subtitles", "Copy Link", "Remove Link", "Reload" }; // 

            if (hasDownloadedFile) {
                downloadKeyData = App.GetDownloadInfo(GetCorrectId(episodeResult), false).info.fileUrl;//.GetKey("Download", GetId(episodeResult), "");
                print("INFOOOOOOOOO:::" + downloadKeyData);
                actions.Add("Play Downloaded File"); actions.Add("Delete Downloaded File");
            }
            if (MainChrome.IsConnectedToChromeDevice) {
                actions.Insert(0, "Chromecast");
            }

            action = await ActionPopup.DisplayActionSheet(episodeResult.Title, actions.ToArray());//await DisplayActionSheet(episodeResult.Title, "Cancel", null, actions.ToArray());


            if (action == "Play in Browser") {
                string copy = await ActionPopup.DisplayActionSheet("Open Link", episodeResult.Mirros.ToArray()); // await DisplayActionSheet("Open Link", "Cancel", null, episodeResult.Mirros.ToArray());
                for (int i = 0; i < episodeResult.Mirros.Count; i++) {
                    if (episodeResult.Mirros[i] == copy) {
                        App.OpenSpecifiedBrowser(episodeResult.mirrosUrls[i]);
                    }
                }
            }
            else if (action == "Remove Link") {
                string rLink = await ActionPopup.DisplayActionSheet("Remove Link", episodeResult.Mirros.ToArray()); //await DisplayActionSheet("Download", "Cancel", null, episodeResult.Mirros.ToArray());
                for (int i = 0; i < episodeResult.Mirros.Count; i++) {
                    if (episodeResult.Mirros[i] == rLink) {
                        App.ShowToast("Removed " + episodeResult.Mirros[i]);
                        episodeResult.mirrosUrls.RemoveAt(i);
                        episodeResult.Mirros.RemoveAt(i);
                        EpisodeSettings(episodeResult);
                        break;
                    }
                }
            }
            else if (action == "Chromecast") { // ============================== CHROMECAST ==============================
                chromeResult = episodeResult;
                chromeMovieResult = currentMovie;
                bool succ = false;
                int count = -1;
                episodeView.SelectedItem = null;

                while (!succ) {
                    count++;

                    if (count >= episodeResult.Mirros.Count) {
                        succ = true;
                    }
                    else {
                        /*
                        string _sub = "";
                        if (currentMovie.subtitles != null) {
                            if (currentMovie.subtitles.Count > 0) {
                                _sub = currentMovie.subtitles[0].data;
                            }
                        }*/

                        succ = await MainChrome.CastVideo(episodeResult.mirrosUrls[count], episodeResult.Mirros[count], subtitleUrl: "", posterUrl: currentMovie.title.hdPosterUrl, movieTitle: currentMovie.title.name, subtitleDelay:0);
                    }
                }
                ChromeCastPage.currentSelected = count;

                print("CASTOS");
                /*
                string download = await DisplayActionSheet("Download", "Cancel", null, episodeResult.Mirros.ToArray());
                for (int i = 0; i < episodeResult.Mirros.Count; i++) {
                    if (episodeResult.Mirros[i] == download) {
                        MainChrome.CastVideo(episodeResult.mirrosUrls[i], episodeResult.Mirros[i]);
                    }
                }*/
            }

            if (action == "Play") { // ============================== PLAY ==============================
                PlayEpisode(episodeResult);
            }
            else if (action == "Play External App") {
                PlayEpisode(episodeResult, false);
            }
            else if (action == "Play in App") {
                PlayEpisode(episodeResult, true);
            }
            else if (action == "Copy Link") { // ============================== COPY LINK ==============================
                string copy = await ActionPopup.DisplayActionSheet("Copy Link", episodeResult.Mirros.ToArray());//await DisplayActionSheet("Copy Link", "Cancel", null, episodeResult.Mirros.ToArray());
                for (int i = 0; i < episodeResult.Mirros.Count; i++) {
                    if (episodeResult.Mirros[i] == copy) {
                        await Clipboard.SetTextAsync(episodeResult.mirrosUrls[i]);
                        App.ShowToast("Copied Link to Clipboard");
                        break;
                    }
                }
            }
            else if (action == "Download") {  // ============================== DOWNLOAD FILE ==============================
                string download = await ActionPopup.DisplayActionSheet("Download", episodeResult.Mirros.ToArray()); //await DisplayActionSheet("Download", "Cancel", null, episodeResult.Mirros.ToArray());
                for (int i = 0; i < episodeResult.Mirros.Count; i++) {
                    if (episodeResult.Mirros[i] == download) {
                        string mirrorUrl = episodeResult.mirrosUrls[i];
                        string mirrorName = episodeResult.Mirros[i];
                        DownloadSubtitlesToFileLocation(episodeResult, currentMovie, currentSeason, showToast: false);
                        TempThread tempThred = mainCore.CreateThread(4);
                        mainCore.StartThread("DownloadThread", async () => {
                            try {
                                //UserDialogs.Instance.ShowLoading("Checking link...", MaskType.Gradient);
                                ActionPopup.StartIndeterminateLoadinbar("Checking link...");
                                double fileSize = CloudStreamCore.GetFileSize(mirrorUrl);
                                //    UserDialogs.Instance.HideLoading();
                                await ActionPopup.StopIndeterminateLoadinbar();
                                if (fileSize > 1) {
                                    print("DSUZE:::::" + episodeResult.Episode);

                                    // ImageService.Instance.LoadUrl(episodeResult.PosterUrl, TimeSpan.FromDays(30)); // CASHE IMAGE
                                    App.UpdateDownload(GetCorrectId(episodeResult), -1);
                                    print("CURRENTSESON: " + currentSeason);

                                    string dpath = App.RequestDownload(GetCorrectId(episodeResult), episodeResult.OgTitle, episodeResult.Description, episodeResult.Episode, currentSeason, new List<string>() { mirrorUrl }, new List<string>() { mirrorName }, episodeResult.GetDownloadTitle(currentSeason, episodeResult.Episode) + ".mp4", episodeResult.PosterUrl, currentMovie.title);

                                    App.ShowToast("Download Started - " + fileSize + "MB");
                                    episodeResult.downloadState = 2;
                                    ForceUpdate(episodeResult.Id);
                                }
                                else {
                                    EpisodeSettings(episodeResult);
                                    App.ShowToast("Download Failed");
                                    ForceUpdate(episodeResult.Id);
                                }
                            }
                            finally {
                                //UserDialogs.Instance.HideLoading();
                                mainCore.JoinThred(tempThred);
                            }
                        });
                    }
                }
            }
            else if (action == "Reload") { // ============================== RELOAD ==============================
                try {
                    await LoadLinksForEpisode(episodeResult, false, true);
                }
                catch (Exception) { }

                //await Task.Delay(LoadingMiliSec + 40);

                if (!episodeResult.LoadedLinks) {
                    return;
                }
                EpisodeSettings(episodeResult);
            }
            else if (action == "Play Downloaded File") { // ============================== PLAY FILE ==============================
                                                         //  bool succ = App.DeleteFile(info.info.fileUrl); 
                                                         //  Download.PlayVLCFile(downloadKeyData, episodeResult.Title);
                PlayDownloadedEp(episodeResult, downloadKeyData);
            }
            else if (action == "Delete Downloaded File") {  // ============================== DELETE FILE ==============================
                DeleteFile(downloadKeyData, episodeResult);
            }
            else if (action == "Download Subtitles") {  // ============================== DOWNLOAD SUBTITLE ==============================
                DownloadSubtitlesToFileLocation(episodeResult, currentMovie, currentSeason, true);
            }
            episodeView.SelectedItem = null;
        }

        static Dictionary<string, bool> hasSubtitles = new Dictionary<string, bool>();

        static void DownloadSubtitlesToFileLocation(EpisodeResult episodeResult, Movie currentMovie, int currentSeason, bool renew = false, bool showToast = true)
        {
            string id = GetId(episodeResult, currentMovie);
            if (!renew && hasSubtitles.ContainsKey(id)) {
                if (showToast) {
                    App.ShowToast("Subtitles Already Downloaded");
                }
                return;
            }
            TempThread tempThred = mainCore.CreateThread(4);
            mainCore.StartThread("Subtitle Download", () => {
                try {
                    if (id.Replace(" ", "") == "") {
                        if (showToast) {
                            App.ShowToast("Id not found");
                        }
                        return;
                    }

                    string s = mainCore.DownloadSubtitle(id, showToast: false);
                    if (s == "") {
                        if (showToast) {
                            App.ShowToast("No Subtitles Found");
                        }
                        return;
                    }
                    else {
                        string extraPath = "/" + GetPathFromType(currentMovie.title.movieType);
                        if (!currentMovie.title.IsMovie) {
                            extraPath += "/" + CensorFilename(currentMovie.title.name);
                        }
                        App.DownloadFile(s, episodeResult.GetDownloadTitle(currentSeason, episodeResult.Episode) + ".srt", true, extraPath); // "/Subtitles" +
                        if (showToast) {
                            App.ShowToast("Subtitles Downloaded");
                        }
                        hasSubtitles.Add(id, true);
                    }
                }
                finally {
                    mainCore.JoinThred(tempThred);
                }
            });

        }

        void DeleteFile(string downloadKeyData, EpisodeResult episodeResult)
        {
            App.DeleteFile(downloadKeyData);
            App.DeleteFile(downloadKeyData.Replace(".mp4", ".srt"));
            App.UpdateDownload(GetCorrectId(episodeResult), 2);
            Download.RemoveDownloadCookie(GetCorrectId(episodeResult));//.DeleteFileFromFolder(downloadKeyData, "Download", GetId(episodeResult));
            SetColor(episodeResult);
            ForceUpdate(episodeResult.Id);
        }

        public int GetCorrectId(EpisodeResult episodeResult)
        {
            return int.Parse(((currentMovie.title.movieType == MovieType.TVSeries || currentMovie.title.movieType == MovieType.Anime) ? currentMovie.episodes[episodeResult.Id].id : currentMovie.title.id).Replace("tt", ""));
        }

        // ============================== ID OF EPISODE ==============================
        public string GetId(EpisodeResult episodeResult)
        {
            return GetId(episodeResult, currentMovie);
        }


        public static string GetId(EpisodeResult episodeResult, Movie currentMovie)
        {
            try {
                return (currentMovie.title.movieType == MovieType.TVSeries || currentMovie.title.movieType == MovieType.Anime) ? currentMovie.episodes[episodeResult.Id].id : currentMovie.title.id;
            }
            catch (Exception _ex) {
                print("FATAL EX IN GETID: " + _ex);
                return episodeResult.Id + "Extra=" + ToDown(episodeResult.Title) + "=EndAll";
            }
        }



        // ============================== TOGGLE HAS SEEN EPISODE ==============================

        bool toggleViewState = false;
        private void ViewToggle_Clicked(object sender, EventArgs e)
        {
            toggleViewState = !toggleViewState;
            ChangeViewToggle();
        }

        void ChangeViewToggle()
        {
            ViewToggle.Source = GetImageSource((toggleViewState ? "outline_visibility_off_white_48dp.png" : "outline_visibility_white_48dp.png"));// GetImageSource((toggleViewState ? "viewOffIcon.png" : "viewOnIcon.png"));
            ViewToggle.Transformations = new List<FFImageLoading.Work.ITransformation>() { (new FFImageLoading.Transformations.TintTransformation(toggleViewState ? DARK_BLUE_COLOR : LIGHT_LIGHT_BLACK_COLOR)) };
        }

        public void SetEpisode(EpisodeResult episodeResult)
        {
            string id = GetId(episodeResult);
            SetEpisode(id);
            SetColor(episodeResult);
            ForceUpdate(episodeResult.Id);
        }

        public static void SetEpisode(string id)
        {
            App.SetKey("ViewHistory", id, true);
        }

        void ToggleEpisode(EpisodeResult episodeResult)
        {
            string id = GetId(episodeResult);
            ToggleEpisode(id);
            SetColor(episodeResult);
            ForceUpdate(episodeResult.Id);
        }

        public static void ToggleEpisode(string id)
        {
            if (id != "") {
                if (App.KeyExists("ViewHistory", id)) {
                    App.RemoveKey("ViewHistory", id);
                }
                else {
                    SetEpisode(id);
                }
            }
        }



        // ============================== USED FOR SMALL VIDEO PLAY ==============================
        /*  private void Grid_LayoutChanged(object sender, EventArgs e)
          {
              var s = ((Grid)sender);
              Commands.SetTap(s, new Command((o) => {
                  var episodeResult = (EpisodeResult)s.BindingContext;
                  PlayEpisodeRes(episodeResult);
              }));
          }*/

        // ============================== SHOW SETTINGS OF VIDEO ==============================
        private void ViewCell_Tapped(object sender, EventArgs e)
        {
            EpisodeResult episodeResult = ((EpisodeResult)(((ViewCell)sender).BindingContext));

            if (toggleViewState) {
                ToggleEpisode(episodeResult);
                episodeView.SelectedItem = null;
            }
            else {
                EpisodeSettings(episodeResult);
            }
        }



        #region ===================================================== MOVE RECOMENDATION ECT BAR  =====================================================
        /// <summary>
        /// 0 = episodes, 1 = recommendations, 2 = trailers
        /// </summary>
        int showState = 0;
        int prevState = 0;
        void ChangedRecState(int state, bool overrideCheck = false)
        {
            prevState = int.Parse(showState.ToString());
            if (state == showState && !overrideCheck) return;
            showState = state;
            Device.BeginInvokeOnMainThread(() => {
                Grid.SetRow(EpPickers, (state == 0) ? 1 : 0);

                FadeEpisodes.Scale = (state == 0) ? 1 : 0;
                //episodeView.IsEnabled = state == 0;

                trailerStack.Scale = (state == 2) ? 1 : 0;
                trailerStack.IsEnabled = state == 2;
                trailerStack.IsVisible = state == 2;
                trailerStack.InputTransparent = state != 2;
                // trailerView.HeightRequest = state == 2 ? Math.Min(epView.CurrentTrailers.Count, 4) * 350 : 0;

                EpPickers.IsEnabled = state == 0;
                EpPickers.Scale = state == 0 ? 1 : 0;
                EpPickers.IsVisible = state == 0;

                Recommendations.Scale = state == 1 ? 1 : 0;
                Recommendations.IsVisible = state == 1;
                Recommendations.IsEnabled = state == 1;
                Recommendations.InputTransparent = state != 1;

                SetHeight(state != 0);
                //SetTrailerRec(state == 2);

                if (state == 1) {
                    SetRecs();
                }

            });

            System.Timers.Timer timer = new System.Timers.Timer(10);
            ProgressBar GetBar(int _state)
            {
                switch (_state) {
                    case 0:
                        return EPISODESBar;
                    case 1:
                        return RECOMMENDATIONSBar;
                    case 2:
                        return TRAILERSBar;
                    default:
                        return null;
                }
            }
            GetBar(prevState).ScaleXTo(0, 70, Easing.Linear);
            GetBar(state).ScaleXTo(1, 70, Easing.Linear);
            timer.Start();
        }

        private void Episodes_Clicked(object sender, EventArgs e)
        {
            ChangedRecState(0);
        }

        private void Recommendations_Clicked(object sender, EventArgs e)
        {
            ChangedRecState(1);
        }

        private void Trailers_Clicked(object sender, EventArgs e)
        {
            ChangedRecState(2);
        }

        #endregion


        private void episodeView_ItemAppearing(object sender, ItemVisibilityEventArgs e)
        {
            //  episodeView.Style = TabsStyle.

        }

        private void RecomendationLoaded_SizeChanged(object sender, EventArgs e)
        {

        }
    }
}

public class MovieResultMainEpisodeView
{
    public ObservableCollection<Trailer> CurrentTrailers { get; set; }

    public ObservableCollection<EpisodeResult> MyEpisodeResultCollection { set; get; }
    //public ObservableCollection<EpisodeResult> MyEpisodeResultCollection { set { Added?.Invoke(null, null); _MyEpisodeResultCollection = value; } get { return _MyEpisodeResultCollection; } }

    public const int MAX_EPS_PER = 50;
    public EpisodeResult[] AllEpisodes;

    // public event EventHandler Added;

    public MovieResultMainEpisodeView()
    {
        MyEpisodeResultCollection = new ObservableCollection<EpisodeResult>();
        CurrentTrailers = new ObservableCollection<Trailer>();
    }
}





