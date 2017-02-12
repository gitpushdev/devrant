﻿using Innouvous.Utils.Merged45.MVVM45;
using mvvm = Innouvous.Utils.MVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Innouvous.Utils;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.ComponentModel;
using DevRant.WPF.ViewModels;
using System.Diagnostics;
using DevRant.WPF.Converters;
using DevRant.Dtos;

namespace DevRant.WPF
{
    internal class MainWindowViewModel : ViewModel
    {
        private Window window;
        private IDataStore ds;
        private DevRantClient api;
        private FollowedUserChecker checker;

        private FeedType currentSection;
        private ObservableCollection<FeedItem> feeds = new ObservableCollection<FeedItem>();
        private CollectionViewSource feedView;

        public MainWindowViewModel(Window window)
        {
            this.window = window;
            ds = new AppSettingsDataStore();
            api = new DevRantClient();

            statusMessages = new MessageCollection();
            statusMessages.Changed += StatusChanged;

            feedView = new CollectionViewSource();
            feedView.Source = feeds;

            //Initialize the properties
            UpdateFollowedPosts(new FollowedUserChecker.UpdateArgs(), false);

            checker = new FollowedUserChecker(ds, api);
            checker.OnUpdate += UpdateFollowedPosts;
            checker.Start();

            //Test();
        }

        
        #region Status

        private MessageCollection statusMessages;
        public string StatusMessage
        {
            get { return statusMessages.LastMessage; }
        }
        private void StatusChanged()
        {
            RaisePropertyChanged("StatusMessage");
        }

        private void UpdateStatus(string message, bool includeTime = true)
        {
            StringBuilder sb = new StringBuilder();

            if (includeTime)
            {
                string time = DateTime.Now.ToShortTimeString();
                sb.Append(time + ": ");
            }

            sb.Append(message);

            statusMessages.AddMessage(sb.ToString());
        }
        #endregion

        #region Properties
        public bool IsLoading
        {
            get { return Get<bool>(); }
            private set
            {
                Set(value);
                RaisePropertyChanged();
            }
        }
        public ICollectionView FeedView
        {
            get
            {
                return feedView.View;
            }
        }

        public string FollowedUsersLabel
        {
            get { return Get<string>(); }
            private set
            {
                Set(value);
                RaisePropertyChanged();
            }
        }

        public FontWeight FollowedUsersWeight
        {
            get
            {
                return Get<FontWeight>();
            }
            private set
            {
                Set(value);
                RaisePropertyChanged();
            }
        }
        
        public FeedItem SelectedPost
        {
            get { return Get<FeedItem>(); }
            set
            {
                if (value != null)
                {
                    value.Read = true;

                    if (currentSection == FeedType.Updates)
                    {
                        checker.SendUpdate();
                    }
                }

                Set(value);
                VisibilityConverter.State.SetSelectedItem(value);
                RaisePropertyChanged();
            }
        }
        
        #endregion

        #region Public Methods
        public void OpenPost()
        {
            if (SelectedPost == null)
                return;
            else if (SelectedPost is Rant)
                Process.Start(((Rant)SelectedPost).PostURL);
            //TODO: Add For Notification
        }

        #region Sections

        private enum FeedType
        {
            Stories,
            General,
            Collab,
            Updates
        }

        public const string SectionGeneral = "GeneralFeed";
        public const string SectionGeneralAlgo = "GeneralAlgo";
        public const string SectionGeneralTop = "GeneralTop";
        public const string SectionGeneralRecent = "GeneralRecent";

        public const string SectionStories = "StoriesFeed";
        public const string SectionStoriesDay = "StoriesDay";
        public const string SectionStoriesWeek = "StoriesWeek";
        public const string SectionStoriesMonth = "StoriesMonth";
        public const string SectionStoriesAll = "StoriesAll";
        
        public const string SectionCollab = "CollabFeed";
        
        public const string SectionNotifications = "MyNotifications";
        
        public const string SectionFollowed = "FollowedUsers";

        public async Task LoadSection(string section)
        {
            IsLoading = true;

            switch (section)
            {
                case SectionGeneral:
                    await LoadFeed(FeedType.General, sort: ds.DefaultFeed); //TODO: Add params from Settings
                    break;
                case SectionGeneralAlgo:
                    await LoadFeed(FeedType.General, sort: RantSort.Algo);
                    break;
                case SectionGeneralRecent:
                    await LoadFeed(FeedType.General, sort: RantSort.Recent);
                    break;
                case SectionGeneralTop:
                    await LoadFeed(FeedType.General, sort: RantSort.Top);
                    break;

                case SectionStories:
                    await LoadFeed(FeedType.Stories, ds.DefaultFeed, ds.DefaultRange);
                    break;
                case SectionStoriesDay:
                    await LoadFeed(FeedType.Stories, ds.DefaultFeed, StoryRange.Day);
                    break;
                case SectionStoriesWeek:
                    await LoadFeed(FeedType.Stories, ds.DefaultFeed, StoryRange.Week);
                    break;
                case SectionStoriesMonth:
                    await LoadFeed(FeedType.Stories, ds.DefaultFeed, StoryRange.Month);
                    break;
                case SectionStoriesAll:
                    await LoadFeed(FeedType.Stories, ds.DefaultFeed, StoryRange.All);
                    break;

                /*
                case SectionNotifications:
                    await LoadNotifications();
                    break;
                */

                case SectionFollowed:
                    LoadFollowed();
                    break;
            }

            IsLoading = false;
        }

        #endregion
        #endregion

        #region Commands

        public ICommand OpenOptionsCommand
        {
            get { return new mvvm.CommandHelper(OpenOptions); }
        }

        private void OpenOptions()
        {
            var dlg = new OptionsWindow(ds, api);
            dlg.ShowDialog();

            if (!dlg.Cancelled)
            {
                checker.Stop();
                checker.Start();
            }
        }

        public ICommand OpenPostCommand
        {
            get { return new mvvm.CommandHelper(OpenPost); }
        }

        public ICommand UnfollowUserCommand
        {
            get { return new mvvm.CommandHelper(UnfollowUser); }
        }

        private void UnfollowUser()
        {
            if (SelectedPost == null)
                return;

            ds.Unfollow(SelectedPost.AsRant().Username);

            UpdateFollowedInRants();
        }

        public ICommand FollowUserCommand
        {
            get { return new mvvm.CommandHelper(FollowUser); }
        }

        private void FollowUser()
        {
            if (SelectedPost == null)
                return;

            ds.Follow(SelectedPost.AsRant().Username);

            UpdateFollowedInRants();
        }

        public ICommand ViewProfileCommand
        {
            get { return new mvvm.CommandHelper(ViewProfile); }
        }

        private void ViewProfile()
        {
            if (SelectedPost == null)
                return;

            Process.Start(((Rant)SelectedPost).ProfileURL);
        }

        public ICommand ViewNotificationsCommand
        {
            get { return new mvvm.CommandHelper(ViewNotifications); }
        }

        private void ViewNotifications()
        {
            Process.Start(Utilities.BaseURL + "/notifs");
        }

        #endregion

        #region Test Functions

        public ICommand TestCommand
        {
            get { return new mvvm.CommandHelper(Test); }
        }

        private async void Test()
        {
            var profile = await GetProfile();
            MessageBoxFactory.ShowInfo(profile.Skills, "Test");
        }

        public async Task<Dtos.Profile> GetProfile()
        {
            var profile = await api.GetProfileAsync("allanx2000");
            return profile;
        }

        #endregion

        private void UpdateFollowedPosts(FollowedUserChecker.UpdateArgs args)
        {
            UpdateFollowedPosts(args, true);
        }

        private void UpdateFollowedPosts(FollowedUserChecker.UpdateArgs args, bool updateStatus)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Updates");

            if (args.TotalUnread > 0)
            {
                FollowedUsersWeight = FontWeights.Bold;
                sb.Append(" (" + args.TotalUnread + ")");
            }
            else
            {
                FollowedUsersWeight = FontWeights.Normal;
            }

            FollowedUsersLabel = sb.ToString();

            if (updateStatus)
            {
                string message = string.Format("Checker found {0} new posts.", args.Added);
                UpdateStatus(message);
            }
        }

        private void LoadFollowed()
        {
            feeds.Clear();

            foreach (var rant in checker.Posts)
            {
                if (!rant.Read)
                    feeds.Add(rant);
            }

            UpdateFollowedInRants();
            currentSection = FeedType.Updates;
        }

        /*
        private async Task LoadNotifications()
        {
            //TODO: Add get notifications

            //var notif = await api.GetNotificationsAsync("allanx2000");

            feeds.Clear();

            feeds.Add(new Notification());
            feeds.Add(new Notification());
            feeds.Add(new Notification());
        }
        */
        
        private async Task LoadFeed(FeedType type, RantSort sort = RantSort.Algo, StoryRange range = StoryRange.Day)
        {
            IReadOnlyCollection<Dtos.RantInfo> rants;
            switch (type)
            {
                case FeedType.General:
                    rants = await api.GetRantsAsync(sort: sort);
                    break;
                case FeedType.Stories:
                    rants = await api.GetStoriesAsync(range: range, sort: sort);
                    break;
                default:
                    return;
            }

            feeds.Clear();

            foreach (var rant in rants)
            {
                Rant r = new Rant(rant);
                feeds.Add(r);
            }

            UpdateFollowedInRants();

            currentSection = type;
            UpdateStatus("Loaded " + rants.Count + " rants");
        }
        
        /// <summary>
        /// Updates the Followed property in the rants, which is also shown in the View
        /// </summary>
        private void UpdateFollowedInRants()
        {
            var followed = ds.FollowedUsers;

            foreach (Rant rant in feeds)
            {
                if (followed.Contains(rant.Username))
                    rant.Followed = true;
                else
                    rant.Followed = false;
            }

            //Updates the ContextMenu
            RaisePropertyChanged("SelectedPost");
        }
        
    }
}
