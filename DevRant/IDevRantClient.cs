﻿using System.Collections.Generic;
using System.Threading.Tasks;
using DevRant.Dtos;
using DevRant.Enums;
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Media;
using System;

namespace DevRant
{
    /// <summary>
    /// API commands to get feeds
    /// </summary>
    public interface IFeeds
    {
        /// <summary>
        /// Requests a collection of collabs
        /// </summary>
        /// <param name="limit"></param>
        /// <param name="skip"></param>
        /// <returns></returns>
        Task<IReadOnlyCollection<Collab>> GetCollabsAsync(int limit = 50, int skip = 0);

        /// <summary>
        /// Requests a collection of rants sorted and selected by the arguments from the rest-api.
        /// </summary>
        /// <param name="sort">Sorting of the rant collection.</param>
        /// <param name="range">Range of feed, only for Top</param>
        /// <param name="limit">Maximal rants to return.</param>
        /// <param name="skip">Number of rants to skip.</param>
        /// <param name="settings">If passed, will hold the values of settings that may be returned in the response</param>
        Task<IReadOnlyCollection<Rant>> GetRantsAsync(RantSort sort = RantSort.Algo, RantRange range = RantRange.Day, int limit = 50, int skip = 0, ValuesCollection settings = null);
        
        /// <summary>
        /// Requests a collection of stories 
        /// </summary>
        /// <param name="sort">Sorting of the rant collection.</param>
        /// <param name="range">Range of stories</param>
        /// <param name="limit">Maximal rants to return.</param>
        /// <param name="skip">Number of rants to skip.</param>
        /// <returns></returns>
        Task<IReadOnlyCollection<Rant>> GetStoriesAsync(RantSort sort = RantSort.Top, RantRange range = RantRange.Day, int limit = 50, int skip = 0);
    }

    /// <summary>
    /// API commands to perform actions related to the user
    /// </summary>
    public interface IUserCommands
    {
        /// <summary>
        /// 
        /// </summary>
        AccessInfo Token { get; }

        /// <summary>
        /// Returns the user's notifications
        /// </summary>
        /// <returns></returns>
        Task<List<Dtos.Notification>> GetNotificationsAsync();

        /// <summary>
        /// Mute notifications for the rant
        /// </summary>
        /// <param name="rantId"></param>
        /// <returns></returns>
        Task MuteRant(long rantId);

        /// <summary>
        /// Mute notifications for the rant
        /// </summary>
        /// <param name="rantId"></param>
        /// <returns></returns>
        Task Favorite(long rantId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rantId"></param>
        /// <returns></returns>
        Task Unfavorite(long rantId);


        /// <summary>
        /// Whether a user is logged in. Certain methods may throw an NotLoggedInException if authentication is required
        /// </summary>
        bool LoggedIn { get; }

        /// <summary>
        /// The username of the current user
        /// </summary>
        string LoggedInUser { get; }

        /// <summary>
        /// Tries to login. May throw an exception if failed.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        Task Login(string username, string password);

        /// <summary>
        /// Logs out the user if he is logged in.
        /// </summary>
        /// <returns></returns>
        Task Logout();

        /// <summary>
        /// Vote on a rant
        /// </summary>
        /// <param name="rantId"></param>
        /// <param name="vote"></param>
        /// <returns></returns>
        Task<Rant> VoteRant(long rantId, Vote vote);

        /// <summary>
        /// Vote on a rant
        /// </summary>
        /// <param name="collabId"></param>
        /// <param name="vote"></param>
        /// <returns></returns>
        Task<Collab> VoteCollab(long collabId, Vote vote);


        /// <summary>
        /// Uploads a rant
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        Task PostRant(PostContent data);
        
        /// <summary>
        /// Upload a comment to a rant
        /// </summary>
        /// <param name="rantId"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        Task PostComment(long rantId, PostContent data);
        
        /// <summary>
        /// Vote on a comment
        /// </summary>
        /// <param name="commentId"></param>
        /// <param name="vote"></param>
        /// <returns></returns>
        Task<Comment> VoteComment(long commentId, Vote vote);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="commentId"></param>
        /// <param name="post"></param>
        void EditComment(int commentId, PostContent post);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="commentId"></param>
        void DeleteComment(int commentId);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rantId"></param>
        /// <param name="post"></param>
        void EditRant(int rantId, PostContent post);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rantId"></param>
        void DeleteRant(int rantId);

    }

    /// <summary>
    /// Represents an interface which describes the public api for devrant.
    /// </summary>
    public interface IDevRantClient
    {
        /// <summary>
        /// 
        /// </summary>
        IFeeds Feeds { get; }

        /// <summary>
        /// 
        /// </summary>
        IUserCommands User { get; }

        /// <summary>
        /// Requests profile details to the rest-api.
        /// </summary>
        /// <param name="username">Username of the profile to request.</param>
        Task<Profile> GetProfileAsync(string username, ProfileContentType type = ProfileContentType.Rants, int skip = 0);

        /// <summary>
        /// Checks if user exists
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        Task<bool> IsValidUser(string username);

        /// <summary>
        /// Gets a rant by Id
        /// </summary>
        /// <param name="rantId"></param>
        /// <returns></returns>
        Task<Rant> GetRant(long rantId);


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task<Rant> SurpriseMe(Func<Rant, bool> acceptor = null);

        /// <summary>
        /// Gets the Avatar image from devRant
        /// </summary>
        /// <returns></returns>
        ImageSource GetAvatar(string imgName);
    }

}