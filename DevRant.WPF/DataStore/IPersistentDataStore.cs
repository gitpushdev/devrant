﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevRant.WPF.DataStore
{
    public interface IPersistentDataStore
    {
        void Close();

        string DBPath { get; }
        
        void MarkRead(int postId);
        bool IsRead(int postId);

        void RemoveDraft(long id);
        void AddDraft(SavedPostContent pc);
        int GetNumberOfDrafts();
        List<SavedPostContent> GetDrafts();
        SavedPostContent GetDraft(long id);
        void UpdateDraft(SavedPostContent data);


        List<long> GetSubscribedRantIds();
        bool IsSubscribed(int postId);
        void Subscribe(int postId);
        void Unsubscribe(int postId);

    }
}
