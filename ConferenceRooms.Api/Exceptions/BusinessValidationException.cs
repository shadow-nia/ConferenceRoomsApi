namespace ConferenceRooms.Api.Exceptions;

public sealed class BusinessValidationException(string message) : Exception(message);
