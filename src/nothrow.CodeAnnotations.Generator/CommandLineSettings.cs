using System;
using System.Runtime.ExceptionServices;

namespace code_annotations.Generator
{
    internal class CommandLineSettings
    {
        
        public CommandLineSettings(string[] args)
        {
            try
            {
                int state = 0;
                for (int i = 0; i < args.Length; i++)
                {
                    var arg = args[i];
                    switch (state)
                    {
                        case 0:

                            switch (arg)
                            {
                                case "-h":
                                case "-?":
                                case "/h":
                                case "/?":
                                    ShowHelp = true;
                                    return;
                                case "scaffold":
                                    Task = "scaffold";
                                    state = 1;
                                    break;
                                default:
                                    throw new CommandLineSettingsInvalidException($"Unexpected argument '{arg}'");
                            }

                            break;
                        case 1: // scaffold
                            switch (arg)
                            {

                                case "-s":
                                    state = 2;
                                    break;
                                case "-i":
                                    state = 3;
                                    break;
                                default:
                                    throw new CommandLineSettingsInvalidException($"Unexpected argument '{arg}'");
                            }
                            break;
                        case 2:
                            ScaffoldingDirectory = arg;
                            state = 1;
                            break;
                        case 3:
                            InputAssembly = arg;
                            state = 1;
                            break;
                        default:
                            throw new InvalidOperationException();
                    }
                }

                if (state == 2 || state == 3)
                    throw new CommandLineSettingsInvalidException("Missing argument.");
            }
            catch (Exception ex)
            {
                _valid = ex;
            }
        }

        private readonly Exception _valid;

        public bool ShowHelp { get; }
        public string Task { get; }
        public string InputAssembly { get; }
        public string ScaffoldingDirectory { get; } = "out";

        public void AssertValid()
        {
            if (_valid != null)
                ExceptionDispatchInfo.Capture(_valid).Throw();
        }
    }
}