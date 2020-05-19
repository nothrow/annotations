using System;

namespace code_annotations.Generator
{
    internal class CommandLineSettingsInvalidException : ApplicationException
    {
        public CommandLineSettingsInvalidException(string message)
            : base(message)
        {
        }
    }
}