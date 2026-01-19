using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MovieMake.Models
{
    public class Script
    {
        public string Title { get; set; } = "Untitled Script";
        public List<Character> Characters { get; set; } = new List<Character>();
        public ObservableCollection<Line> Lines { get; set; } = new ObservableCollection<Line>();
    }
}
