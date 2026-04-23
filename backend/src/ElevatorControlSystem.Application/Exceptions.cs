namespace ElevatorControlSystem.Application;

public abstract class ApplicationExceptionBase : Exception
{
    protected ApplicationExceptionBase(string message)
        : base(message)
    {
    }
}

public sealed class NotFoundApplicationException : ApplicationExceptionBase
{
    public NotFoundApplicationException(string message)
        : base(message)
    {
    }
}

public sealed class ValidationApplicationException : ApplicationExceptionBase
{
    public ValidationApplicationException(string message)
        : base(message)
    {
    }
}

public sealed class ConflictApplicationException : ApplicationExceptionBase
{
    public ConflictApplicationException(string message)
        : base(message)
    {
    }
}

public sealed class UnauthorizedApplicationException : ApplicationExceptionBase
{
    public UnauthorizedApplicationException(string message)
        : base(message)
    {
    }
}

public sealed class ForbiddenApplicationException : ApplicationExceptionBase
{
    public ForbiddenApplicationException(string message)
        : base(message)
    {
    }
}
