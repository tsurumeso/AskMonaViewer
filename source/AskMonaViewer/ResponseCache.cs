using AskMonaViewer.Api;

namespace AskMonaViewer
{
    public class ResponseCache
    {
        public Topic Topic;
        public string Html;

        public ResponseCache()
        {
            Topic = new Topic();
            Html = "";
        }

        public ResponseCache(Topic topic, string html)
        {
            Topic = topic;
            Html = html;
        }
    }
}
