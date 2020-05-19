using System;
using System.Runtime.ExceptionServices;

namespace code_annotations.Generator
{
    internal class CommandLineSettings
    {
        private readonly Exception _valid;

        public CommandLineSettings(string[] args)
        {
            try
            {
                int state = 0;
                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i];
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
                                case "generate":
                                    Task = "generate";
                                    state = 2;
                                    break;
                                default:
                                    throw new CommandLineSettingsInvalidException($"Unexpected argument '{arg}'");
                            }

                            break;
                        case 1: // scaffold
                            switch (arg)
                            {
                                case "-s":
                                    state = 12;
                                    break;
                                case "-i":
                                    state = 13;
                                    break;
                                default:
                                    throw new CommandLineSettingsInvalidException($"Unexpected argument '{arg}'");
                            }

                            break;
                        case 2: // generate
                            switch (arg)
                            {
                                case "-s":
                                    state = 21;
                                    break;
                                case "-o":
                                    state = 22;
                                    break;
                                default:
                                    throw new CommandLineSettingsInvalidException($"Unexpected argument '{arg}'");
                            }

                            break;

                        case 12: //scaffold-s
                            AnnotationDirectory = arg;
                            state = 1;
                            break;
                        case 13: //scaffold-i
                            InputAssembly = arg;
                            state = 1;
                            break;
                        case 21: //generate-s
                            AnnotationDirectory = arg;
                            state = 2;
                            break;
                        case 22: //generate-o
                            OutputDirectory = arg;
                            state = 2;
                            break;
                        default:
                            throw new InvalidOperationException();
                    }
                }

                if (state > 10)
                {
                    throw new CommandLineSettingsInvalidException("Missing argument.");
                }
            }
            catch (Exception ex)
            {
                _valid = ex;
            }
        }

        public bool ShowHelp { get; }
        public string Task { get; }
        public string InputAssembly { get; }
        public string OutputDirectory { get; }
        public string AnnotationDirectory { get; } = "out";

        public void AssertValid()
        {
            if (_valid != null)
            {
                ExceptionDispatchInfo.Capture(_valid).Throw();
            }
        }
    }
}