namespace dotnet_trx_merge.Domain
{
    internal record TestIdentities
    {
        public TestIdentities(string testId, string testName, string executionId)
        {
            TestIdentity = new TestIdentity(testId, testName);
            TestEntryId = new TestEntry(testId, executionId);
        }

        public TestIdentity TestIdentity { get; }
        public TestEntry TestEntryId { get; }

        public override string? ToString()
        { 
            return $"{TestIdentity.TestId} - {TestEntryId.ExecutionId}";
        }
    }
}