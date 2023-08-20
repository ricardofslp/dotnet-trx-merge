using System.Xml.Linq;
using dotnet_trx_merge.Logging;

namespace dotnet_trx_merge.Services;

public class TrxFetcher : ITrxFetcher
{
    private readonly ILogger _log;
    
    public TrxFetcher(ILogger logger)
    {
        _log = logger;
    }

    public void AddLatestTests(XDocument mergedDocument, string[] filesToMerge)
        => FetchOnlyLatest(mergedDocument, filesToMerge);

    private void FetchOnlyLatest(XDocument mergedDocument, string[] filesToMerge)
    {
        var testDictionary = new Dictionary<string, XElement>();
        var outcomeDictionary = new Dictionary<string, int>();
        foreach (var trxFile in filesToMerge)
        {
            var trxDocument = XDocument.Load(trxFile);
            var results = trxDocument.Descendants("UnitTestResult");
            _log.Debug($"Found {results?.Count()} tests in file {trxFile}");
            foreach (var unitTestResult in results)
            {
                var testId = unitTestResult.Attribute("testId")!.Value;
                var endTime = DateTime.Parse(unitTestResult.Attribute("endTime")?.Value!);

                if (!testDictionary.ContainsKey(testId) || DateTime.Parse(testDictionary[testId].Attribute("endTime")!.Value) < endTime)
                {
                    if (testDictionary.ContainsKey(testId))
                        outcomeDictionary[testDictionary[testId].Attribute("outcome")!.Value.ToLower()]--;
                    
                    testDictionary[testId] = unitTestResult;
                    var outcomeValue = testDictionary[testId].Attribute("outcome")!.Value.ToLower();
                    
                    if(!outcomeDictionary.ContainsKey(outcomeValue!))
                        outcomeDictionary[outcomeValue] = 0;
                    outcomeDictionary[outcomeValue]++;
                    _log.Debug($"New result of test {testId} was found in file {trxFile}");
                }
            }
        }
        // Create a new XElement for TestResults and add testDictionary.Values as child elements
        var testResultsSection = new XElement("Results", testDictionary.Values);
        
        // Add the testResultsSection to the mergedDocument
        mergedDocument.Root.Add(testResultsSection);
        mergedDocument.Root.Add(CreateOutcome(outcomeDictionary));
    }

    private XElement CreateOutcome(Dictionary<string, int> outcomes)
    {
        var resultSummaryElement = new XElement("ResultSummary");
        var countersElement = new XElement("Counters");
        // Define the default attribute values
        var attributes = new Dictionary<string, int>
        {
            { "total", 0 },
            { "passed", 0 },
            { "failed", 0 }
        };

        foreach (var outcome in outcomes)
        {
            attributes[outcome.Key] = outcome.Value;
            attributes["total"] += outcome.Value;
        }

        foreach (var attribute in attributes)
            countersElement.Add(new XAttribute(attribute.Key, attribute.Value));
        
        resultSummaryElement.Add(countersElement);

        // Determine the outcome based on individual test results
        string outcomeResult = "Completed"; // Default outcome

        if (outcomes.ContainsKey("failed") &&
            outcomes["failed"] > 0)
        {
            outcomeResult = "Failed";
        }
        else if ((outcomes.ContainsKey("error") && outcomes["error"]> 0) || 
                 (outcomes.ContainsKey("timeout") && outcomes["timeout"]> 0) || 
                 (outcomes.ContainsKey("aborted") && outcomes["aborted"]> 0))
        {
            outcomeResult = "Error";
        }
        else if ((outcomes.ContainsKey("inprogress") && outcomes["inprogress"]> 0) || 
                 (outcomes.ContainsKey("pending") && outcomes["pending"]> 0))
        {
            outcomeResult = "InProgress";
        }

        // Set the outcome attribute of the ResultSummary element
        resultSummaryElement.Add(new XAttribute("outcome", outcomeResult));
        
        return resultSummaryElement;
    }
}