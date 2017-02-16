﻿using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DevRant.WPF
{

    //TODO: Move libs into project/GitHub

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel vm;

        public MainWindow()
        {
            vm = new MainWindowViewModel(this);
            this.DataContext = vm;

            InitializeComponent();
        }

        private async void RefreshFeed(object sender, RoutedEventArgs e)
        {
            var item = (ListBoxItem)SectionsListBox.SelectedItem;
            if (item != null)
            {
                await LoadFeed(item.Name);
            }
        }

        private async void SectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = (ListBox)sender;
            var item = (ListBoxItem)listBox.SelectedItem;

            if (item == null)
                return;

            await LoadFeed(item.Name);
        }

        private async Task LoadFeed(string section)
        {
            IsEnabled = false;
            await vm.LoadSection(section);
            IsEnabled = true;

            if (FeedListBox.Items.Count > 0)
                FeedListBox.ScrollIntoView(FeedListBox.Items[0]);
        }

        private void FeedListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            vm.OpenPost();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            App.Current.Shutdown();
        }

        private void StatusBar_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            vm.ShowStatusHistory();
        }

        private void VoteButton_Clicked(object sender, Controls.VoteButton.ButtonType type)
        {

        }
    }
}
