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
using DevRant.WPF.DataStore;
using DevRant.Exceptions;
using DevRant.Enums;
using DevRant.V1;
using DevRant.WPF.Checkers;
using DevRant.WPF.Controls;
using System.IO;

namespace DevRant.WPF
{
    internal class MainWindowViewModel : ViewModel
    {
        private Window window;

        private IDataStore ds;
        private IDevRantClient api;
        private IPersistentDataStore db;

        private FollowedUserChecker fchecker;

        private FeedType currentSection;
        private string currentSectionName;
        
        private ObservableCollection<FeedItem> feeds = new ObservableCollection<FeedItem>();
        private CollectionViewSource feedView;

        public MainWindowViewModel(Window window)
        {
            this.window = window;
            ds = new AppSettingsDataStore();
            api = new DevRantClient();
            db = SQLiteStore.CreateInstance(ds.DBFolder);

            statusMessages = new MessageCollection();
            statusMessages.Changed += StatusChanged;

            feedView = new CollectionViewSource();
            feedView.Source = feeds;

            //Initialize the properties
            fchecker = new FollowedUserChecker(ds, api, db);
            fchecker.OnUpdate += UpdateFollowedPosts;
            fchecker.Start();

            nchecker = new NotificationsChecker(ds, api);
            nchecker.OnUpdate += UpdateNotifications;

            Startup();
            
            //TestLogin();
            //Test();
        }

        private async void Startup()
        {
            try
            {
               if (await Login())
               {
                    await LoadSection(currentSectionName);
               }
            }
            catch (Exception e)
            {
                statusMessages.AddMessage(e.Message);
            }

            UpdateFollowedPosts(fchecker.GetFeedUpdate());
            UpdateNotifications(new NotificationsChecker.UpdateArgs(0, 0), true);
            UpdateDrafts(db.GetNumberOfDrafts());
        }
        

        public async void Vote(VoteClickedEventArgs args)
        {
            try
            {
                Vote vote = null;

                Votable votable = args.SelectedItem as Votable;

                if (votable != null)
                {
                    switch (args.Type)
                    {
                        case VoteButton.ButtonType.Down:
                            throw new NotImplementedException();
                            break;
                        case VoteButton.ButtonType.Up:

                            if (votable.Voted == VoteState.Up)
                                vote = Dtos.Vote.ClearVote();
                            else
                                vote = Dtos.Vote.UpVote();
                            break;
                    }
                    
                    FeedItem item = args.SelectedItem as FeedItem;
                    
                    switch (args.SelectedItem.Type)
                    {
                            case FeedItem.FeedItemType.Post:
                            var rant = item.AsRant();

                            var r1 = await api.User.VoteRant(rant.ID, vote);

                            rant.Update(r1);
                            rant.Read = true;

                            if (db != null)
                                db.MarkRead(rant.ID);

                            break;
                        case FeedItem.FeedItemType.Collab:
                            var collab = item.AsCollab();

                            var r2 = await api.User.VoteCollab(collab.ID, vote);
                            collab.Update(r2);
                            break;
                    }
                    
                    args.InvokeCallback();
                }
            }
            catch (InvalidCredentialsException e)
            {
                await Login();
                Vote(args);
            }
            catch (Exception e)
            {
                
                UpdateStatus(e.Message);
            }

        }



        private async void UpdateNotifications(NotificationsChecker.UpdateArgs args)
        {
            await UpdateNotifications(args, false);
        }
        private async Task UpdateNotifications(NotificationsChecker.UpdateArgs args, bool forInit)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Notifications");

            if (args.TotalUnread > 0)
            {
                sb.Append(" (" + args.TotalUnread + ")");
                NotificationsWeight = FontWeights.Bold;

                if (currentSection == FeedType.Drafts)
                {
                   LoadNotifications();
                }
            }
            else
            {
                NotificationsWeight = FontWeights.Normal;
            }

            NotificationsLabel = sb.ToString();

            if (forInit)
                return;

            string msg;

            if (args.Error != null)
            {
                msg = args.Error.Message;

                if (args.Error is InvalidCredentialsException)
                {
                    await Login();
                    nchecker.Check();
                }
            }
            else
                msg = string.Format("Found {0} new notifications", args.TotalUnread);

            UpdateStatus(msg);
        }

        private void UpdateDrafts(int drafts)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Drafts");

            if (drafts > 0)
            {
                sb.Append(" (" + drafts + ")");
                DraftsWeight = FontWeights.Bold;

                if (currentSection == FeedType.Drafts)
                {
                    LoadDrafts();
                }
            }
            else
            {
                DraftsWeight = FontWeights.Normal;
            }

            DraftsLabel = sb.ToString();
            
        }

        private async Task<bool> Login()
        {
            bool loggedIn = false;

            try
            {
                var loginInfo = ds.GetLoginInfo();

                if (loginInfo != null)
                {
                    await api.User.Login(loginInfo.Username, loginInfo.Password);
                    
                    nchecker.Start();
                    loggedIn = true;
                }
            }
            catch (Exception e)
            {
                UpdateStatus("Failed to login, error: " + e.Message);
            }

            RaisePropertyChanged("DraftsVisibility");
            RaisePropertyChanged("NotificationsVisibility");
            RaisePropertyChanged("LoggedInUser");
            RaisePropertyChanged("LoggedIn");

            return loggedIn;
        }

        private async void TestLogin()
        {
            //await api.Login();
            //await api.GetNotificationsAsync();
        }


        #region Status

        public void ShowStatusHistory()
        {
            var dlg = new StatusViewerWindow(statusMessages);
            dlg.Owner = window;

            dlg.ShowDialog();
        }

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


        public Visibility DraftsVisibility { get { return LoggedIn ? Visibility.Visible : Visibility.Collapsed; } }
        public string DraftsLabel
        {
            get
            {
                return Get<string>();
            }
            set
            {
                Set(value);
                RaisePropertyChanged();
            }
        }

        public FontWeight DraftsWeight
        {
            get
            {
                return Get<FontWeight>();
            }
            set
            {
                Set(value);
                RaisePropertyChanged();
            }
        }

        public Visibility NotificationsVisibility { get { return LoggedIn ? Visibility.Visible : Visibility.Collapsed; } }
        public string NotificationsLabel
        {
            get
            {
                return Get<string>();
            }
            set
            {
                Set(value);
                RaisePropertyChanged();
            }
        }

        public FontWeight NotificationsWeight
        {
            get
            {
                return Get<FontWeight>();
            }
            set
            {
                Set(value);
                RaisePropertyChanged();
            }
        }

        public string LoggedInUser
        {
            get {
                return api.User.LoggedInUser;
            }
        }

        public bool LoggedIn
        {
            get { return api.User.LoggedIn; }
        }

        public bool IsLoading
        {
            get { return Get<bool>(); }
            private set
            {
                Set(value);
                RaisePropertyChanged();
            }
        }

        public bool DateVisible
        {
            get { return ds.ShowCreateTime; }
        }

        public Visibility DateVisibility
        {
            get
            {
                return ds.ShowCreateTime ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        
        public Visibility UsernameVisibility
        {
            get {
                return ds.HideUsername ? Visibility.Collapsed : Visibility.Visible;
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
                    
                    if (value.Type == FeedItem.FeedItemType.Post)
                    {
                        db.MarkRead(value.AsRant().ID);
                    }

                    if (currentSection == FeedType.Updates)
                    {                        
                        UpdateFollowedPosts(fchecker.GetFeedUpdate());
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
            else if (SelectedPost is ViewModels.Rant)
            {
                /*
                var dlg = new RantViewerWindow((Rant)SelectedPost, api);
                dlg.Owner = window;
                dlg.ShowDialog();
                */

                Process.Start(((ViewModels.Rant)SelectedPost).PostURL);
            }
            else if (SelectedPost is ViewModels.Collab)
            {
                /*
                var dlg = new RantViewerWindow((Rant)SelectedPost, api);
                dlg.Owner = window;
                dlg.ShowDialog();
                */

                Process.Start(((ViewModels.Collab) SelectedPost).PostURL);
            }
            else if (SelectedPost is ViewModels.Notification)
            {
                ViewModels.Notification notif = SelectedPost as ViewModels.Notification;
                foreach (ViewModels.Notification n in feeds)
                {
                    if (n.RantId == notif.RantId)
                        n.MarkRead();
                }

                UpdateNotifications(new NotificationsChecker.UpdateArgs(feeds.Where(x => !x.Read).Count(), feeds.Count));
                FeedView.Refresh();

                Process.Start(notif.URL);

                //nchecker.Check();
            }
            else if (SelectedPost is Draft)
            {
                var dlg = EditPostWindow.CreateForRant(api, db, SelectedPost as Draft);
                dlg.Owner = window;

                dlg.ShowDialog();

                if (!dlg.Cancelled)
                {
                    UpdateDrafts(db.GetNumberOfDrafts());
                    LoadDrafts();
                }
            }
            
        }

        #region Sections

        private enum FeedType
        {
            Stories,
            General,
            Collab,
            Updates,
            Drafts
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
        public const string SectionDrafts = "RantDrafts";

        public const string SectionFollowed = "FollowedUsers";
        private NotificationsChecker nchecker;
        private const string NotificationCount = "notif_state";
        
        public async Task LoadSection(string section)
        {
            IsLoading = true;
            bool filter;

            switch (section)
            {
                case SectionGeneral:
                    filter = ds.DefaultFeed != RantSort.Top ? ds.FilterOutRead : false;

                    await LoadFeed(FeedType.General, sort: ds.DefaultFeed, 
                        filter: filter); //TODO: Add params from Settings
                    break;
                case SectionGeneralAlgo:
                    await LoadFeed(FeedType.General, sort: RantSort.Algo, filter: ds.FilterOutRead);
                    break;
                case SectionGeneralRecent:
                    await LoadFeed(FeedType.General, sort: RantSort.Recent, filter: ds.FilterOutRead);
                    break;
                case SectionGeneralTop:
                    await LoadFeed(FeedType.General, sort: RantSort.Top);
                    break;
                case SectionStories:
                    filter = ds.DefaultRange != StoryRange.All ? ds.FilterOutRead : false;

                    await LoadFeed(FeedType.Stories, ds.DefaultFeed, ds.DefaultRange,
                        filter: filter);
                    break;
                case SectionStoriesDay:
                    await LoadFeed(FeedType.Stories, ds.DefaultFeed, StoryRange.Day, filter: ds.FilterOutRead);
                    break;
                case SectionStoriesWeek:
                    await LoadFeed(FeedType.Stories, ds.DefaultFeed, StoryRange.Week, filter: ds.FilterOutRead);
                    break;
                case SectionStoriesMonth:
                    await LoadFeed(FeedType.Stories, ds.DefaultFeed, StoryRange.Month, filter: ds.FilterOutRead);
                    break;
                case SectionStoriesAll:
                    await LoadFeed(FeedType.Stories, ds.DefaultFeed, StoryRange.All);
                    break;
                                        
                case SectionNotifications:
                    LoadNotifications();
                    break;

                case SectionDrafts:
                    LoadDrafts();
                    break;

                case SectionFollowed:
                    LoadFollowed();
                    break;

                case SectionCollab:
                    await LoadCollabs();
                    break;
            }

            currentSectionName = section;

            IsLoading = false;
        }
        
        private void LoadDrafts()
        {
            var drafts = db.GetDrafts();
            feeds.Clear();

            foreach (var draft in drafts)
            {
                ViewModels.Draft vm = new Draft(draft);

                feeds.Add(vm);
            }

            
            currentSection = FeedType.Drafts;

            feedView.SortDescriptions.Clear();
        }

        private async Task LoadCollabs()
        {
            var collabs = await api.Feeds.GetCollabsAsync();
            feeds.Clear();

            foreach (var c in collabs)
            {
                var collab = new ViewModels.Collab(c);
                feeds.Add(collab);
            }

            currentSection = FeedType.Collab;
            feedView.SortDescriptions.Clear();
        }


        #endregion
        #endregion

        #region Commands
        
        public ICommand DeleteDraftCommand
        {
            get { return new mvvm.CommandHelper(DeleteDraft); }
        }

        private void DeleteDraft()
        {
            Draft draft = SelectedPost as Draft;

            if (draft == null)
                return;

            db.RemoveDraft(draft.ID.Value);
            UpdateDrafts(db.GetNumberOfDrafts());
            LoadDrafts();
        }

        public ICommand PostCommand
        {
            get { return new mvvm.CommandHelper(MakePost); }
        }

        private void MakePost()
        {
            var dlg = EditPostWindow.CreateForRant(api, db);
            dlg.Owner = window;

            dlg.ShowDialog();

            if (!dlg.Cancelled)
            {
                if (dlg.AddedDraft != null)
                {
                    //TODO: Should store the added draft in mem?
                    UpdateDrafts(db.GetNumberOfDrafts());
                }
            }
        }

        public ICommand AddCommentCommand
        {
            get { return new mvvm.CommandHelper(AddComment); }

        }

        private void AddComment()
        {
            if (SelectedPost == null)
                return;

            Commentable post = SelectedPost as Commentable;

            var dlg = EditPostWindow.CreateForComment(api, post);
            dlg.Owner = window;

            dlg.ShowDialog();

            if (!dlg.Cancelled)
            {
                post.IncrementComments();
            }
        }

        public ICommand CheckUpdatesCommand
        {
            get { return new mvvm.CommandHelper(CheckForUpdates); }
        }
        private void CheckForUpdates()
        {
            UpdateStatus("Checking for updates...");
            fchecker.Restart();
            nchecker.Check();
        }

        public ICommand OpenOptionsCommand
        {
            get { return new mvvm.CommandHelper(OpenOptions); }
        }

        private async void OpenOptions()
        {
            var dlg = new OptionsWindow(ds, api);
            dlg.Owner = window;

            dlg.ShowDialog();

            if (!dlg.Cancelled)
            {
                if (dlg.AddedUsers.Count > 0)
                    fchecker.GetAll(dlg.AddedUsers);

                RaisePropertyChanged("UsernameVisibility");
                RaisePropertyChanged("DateVisibility");
                
                if (dlg.LoginChanged)
                {
                        await api.User.Logout();

                        await Login();
                }

                if (dlg.DatabaseChanged)
                {
                    db.Close();

                    FileInfo info = new FileInfo(db.DBPath);
                    string newPath = Path.Combine(ds.DBFolder, info.Name);


                    if (!File.Exists(newPath))
                        info.MoveTo(newPath);
                    //else just use existing

                    //Restart
                    nchecker.Stop();
                    fchecker.Stop();

                    MainWindow newWindow = new MainWindow();
                    newWindow.Show();
                    
                    window.Close();
                }
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

            var username = SelectedPost.AsRant().Username;
            ds.Follow(username);

            UpdateFollowedInRants();
            fchecker.GetAll(username);
        }

        public ICommand ViewMyProfileCommand
        {
            get { return new mvvm.CommandHelper(ViewMyProfile); }
        }

        private void ViewMyProfile()
        {
            if (LoggedInUser != null)
            {
                ViewProfile(LoggedInUser);
            }
        }

        public ICommand ViewProfileCommand
        {
            get { return new mvvm.CommandHelper(ViewProfile); }
        }

        private void ViewProfile()
        {
            if (SelectedPost == null)
                return;

            var profile = SelectedPost as ViewModels.Rant;

            ViewProfile(profile.Username);
        }

        private void ViewProfile(string name)
        {
            if (ds.OpenInProfileViewer)
                Utilities.OpenProfile(name, window, api);
            else
                Utilities.OpenProfile(name);
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

        public int Limit { get { return 50; } }

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

        /// <summary>
        /// Updates the Update label in the Sections list
        /// </summary>
        /// <param name="args"></param>
        private void UpdateFollowedPosts(FollowedUserChecker.UpdateArgs args)
        {
            UpdateFollowedPosts(args, true);
        }

        /// <summary>
        /// Updates the Update label in the Sections list
        /// </summary>
        /// <param name="args"></param>
        /// <param name="updateStatus">Whether to send a status</param>
        private void UpdateFollowedPosts(FollowedUserChecker.UpdateArgs args, bool updateStatus)
        {
            StringBuilder labelBuilder = new StringBuilder();
            labelBuilder.Append("Updates");

            if (args.TotalUnread > 0)
            {
                FollowedUsersWeight = FontWeights.Bold;
                labelBuilder.Append(" (" + args.TotalUnread + ")");
            }
            else
            {
                FollowedUsersWeight = FontWeights.Normal;
            }

            FollowedUsersLabel = labelBuilder.ToString();

            if (updateStatus && args.Type != FollowedUserChecker.UpdateType.UpdateFeed)
            {
                string message = null;

                switch (args.Type)
                {
                    case FollowedUserChecker.UpdateType.UpdatesCheck:
                        message = string.Format("Found {0} new posts", args.Added);
                        break;
                    case FollowedUserChecker.UpdateType.GetAllForUser:
                        message = "Got rants for user: " + args.Users;
                        break;
                    case FollowedUserChecker.UpdateType.Error:

                        if (args.Error is InvalidCredentialsException)
                        {
                            Login();
                        }

                        message = args.Error.Message;
                        break;
                }

                if (!string.IsNullOrEmpty(message))
                    UpdateStatus(message);
            }
        }

        private void LoadFollowed()
        {
            feeds.Clear();

            foreach (var rant in fchecker.Posts)
            {
                if (!rant.Read)
                    feeds.Add(rant);
            }

            UpdateFollowedInRants();
            currentSection = FeedType.Updates;

            feedView.SortDescriptions.Clear();
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


        private void LoadNotifications()
        {
            var notifs = nchecker.Notifications;
            feeds.Clear();

            foreach (var notif in notifs)
            {
                feeds.Add(notif);
            }
            
            currentSection = FeedType.Drafts;
            
            feedView.SortDescriptions.Clear();
            
            feedView.SortDescriptions.Add(new SortDescription("Read", ListSortDirection.Ascending));
            feedView.SortDescriptions.Add(new SortDescription("CreateTimeRaw", ListSortDirection.Descending));
        }

        private async Task LoadFeed(FeedType type, RantSort sort = RantSort.Algo, StoryRange range = StoryRange.Day, bool filter = false)
        {
            Func<int, Task<IReadOnlyCollection<Dtos.Rant>>> getter;

            SettingsCollection collection = new SettingsCollection();

            switch (type)
            {
                case FeedType.General:
                    getter = async (skip) => await api.Feeds.GetRantsAsync(sort: sort, skip: skip, settings: collection);
                    break;
                case FeedType.Stories:
                    getter = async (skip) => await api.Feeds.GetStoriesAsync(range: range, sort: sort, skip: skip);
                    break;
                default:
                    return;
            }

            List<Dtos.Rant> rants = new List<Dtos.Rant>();

            //Remove duplicates
            List<long> ids = new List<long>();

            int page = 0;
            while (rants.Count < Limit && page < 5)
            {
                int skip = page * Limit;

                var tmp = await getter.Invoke(skip);
                if (tmp.Count == 0)
                    break;

                foreach (var r in tmp)
                {
                    if (!ds.FilterOutRead || !db.IsRead(r.Id))
                    {
                        if (!ids.Contains(r.Id))
                        {
                            rants.Add(r);
                            ids.Add(r.Id);
                        }
                    }
                }

                page++;
            }

            feeds.Clear();
            
            foreach (var rant in rants)
            {
                ViewModels.Rant r = new ViewModels.Rant(rant);
                feeds.Add(r);
            }

            if (collection.Count > 0 && collection.ContainsKey(NotificationCount))
            {
                int count = Convert.ToInt32(collection[NotificationCount]);
                UpdateNotifications(new NotificationsChecker.UpdateArgs(count, count));
            }

            UpdateFollowedInRants();

            feedView.SortDescriptions.Clear();
            
            currentSection = type;

            UpdateStatus("Loaded " + rants.Count + " rants");
        }
        
        /// <summary>
        /// Updates the Followed property in the rants, which is also shown in the View
        /// </summary>
        private void UpdateFollowedInRants()
        {
            var followed = ds.FollowedUsers;

            foreach (ViewModels.Rant rant in feeds)
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
