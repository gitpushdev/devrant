﻿using Innouvous.Utils.Merged45.MVVM45;
using System.Windows;
using System.Windows.Input;
using mvvm = Innouvous.Utils.MVVM;
using System;
using Innouvous.Utils;
using DevRant.Dtos;
using System.Linq;
using System.IO;
using DevRant.WPF.DataStore;
using DevRant.WPF.ViewModels;
using System.Text;

namespace DevRant.WPF
{
    internal class EditPostWindowViewModel : ViewModel
    {
        private Window window;
        private EditPostWindow.Type type;

        private IDevRantClient api;
        private IPersistentDataStore db;
        private Draft existing;
        private Commentable parent;
        private FeedItem editing;

        private enum Mode
        {
            NewRant,
            NewComment,
            EditDraft,
            EditExisting
        }

        private Mode mode = Mode.NewRant;

        public EditPostWindowViewModel(Window window, EditPostWindow.Type type, Draft existing = null, Commentable parent = null, FeedItem edit = null)
        {
            this.window = window;
            this.api = AppManager.Instance.API;
            this.db = AppManager.Instance.DB;

            this.type = type;
            this.existing = existing;
            this.parent = parent;
            this.editing = edit;

            Cancelled = true;

            if (parent != null)
            {
                mode = Mode.NewComment;

                ViewModels.Comment comment = parent as ViewModels.Comment;
                if (comment != null)
                {
                    if (AppManager.Instance.API.User.LoggedInUser != comment.Username)
                    {
                        Text = "@" + comment.Username + " ";
                    }
                }
            }
            else if (existing != null)
            {
                mode = Mode.EditDraft;

                Text = existing.Text;
                TagsString = existing.Tags;
                ImagePath = existing.ImagePath;
            }
            else if (edit != null)
            {
                mode = Mode.EditExisting;

                var r = edit.AsRant();
                var comment = edit.AsComment();
                if (r != null)
                {
                    Text = r.Text;
                    TagsString = r.TagsString;
                }
                else if (comment != null)
                {
                    Text = comment.Text;
                }
                else
                    throw new NotSupportedException();
            }
        }

        public bool Cancelled { get; private set; }

        
        public Visibility TagsVisibility
        {
            get { return mode == Mode.NewRant || mode == Mode.EditDraft ? Visibility.Visible : Visibility.Collapsed; }
        }

        public Visibility SaveDraftVisibility
        {
            get
            {
                if (type == EditPostWindow.Type.Comment)
                    return Visibility.Collapsed;
                else if (db != null)
                    return (mode == Mode.NewRant || mode == Mode.EditDraft) ? Visibility.Visible : Visibility.Collapsed;
                else
                    return Visibility.Collapsed;
            }
        }

        public string TextType
        {
            get {
                /*
                switch (mode)
                {
                    case Mode.EditDraft:
                    case Mode.NewRant:
                        return "Rant";
                    case Mode.NewComment:
                        return "Comment";
                    case Mode.EditExisting:
                        return editing.Type.ToString();
                    default:
                        throw new NotSupportedException();
                }
                */

                return type.ToString();
            }
        }
        
        public int Remaining
        {
            get {
                if (Text == null)
                    return MaxCharacters;

                int adjusted = Utilities.ReplaceNewLines(Text).Length;
                return MaxCharacters - adjusted;
            }
        }

        public int MaxCharacters
        {
            get
            {
                switch (type)
                {
                    case EditPostWindow.Type.Comment:
                        return 1000;
                    case EditPostWindow.Type.Rant:
                        return 5000;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public string WindowTitle
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                if (type == EditPostWindow.Type.Rant)
                {
                    if (existing != null)
                        sb.Append("Edit ");
                    else 
                        sb.Append("Create New");

                    sb.Append(" Rant");
                }
                else if (type == EditPostWindow.Type.Comment)
                {
                    sb.AppendLine("Add a Comment");
                }

                string str = sb.ToString();
                return str;
            }
        }

        public string Text
        {
            get { return Get<string>(); }
            set
            {
                Set(value);
                RaisePropertyChanged();
                RaisePropertyChanged("Remaining");
            }
        }

        public string TagsString
        {
            get { return Get<string>(); }
            set
            {
                Set(value);
                RaisePropertyChanged();
            }
        }

        public string ImagePath
        {
            get { return Get<string>(); }
            private set
            {
                Set(value);
                RaisePropertyChanged();
            }
        }

        public ICommand BrowseCommand
        {
            get { return new mvvm.CommandHelper(Browse); }
        }

        private void Browse()
        {
            var dlg = DialogsUtility.CreateOpenFileDialog("Select an image");
            
            var types = from i in PostContent.GetSupportedTypes() select "*." + i;
            string supported = string.Join(";", types);
            DialogsUtility.AddExtension(dlg, "Images", supported);

            dlg.ShowDialog();

            if (!string.IsNullOrEmpty(dlg.FileName))
                ImagePath = dlg.FileName;
        }

        public ICommand ClearCommand
        {
            get { return new mvvm.CommandHelper(() => ImagePath = null); }
        }

        public ICommand SaveDraftCommand
        {
            get { return new mvvm.CommandHelper(SaveDraft); }
        }

        private void SaveDraft()
        {
            try
            {
                if (string.IsNullOrEmpty(Text) || Text.Length < 5)
                    throw new Exception("Rant or comment must be more than 5 characters long.");

                SavedPostContent data = new SavedPostContent(Text);

                if (type == EditPostWindow.Type.Rant && !string.IsNullOrEmpty(TagsString))
                    data.Tags = TagsString;

                if (!string.IsNullOrEmpty(ImagePath))
                {
                    data.ImagePath = ImagePath;
                }

                if (existing == null)
                {
                    db.AddDraft(data);
                    AddedDraft = data;

                }
                else
                {
                    data.SetId(existing.ID.Value);
                    db.UpdateDraft(data);
                }
                Cancelled = false;
                window.Close();
            }
            catch (Exception e)
            {
                MessageBoxFactory.ShowError(e, owner: window);
            }
        }

        public ICommand PostCommand
        {
            get { return new mvvm.CommandHelper(Post); }
        }
        
        private async void Post()
        {
            try
            {
                string tmp = Utilities.ReplaceNewLines(Text);
                if (string.IsNullOrEmpty(tmp) || tmp.Length < 5)
                    throw new Exception("Rant or comment must be more than 5 characters long.");
                
                PostContent data = new PostContent(tmp);

                if (type == EditPostWindow.Type.Rant && !string.IsNullOrEmpty(TagsString))
                    data.SetTag(TagsString);

                if (!string.IsNullOrEmpty(ImagePath))
                {
                    byte[] bytes = File.ReadAllBytes(ImagePath);
                    data.AddImage(bytes, ImagePath);
                }

                window.IsEnabled = false;

                if (type == EditPostWindow.Type.Rant)
                {
                    
                    if (editing != null)
                    {
                        api.User.EditRant(editing.AsRant().ID, data);
                    }
                    else
                    {
                        await api.User.PostRant(data);
                    }

                    if (existing != null)
                    {
                        db.RemoveDraft(existing.ID.Value);
                    }
                }
                else if (type == EditPostWindow.Type.Comment)
                {
                    if (editing != null)
                    {
                        api.User.EditComment(editing.AsComment().ID, data);
                    }
                    else
                    {
                        await api.User.PostComment(parent.RantId, data);
                    }
                }
                
                Cancelled = false;
                window.Close();
            }
            catch (Exception e)
            {
                MessageBoxFactory.ShowError(e, owner: window);
            }
            finally
            {
                window.IsEnabled = true;
            }
        }

        public ICommand CancelCommand
        {
            get { return new mvvm.CommandHelper(() => window.Close()); }
        }

        public SavedPostContent AddedDraft { get; private set; }
    }
}