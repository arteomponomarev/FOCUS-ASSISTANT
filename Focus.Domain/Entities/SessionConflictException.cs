namespace Focus.Domain.Entities;

public sealed class SessionConflictException(string message) : InvalidOperationException(message);
