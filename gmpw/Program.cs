using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;
using CommandLine;
using CommandLine.Text;

namespace gmpw
{
    class GMPWException : Exception
    {
        public GMPWException(String message) :
            base(message)
        {
        }
    }

    class Options
    {
        public const String commandOptionLength = "length";
        public const String commandOptionDigit = "digit";
        public const String commandOptionUpper = "upper";
        public const String commandOptionLower = "lower";
        public const String commandOptionAvoidAmbiguous = "avoid-ambiguous";

        [Option('n', commandOptionLength, Required = true, HelpText = "Length of password, must between 3-20")]
        public int Length { get; set; }

        [Option('d', commandOptionDigit, Required = false, DefaultValue = -1, HelpText = "Number of digits")]
        public int NumberOfDigit { get; set; }

        [Option('u', commandOptionUpper, Required = false, DefaultValue = -1, HelpText = "Number of upper case letters")]
        public int NumberOfUpper { get; set; }

        [Option('l', commandOptionLower, Required = false, DefaultValue = -1, HelpText = "Number of lower case letters")]
        public int NumberOfLower { get; set; }

        [Option('a', commandOptionAvoidAmbiguous, Required = false, DefaultValue = "true", HelpText = "Avoid chars \'oO0l1\'")]
        public String AvoidAmbiguous { get; set; }

        [HelpOption]
        public String GetUsage()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(asm.Location);
            var help = new HelpText {
                Heading = new HeadingInfo(fvi.ProductName, String.Format("{0}.{1}", fvi.ProductMajorPart, fvi.ProductMinorPart)),
                Copyright = new CopyrightInfo(fvi.CompanyName, 2013),
                AdditionalNewLineAfterOption = true,
                AddDashesToOption = true
            };
            help.AddPreOptionsLine("Usage: gmpw -n10");
            help.AddOptions(this);
            return help;
        }
    }

    class Program
    {
        static String myPassword = "";

        static void ThrowInvalidOptionArgument(String longOption, int argument)
        {
            throw new GMPWException(String.Format("Invalid option argument. Option \"{0}\", Arugment \"{1}\".", longOption, argument));
        }

        static void ThrowInvalidOptionArgument(String longOption, string argument)
        {
            throw new GMPWException(String.Format("Invalid option argument. Option \"{0}\", Arugment \"{1}\".", longOption, argument));
        }

        static void AssertFailure()
        {
            throw new GMPWException("Assertion failure");
        }

        static bool ShouldAvoidAmbiguous(Options options, char letter)
        {
            bool shouldAvoidAmbiguous = true;
            if (Boolean.TryParse(options.AvoidAmbiguous, out shouldAvoidAmbiguous))
                if (shouldAvoidAmbiguous)
                {
                    Regex ambiguous = new Regex("[oO0l1]+");
                    return ambiguous.IsMatch(String.Format("{0}", letter));
                }
            else
                ThrowInvalidOptionArgument(Options.commandOptionAvoidAmbiguous, options.AvoidAmbiguous);
            
            return false;
        }

        static void GenerateMyPassword(Options options)
        {
            if (options.Length < 3 || options.Length > 20)
            {
                throw new GMPWException("Need a number between [3-20] as argument.");
            }

            List<KeyValuePair<int, String>> dulList = new List<KeyValuePair<int, String>>();
            dulList.Add(new KeyValuePair<int, String>(options.NumberOfDigit, Options.commandOptionDigit));
            dulList.Add(new KeyValuePair<int, String>(options.NumberOfUpper, Options.commandOptionUpper));
            dulList.Add(new KeyValuePair<int, String>(options.NumberOfLower, Options.commandOptionLower));
            dulList.Sort((x, y) => y.Key - x.Key);

            Dictionary<String, int> dulMap = new Dictionary<String, int>();

            Random rand = new Random(Environment.TickCount);
            int dulListIter = 0;
            dulList.ForEach(
                    delegate(KeyValuePair<int, String> x) {
                        int usedCapacity = dulMap.Aggregate(0, (sum, value) =>
                        {
                            sum += value.Value;
                            return sum;
                        });

                        if (usedCapacity > options.Length)
                            AssertFailure();

                        if (x.Key == -1)
                        {
                            int number = 0;
                            if (dulListIter == dulList.Count - 1)
                                number = options.Length - usedCapacity;
                            else
                                number = rand.Next(options.Length - usedCapacity);

                            dulMap.Add(x.Value, number);
                        }
                        else
                        {
                            if ((x.Key < -1) || (x.Key > options.Length - usedCapacity))
                                ThrowInvalidOptionArgument(x.Value, x.Key);

                            if (dulListIter == dulList.Count - 1)
                                if (usedCapacity + x.Key != options.Length)
                                    ThrowInvalidOptionArgument(Options.commandOptionLength, options.Length);

                            dulMap.Add(x.Value, x.Key);
                        }

                        ++ dulListIter;
                    }
                );


            String pwInfo = String.Format("numbers : {0}\nlower letters: {1}\nupper letters: {2}", 
                                                        dulMap[Options.commandOptionDigit],
                                                        dulMap[Options.commandOptionLower],
                                                        dulMap[Options.commandOptionUpper]);

            StringBuilder pwBuilder = new StringBuilder();
            while (dulMap[Options.commandOptionDigit] > 0 || 
                        dulMap[Options.commandOptionUpper] > 0 || 
                        dulMap[Options.commandOptionLower] > 0)
            {
                int picker = rand.Next(3);
                switch (picker)
                {
                    case 0:
                        if (dulMap[Options.commandOptionDigit] > 0)
                        {
                            Char c = rand.Next(10).ToString().ElementAt(0);
                            if (!ShouldAvoidAmbiguous(options, c))
                            {
                                pwBuilder.Append(c);
                                --dulMap[Options.commandOptionDigit];
                            }
                        }
                        break;
                    case 1:
                        if (dulMap[Options.commandOptionLower] > 0)
                        {
                            Char c = (Char)(rand.Next(26) + 97);
                            if (!ShouldAvoidAmbiguous(options, c))
                            {
                                pwBuilder.Append(c);
                                -- dulMap[Options.commandOptionLower];
                            }
                        }
                        break;
                    case 2:
                        if (dulMap[Options.commandOptionUpper] > 0)
                        {
                            Char c = (Char)(rand.Next(26) + 65);
                            if (!ShouldAvoidAmbiguous(options, c))
                            {
                                pwBuilder.Append(c);
                                -- dulMap[Options.commandOptionUpper];
                            }
                        }
                        break;
                    default:
                        AssertFailure();
                        break;
                }
            }

            myPassword = pwBuilder.ToString();
            Console.WriteLine(pwInfo);
            Console.WriteLine(myPassword);
        }

        static void Main(string[] args)
        {
            Options opts = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, opts))
            {
                try
                {
                    GenerateMyPassword(opts);
                }
                catch (Exception exp)
                {
                    Console.WriteLine(exp.Message);
                    Console.WriteLine(opts.GetUsage());
                }
            }
            
            Console.ReadKey();
        }
    }
}
