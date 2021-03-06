﻿using Newtonsoft.Json;
using System.Collections.Generic;

namespace DevRant.Dtos
{
    /// <summary>
    /// Represents a data-transfer-object for a devrant profile.
    /// </summary>
    public class Profile : HasAvatar
    {
        /// <summary>
        /// Represents the username of the profile.
        /// </summary>
        [JsonProperty("username")]
        public string Username { get; set; }

        /// <summary>
        /// Represents the location of the profile.
        /// </summary>
        [JsonProperty("location")]
        public string Location { get; set; }

        /// <summary>
        /// Represents the about message of the profile.
        /// </summary>
        [JsonProperty("about")]
        public string About { get; set; }

        /// <summary>
        /// Represents the Github name of the profile.
        /// </summary>
        [JsonProperty("github")]
        public string Github { get; set; }

        /// <summary>
        /// Represents the skills of the profile.
        /// </summary>
        [JsonProperty("skills")]
        public string Skills { get; set; }

        /// <summary>
        /// Represents the score of the profile.
        /// </summary>
        [JsonProperty("score")]
        public int Score { get; set; }

        /// <summary>
        /// Stores the Rants for Rants, Upvoted, Viewed, and Favorited
        /// </summary>
        public List<Rant> Rants { get; set; }

        /// <summary>
        /// Comments
        /// </summary>
        public List<Comment> Comments { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int RantsCount { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int UpvotedCount { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int CommentsCount { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int ViewedCount { get; set; }        
        /// <summary>
        /// 
        /// </summary>
        public int FavoritesCount { get; set; }

        /// <summary>
        /// The image name for the avatar
        /// PNG = full
        /// JPG = head only
        /// </summary>
        public string AvatarImage { get; internal set; }
    }
}
