namespace dotnet_trx_merge.Exceptions
{
    public class MergeException : Exception
    {
        public MergeException()
        { }

        public MergeException(string? message) : base(message)
        {
        }

        public MergeException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}