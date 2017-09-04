using System.Collections.Generic;

namespace AskMonaViewer.Settings
{
    public class MainFormSettings : FormSettings
    {
        public bool IsHorizontal { get; set; }
        public int VSplitterDistance { get; set; }
        public int HSplitterDistance { get; set; }
        public int CategoryId { get; set; }
        public List<int> TabTopicList { get; set; }

        public MainFormSettings()
        {
            TabTopicList = new List<int>();
        }
    }
}
