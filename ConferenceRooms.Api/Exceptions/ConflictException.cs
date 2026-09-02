namespace ConferenceRooms.Api.Exceptions;

public sealed class ConflictException(string message) : Exception(message);
