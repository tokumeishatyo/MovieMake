using System;

namespace MovieMake.Models
{
    public class Line
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string CharacterId { get; set; } = "";
        public string Text { get; set; } = "";
    }
}
