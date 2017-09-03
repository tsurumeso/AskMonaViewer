namespace AskMonaViewer.Settings
{
    public class Options
    {
        public double FirstButtonMona { get; set; }
        public double SecondButtonMona { get; set; }
        public double ThirdButtonMona { get; set; }
        public double ForthButtonMona { get; set; }
        public bool AlwaysSage { get; set; }
        public bool AlwaysNonAnonymous { get; set; }

        public Options()
        {
            FirstButtonMona = 0.3939;
            SecondButtonMona = 0.003939;
            ThirdButtonMona = 0.114114;
            ForthButtonMona = 0.00114114;
            AlwaysSage = false;
            AlwaysNonAnonymous = false;
        }
    }
}
