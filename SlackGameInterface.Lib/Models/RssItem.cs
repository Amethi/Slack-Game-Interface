using System;
using System.ComponentModel.DataAnnotations;

namespace SlackGameInterface.Lib.Models
{
    public class RssItem
    {
        [Key]
        public string Url { get; set; }
        public DateTime? LastItemCreated { get; set; }
    }
}