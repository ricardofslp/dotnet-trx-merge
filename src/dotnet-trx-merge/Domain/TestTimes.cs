using System.Xml.Linq;

namespace dotnet_trx_merge.Domain
{
    internal class TestTimes
    {
        private string? earliestCreation;
        private string? earliestQueued;
        private string? earliestStart;
        private string? latestFinish;
        public void AddTestTimes(XElement? element)
        {
            if(element == null)
                return;
            string? currentCreationValue = element.Attribute("creation")?.Value;
            if (currentCreationValue != null && (earliestCreation == null || DateTime.Parse(earliestCreation) > DateTime.Parse(currentCreationValue)))
                earliestCreation = currentCreationValue;
            string? currentQueuedValue = element.Attribute("queuing")?.Value;
            if (currentQueuedValue != null && (earliestQueued == null || DateTime.Parse(earliestQueued) > DateTime.Parse(currentQueuedValue)))
                earliestQueued = currentQueuedValue;
            string? currentStartValue = element.Attribute("start")?.Value;
            if (currentStartValue != null && (earliestStart == null || DateTime.Parse(earliestStart) > DateTime.Parse(currentStartValue)))
                earliestStart = currentStartValue;
            string? currentFinishValue = element.Attribute("finish")?.Value;
            if (currentFinishValue != null && (latestFinish == null || DateTime.Parse(latestFinish) < DateTime.Parse(currentFinishValue)))
                latestFinish = currentFinishValue;

        }

        public void SetTestTimes(XElement element)
        {
            if (earliestCreation != null)
                element.Add(new XAttribute("creation",earliestCreation));
            if (earliestQueued != null)
                element.Add(new XAttribute("queuing", earliestQueued));
            if (earliestStart != null)
                element.Add(new XAttribute("start", earliestStart));
            if (latestFinish != null)
                element.Add(new XAttribute("finish", latestFinish));
        }
    }
}
