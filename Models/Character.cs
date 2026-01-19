using System;

namespace MovieMake.Models
{
    public class Character
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public string? DefaultVoiceId { get; set; }
        public string? ImageBasePath { get; set; }
    }
}
