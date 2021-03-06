﻿using CloudStreamForms.Core;
using CloudStreamForms.InterfacePages;
using CloudStreamForms.Models;
using CloudStreamForms.Script;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamEffects;
using static CloudStreamForms.App;
using static CloudStreamForms.Core.CloudStreamCore;
using static CloudStreamForms.Core.CoreHelpers;
using static CloudStreamForms.InterfacePages.MovieResultHolder;
using static CloudStreamForms.MainPage;
using static CloudStreamForms.Settings;

namespace CloudStreamForms
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class MovieResult : ContentPage
	{
		const uint FATE_TIME_MS = 500;
		bool IsDead { get { return Core == null; } }

		readonly MovieResultHolder controller = new MovieResultHolder();
		CloudStreamCore Core { get { return controller.core; } }

		public MovieResultMainEpisodeView epView;

		readonly LabelList SeasonPicker;
		readonly LabelList DubPicker;
		readonly LabelList FromToPicker;

		public Poster mainPoster;
		public string trailerUrl = "";
		List<Poster> RecomendedPosters { get { return CurrentMovie.title.recomended; } }  //= new List<Poster>();

		int CurrentSeason { get { return controller.currentSeason; } }

		bool isLoaded = false; // BY DEAFULT IT IS TV SERIES
		bool IsMovie { get { return isLoaded && CurrentMovie.title.IsMovie; } }
		Movie CurrentMovie { get { return Core.activeMovie; } }
		bool IsDub { get { return controller.isDub; } }

		string CurrentMalLink {
			get {
				return controller.CurrentMalLink;
			}
		}

		string CurrentAniListLink {
			get {
				return controller.CurrentAniListLink;
			}
		}

		protected override bool OnBackButtonPressed()
		{
			if (ActionPopup.isOpen) return true;

			Search.mainPoster = new Poster();
			if (setKey) {
				App.RemoveKey(App.BOOKMARK_DATA, CurrentMovie.title.id);
			}
			Dispose();
			return base.OnBackButtonPressed();
		}

		bool setKey = false;
		void SetKey()
		{
			App.SetKey(App.BOOKMARK_DATA, CurrentMovie.title.id, "Name=" + CurrentMovie.title.name + "|||PosterUrl=" + CurrentMovie.title.hdPosterUrl + "|||Id=" + CurrentMovie.title.id + "|||TypeId=" + ((int)CurrentMovie.title.movieType) + "|||ShortEpView=" + CurrentMovie.title.shortEpView + "|||=EndAll");
			setKey = false;
		}

		private void StarBttClicked(object sender, EventArgs e)
		{
			try {
				Home.UpdateIsRequired = true;

				bool keyExists = App.KeyExists(App.BOOKMARK_DATA, CurrentMovie.title.id);
				if (keyExists) {
					App.RemoveKey(App.BOOKMARK_DATA, CurrentMovie.title.id);
				}
				else {
					if (CurrentMovie.title.name == null) {
						App.SetKey(App.BOOKMARK_DATA, CurrentMovie.title.id, "Name=" + CurrentMovie.title.name + "|||Id=" + CurrentMovie.title.id + "|||");

						setKey = true;
					}
					else {
						SetKey();
					}
				}
				ChangeStar(!keyExists);
			}
			catch (Exception _ex) {
				App.ShowToast("Error bookmarking: " + _ex);
			}
		}

		private void SubtitleBttClicked(object sender, EventArgs e)
		{
			Settings.SubtitlesEnabled = !Settings.SubtitlesEnabled;
			ChangeSubtitle(SubtitlesEnabled);
		}

		private void ShareBttClicked(object sender, EventArgs e)
		{
			if (CurrentMovie.title.id != "" && CurrentMovie.title.name != "") {
				Share();
			}
		}

		async void Share()
		{
			List<string> actions = new List<string>() { "Everything", "CloudStream Link", "IMDb Link", "Title", "Title and Description", "Create shortcut" };
			if (CurrentMalLink != "") {
				actions.Insert(3, "MAL Link");
			}
			if (CurrentAniListLink.IsClean()) {
				actions.Insert(4, "AniList Link");
			}
			if (trailerUrl != "") {
				actions.Insert(actions.Count - 2, "Trailer Link");
			}
			string a = await ActionPopup.DisplayActionSheet("Share", actions.ToArray());//await DisplayActionSheet("Copy", "Cancel", null, actions.ToArray());
			if (a == "Create shortcut") {
				App.AddShortcut(CurrentMovie.title.name, CurrentMovie.title.id, CurrentMovie.title.hdPosterUrl);
				return;
			}
			string copyTxt = "";

			async Task<string> GetMovieCode()
			{
				await ActionPopup.StartIndeterminateLoadinbar("Loading...");
				bool done = false;
				string _s = "";
				Thread t = new Thread(() => {
					try {
						_s = CloudStreamCore.ShareMovieCode(CurrentMovie.title.id + "Name=" + CurrentMovie.title.name + "=EndAll");
					}
					catch (System.Exception) {
					}
					finally {
						done = true;
					}
				});
				t.Start();
				while (!done) {
					await Task.Delay(10);
				}
				await ActionPopup.StopIndeterminateLoadinbar();
				return _s;
			}

			if (a == "CloudStream Link") {
				string _s = await GetMovieCode();
				if (_s != "") {
					copyTxt = _s;
				}
			}
			else if (a == "IMDb Link") {
				copyTxt = "https://www.imdb.com/title/" + CurrentMovie.title.id;
			}
			else if (a == "Title") {
				copyTxt = CurrentMovie.title.name;
			}
			else if (a == "MAL Link") {
				copyTxt = CurrentMalLink;
			}
			else if (a == "Title and Description") {
				copyTxt = CurrentMovie.title.name + "\n" + CurrentMovie.title.description;
			}
			else if (a == "Trailer Link") {
				copyTxt = trailerUrl;
			}
			else if (a == "AniList Link") {
				copyTxt = CurrentAniListLink;
			}
			else if (a == "Everything") {
				copyTxt = CurrentMovie.title.name + " | " + RatingLabel.Text + "\n" + CurrentMovie.title.description;
				string _s = await GetMovieCode();

				if (_s != "") {
					copyTxt = copyTxt + "\nCloudStream: " + _s;
				}
				copyTxt = copyTxt + "\nIMDb: " + "https://www.imdb.com/title/" + CurrentMovie.title.id;
				if (CurrentMalLink != "") {
					copyTxt += "\nMAL: " + CurrentMalLink;
				}
				if (CurrentAniListLink.IsClean()) {
					copyTxt += "\nAniList: " + CurrentAniListLink;
				}
				if (trailerUrl != "") {
					copyTxt += "\nTrailer: " + trailerUrl;
				}
			}
			if (a != "Cancel" && copyTxt != "") {
				await Clipboard.SetTextAsync(copyTxt);
				App.ShowToast("Copied " + a + " to Clipboard");
			}

		}

		void ChangeStar(bool? overrideBool = null, string key = null)
		{
			if (Core == null) return;
			try {
				bool keyExists = false;
				if (key == null) {
					key = CurrentMovie.title.id;
				}
				if (overrideBool == null) {
					keyExists = App.KeyExists(App.BOOKMARK_DATA, key);
					print("KEYEXISTS:" + keyExists + "|" + CurrentMovie.title.id);
				}
				else {
					keyExists = (bool)overrideBool;
				}
				Device.BeginInvokeOnMainThread(() => {
					try {
						StarBtt.Source = keyExists ? "bookmark.svg" : "bookmark_off.svg";//GetImageSource(());
						StarBtt.Transformations = new List<FFImageLoading.Work.ITransformation>() { (new FFImageLoading.Transformations.TintTransformation(keyExists ? DARK_BLUE_COLOR : LIGHT_LIGHT_BLACK_COLOR)) };
					}
					catch (Exception) {
					}
				});
			}
			catch (Exception _ex) {
				App.ShowToast("Error bookmarking: " + _ex);
			}
		}

		void ChangeSubtitle(bool? overrideBool = null)
		{
			if (Core == null) return;

			bool res = false;
			if (overrideBool == null) {

				res = SubtitlesEnabled;
			}
			else {
				res = (bool)overrideBool;
				//SubtitlesEnabled = res;
			}

			Device.BeginInvokeOnMainThread(() => {
				SubtitleBtt.Source = res ? "subtitles.svg" : "subtitles_off.svg";//App.GetImageSource();
				SubtitleBtt.Transformations = new List<FFImageLoading.Work.ITransformation>() { (new FFImageLoading.Transformations.TintTransformation(res ? DARK_BLUE_COLOR : LIGHT_LIGHT_BLACK_COLOR)) };
			});
		}

		public void SetChromeCast(bool enabled)
		{
			if (Core == null) return;

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
		int lastPlayedEp = -1;
		protected override void OnAppearing()
		{
			base.OnAppearing();

			if (Settings.CacheNextEpisode && lastPlayedEp != -1) {
				CacheEpisode(lastPlayedEp);
			}

			ForceUpdate(checkColor: true);
			OnSomeDownloadFinished += OnHandleDownload;
			OnSomeDownloadFailed += OnHandleDownload;
			App.OnDStateChanged += OnForceUpdateDload;


			if (!hasAppeared) {
				hasAppeared = true;
				ForceUpdateVideo += ForceUpdateAppearing;
			}
			SetChromeCast(MainChrome.IsChromeDevicesOnNetwork);
		}

		public void OnHandleDownload(object sender, EventArgs arg)
		{
			ForceUpdate(checkColor: true);
		}

		protected override void OnDisappearing()
		{
			OnSomeDownloadFinished -= OnHandleDownload;
			OnSomeDownloadFailed -= OnHandleDownload;
			App.OnDStateChanged -= OnForceUpdateDload;

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

		public static void OpenChrome()
		{
			if (!ChromeCastPage.isActive) {

				Page p = ChromeCastPage.CreateChromePage(chromeResult, chromeMovieResult);// new (chromeResult, chromeMovieResult); //{ episodeResult = chromeResult, chromeMovieResult = chromeMovieResult };
				MainPage.mainPage.Navigation.PushModalAsync(p, false);
			}
		}

		private void OpenChromecastView(object sender, EventArgs e)
		{
			if (Core == null) return;

			if (sender != null) {
				ChromeCastPage.isActive = false;
			}
			if (!ChromeCastPage.isActive) {
				OpenChrome();
			}
		}

		public static EpisodeResult chromeResult;
		public static Movie chromeMovieResult;
		async void WaitChangeChromeCast()
		{
			if (Core == null) return;

			if (MainChrome.IsCastingVideo) {
				Device.BeginInvokeOnMainThread(() => {
					OpenChromecastView(1, EventArgs.Empty);
				});
			}
			else {
				List<string> names = MainChrome.GetChromeDevicesNames();
				if (MainChrome.IsConnectedToChromeDevice) { names.Add("Disconnect"); }
				string a = await ActionPopup.DisplayActionSheet("Cast to", names.ToArray());
				if (a != "Cancel") {
					MainChrome.ConnectToChromeDevice(a);
				}
			}
		}

		public static ImageSource GetGradient()
		{
			return GetImageSource("gradient" + Settings.BlackColor + ".png");
		}

		public MovieResult(Movie? _movie = null)
		{
			InitializeComponent();

			SeasonPicker = new LabelList(SeasonBtt, new List<string>());
			DubPicker = new LabelList(DubBtt, new List<string>());
			FromToPicker = new LabelList(FromToBtt, new List<string>());

			mainPoster = Search.mainPoster;

			controller.OnDateAdded += (object o, ReleaseDateEvent e) => {
				if (IsDead) return;
				Device.InvokeOnMainThreadAsync(() => {
					NextEpisodeInfoBtt.Opacity = 0;
					nextEpTimeShouldBeDisplayed = true;
					if (showState == 0) {
						NextEpisodeInfoBtt.IsVisible = true;
						NextEpisodeInfoBtt.IsEnabled = true;
					}

					NextEpisodeInfoBtt.FadeTo(1, easing: Easing.SinIn);
					NextEpisodeInfoBtt.Text = e.FormatedDate;
				});
			};

			controller.OnExposedEpisodesChanged += (object o, EpisodeData[] e) => {
				if (IsDead) return;
#if DEBUG
				if (CurrentMovie.title.movieType == MovieType.Anime && Settings.IsDatabasePublisher) {
					CSDatabaseApi.PostSeason(CurrentMovie, CurrentSeason);
				}
#endif
				Device.InvokeOnMainThreadAsync(async () => {
					episodeView.AbortAnimation("FadeTo");
					episodeView.Opacity = 0;
					if (IsDead) return;
					epView.MyEpisodeResultCollection.Clear();
					for (int i = 0; i < e.Length; i++) {
						var c = e[i];
						var _ep = ChangeEpisode(new EpisodeResult() { Episode = c.Episode, Season = c.Season, Description = c.Description, Rating = c.Rating, PosterUrl = c.PosterUrl, IMDBEpisodeId = c.IMDBEpisodeId, Id = c.Id, Title = c.Title });
						epView.MyEpisodeResultCollection.Add(_ep);
					}
					SetHeight();
					await Task.Delay(100);
					await episodeView.FadeTo(1, FATE_TIME_MS);
					if (Settings.HasMalAccountLogin || Settings.HasAniListLogin) {
						await UpdateAllMalValues();
						if (maxAnimeEpisode == 0) {
							maxAnimeEpisode = e.Length;
						}
						UpdateMalVisual();
					}
				});
			};

			/*
			controller.malDataLoaded += (o, e) => {
				if (HasMalAccountLogin) {
					UpdateAllMalValues();
				}
			};*/

			controller.TitleLoaded += (o, e) => {
				try {
					isLoaded = true;
					if (IsDead) return;
#if DEBUG
					if (CurrentMovie.title.movieType == MovieType.Anime && Settings.IsDatabasePublisher) {
						CSDatabaseApi.PostTitle(e.title);
					}
#endif
					if (setKey) {
						SetKey();
					}

					DubPicker.SelectedIndexChanged += (o, e) => {
						controller.DubPickerSelectIndex("Dub" == DubPicker.ItemsSource[DubPicker.SelectedIndex]);
					};
					SeasonPicker.SelectedIndexChanged += (o, e) => {
						controller.SeasonPickerSelectIndex(e);
						episodeView.FadeTo(0);
					};
					FromToPicker.SelectedIndexChanged += (object o, int e) => {
						controller.SelFromToPickerSelectIndex(e);
					};


					Device.BeginInvokeOnMainThread(() => {
						if (IsDead) return;
						_ = ReviewLabel.FadeTo(1, FATE_TIME_MS);

						EPISODES.Text = IsMovie ? "MOVIE" : "EPISODES";
						if (IsMovie) { // FORCE UPDATE WHEN INITAL LOAD MOVIE
							EpPickers.IsVisible = false;
							EpPickers.IsEnabled = false;
						}

						//	epView.MyGenres.Clear();
						var genr = CurrentMovie.title.genres;
						if (genr != null && genr.Count > 0) {
							string _txt = "";
							foreach (var name in genr.GetRange(0, Math.Min(genr.Count, 5))) {
								_txt += name + "  ";
								//GenresGrid.Children.AddHorizontal(new Button() { Text = name, BackgroundColor = Color.Transparent, HeightRequest = 20, BorderColor = new Color(1, 1, 1, 0.4), TextColor = Color.White, CornerRadius = 4, BorderWidth = 1 });
								//epView.MyGenres.Add(new MovieResultMainEpisodeView.Genre() { Name = name });
							}
							GenreLabel.Text = _txt;
						}

						Recommendations.Children.Clear();
						for (int i = 0; i < RecomendedPosters.Count; i++) {
							Poster p = e.title.recomended[i];
							string posterURL = ConvertIMDbImagesToHD(p.posterUrl, 76, 113, 1.75); //.Replace(",76,113_AL", "," + pwidth + "," + pheight + "_AL").Replace("UY113", "UY" + pheight).Replace("UX76", "UX" + pwidth);
							if (CheckIfURLIsValid(posterURL)) {
								Grid stackLayout = new Grid() { VerticalOptions = LayoutOptions.Start };
								// Button imageButton = new Button() { HeightRequest = RecPosterHeight, WidthRequest = RecPosterWith, BackgroundColor = Color.Transparent, VerticalOptions = LayoutOptions.Start,CornerRadius=10 };
								var ff = new FFImageLoading.Forms.CachedImage {
									Source = posterURL,
									HeightRequest = RecPosterHeight,
									WidthRequest = RecPosterWith,
									BackgroundColor = Color.Transparent,
									VerticalOptions = LayoutOptions.Start,
									Transformations = {
                            //  new FFImageLoading.Transformations.RoundedTransformation(10,1,1.5,10,"#303F9F")
									new FFImageLoading.Transformations.RoundedTransformation(3, 1, 1.5, 0, "#303F9F")
								},
									InputTransparent = true,
								};

								// ================================================================ RECOMMENDATIONS CLICKED ================================================================
								stackLayout.SetValue(XamEffects.TouchEffect.ColorProperty, Color.White);
								Commands.SetTap(stackLayout, new Command((o) => {
									int z = (int)o;
									if (Search.mainPoster.url != RecomendedPosters[z].url) {
										/*if (lastMovie == null) {
											lastMovie = new List<Movie>();
										}
										lastMovie.Add(core.activeMovie);*/
										Search.mainPoster = RecomendedPosters[z];
										Page _p = new MovieResult();// { mainPoster = mainPoster };
										Navigation.PushModalAsync(_p);
									}
									//do something
								}));
								Commands.SetTapParameter(stackLayout, i);

								stackLayout.Children.Add(ff);
								//stackLayout.Children.Add(imageButton);

								Recommendations.Children.Add(stackLayout);
							}
						}

						SetRecs();
					});

				}
				catch (Exception _ex) {
					error(_ex);
				}
			};

			controller.OnBackgroundChanged += (o, e) => {
				if (IsDead) return;
				Device.BeginInvokeOnMainThread(() => {
					TrailerBtt.Opacity = 0;
					TrailerBtt.FadeTo(1);
					if (e.posterUrl == null) {
						TrailerBtt.Source = ImageSource.FromResource("CloudStreamForms.Resource.gradient.png", Assembly.GetExecutingAssembly());
					}
					else {
						TrailerBtt.Source = ConvertIMDbImagesToHD(e.posterUrl, 356, 200);
					}
				});
			};

			controller.OnRunningChanged += (object o, bool e) => {
				if (IsDead) return;
				Device.BeginInvokeOnMainThread(() => {
					RecomendationLoaded.IsVisible = e;
					RecomendationLoaded.IsEnabled = e;
				});
			};

			Commands.SetTap(AnalyticsBtt, new Command(async () => {
				if (controller.pData != null) {
					var p = controller.pData.Where(t => t.maxEpisode > 0);
					if (p.Count() > 0) {
						var list = await ActionPopup.DisplaySwitchList(p.Select(t => $"{t.provider} • {t.maxEpisode}").ToList(), p.Select(t => Settings.IsProviderActive(t.provider)).ToList(), $"{p.Count()} Providers found");
						var pNames = p.Select(t => t.provider).ToArray();
						for (int i = 0; i < list.Count; i++) {
							App.SetKey("ProviderActive", pNames[i], list[i]);
						}
					}
				}
			}));

			controller.OnPDataChanged += (object o, AnimeProviderEpisodes[] data) => {
				if (controller.pData.Where(t => t.maxEpisode > 0).Count() > 0) { // IF ANY EPISODES
					Device.BeginInvokeOnMainThread(() => {
						if (AnalyticsBtt.IsVisible) return;
						AnalyticsBtt.IsVisible = true;
						AnalyticsBtt.IsEnabled = true;
						AnalyticsImg.IsVisible = true;
						AnalyticsImg.IsEnabled = true;
						AnalyticsBg.IsEnabled = true;
						AnalyticsBg.IsVisible = true;
						AnalyticsImg.FadeTo(1, FATE_TIME_MS);
						AnalyticsBtt.FadeTo(0.4, FATE_TIME_MS);
						AnalyticsBg.FadeTo(1, FATE_TIME_MS);
					});
				}
			};

			controller.OnTextChanged += (object o, LabelInfo e) => {
				if (IsDead) return;

				Label label = e.label switch {
					LabelType.NameLabel => NameLabel,
					LabelType.YearLabel => RatingLabel,
					LabelType.RatingLabel => RatingLabelRating,
					LabelType.DescriptionLabel => DescriptionLabel,
					_ => null,
				};
				if (label == null) return;
				Device.BeginInvokeOnMainThread(() => {
					label.IsVisible = e.isVisible;
					label.Text = e.text;
					if (label.Opacity == 0 && e.isVisible) {
						label.FadeTo(1, FATE_TIME_MS);
					}
					else if (label.Opacity == 1 && !e.isVisible) {
						label.FadeTo(0, FATE_TIME_MS);
					}
				});
			};

			controller.OnPickerChanged += (object o, PickerInfo e) => {
				if (IsDead) return;

				LabelList labelList = e.picker switch {
					PickerType.SeasonPicker => SeasonPicker,
					PickerType.DubPicker => DubPicker,
					PickerType.EpisodeFromToPicker => FromToPicker,
					_ => null,
				};
				if (labelList == null) return;
				labelList.ItemsSource = e.source;
				Device.InvokeOnMainThreadAsync(async () => {
					try {
						var btt = labelList.button;
						//if (labelList.SelectedIndex == -1) {
						labelList.SetIndexWithoutChange(e.index);
						//}
						if (e.picker != PickerType.EpisodeFromToPicker) {
							btt.IsVisible = true;
						}
						btt.Text = e.Text;
						btt.IsEnabled = e.isVisible;
						if (btt.Opacity == 0 && e.isVisible) {
							await btt.FadeTo(1, FATE_TIME_MS);
						}
						else if (btt.Opacity == 1 && !e.isVisible) {
							await btt.FadeTo(0, FATE_TIME_MS);
						}
						if (e.picker == PickerType.EpisodeFromToPicker) {
							btt.IsVisible = e.isVisible;
						}
					}
					catch (Exception _ex) {
						error(_ex);
					}
				});
			};

			controller.OnBttChanged += (object o, ButtonInfo e) => {
				if (IsDead) return;

				Button btt = e.button switch {
					ButtonType.BatchDownloadPicker => BatchDownloadBtt,
					ButtonType.SkipAnimeBtt => SkipAnimeBtt,
					_ => null,
				};

				if (btt == null) return;
				Device.BeginInvokeOnMainThread(() => {
					btt.IsEnabled = e.isVisible;
					btt.IsVisible = e.isVisible;
					if (btt.Opacity == 0 && e.isVisible) {
						btt.FadeTo(1, FATE_TIME_MS);
					}
					else if (btt.Opacity == 1 && !e.isVisible) {
						btt.FadeTo(0, FATE_TIME_MS);
					}
					btt.Text = e.text;
				});
			};

			/*
			Thread cThread = new Thread(() =>
			{*/
			if (_movie.HasValue) {
				controller.Init(_movie.Value);
			}
			else {
				controller.Init(mainPoster.url, mainPoster.name, mainPoster.year);
			}
			/*})
			{ Name = "Create Controller Thread"};
			cThread.Start();*/

			Gradient.Source = GetGradient();
			Gradient.HeightRequest = 200;

			ReviewLabel.Clicked += (o, e) => {
				if (!ReviewPage.isOpen) {
					Page _p = new ReviewPage(CurrentMovie.title.id, mainPoster.name);
					MainPage.mainPage.Navigation.PushModalAsync(_p);
				}
			};

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
			/*
			Recommendations.SizeChanged += (o, e) => {
				SetRecs();
			};*/

			ChangeViewToggle();
			ChangeSubtitle();

			//NameLabel.Text = activeMovie.title.name;
			NameLabel.Text = mainPoster.name;
			RatingLabel.Text = mainPoster.year;

			Core.TrailerLoaded += MovieResult_trailerLoaded;

			// TrailerBtt.Clicked += TrailerBtt_Clicked;
			TrailerBtt.Clicked += TrailerBtt_Clicked;

			BatchDownloadBtt.Clicked += async (o, e) => {
				var episodes = epView.MyEpisodeResultCollection.Select(t => (EpisodeResult)t.Clone()).ToArray();
				CloudStreamCore coreCopy = (CloudStreamCore)Core.Clone();
				int _currentSeason = CurrentSeason;
				bool _isDub = IsDub;

				int max = episodes.Length;
				List<string> res = await ActionPopup.DisplayLogin("Download", "Cancel", "Download Episodes", new LoginPopupPage.PopupFeildsDatas() { placeholder = "First episode (1)", res = InputPopupPage.InputPopupResult.integrerNumber }, new LoginPopupPage.PopupFeildsDatas() { placeholder = $"Last episode ({max})", res = InputPopupPage.InputPopupResult.integrerNumber });
				if (res.Count == 2) {
					try {
						int firstEp = res[0].IsClean() ? int.Parse(res[0]) : 1;
						int lastEp = res[1].IsClean() ? int.Parse(res[1]) : max;
						if (lastEp <= 0 || firstEp <= 0) {
							return;
						}
						lastEp = Math.Clamp(lastEp, 1, max);
						firstEp = Math.Clamp(firstEp, 1, lastEp);

						App.ShowToast($"Downloading Episodes {firstEp}-{lastEp}");
						for (int i = firstEp - 1; i < lastEp; i++) {
							var ep = episodes[i];
							string imdbId = ep.IMDBEpisodeId;
							if (SubtitlesEnabled) {
								DownloadSubtitlesToFileLocation(ep, CurrentMovie, CurrentSeason, showToast: false);
							}

							CloudStreamCore.Title titleName = (Title)coreCopy.activeMovie.title.Clone();
							if (IsDead) return;
							coreCopy.GetEpisodeLink(coreCopy.activeMovie.title.IsMovie ? -1 : (ep.Id + 1), _currentSeason, false, false, _isDub);

							int epId = ep.InternalId;
							await Task.Delay(10000); // WAIT 10 Sec
							try {
								BasicLink[] info = null;
								bool hasMirrors = false;
								var baseLinks = CloudStreamCore.GetCachedLink(imdbId);
								if (baseLinks.HasValue) {
									info = baseLinks.Value.links.Where(t => t.CanBeDownloaded).ToList().OrderHDLinks().ToArray();
									hasMirrors = info.Length > 0;
								}

								if (hasMirrors && info != null) {
									App.UpdateDownload(epId, -1);
									string dpath = App.RequestDownload(epId, ep.OgTitle, ep.Description, ep.Episode, CurrentSeason, info.Select(t => { return new BasicMirrorInfo() { mirror = t.baseUrl, name = t.PublicName, referer = t.referer }; }).ToList(), ep.GetDownloadTitle(_currentSeason, ep.Episode) + ".mp4", ep.PosterUrl, titleName, ep.IMDBEpisodeId);
								}
								else {
									App.ShowToast("Download Failed, No Mirrors Found");
								}
							}
							catch (Exception _ex) {
								print("EX DLOAD::: DOWNLOAD:::: " + _ex);
							}
						}
					}
					catch {
						App.ShowToast("Error batch downloading episodes");
					}
				}
			};
			//  core.linkAdded += MovieResult_linkAdded; 

			Core.MoeDone += MovieResult_moeDone;

			BackgroundColor = Settings.BlackRBGColor;
			BgColorSet.BackgroundColor = Settings.BlackRBGColor;

			DubBtt.IsVisible = false;
			SeasonBtt.IsVisible = false;

			epView = new MovieResultMainEpisodeView();
			SetHeight();

			BindingContext = epView;
			episodeView.VerticalScrollBarVisibility = Settings.ScrollBarVisibility;
			//  RecStack.HorizontalScrollBarVisibility = Settings.ScrollBarVisibility; // REPLACE REC

			Commands.SetTap(BackAllBtt, new Command(() => {
				Navigation.PopModalAsync(true);
				Search.mainPoster = new Poster();
				Dispose();
			}));
			//BackAllBtt.Source = GetImageSource("baseline_arrow_back_white_48dp.png");

			Commands.SetTap(ReloadAllBtt, new Command(() => {
				App.RemoveKey("CacheImdb", CurrentMovie.title.id);
				App.RemoveKey("CacheMAL", CurrentMovie.title.id);
				Search.mainPoster = new Poster();
				Navigation.PopModalAsync(false);
				PushPageFromUrlAndName(CurrentMovie.title.id, mainPoster.name);
				Dispose();
			}));
			//ReloadAllBtt.Source = GetImageSource("baseline_refresh_white_48dp.png");

			//core.GetImdbTitle(mainPoster);
			ChangeStar();

			ChangedRecState(0, true);


			/*
            Commands.SetTap(NotificationBtt, new Command(() => {
                ToggleNotify();
            }));
            */

			SkipAnimeBtt.Clicked += (o, e) => {
				controller.SkipAnimeLoading();
			};
		}

		// NOTIFICATIONS
		/*
        void CancelNotifications()
        {
            if (core == null) return;

            var keys = App.GetKey<List<int>>("NotificationsIds", currentMovie.title.id, new List<int>());
            for (int i = 0; i < keys.Count; i++) {
                App.CancelNotifaction(keys[i]);
            }
        }

        void AddNotifications()
        {
            if (core == null) return;

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
            if (core == null) return;

            bool hasNot = App.GetKey<bool>("Notifications", currentMovie.title.id, false);
            App.SetKey("Notifications", currentMovie.title.id, !hasNot);
            UpdateNotification(!hasNot);

            if (!hasNot) {
                AddNotifications();
            }
            else {
                CancelNotifications();
            }
        }*/
		/*
        void UpdateNotification(bool? overrideNot = null)
        {
            if (core == null) return;
            if (!FETCH_NOTIFICATION) return;
            
            bool hasNot = overrideNot ?? App.GetKey<bool>("Notifications", currentMovie.title.id, false);
            NotificationImg.Source = hasNot ? "notifications_active.svg" : "notifications.svg"; //App.GetImageSource(hasNot ? "baseline_notifications_active_white_48dp.png" : "baseline_notifications_none_white_48dp.png");
            NotificationImg.Transformations = new List<FFImageLoading.Work.ITransformation>() { (new FFImageLoading.Transformations.TintTransformation(hasNot ? DARK_BLUE_COLOR : LIGHT_LIGHT_BLACK_COLOR)) };
            NotificationTime.TextColor = hasNot ? Color.FromHex(DARK_BLUE_COLOR) : Color.Gray;
    }*/

		// List<MoeEpisode> setNotificationsTimes = new List<MoeEpisode>();

		private void MovieResult_moeDone(object sender, List<MoeEpisode> e)
		{
			/*
            if (core == null) return;
            if (!FETCH_NOTIFICATION) return;
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
            });*/
		}

		public void SetColor(EpisodeResult episodeResult)
		{
			if (Core == null) return;

			string id = episodeResult.IMDBEpisodeId;
			if (id != "") {
				List<string> hexColors = new List<string>() { "#ffffff", LIGHT_BLUE_COLOR, "#e5e598" };
				List<string> darkHexColors = new List<string>() { "#909090", DARK_BLUE_COLOR, "#d3c450" };
				int color = 0;
				if (App.KeyExists(App.VIEW_HISTORY, id)) {
					color = 1;
				}

				DownloadState state = App.GetDstate(episodeResult.InternalId);
				switch (state) {
					case DownloadState.Downloading:
						episodeResult.DownloadState = 2;
						break;
					case DownloadState.Downloaded:
						episodeResult.DownloadState = 1;
						break;
					case DownloadState.NotDownloaded:
						episodeResult.DownloadState = 0;
						break;
					case DownloadState.Paused:
						episodeResult.DownloadState = 3;
						break;
					default:
						break;
				}

				episodeResult.MainTextColor = hexColors[color];
				episodeResult.MainDarkTextColor = darkHexColors[color];
			}
		}


		void OnForceUpdateDload(object sender, DownloadProgressInfo info)
		{
			if (sender is int internalId) {
				for (int i = 0; i < epView.MyEpisodeResultCollection.Count; i++) {
					if (epView.MyEpisodeResultCollection[i].InternalId == internalId) {
						print("PPPP:::: " + info.ProcentageDownloaded);
						epView.MyEpisodeResultCollection[i].ExtraProgress = info.ProcentageDownloaded / 100.0;
						Device.BeginInvokeOnMainThread(() => {
							epView.MyEpisodeResultCollection[i] = (EpisodeResult)epView.MyEpisodeResultCollection[i].Clone();
						});
						break;
					}
				}
			}
		}

		EpisodeResult UpdateLoad(EpisodeResult episodeResult, bool checkColor = false)
		{
			if (Core == null) return null;
			long pos;
			long len;
			if (checkColor) {
				SetColor(episodeResult);
			}
			//print("POST PRO ON: " + episodeResult.IMDBEpisodeId);
			string realId = episodeResult.IMDBEpisodeId;
			var _info = App.GetDownloadInfo(episodeResult.InternalId, false);

			//episodeResult.ExtraProgress = _info == null || _info.state == null ? 0 : _info.state.ProcentageDownloaded;
			//print("ID::::::: ON " + realId + "|" + App.GetKey(VIEW_TIME_POS, realId, -1L));
			if ((pos = App.GetViewPos(realId)) > 0) {
				if ((len = App.GetViewDur(realId)) > 0) {
					episodeResult.Progress = (double)pos / (double)len;
					episodeResult.ProgressState = pos;
					//print("MAIN DRI:: " + pos + "|" + len + "|" + episodeResult.Progress);
				}//tt8993804 // tt0772328
			}
			return episodeResult;
		}

		static bool canTapEpisode = true;
		EpisodeResult ChangeEpisode(EpisodeResult episodeResult)
		{
			episodeResult.OgTitle = episodeResult.Title;
			episodeResult.ExtraColor = Settings.ItemBackGroundColor.ToHex();
			episodeResult.Season = CurrentSeason;

			if (IsDead) return episodeResult;
			if (!IsMovie) {
				episodeResult.Title = episodeResult.Episode + ". " + episodeResult.Title;
			}

			if (episodeResult.PosterUrl == "") {
				if (Core.activeMovie.title.posterUrl != "") {
					string posterUrl = "";
					try {
						if (Core.activeMovie.title.trailers.Count > 0) {
							if (Core.activeMovie.title.trailers[0].PosterUrl != null) {
								posterUrl = Core.activeMovie.title.trailers[0].PosterUrl;
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

			episodeResult.InternalId = GetInternalId(episodeResult.IMDBEpisodeId);
			var dstate = App.GetDownloadInfo(episodeResult.InternalId);
			if (dstate != null && dstate.state != null) {
				episodeResult.ExtraProgress = dstate.state.ProcentageDownloaded / 100.0;
			}
			SetColor(episodeResult);

			episodeResult.TapComThree = new Command(async (s) => {
				if (!canTapEpisode) return;
				if (Core == null) return;

				int _id = GetRealIdFromId();
				if (_id == -1) return;
				canTapEpisode = false;
				try {
					var epRes = epView.MyEpisodeResultCollection[_id];
					if (toggleViewState) {
						ToggleEpisode(epRes);
						episodeView.SelectedItem = null;
					}
					else {
						await EpisodeSettings(epRes);
					}
				}
				finally {
					canTapEpisode = true;
				}
			});

			episodeResult.TapComTwo = new Command(async (s) => {
				if (!canTapEpisode) return;
				if (Core == null) return;

				int _id = GetRealIdFromId();
				if (_id == -1) return;
				canTapEpisode = false;
				try {
					var epRes = epView.MyEpisodeResultCollection[_id];
					if (epRes.DownloadState == 1) {
						PlayDownloadedEp(epRes);
					}
					else {
						await LoadLinksForEpisode(epRes);
					}
				}
				finally {
					canTapEpisode = true;
				}
			});

			episodeResult.TapCom = new Command(async (s) => {
				if (Core == null) return;

				int _id = GetRealIdFromId();
				if (_id == -1) return;

				var epRes = epView.MyEpisodeResultCollection[_id];
				if (epRes.DownloadState == 4) return;

				void DeleteData()
				{
					string downloadKeyData = App.GetDownloadInfo(epRes.InternalId, false).info.fileUrl;//.GetKey("Download", GetId(episodeResult), "");
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
				if (epRes.ExtraProgress > 0) {
					if (Core == null) return;
					//App.onExtendedButtonPressed?.Invoke(null, epRes.InternalId);
					//App.PlatformDep.ShowMenu((View)ob, "Play", "Delete File");
					string action = await ActionPopup.DisplayActionSheet(epRes.OgTitle, "Play", "Delete File"); //await DisplayActionSheet(epRes.OgTitle, "Cancel", null, "Play", "Delete File");
					if (Core == null) return;
					if (action == "Delete File") {
						DeleteData();
					}
					else if (action == "Play") {
						PlayDownloadedEp(epRes);
					}
				}
				else { // DOWNLOAD
					if (Core == null) return;
					epView.MyEpisodeResultCollection[_id].DownloadState = 4; // SET IS SEARCHING
					ForceUpdate(_id);
					currentDownloadSearchesHappening++;
					CloudStreamCore coreCopy = (CloudStreamCore)Core.Clone();
					string imdbId = episodeResult.IMDBEpisodeId;
					CloudStreamCore.Title titleName = (Title)CurrentMovie.title.Clone();

					coreCopy.GetEpisodeLink(IsMovie ? -1 : (episodeResult.Id + 1), CurrentSeason, isDub: IsDub, purgeCurrentLinkThread: currentDownloadSearchesHappening > 0);
					print("!!___" + _id);
					int epId = episodeResult.InternalId;

					var _episodeResult = (EpisodeResult)episodeResult.Clone();
					await Task.Delay(10000); // WAIT 10 Sec
					try {
						BasicLink[] info = null;
						bool hasMirrors = false;
						var baseLinks = CloudStreamCore.GetCachedLink(imdbId);
						if (baseLinks.HasValue) {
							info = baseLinks.Value.links.Where(t => t.CanBeDownloaded).ToList().OrderHDLinks().ToArray();
							hasMirrors = info.Length > 0;
						}

						if (hasMirrors && info != null) {
							App.UpdateDownload(epId, -1);
							string dpath = App.RequestDownload(epId, _episodeResult.OgTitle, _episodeResult.Description, _episodeResult.Episode, CurrentSeason, info.Select(t => { return new BasicMirrorInfo() { mirror = t.baseUrl, name = t.PublicName, referer = t.referer }; }).ToList(), _episodeResult.GetDownloadTitle(CurrentSeason, _episodeResult.Episode) + ".mp4", _episodeResult.PosterUrl, titleName, _episodeResult.IMDBEpisodeId);

							try {
								epView.MyEpisodeResultCollection[_id].DownloadState = 2; // SET IS DOWNLOADING
								ForceUpdate(_id);
							}
							catch (Exception) { }
						}
						else {
							App.ShowToast("Download Failed, No Mirrors Found");
							try {
								epView.MyEpisodeResultCollection[_id].DownloadState = 0;
								ForceUpdate(_id);
							}
							catch (Exception) { }
						}
					}
					catch (Exception _ex) {
						print("EX DLOAD::: DOWNLOAD:::: " + _ex);
					}
					currentDownloadSearchesHappening--;
				}
			});

			return episodeResult;
		}

		public void ClearEpisodes()
		{
			if (IsDead) return;

			episodeView.ItemsSource = null;
			epView.MyEpisodeResultCollection.Clear();
			RecomendationLoaded.IsVisible = true;
			episodeView.ItemsSource = epView.MyEpisodeResultCollection;
			SetHeight();
		}


		void SetHeight(bool? setNull = null, int? overrideCount = null)
		{
			if (Core == null) return;

			episodeView.RowHeight = Settings.EpDecEnabled ? 170 : 100;
			Device.BeginInvokeOnMainThread(() => episodeView.HeightRequest = (((setNull ?? showState != 0) || epView.MyEpisodeResultCollection.Count == 0) ? 0 : ((overrideCount ?? epView.MyEpisodeResultCollection.Count) * (episodeView.RowHeight) + Settings.TransparentAddPaddingEnd + 20))); // + 40 
		}

		private async void TrailerBtt_Clicked(object sender, EventArgs e)
		{
			if (Core == null) return;

			if (trailerUrl != null) {
				if (trailerUrl != "") {
					await App.RequestVlc(trailerUrl, CurrentMovie.title.name + " - Trailer");
					//  App.PlayVLCWithSingleUrl(trailerUrl, currentMovie.title.name + " - Trailer");
				}
			}
		}

		const double _RecPosterMulit = 1.75;
		const int _RecPosterHeight = 100;
		const int _RecPosterWith = 65;
		int RecPosterHeight { get { return (int)Math.Round(_RecPosterHeight * _RecPosterMulit); } }
		int RecPosterWith { get { return (int)Math.Round(_RecPosterWith * _RecPosterMulit); } }

		double lastWidth = -1;
		double lastHeight = -1;
		protected override void OnSizeAllocated(double width, double height)
		{
			base.OnSizeAllocated(width, height);
			if (lastHeight != height || lastWidth != width) {
				lastWidth = width;
				lastHeight = height;
				SetRecs(true, (int)height, (int)width);
			}
		}

		void SetRecs(bool isFromSizeChange = false, int? height = null, int? width = null)
		{
			if (Core == null) return;
			if (isFromSizeChange) {
				Recommendations.Opacity = 0;
			}
			Device.BeginInvokeOnMainThread(() => {
				const int total = 12;
				int perCol = ((width ?? Application.Current.MainPage.Width) < (height ?? Application.Current.MainPage.Height)) ? 3 : 6;

				for (int i = 0; i < Recommendations.Children.Count; i++) { // GRID
					Grid.SetColumn(Recommendations.Children[i], i % perCol);
					Grid.SetRow(Recommendations.Children[i], (int)Math.Floor(i / (double)perCol));
				}
				// Recommendations.HeightRequest = (RecPosterHeight + Recommendations.RowSpacing) * (total / perCol);
				Recommendations.HeightRequest = (RecPosterHeight + Recommendations.RowSpacing) * (total / perCol) - 7 + Recommendations.RowSpacing; // + + Settings.TransparentAddPaddingEnd
				Recommendations.Padding = new Thickness(0, 0, 0, DescriptionLabel.Height + NameLabel.Height - 30);
				episodeView.Margin = new Thickness(0, 0, 0, DescriptionLabel.Height);
				if (isFromSizeChange) {
					Recommendations.FadeTo(1, 75);
				}
			});
		}

		bool nextEpTimeShouldBeDisplayed = false;

		private void MovieResult_trailerLoaded(object sender, List<Trailer> e)
		{
			if (Core == null) return;

			if (e == null) return;
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
						Label textLb = new Label() { Text = e[i].Name, TextColor = Color.FromHex("#e7e7e7"), FontAttributes = FontAttributes.Bold, FontSize = 15, TranslationX = 10, Margin = new Thickness(0, 0, 0, 20) };
						Image playBtt = new Image() { Source = GetImageSource("nexflixPlayBtt.png"), VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center, Scale = 0.5, InputTransparent = true };
						var ff = new FFImageLoading.Forms.CachedImage {
							Source = p,
							BackgroundColor = Color.Transparent,
							VerticalOptions = LayoutOptions.Fill,
							Aspect = Aspect.AspectFill,
							HorizontalOptions = LayoutOptions.Fill,
							Transformations = {
							new FFImageLoading.Transformations.RoundedTransformation(3, 1.7, 1, 0, "#303F9F")
						},
							InputTransparent = true,
						};

						int _sel = int.Parse(i.ToString());
						stackLayout.Children.Add(new Frame() { InputTransparent = true, BackgroundColor = Settings.ItemBackGroundColor, CornerRadius = 5, Margin = new Thickness(-20, -20, -20, -50), HasShadow = true });
						stackLayout.Children.Add(ff);
						stackLayout.Children.Add(playBtt);

						trailerView.Children.Add(stackLayout);
						trailerView.Children.Add(textLb);

						stackLayout.SetValue(XamEffects.TouchEffect.ColorProperty, new Color(1, 1, 1, 0.3));
						int z = _sel;

						Commands.SetTap(stackLayout, new Command(async () => {
							var _t = epView.CurrentTrailers[z];
							await RequestVlc(_t.Url, _t.Name);
							//PlayVLCWithSingleUrl(_t.Url, _t.Name);
						}));
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
		private async void IMDb_Clicked(object sender, EventArgs e)
		{
			// if (!SameAsActiveMovie()) return;
			List<string> options = new List<string>() {
				"IMDb",
			};
			if (CurrentMalLink.IsClean()) {
				options.Add("MAL");
			}
			if (CurrentAniListLink.IsClean()) {
				options.Add("AniList");
			}
			string option = await ActionPopup.DisplayActionSheet("Open", options.ToArray());
			if (option == "IMDb") {
				await App.OpenBrowser("https://www.imdb.com/title/" + mainPoster.url);
			}
			else if (option == "MAL") {
				await App.OpenBrowser(CurrentMalLink);
			}
			else if (option == "AniList") {
				await App.OpenBrowser(CurrentAniListLink);
			}
		}

		void PlayDownloadedEp(EpisodeResult episodeResult)
		{
			var _info = App.GetDownloadInfo(episodeResult.InternalId, false);
			//   var downloadKeyData = data ?? _info.info.fileUrl;
			SetEpisode(episodeResult);
			Download.PlayDownloadedFile(_info);
			//  Download.PlayVLCFile(downloadKeyData, episodeResult.Title, GetCorrectId(episodeResult).ToString());
		}


		bool loadingLinks = false;

		static int currentDownloadSearchesHappening = 0;

		public async Task<EpisodeResult> LoadLinksForEpisode(EpisodeResult episodeResult, bool autoPlay = true, bool overrideLoaded = false, bool showloading = true)
		{
			if (loadingLinks) return episodeResult;

			if (episodeResult.GetHasLoadedLinks()) {
				if (autoPlay) { PlayEpisode(episodeResult); }
			}
			else {
				Core.GetEpisodeLink(IsMovie ? -1 : (episodeResult.Id + 1), CurrentSeason, isDub: IsDub, purgeCurrentLinkThread: currentDownloadSearchesHappening > 0);

				await Device.InvokeOnMainThreadAsync(async () => {
					// NormalStack.IsEnabled = false;
					loadingLinks = true;
					try {
						if (showloading) {
							await ActionPopup.DisplayLoadingBar(LoadingMiliSec, "Loading Links...");
						}
						else {
							await Task.Delay(LoadingMiliSec);
						}
					}
					catch (Exception) {

					}
					finally {
						loadingLinks = false;
					}

					int errorCount = 0;
					const int maxErrorcount = 1;
					bool gotError = false;
					checkerror:;
					if (episodeResult == null) {
						gotError = true;
					}
					else {

						if (episodeResult.GetBasicLinks().Count > 0) {
							if (autoPlay) {
								if (MainChrome.IsConnectedToChromeDevice) {
									ChromecastAt(0, episodeResult);
								}
								else {
									PlayEpisode(episodeResult);
								}
							}
							//episodeResult.GetHasLoadedLinks() = true;
						}
						else {
							gotError = true;
						}

					}
					if (gotError) {
						if (errorCount < maxErrorcount) {
							errorCount++;
							if (showloading) {
								await ActionPopup.DisplayLoadingBar(2000, "Loading More Links...");
							}
							else {
								await Task.Delay(2000);
							}
							goto checkerror;
						}
						else {
							episodeView.SelectedItem = null;
							if (showloading) {
								App.ShowToast(errorEpisodeToast);
							}
						}
					}
				});
			}

			return episodeResult;
		}

		void Dispose()
		{
			controller.Dispose();
		}

		static bool isRequestingPlayEpisode = false;

		void SetAsViewed(EpisodeResult episodeResult)
		{
			string id = episodeResult.IMDBEpisodeId;
			if (id != "") {
				if (ViewHistory) {
					App.SetKey(App.VIEW_HISTORY, id, true);
					SetColor(episodeResult);
					// FORCE UPDATE
					ForceUpdate(episodeResult.Id);
				}
			}
		}

		// ============================== PLAY VIDEO ==============================
		async void PlayEpisode(EpisodeResult episodeResult, bool? overrideSelectVideo = null)
		{
			if (isRequestingPlayEpisode) return;
			isRequestingPlayEpisode = true;
			SetAsViewed(episodeResult);
			lastPlayedEp = episodeResult.Id;

			/*
            string _sub = "";
            if (currentMovie.subtitles != null) {
                if (currentMovie.subtitles.Count > 0) {
                    _sub = currentMovie.subtitles[0].data;
                }
            }*/
			if (CurrentMovie.subtitles == null) {
				Core.activeMovie.subtitles = new List<Subtitle>();
			}

			bool useVideo = overrideSelectVideo ?? Settings.UseVideoPlayer;
			if (useVideo) {
				if (VideoPage.isShown) {
					VideoPage.isShown = false;
					VideoPage.changeFullscreenWhenPop = false;
					await Navigation.PopModalAsync(true);
					await Task.Delay(30);
				}
				VideoPage.loadLinkValidForHeader = CurrentMovie.title.id;

				VideoPage.loadLinkForNext = async (t, load) => {
					return await LoadLinkFrom(t, load);
				};

				VideoPage.canLoad = (t) => {
					return CanLoadLinkFrom(t, out int index);
				};
			}
			await App.RequestVlc(episodeResult.GetMirrosUrls(), episodeResult.GetMirros(), episodeResult.OgTitle, episodeResult.IMDBEpisodeId, episode: episodeResult.Episode, season: CurrentSeason, subtitleFull: CurrentMovie.subtitles.Select(t => t.data).FirstOrDefault(), descript: episodeResult.Description, overrideSelectVideo: overrideSelectVideo, startId: (int)episodeResult.ProgressState, headerId: CurrentMovie.title.id, isFromIMDB: true);// startId: FROM_PROGRESS); //  (int)episodeResult.ProgressState																																																																													  //App.PlayVLCWithSingleUrl(episodeResult.mirrosUrls, episodeResult.Mirros, currentMovie.subtitles.Select(t => t.data).ToList(), currentMovie.subtitles.Select(t => t.name).ToList(), currentMovie.title.name, episodeResult.Episode, currentSeason, overrideSelectVideo);
			isRequestingPlayEpisode = false;
		}

		public bool CanLoadLinkFrom(string id, out int index)
		{
			index = 0;
			if (Core == null) return false;
			index = (epView.MyEpisodeResultCollection.Select(t => t.IMDBEpisodeId).IndexOf(id));
			if (index == -1) return false;
			if (epView.MyEpisodeResultCollection.Count - 1 <= index) { // NEXT EPISODE DOSENT EXIST
				return false;
			}
			return true;
		}

		public void CacheEpisode(int index)
		{
			string tId = CurrentMovie.title.id;
			App.RemoveKey(nameof(CachedCoreEpisode), tId);
			if (epView.MyEpisodeResultCollection.Count > index && index >= 0) {
				var _ep = epView.MyEpisodeResultCollection[index];
				double progress = 0;
				long dur;
				long pos;
				if ((dur = App.GetViewDur(_ep.IMDBEpisodeId)) != -1) {
					if ((pos = App.GetViewPos(_ep.IMDBEpisodeId)) != -1) {
						progress = pos / (double)dur;
					}
				}
				if (progress > 0.9) {
					CacheEpisode(index + 1);
					return;
				}

				App.SetKey(nameof(CachedCoreEpisode), tId, new CachedCoreEpisode() {
					origin = EpisodeOrigin.IMDbTitle,
					progress = progress,
					createdAt = DateTime.UtcNow,
					description = _ep.Description,
					episode = _ep.Episode,
					season = _ep.Season,
					episodeName = _ep.OgTitle,
					rating = _ep.Rating.Replace(" ", ""),
					poster = _ep.PosterUrl,
					parentName = CurrentMovie.title.name,
					state = CurrentMovie,
					imdbId = _ep.IMDBEpisodeId,
					parentImdbId = CurrentMovie.title.id,
				});
			}
		}

		public async Task<string> LoadLinkFrom(string id, bool load)
		{
			if (!CanLoadLinkFrom(id, out int index)) return "";
			var _ep = epView.MyEpisodeResultCollection[index + 1];
			if (load) {
				App.SetViewPos(_ep.IMDBEpisodeId, -1); // IN CASE YOU HAVE ALREADY WATCHED IT HALFWAY
			}
			await LoadLinksForEpisode(_ep, load, false, load);
			return _ep.IMDBEpisodeId;
		}

		// ============================== FORCE UPDATE ==============================
		void ForceUpdate(int? item = null, bool checkColor = false)
		{
			if (Core == null) return;
			if (epView == null) return;
			if (epView.MyEpisodeResultCollection.Count == 0) return;
			//return;
			print("FORCE UPDATING");
			var _e = epView.MyEpisodeResultCollection.ToList();
			Device.BeginInvokeOnMainThread(() => {
				if (Core == null) return;
				if (item.HasValue) {
					int id = item.Value % MAX_EPS_PER;
					epView.MyEpisodeResultCollection[id] = (UpdateLoad((EpisodeResult)_e[id].Clone(), checkColor));
				}
				else {
					epView.MyEpisodeResultCollection.Clear();
					for (int i = 0; i < _e.Count; i++) {
						epView.MyEpisodeResultCollection.Add(UpdateLoad((EpisodeResult)_e[i].Clone(), checkColor));
					}
				}
			});
		}

		async void ChromecastAt(int count, EpisodeResult episodeResult)
		{
			chromeResult = episodeResult;
			chromeMovieResult = CurrentMovie;
			bool succ = false;
			count--;
			episodeView.SelectedItem = null;

			while (!succ) {
				count++;

				if (count >= episodeResult.GetMirros().Count) {
					succ = true;
				}
				else {
					succ = await MainChrome.CastVideo(episodeResult.GetMirrosUrls()[count], episodeResult.GetMirros()[count], subtitleUrl: "", posterUrl: CurrentMovie.title.hdPosterUrl, movieTitle: CurrentMovie.title.name, subtitleDelay: 0);
				}
			}
			ChromeCastPage.currentSelected = count;
		}

		async Task EpisodeSettings(EpisodeResult episodeResult)
		{
			try {
				if (loadingLinks) return;

				if (!episodeResult.GetHasLoadedLinks()) {
					try {
						await LoadLinksForEpisode(episodeResult, false);
					}
					catch (Exception) { }
				}

				if (!episodeResult.GetHasLoadedLinks()) {
					//   App.ShowToast(errorEpisodeToast); episodeView.SelectedItem = null;
					return;
				}

				// ============================== GET ACTION ==============================
				string action = "";

				bool hasDownloadedFile = App.KeyExists("dlength", "id" + episodeResult.InternalId);
				string downloadKeyData = "";

				List<string> actions = new List<string>() { "Play in App", "Play in Browser", "Auto Download", "Download", "Download Subtitles", "Copy Link", "Reload" }; // "Remove Link",

				if (App.CanPlayExternalPlayer()) {
					actions.Insert(1, "Play External App");
					actions.Insert(2, "Play Single Mirror");
				}

				if (hasDownloadedFile) {
					downloadKeyData = App.GetDownloadInfo(episodeResult.InternalId, false).info.fileUrl;//.GetKey("Download", GetId(episodeResult), "");
					print("INFOOOOOOOOO:::" + downloadKeyData);
					actions.Add("Play Downloaded File"); actions.Add("Delete Downloaded File");
				}
				if (MainChrome.IsConnectedToChromeDevice) {
					actions.Insert(0, "Chromecast");
					actions.Insert(1, "Chromecast mirror");
				}

				action = await ActionPopup.DisplayActionSheet(episodeResult.Title, actions.ToArray());//await DisplayActionSheet(episodeResult.Title, "Cancel", null, actions.ToArray());

				if (action == "Play in Browser") {
					string copy = await ActionPopup.DisplayActionSheet("Open Link", episodeResult.GetMirros().ToArray()); // await DisplayActionSheet("Open Link", "Cancel", null, episodeResult.Mirros.ToArray());
					for (int i = 0; i < episodeResult.GetMirros().Count; i++) {
						if (episodeResult.GetMirros()[i] == copy) {
							App.OpenSpecifiedBrowser(episodeResult.GetMirrosUrls()[i]);
						}
					}
				}
				else if (action == "Remove Link") {
					string rLink = await ActionPopup.DisplayActionSheet("Remove Link", episodeResult.GetMirros().ToArray()); //await DisplayActionSheet("Download", "Cancel", null, episodeResult.Mirros.ToArray());
					for (int i = 0; i < episodeResult.GetMirros().Count; i++) {
						if (episodeResult.GetMirros()[i] == rLink) {
							App.ShowToast("Removed " + episodeResult.GetMirros()[i]);
							episodeResult.GetMirrosUrls().RemoveAt(i);
							episodeResult.GetMirros().RemoveAt(i);
							await EpisodeSettings(episodeResult);
							break;
						}
					}
				}
				else if (action == "Chromecast") { // ============================== CHROMECAST ==============================
					SetAsViewed(episodeResult);
					ChromecastAt(0, episodeResult);
				}
				else if (action == "Chromecast mirror") {
					SetAsViewed(episodeResult);
					string subMirror = await ActionPopup.DisplayActionSheet("Cast Mirror", episodeResult.GetMirros().ToArray());//await DisplayActionSheet("Copy Link", "Cancel", null, episodeResult.Mirros.ToArray());
					ChromecastAt(episodeResult.GetMirros().IndexOf(subMirror), episodeResult);
				}
				else if (action == "Play") { // ============================== PLAY ==============================
					PlayEpisode(episodeResult);
				}
				else if (action == "Play External App") {
					PlayEpisode(episodeResult, false);
				}
				else if (action == "Play Single Mirror") {
					string copy = await ActionPopup.DisplayActionSheet("Copy Link", episodeResult.GetMirros().ToArray());//await DisplayActionSheet("Copy Link", "Cancel", null, episodeResult.Mirros.ToArray());
					for (int i = 0; i < episodeResult.GetMirros().Count; i++) {
						if (episodeResult.GetMirros()[i] == copy) {
							await App.RequestVlc(new List<string> { episodeResult.GetMirrosUrls()[i] }, new List<string> { episodeResult.GetMirros()[i] }, episodeResult.OgTitle, episodeResult.IMDBEpisodeId, episode: episodeResult.Episode, season: CurrentSeason, subtitleFull: CurrentMovie.subtitles.Select(t => t.data).FirstOrDefault(), descript: episodeResult.Description, overrideSelectVideo: false, startId: (int)episodeResult.ProgressState, headerId: CurrentMovie.title.id, isFromIMDB: true, generateM3u8: false);// startId: FROM_PROGRESS); //  (int)episodeResult.ProgressState																																																																													  //App.PlayVLCWithSingleUrl(episodeResult.mirrosUrls, episodeResult.Mirros, currentMovie.subtitles.Select(t => t.data).ToList(), currentMovie.subtitles.Select(t => t.name).ToList(), currentMovie.title.name, episodeResult.Episode, currentSeason, overrideSelectVideo);
							break;
						}
					}
				}
				else if (action == "Play in App") {
					PlayEpisode(episodeResult, true);
				}
				else if (action == "Copy Link") { // ============================== COPY LINK ==============================
					string copy = await ActionPopup.DisplayActionSheet("Copy Link", episodeResult.GetMirros().ToArray());//await DisplayActionSheet("Copy Link", "Cancel", null, episodeResult.Mirros.ToArray());
					for (int i = 0; i < episodeResult.GetMirros().Count; i++) {
						if (episodeResult.GetMirros()[i] == copy) {
							await Clipboard.SetTextAsync(episodeResult.GetMirrosUrls()[i]);
							App.ShowToast("Copied Link to Clipboard");
							break;
						}
					}
				}
				else if (action == "Auto Download") {
					if (SubtitlesEnabled) {
						DownloadSubtitlesToFileLocation(episodeResult, CurrentMovie, CurrentSeason, showToast: false);
					}

					int epId = episodeResult.InternalId;
					BasicLink[] info = null;
					bool hasMirrors = false;
					var baseLinks = CloudStreamCore.GetCachedLink(episodeResult.IMDBEpisodeId);
					if (baseLinks.HasValue) {
						info = baseLinks.Value.links.Where(t => t.CanBeDownloaded).ToList().OrderHDLinks().ToArray();
						hasMirrors = info.Length > 0;
					}
					if (hasMirrors && info != null) {
						App.ShowToast("Download Started");
						App.UpdateDownload(epId, -1);
						string dpath = App.RequestDownload(epId, episodeResult.OgTitle, episodeResult.Description, episodeResult.Episode, CurrentSeason, info.Select(t => { return new BasicMirrorInfo() { mirror = t.baseUrl, name = t.PublicName, referer = t.referer }; }).ToList(), episodeResult.GetDownloadTitle(CurrentSeason, episodeResult.Episode) + ".mp4", episodeResult.PosterUrl, CurrentMovie.title, episodeResult.IMDBEpisodeId);
						episodeResult.DownloadState = 2; // SET IS DOWNLOADING
						ForceUpdate(episodeResult.Id);
					}
					else {
						App.ShowToast("Download Failed, No Mirrors Found");
						episodeResult.DownloadState = 0;
						ForceUpdate(episodeResult.Id);
					}
				}
				else if (action == "Download") {  // ============================== DOWNLOAD FILE ==============================
					List<BasicLink> links = episodeResult.GetBasicLinks().Where(t => t.CanBeDownloaded).ToList();

					string download = await ActionPopup.DisplayActionSheet("Download", links.Select(t => t.PublicName).ToArray()); //await DisplayActionSheet("Download", "Cancel", null, episodeResult.Mirros.ToArray());
					for (int i = 0; i < links.Count; i++) {
						if (links[i].PublicName == download) {
							var link = links[i];
							if (SubtitlesEnabled) {
								DownloadSubtitlesToFileLocation(episodeResult, CurrentMovie, CurrentSeason, showToast: false);
							}
							TempThread tempThred = Core.CreateThread(4);
							Core.StartThread("DownloadThread", async () => {
								try {
									//UserDialogs.Instance.ShowLoading("Checking link...", MaskType.Gradient);
									bool doNotCheckLink = link.baseUrl.Contains(".m3u8") || !Settings.CheckDownloadLinkBefore;
									double fileSize = 2;
									if (!doNotCheckLink) {
										await ActionPopup.StartIndeterminateLoadinbar("Checking link...");
										fileSize = doNotCheckLink ? 2 : CloudStreamCore.GetFileSize(link.baseUrl, link.referer ?? "");
										await ActionPopup.StopIndeterminateLoadinbar();
									}

									if (fileSize > 1) {
										// ImageService.Instance.LoadUrl(episodeResult.PosterUrl, TimeSpan.FromDays(30)); // CASHE IMAGE
										App.UpdateDownload(episodeResult.InternalId, -1);
										print("CURRENTSESON: " + CurrentSeason);
										App.ShowToast($"Download Started{(doNotCheckLink ? "" : $" - {fileSize}MB")}");
										episodeResult.DownloadState = 2;
										ForceUpdate(episodeResult.Id);

										string dpath = App.RequestDownload(episodeResult.InternalId, episodeResult.OgTitle, episodeResult.Description, episodeResult.Episode, CurrentSeason, new List<BasicMirrorInfo>() { new BasicMirrorInfo() { mirror = link.baseUrl, name = link.PublicName, referer = link.referer } }, episodeResult.GetDownloadTitle(CurrentSeason, episodeResult.Episode) + ".mp4", episodeResult.PosterUrl, CurrentMovie.title, episodeResult.IMDBEpisodeId);
									}
									else {
										App.ShowToast("Download Failed");
										ForceUpdate(episodeResult.Id);
										await EpisodeSettings(episodeResult);
									}
								}
								finally {
									//UserDialogs.Instance.HideLoading();
									Core.JoinThred(tempThred);
								}
							});
							break;
						}
					}
				}
				else if (action == "Reload") { // ============================== RELOAD ==============================
					try {
						episodeResult.ClearMirror();
						await LoadLinksForEpisode(episodeResult, false, true);
					}
					catch (Exception) { }

					//await Task.Delay(LoadingMiliSec + 40);

					if (!episodeResult.GetHasLoadedLinks()) {
						return;
					}
					await EpisodeSettings(episodeResult);
				}
				else if (action == "Play Downloaded File") { // ============================== PLAY FILE ==============================
					PlayDownloadedEp(episodeResult);
				}
				else if (action == "Delete Downloaded File") {  // ============================== DELETE FILE ==============================
					DeleteFile(downloadKeyData, episodeResult);
				}
				else if (action == "Download Subtitles") {  // ============================== DOWNLOAD SUBTITLE ==============================
					DownloadSubtitlesToFileLocation(episodeResult, CurrentMovie, CurrentSeason, true);
				}
			}
			catch (Exception) {
			}
			finally {
				episodeView.SelectedItem = null;
			}
		}

		readonly static Dictionary<string, bool> hasSubtitles = new Dictionary<string, bool>();

		static void DownloadSubtitlesToFileLocation(EpisodeResult episodeResult, Movie currentMovie, int currentSeason, bool renew = false, bool showToast = true)
		{
			string id = episodeResult.IMDBEpisodeId;
			if (!renew && hasSubtitles.ContainsKey(id)) {
				if (showToast) {
					App.ShowToast("Subtitles already downloaded");
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

					string s = mainCore.DownloadSubtitle(id, Settings.NativeSubShortName, false);
					if (!s.IsClean()) {
						if (showToast) {
							App.ShowToast(s == null ? "Connection error" : "No subtitles found");
						}
						return;
					}
					else {
						string fullpath = App.GetPath(currentMovie.title.movieType, currentSeason, episodeResult.Episode, episodeResult.OgTitle, currentMovie.title.name, ".srt");

						string extraPath = "/" + GetPathFromType(currentMovie.title.movieType);
						if (!currentMovie.title.IsMovie) {
							extraPath += "/" + CensorFilename(currentMovie.title.name);
						}
						//	App.DownloadFile(s, episodeResult.GetDownloadTitle(currentSeason, episodeResult.Episode) + ".srt", true, extraPath); // "/Subtitles" +
						App.DownloadFile(fullpath, s); // "/Subtitles" +
						if (showToast) {
							App.ShowToast("Subtitles Downloaded");
						}
						if (!hasSubtitles.ContainsKey(id)) {
							hasSubtitles.Add(id, true);
						}
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
			App.UpdateDownload(episodeResult.InternalId, 2);
			Download.RemoveDownloadCookie(episodeResult.InternalId);//.DeleteFileFromFolder(downloadKeyData, "Download", GetId(episodeResult));
			SetColor(episodeResult);
			episodeResult.ExtraProgress = 0;
			ForceUpdate(episodeResult.Id);
		}

		private async void MalRating_Clicked(object sender, EventArgs e)
		{
			string rate = await ActionPopup.DisplayActionSheet("Rate anime", 10 - currentMalScore, MALSyncApi.MalRatingNames.Reverse().ToArray());
			int index = MALSyncApi.MalRatingNames.IndexOf(rate);
			if (index >= 0) {
				if (currentMalScore == 0 && index != 0 && currentMalWatchType == AnimeStatusType.none) {
					currentMalWatchType = AnimeStatusType.Watching;
				}
				currentMalScore = index;
				MalValueUpdated();
			}
		}

		private async void MalProgress_Clicked(object sender, EventArgs e)
		{
			try {
				int action = await ActionPopup.DisplayIntEntry("0", "Current Progress", 1, false, currentMalEpisodesProgress.ToString(), "Set Progress", 0, maxAnimeEpisode);
				if (action != -1) {
					if (currentMalEpisodesProgress == 0 && action != 0 && currentMalWatchType == AnimeStatusType.none) {
						currentMalWatchType = AnimeStatusType.Watching;
					}
					currentMalEpisodesProgress = action;
					if (currentMalEpisodesProgress == maxAnimeEpisode) {
						currentMalWatchType = AnimeStatusType.Completed;
					}

					MalValueUpdated();
				}
			}
			catch (Exception) { }
		}

		private async void AniListFav_Clicked(object sender, EventArgs e)
		{
			Device.BeginInvokeOnMainThread(() => {
				UpdateLikeIconVisual(!isLikedByAniList);
			});
			bool succ = await AniListSyncApi.ToggleLike(CurrentAniListId);
			if (succ) {
				isLikedByAniList = !isLikedByAniList;
			}
			Device.BeginInvokeOnMainThread(() => {
				UpdateLikeIconVisual(isLikedByAniList);
			});
		}

		private async void MalWatching_Clicked(object sender, EventArgs e)
		{
			try {
				string action = await ActionPopup.DisplayActionSheet("Set Status", (int)currentMalWatchType, MALSyncApi.StatusNames);
				if (action != "Cancel") {
					currentMalWatchType = (AnimeStatusType)(MALSyncApi.StatusNames.IndexOf(action));
					if (currentMalWatchType == AnimeStatusType.Completed && currentMalEpisodesProgress == 0) {
						currentMalEpisodesProgress = maxAnimeEpisode;
					}
					MalValueUpdated();
				}
			}
			catch (Exception) { }
		}

		public void UpdateMalVisual()
		{
			UpdateMalSyncVisual();
			UpdateLikeIconVisual(isLikedByAniList);
			bool isVis = CurrentMovie.title.movieType == MovieType.Anime;
			SecMalRow.IsVisible = isVis;
			SecMalRow.IsEnabled = isVis;
			Grid.SetRow(SecMalRow, isVis ? 6 : 5);
			if (isVis) {
				LikeBtt.IsVisible = showLike;
				LikeBtt.IsEnabled = showLike;
				LikeLabel.IsEnabled = showLike;
				LikeLabel.IsVisible = showLike;

				MalGridHolder.ColumnDefinitions = new ColumnDefinitionCollection() { new ColumnDefinition() { Width = showLike ? GridLength.Star : GridLength.Auto } };

				if (currentMalScore >= 0) {
					MalRatingTxt.Text = MALSyncApi.MalRatingNames[currentMalScore];
				}
				else {
					MalRatingTxt.Text = "No Rating";
				}

				if (currentMalWatchType != AnimeStatusType.none) {
					if (currentMalWatchType == AnimeStatusType.ReWatching) {
						MalWatchingTxt.Text = "Re Watching";
					}
					else {
						MalWatchingTxt.Text = MALSyncApi.StatusNames[(int)currentMalWatchType];
					}
				}
				else {
					MalWatchingTxt.Text = "Not Watched";
				}

				MalEpisodes.Text = $"{currentMalEpisodesProgress}/{maxAnimeEpisode}";
			}
		}

		public List<int> malIds;
		public List<int> aniListIds;
		public int CurrentMalId { get { return malIds[0]; } }
		public int CurrentAniListId { get { return aniListIds[0]; } }
		public AnimeStatusType currentMalWatchType = AnimeStatusType.none;
		public int currentMalEpisodesProgress = 0;
		public int currentMalScore = 0; // 0 = no rating
		public bool updateMalData;
		public int maxAnimeEpisode = 0;
		public bool isLikedByAniList = false;
		public bool showLike = false;
		public bool isFromAniList = false;

		public enum AnimeStatusType
		{
			Watching = 0, Completed = 1, OnHold = 2, Dropped = 3, PlanToWatch = 4, ReWatching = 5, none = -1
		}

		void MalValueUpdated()
		{
			updateMalData = true;
			Device.BeginInvokeOnMainThread(() => {
				UpdateMalVisual();
			});
		}

		void UpdateMalSyncVisual()
		{
			SyncButton.Transformations = new List<FFImageLoading.Work.ITransformation>() { (new FFImageLoading.Transformations.TintTransformation(updateMalData ? DARK_BLUE_COLOR : LIGHT_LIGHT_BLACK_COLOR)) };
		}

		async Task UpdateAllMalValues(bool fullUpdate = true)
		{
			try {
				showLike = false;
				updateMalData = false;
				if (CurrentMovie.title.movieType == MovieType.Anime) {
					if (fullUpdate) {
						malIds = new List<int>();
						aniListIds = new List<int>();
						var ms = CurrentMovie.title.MALData.seasonData[CurrentSeason].seasons;
						maxAnimeEpisode = 0;

						for (int i = 0; i < ms.Count; i++) {
							malIds.Add(ms[i].MalId);
							aniListIds.Add(ms[i].AniListId);
							maxAnimeEpisode += ms[i].length;
						}
					}

					bool error = true;
					currentMalWatchType = AnimeStatusType.none;
					currentMalScore = 0;
					currentMalEpisodesProgress = 0;

					if (HasMalAccountLogin && CurrentMalId != 0) {
						if (MALSyncApi.allTitles.ContainsKey(CurrentMalId)) {
							isFromAniList = false;
							var data = MALSyncApi.allTitles[CurrentMalId];
							currentMalWatchType = (AnimeStatusType)data.status.MalStatusType;
							currentMalScore = data.status.score;
							error = false;
						}
					}
					if (HasAniListLogin && CurrentAniListId != 0) {
						showLike = true;
						var show = await AniListSyncApi.GetDataAboutId(CurrentAniListId);
						if (show.HasValue) {
							var val = show.Value;
							isLikedByAniList = val.isFavourite;
							if (error) {
								isFromAniList = true;
								currentMalWatchType = (AnimeStatusType)val.type;
								currentMalScore = val.score;
								currentMalEpisodesProgress = val.progress;
							}
						}
						else {
							isLikedByAniList = false;
						}
					}

					if (isFromAniList) {
						for (int i = 1; i < aniListIds.Count; i++) {
							int _id = aniListIds[0];
							var show = await AniListSyncApi.GetDataAboutId(CurrentAniListId);
							if (show.HasValue) {
								currentMalEpisodesProgress += show.Value.progress;
							}
						}
					}
					else {
						foreach (var id in malIds) {
							if (MALSyncApi.allTitles.ContainsKey(id)) {
								var data = MALSyncApi.allTitles[id];
								currentMalEpisodesProgress += data.status.num_episodes_watched; ;
							}
						}
					}
				}
			}
			catch (Exception _ex) {
				error(_ex);
			}
		}

		void UpdateLikeIconVisual(bool enable)
		{
			LikeBtt.Transformations = new List<FFImageLoading.Work.ITransformation>() { (new FFImageLoading.Transformations.TintTransformation(enable ? DARK_BLUE_COLOR : LIGHT_LIGHT_BLACK_COLOR)) };
			LikeBtt.Source = enable ? "favorite.svg" : "favorite_border.svg";
		}

		private async void MalSync_Clicked(object sender, EventArgs e)
		{
			if (currentMalWatchType == AnimeStatusType.none) {
				App.ShowToast("Must select a watchstatus");
				return;
			}

			updateMalData = false;
			Device.BeginInvokeOnMainThread(() => {
				UpdateMalSyncVisual();
			});

			var ms = CurrentMovie.title.MALData.seasonData[CurrentSeason].seasons;
			int currentMax = currentMalEpisodesProgress;

			bool error = false;
			for (int i = 0; i < ms.Count; i++) {
				var leng = ms[i].length;
				if (leng == 0) {
					leng = currentMalEpisodesProgress;
				}
				int progress = Math.Max(0, Math.Min(leng, currentMax));

				if (Settings.HasMalAccountLogin) {
					AnimeStatusType real = currentMalWatchType;
					if (real == AnimeStatusType.ReWatching) {
						real = AnimeStatusType.Watching;
					}
					var status = await MALSyncApi.SetScoreRequestAndGetTitle(ms[i].MalId, (MALSyncApi.MalStatusType)real, currentMalScore, progress);
					if (status == null) {
						error = true;
					}
				}

				try {
					if (Settings.HasAniListLogin && CurrentAniListLink.IsClean()) {
						error |= !await AniListSyncApi.PostDataAboutId(ms[i].AniListId, (AniListSyncApi.AniListStatusType)(int)currentMalWatchType, currentMalScore, progress);
					}
				}
				catch (Exception) {

				}
				currentMax -= leng;
			}
			if (error) {
				App.ShowToast("Error Syncing");
				MalValueUpdated();
			}
			else {
				App.ShowToast("Sync complete");
				await UpdateAllMalValues(false);
				Device.BeginInvokeOnMainThread(() => {
					UpdateMalVisual();
				});
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
			ViewToggle.Source = toggleViewState ? "visibility.svg" : "visibility_off.svg";// GetImageSource((toggleViewState ? "viewOffIcon.png" : "viewOnIcon.png"));
			ViewToggle.Transformations = new List<FFImageLoading.Work.ITransformation>() { (new FFImageLoading.Transformations.TintTransformation(toggleViewState ? DARK_BLUE_COLOR : LIGHT_LIGHT_BLACK_COLOR)) };
		}

		public void SetEpisode(EpisodeResult episodeResult)
		{
			string id = episodeResult.IMDBEpisodeId;
			SetEpisode(id);
			SetColor(episodeResult);
			ForceUpdate(episodeResult.Id);
		}

		public static void SetEpisode(string id)
		{
			App.SetKey(App.VIEW_HISTORY, id, true);
		}

		void ToggleEpisode(EpisodeResult episodeResult)
		{
			string id = episodeResult.IMDBEpisodeId;
			ToggleEpisode(id);
			SetColor(episodeResult);
			ForceUpdate(episodeResult.Id);
		}

		public static void ToggleEpisode(string id)
		{
			if (id != "") {
				if (App.KeyExists(App.VIEW_HISTORY, id)) {
					App.RemoveKey(App.VIEW_HISTORY, id);
				}
				else {
					SetEpisode(id);
				}
			}
		}

		// ============================== SHOW SETTINGS OF VIDEO ==============================
		private async void ViewCell_Tapped(object sender, EventArgs e)
		{
			if (!canTapEpisode) return;
			canTapEpisode = false;
			try {
				EpisodeResult episodeResult = ((EpisodeResult)(((ViewCell)sender).BindingContext));

				if (toggleViewState) {
					ToggleEpisode(episodeResult);
					episodeView.SelectedItem = null;
				}
				else {
					await EpisodeSettings(episodeResult);
				}
			}
			finally {
				canTapEpisode = true;
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
				Grid.SetRow(EpPickers, (state == 0) ? 1 : 0); // RECOMENATIONS

				episodeView.Scale = (state == 0) ? 1 : 0;
				//episodeView.IsEnabled = state == 0;

				trailerStack.Scale = (state == 2) ? 1 : 0;
				trailerStack.IsEnabled = state == 2;
				trailerStack.IsVisible = state == 2;
				trailerStack.InputTransparent = state != 2;
				// trailerView.HeightRequest = state == 2 ? Math.Min(epView.CurrentTrailers.Count, 4) * 350 : 0;
				if (IsDead) return;

				bool epVis = state == 0 && !IsMovie;
				EpPickers.IsEnabled = epVis;
				EpPickers.Scale = epVis ? 1 : 0;
				EpPickers.IsVisible = epVis;

				Recommendations.Scale = state == 1 ? 1 : 0;
				Recommendations.IsVisible = state == 1;
				Recommendations.IsEnabled = state == 1;
				Recommendations.InputTransparent = state != 1;

				NextEpisodeInfoBtt.IsVisible = state == 0 && nextEpTimeShouldBeDisplayed;
				NextEpisodeInfoBtt.IsEnabled = NextEpisodeInfoBtt.IsVisible;

				SetHeight(state != 0);
				//SetTrailerRec(state == 2);

				if (state == 1) {
					SetRecs();
				}

			});

			System.Timers.Timer timer = new System.Timers.Timer(10);
			ProgressBar GetBar(int _state)
			{
				return _state switch {
					0 => EPISODESBar,
					1 => RECOMMENDATIONSBar,
					2 => TRAILERSBar,
					_ => null,
				};
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

		private void ScrollView_Scrolled(object sender, ScrolledEventArgs e)
		{
			TrailerBtt.TranslationY = -e.ScrollY / 15.0;
			TrailerBtt.Opacity = 1 - (e.ScrollY / 100.0);
			// PlayBttGradient.TranslationY = -e.ScrollY / 15.0;
		}
	}
}

public class MovieResultMainEpisodeView
{
	public ObservableCollection<Trailer> CurrentTrailers { get; set; }

	public ObservableCollection<EpisodeResult> MyEpisodeResultCollection { set; get; }

	public MovieResultMainEpisodeView()
	{
		MyEpisodeResultCollection = new ObservableCollection<EpisodeResult>();
		CurrentTrailers = new ObservableCollection<Trailer>();
	}
}