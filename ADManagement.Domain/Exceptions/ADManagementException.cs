namespace ADManagement.Domain.Exceptions;

/// <summary>
/// Base exception for AD Management operations
/// </summary>
public class ADManagementException : Exception
{
    public ADManagementException() { }
    
    public ADManagementException(string message) : base(message) { }
    
    public ADManagementException(string message, Exception innerException) 
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when AD connection fails
/// </summary>
public class ADConnectionException : ADManagementException
{
    public ADConnectionException(string message) : base(message) { }
    
    public ADConnectionException(string message, Exception innerException) 
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when user is not found
/// </summary>
public class UserNotFoundException : ADManagementException
{
    public UserNotFoundException(string username) 
        : base($"User '{username}' not found in Active Directory") { }
}

/// <summary>
/// Exception thrown when group is not found
/// </summary>
public class GroupNotFoundException : ADManagementException
{
    public GroupNotFoundException(string groupName) 
        : base($"Group '{groupName}' not found in Active Directory") { }
}

/// <summary>
/// Exception thrown when operation is not authorized
/// </summary>
public class UnauthorizedException : ADManagementException
{
    public UnauthorizedException(string operation) 
        : base($"Not authorized to perform operation: {operation}") { }
}

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public class ValidationException : ADManagementException
{
    public List<string> ValidationErrors { get; }
    
    public ValidationException(List<string> errors) 
        : base("Validation failed")
    {
        ValidationErrors = errors;
    }
    
    public ValidationException(string error) 
        : base("Validation failed")
    {
        ValidationErrors = new List<string> { error };
    }
}
