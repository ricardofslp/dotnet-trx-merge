using System.Xml.Linq;
using dotnet_trx_merge.Domain;
using dotnet_trx_merge.Logging;

namespace dotnet_trx_merge.Services;

public class TrxFetcher : ITrxFetcher
{
    private readonly ILogger _log;
    
    public TrxFetcher(ILogger logger)
    {
        _log = logger;
    }

    public XDocument AddLatestTests(string[] filesToMerge)
        => FetchOnlyLatest(filesToMerge);

    private XDocument FetchOnlyLatest(string[] filesToMerge)
    {   
        string creation;
        string queued;
        string start;
        string end;
        var testResultDictionary = new Dictionary<TestIdentity, XElement>();
        var testDefinitionsDictionary = new Dictionary<string, XElement>();
        var testEntriesDictionary = new Dictionary<TestIdentity, XElement>();
        var outcomeDictionary = new Dictionary<string, int>();
        XNamespace ns = "";
        TestTimes testTimes = new TestTimes();
        foreach (var trxFile in filesToMerge)
        {
            var trxDocument = XDocument.Load(trxFile);
            XElement? rootElement = trxDocument.Root;
            if (rootElement == null)
                throw new Exception($"Could not find root element in file {trxFile}");
            ns = rootElement.GetDefaultNamespace();
            testTimes.AddTestTimes(rootElement.Descendants(ns+"Times").FirstOrDefault());
            var results = trxDocument.Descendants(ns + "UnitTestResult");
            var definitions = trxDocument.Descendants(ns + "UnitTest");
            var entries = trxDocument.Descendants(ns + "TestEntry");
            _log.Debug($"Found {results?.Count()} tests in file {trxFile}");
            foreach (var unitTestResult in results)
            {
                TestIdentities testId = new TestIdentities(unitTestResult.Attribute("testId")!.Value, unitTestResult.Attribute("testName")!.Value, unitTestResult.Attribute("executionId")!.Value);
                var endTime = DateTime.Parse(unitTestResult.Attribute("endTime")?.Value!);

                if (!testResultDictionary.ContainsKey(testId.TestIdentity) || DateTime.Parse(testResultDictionary[testId.TestIdentity].Attribute("endTime")!.Value) < endTime)
                {
                    if (testResultDictionary.ContainsKey(testId.TestIdentity))
                        outcomeDictionary[testResultDictionary[testId.TestIdentity].Attribute("outcome")!.Value.ToLower()]--;
                    
                    testResultDictionary[testId.TestIdentity] = unitTestResult;
                    testDefinitionsDictionary[testId.TestIdentity.TestId] = GetTestDefinition(definitions, testId.TestIdentity.TestId);
                    testEntriesDictionary[testId.TestIdentity] = GetTestEntry(entries, testId.TestEntryId);

                    var outcomeValue = testResultDictionary[testId.TestIdentity].Attribute("outcome")!.Value.ToLower();
                    
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

        // Add the components to the mergedDocument        
        var mergedDocument = new XDocument(new XElement(ns + "TestRun"));
        var times = new XElement(ns + "Times");
        testTimes.SetTestTimes(times);
        mergedDocument.Root!.Add(times);
        mergedDocument.Root!.Add(testResultsSection);
        mergedDocument.Root.Add(testDefinitionSection);
        mergedDocument.Root.Add(testEntriesSection);
        mergedDocument.Root.Add(CreateOutcome(outcomeDictionary));
        return mergedDocument;
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

    private XElement GetTestEntry(IEnumerable<XElement> entries, TestEntry testEntryId)
        => entries.SingleOrDefault(entry => new TestEntry(entry.Attribute("testId")!.Value, entry.Attribute("executionId")!.Value).Equals(testEntryId))!;
}