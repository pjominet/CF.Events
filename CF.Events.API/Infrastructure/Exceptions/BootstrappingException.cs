namespace CF.Events.API.Infrastructure.Exceptions;

public class BootstrappingException(string details) : Exception($"Error bootstrapping application: {details}");
