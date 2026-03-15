namespace Console.Core
{
    public static class CommandParser
    {
        public static (string commandName, string[] args) Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return (string.Empty, System.Array.Empty<string>());

            string[] parts = input.Trim().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

            string commandName = parts[0].ToLowerInvariant();
            string[] args = new string[parts.Length - 1];
            System.Array.Copy(parts, 1, args, 0, args.Length);

            return (commandName, args);
        }
    }
}
