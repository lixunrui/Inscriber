using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LXR.Recorder;
using System.Threading;

namespace LogTester
{
    class Program
    {
        static LogParameters LoggingParameters()
        {
            LogParameters result = new DefaultParameters();
            result.LogsDirectory = @"D:\LALALA\";
                return result;
        }

        static void Main(string[] args)
        {
            bool exit = false;
            string input = null;
            if (args.Length>0)
            {
                input = args[0].ToLower();
            }
            
            do 
            {
                if (input == null)
                {
                    ShowHelp();
                    Console.WriteLine("Next...");
                    input = Console.ReadLine();
                }
                if (input != null)
                {
                    input.Replace("/", "");
                    Console.WriteLine("Input is :{0}", input);

                    switch (input.Substring(0, 1))
                    {
                        case "h":
                            ShowHelp();
                            break;
                        case "n":
                            StartNormalTest();
                            break;
                        case "e":
                            StartExceptionTest();
                            break;
                        case "q":
                            exit = true;
                            break;
                    }
                }
                // clear the input
                input = null;
               
            } while (!exit);
            
            Console.WriteLine("Testing completed...");
            Console.ReadKey();
        }

        static void ShowHelp()
        {
            System.Console.WriteLine("Tester log file V0.1");
            System.Console.WriteLine("");
            System.Console.WriteLine("Tester [option] file_pattern");
            System.Console.WriteLine("");
            System.Console.WriteLine("/n  continuous display until interrupt key(Enter)");
            System.Console.WriteLine("/e  continuous display until interrupt key(Enter)");
            System.Console.WriteLine("/q  quit the program");
            System.Console.WriteLine("/h  help");
        }

        static void StartNormalTest()
        {
            Console.WriteLine("Press any key to stop the test.");
            Console.WriteLine("Start normal testing...");
            InitRecorder().TestLogging();
            Console.WriteLine("Normal Test completed...");
        }

        static void StartExceptionTest()
        {
            Console.WriteLine("Press any key to stop the test.");
            Console.WriteLine("Start exception testing...");
            InitRecorder().TestException();
            Console.WriteLine("Exception Test completed...");
        }

        static Tester InitRecorder()
        {
            LXR.Recorder.Recorder TestLog = new LXR.Recorder.Recorder("TestLogging", LoggingParameters());
            Tester t = new Tester(TestLog);
            Console.WriteLine(@"Log will be saved at: D:\LALALA\");
            return t;
        }
    }

    internal class Tester
    {
        LXR.Recorder.Recorder _log;
        Boolean _StopTesting;

        internal Tester(LXR.Recorder.Recorder log)
        {
            _log = log;
            _StopTesting = false;
        }

        internal void TestLogging()
        {
            Thread stopSign = new Thread(StopTesting);
            stopSign.Start();

            double longNumber = 0;
            do 
            {
                if (longNumber >= Double.MaxValue)
                {
                    longNumber = 0;
                }
                
                longNumber++;

                _log.LogNormal(System.Reflection.Assembly.GetCallingAssembly().ManifestModule.ToString(), longNumber.ToString());

            } while (!_StopTesting);

        }

        internal void TestException()
        {
            Thread stopSign = new Thread(StopTesting);
            stopSign.Start();

            LXR.Recorder.MyException e;
            double longNumber = 0;
            do
            {
                if (longNumber >= Double.MaxValue)
                {
                    longNumber = 0;
                }

                longNumber++;
                 e = new LXR.Recorder.MyException(longNumber.ToString());

                _log.TestLogException(e);

            } while (!_StopTesting);
            
        }

        void StopTesting()
        {
            Console.ReadKey();
            _StopTesting = true;
        }
    }
}
