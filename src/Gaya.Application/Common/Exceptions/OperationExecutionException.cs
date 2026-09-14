namespace Gaya.Application.Common.Exceptions;

public class OperationExecutionException : Exception
{
    public OperationExecutionException(string message) : base(message) { }
    public OperationExecutionException(string message, Exception innerException) : base(message, innerException) { }
}
