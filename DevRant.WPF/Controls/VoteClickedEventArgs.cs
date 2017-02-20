﻿using DevRant.WPF.Controls;
using DevRant.WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevRant.WPF.Controls
{
    public class VoteClickedEventArgs
    {
        public VoteButton.ButtonType Type { get; private set; }

        public delegate void CB();

        public event CB Callback;

        public FeedItem SelectedItem { get; internal set; }

        public VoteClickedEventArgs(VoteButton.ButtonType type)
        {
            Type = type;
        }

        internal void InvokeCallback()
        {
            if (Callback != null)
                Callback.Invoke();
        }
    }
}
