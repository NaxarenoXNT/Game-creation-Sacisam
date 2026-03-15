namespace Console.Core
{
    public readonly struct CommandResult
    {
        public bool Success { get; }
        public string Message { get; }

        public CommandResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public static CommandResult Ok(string message) => new CommandResult(true, message);
        public static CommandResult Fail(string message) => new CommandResult(false, message);
    }
}
