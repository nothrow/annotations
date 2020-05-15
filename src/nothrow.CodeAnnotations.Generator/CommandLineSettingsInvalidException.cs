using System;

namespace code_annotations.Generator
{
    class CommandLineSettingsInvalidException : ApplicationException
    {
        public CommandLineSettingsInvalidException(string message)
            : base(message)
        {
        }
    }
}