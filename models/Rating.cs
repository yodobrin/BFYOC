using System;
using Azure.AI.TextAnalytics;

namespace BFYOC.Models
{
    public class Rating
    {
        public string productId;
        public string locationName;
        public int rating;
        public string userNotes;
        public DateTime timestamp;
        public string id;
        public string userId;

        public string Sentiment;

    }
}