namespace RunningPrograms
{
    public sealed record RunningApplication(string Executable, string Summary)
    {
        public string Label => $"{Executable} ({Summary})";
    }
}
