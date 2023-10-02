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
        var testResultDictionary = new Dictionary<string, XElement>();
        var testDefinitionsDictionary = new Dictionary<string, XElement>();
        var testEntriesDictionary = new Dictionary<string, XElement>();
        var outcomeDictionary = new Dictionary<string, int>();
        foreach (var trxFile in filesToMerge)
        {
            var trxDocument = XDocument.Load(trxFile);
                        
            // Strip namespaces from the document; if we don't do this, and the input .trx files have namespaces, no descendants will be found
            // and an empty output file will be generated.
            // https://stackoverflow.com/a/14865785/84898
            foreach (var xe in trxDocument.Elements().DescendantsAndSelf())
            {
                // Stripping the namespace by setting the name of the element to its localname only
                xe.Name = xe.Name.LocalName;
                // replacing all attributes with attributes that are not namespaces and their names are set to only the localname
                xe.ReplaceAttributes((from xattrib in xe.Attributes().Where(xa => !xa.IsNamespaceDeclaration) select new XAttribute(xattrib.Name.LocalName, xattrib.Value)));
            }
            
            var results = trxDocument.Descendants("UnitTestResult");
            var definitions = trxDocument.Descendants("UnitTest");
            var entries = trxDocument.Descendants("TestEntry");
            _log.Debug($"Found {results?.Count()} tests in file {trxFile}");
            foreach (var unitTestResult in results)
            {
                var testId = unitTestResult.Attribute("testId")!.Value;
                var endTime = DateTime.Parse(unitTestResult.Attribute("endTime")?.Value!);

                if (!testResultDictionary.ContainsKey(testId) || DateTime.Parse(testResultDictionary[testId].Attribute("endTime")!.Value) < endTime)
                {
                    if (testResultDictionary.ContainsKey(testId))
                        outcomeDictionary[testResultDictionary[testId].Attribute("outcome")!.Value.ToLower()]--;
                    
                    testResultDictionary[testId] = unitTestResult;
                    testDefinitionsDictionary[testId] = GetTestDefinition(definitions, testId);
                    testEntriesDictionary[testId] = GetTestEntry(entries, testId);
                    
                    var outcomeValue = testResultDictionary[testId].Attribute("outcome")!.Value.ToLower();
                    
                    if(!outcomeDictionary.ContainsKey(outcomeValue!))
                        outcomeDictionary[outcomeValue] = 0;
                    outcomeDictionary[outcomeValue]++;
                    _log.Debug($"New result of test {testId} was found in file {trxFile}");
                }
            }
        }
        // Create a new XElement for TestResults, TestDefinitions and TestEntries
        // Add testDictionary.Values as child elements
        var testResultsSection = new XElement("Results", testResultDictionary.Values);
        var testDefinitionSection = new XElement("TestDefinitions", testDefinitionsDictionary.Values);
        var testEntriesSection = new XElement("TestEntries", testEntriesDictionary.Values);
        
        // Add the componetns to the mergedDocument
        mergedDocument.Root.Add(testResultsSection);
        mergedDocument.Root.Add(testDefinitionSection);
        mergedDocument.Root.Add(testEntriesSection);
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

    private XElement GetTestDefinition(IEnumerable<XElement> definitions, string testId)
        => definitions.SingleOrDefault(entry => entry.Attribute("id")!.Value.Equals(testId))!;

    private XElement GetTestEntry(IEnumerable<XElement> entries, string testId)
        => entries.SingleOrDefault(entry => entry.Attribute("testId")!.Value.Equals(testId))!;
}