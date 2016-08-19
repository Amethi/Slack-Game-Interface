using System;
using System.ComponentModel.DataAnnotations;

namespace SlackGameInterface.Lib.Models
{
    public class ServiceConfig
    {
        [Key]
        public int Id { get; set; }
        public bool Silenced { get; set; }
        public DateTime? LastPoll { get; set; }
    }
}
