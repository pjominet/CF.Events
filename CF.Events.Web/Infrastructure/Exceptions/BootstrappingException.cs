namespace CF.Events.Web.Infrastructure.Exceptions;

public class BootstrappingException(string details) : Exception($"Error bootstrapping application: {details}");
