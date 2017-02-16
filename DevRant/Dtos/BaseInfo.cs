﻿using Newtonsoft.Json;
using System.Collections.Generic;

namespace DevRant.Dtos
{
    /// <summary>
    /// Represents the base items common to rants and comments
    /// </summary>
    public abstract class BaseInfo
    {
        /// <summary>
        /// Represents the identity of the rant.
        /// </summary>
        [JsonProperty("id")]
        public int Id { get; set; }


        #region User Info
        /// <summary>
        /// Username
        /// </summary>
        [JsonProperty("user_username")]
        public string Username { get; set; }

        /// <summary>
        /// User ID
        /// </summary>
        [JsonProperty("user_id")]
        public int UserId { get; set; }

        /// <summary>
        /// User Score
        /// </summary>
        [JsonProperty("user_score")]
        public int UserScore { get; set; }

        #endregion

        /// <summary>
        /// Create Time
        /// </summary>
        [JsonProperty("created_time")]
        public long CreatedTime { get; set; }


        /// <summary>
        /// Attached Image
        /// </summary>
        [JsonProperty("attached_image")]
        public ImageInfo Image { get; set; }

        /// <summary>
        /// Represents the text of the rant.
        /// </summary>
        [JsonProperty("text")]
        public string Text { get; set; }

        /// <summary>
        /// Represents the number of upvotes of the rant.
        /// </summary>
        [JsonProperty("num_upvotes")]
        public int NrOfUpvotes { get; set; }

        /// <summary>
        /// Vote State
        /// </summary>
        public object test;

        /// <summary>
        /// Represents the number of downvotes of the rant.
        /// </summary>
        [JsonProperty("num_downvotes")]
        public int NrOfDownvotes { get; set; }

        /// <summary>
        /// Score
        /// </summary>
        [JsonProperty("score")]
        public int Score { get; set; }
        
    }
}
