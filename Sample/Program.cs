using System;
using System.Threading;
using System.IO;
using TracerX;
using System.Diagnostics;
using System.Reflection;

namespace Sample
{
    // Demonstrate basic features of the TracerX logger.
    class Program
    {
        // Declare a Logger instance for use by this class.
        private static readonly Logger Log = Logger.GetLogger("Program");

        // Just one way to initialize TracerX.
        private static bool LogFileOpened = InitLogging();

        // Initialize the TracerX logging system.
        private static bool InitLogging()
        {
            // Give threads a name when feasible.  Either of 
            // the following methods work (only one is needed).
            // The first is "safer", but only makes the name 
            // known to TracerX.
            Logger.ThreadName = "Main Thread";
            Thread.CurrentThread.Name = "Main Thread";

            // This is optional, but you can apply configuration
            // settings from an XML file.
            Logger.Xml.Configure("LoggerConfig.xml");

            // This override some of settings loaded from LoggerConfig.xml.
            Logger.DefaultBinaryFile.Name = "SampleLog";
            Logger.DefaultBinaryFile.MaxSizeMb = 10;
            Logger.DefaultBinaryFile.CircularStartSizeKb = 1;

            // Open the output file.
            return Logger.DefaultBinaryFile.Open();
        }

        static void Main(string[] args)
        {
            Log.Debug("A message logged at stack depth = 0.");

            using (Log.InfoCall())
            {
                // This demonstrates how to log the value of a property, field, or 
                // method call by passing one or more lambda expressions.
                Log.Info("Some lambda expressions...");
                Log.Info(() => Environment.TickCount,
                         () => Environment.Version,
                         () => Environment.UserInteractive,
                         () => Assembly.GetEntryAssembly(),
                         () => AppDomain.CurrentDomain.FriendlyName,
                         () => AppDomain.CurrentDomain.BaseDirectory
                );

                Log.Info("A message \nwith multiple \nembedded \nnewlines.");

                Log.Info(@"~!@#$%^&*()_+{}|:”<>?/.,;’[]\=-±€£¥√∫©®™¬¶Ω∑");

                Helper.Foo();
                TestLabmdas("Value of parameter");
                Recurse(0, 260);
            }

            Log.Debug("Another message logged at stack depth = 0.");
        }

        static void TestLabmdas(string parameter)
        {
            TestClass instance = new TestClass();
            string localVar = "Value of localVar";
            int anIntegerWithAnUnusuallyLongName = 1234;

            // The logger takes one or more lambda expressions and constructs a string
            // containing both the body of the expression and its value.  For example,
            //    Logger.PrtVar(() => localVar)
            // will return 
            //    localVar = "Value of localVar"
            // The body of the expression can be just about anything that yields a value, 
            // such as a method call.

            Log.Info(() => parameter, () => anIntegerWithAnUnusuallyLongName, () => localVar);

            Log.Info(() => localVar.Trim());
            Log.Info(() => localVar.Trim().Length);
            Log.Info(() => localVar.Trim().Length * 2);
            Log.Info(() => instance);
            Log.Info(() => instance.InstanceFieldMember);
            Log.Info(() => instance.InstancePropertyMember);
            Log.Info(() => TestClass.StaticFieldMember);
            Log.Info(() => TestClass.StaticPropertyMember);

            localVar = null;
            Log.Info(() => localVar);
            Log.Info(() => localVar.Length);
            Log.Info(() => null);
            Log.Info(() => 123);
            Log.Info(() => "literal");
        }

        // Recursive method for testing deeply nested calls.
        private static void Recurse(int i, int max)
        {
            using (Log.InfoCall("R " + i))
            {
                Log.Info("Depth = ", i);
                if (i == max) return;
                else Recurse(i + 1, max);
            }
        }
    }

    class Helper
    {
        // Declare a Logger instance for use by this class.
        private static readonly Logger Log = Logger.GetLogger("Helper");

        public static void Foo()
        {
            using (Logger.Current.DebugCall())
            {
                for (int i = 0; i < 1000; ++i)
                {
                    Log.Debug("i*i = ", i * i);
                    if (i % 9 == 0)
                    {
                        Bar(i);
                    }
                    else if (i % 13 == 0)
                    {
                        // Call Bar in a worker thread.
                        ThreadPool.QueueUserWorkItem(new WaitCallback((object o) => Bar((int)o)), i);
                    }
                }
            }
        }

        public static void Bar(int i)
        {
            if (Thread.CurrentThread.Name == null) Thread.CurrentThread.Name = "Worker " + Logger.ThreadNumber;

            using (Log.DebugCall())
            {
                Log.Verbose("Hello from Bar, i = ", i);
                Log.DebugFormat("i*i*i = {0}", i * i * i);
                Log.Debug("System tick count = ", Environment.TickCount);
            }
        }
    }

    // Class used to test/demo the logging of static and non-static 
    // fields and properties vie lambda expressions.
    class TestClass
    {
        public TestClass()
        {
            InstancePropertyMember = "Value of InstancePropertyMember";
            InstanceFieldMember = "Value of InstanceFieldMember";
        }

        static TestClass()
        {
            StaticPropertyMember = "Value of StaticPropertyMember";
            StaticFieldMember = "Value of StaticFieldMember";
        }

        public static string StaticFieldMember;
        public string InstanceFieldMember;

        public static string StaticPropertyMember
        {
            get;
            set;
        }

        public string InstancePropertyMember
        {
            get;
            set;
        }
    }

}
