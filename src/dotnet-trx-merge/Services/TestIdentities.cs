namespace dotnet_trx_merge.Services
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

    internal record TestIdentity
    {
        public TestIdentity(string testId, string testName)
        {
            TestId = testId;
            TestName = testName;
        }

        public string TestId { get; }
        public string TestName { get; }
    }



    internal record TestEntry
    {
        public TestEntry(string testId, string executionId)
        {
            TestId = testId;
            ExecutionId = executionId;
        }

        public string TestId { get; }
        public string ExecutionId { get; }
    }
}