using System;

namespace SampleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            SampleLibrary1.SampleClass1 sampleClass1 = new SampleLibrary1.SampleClass1();
            sampleClass1.SampleMethod1("AAA", "BBB");
            sampleClass1.SampleMethod1("CCC", "BBB");
            sampleClass1.SampleMethod1("AAA", "DDD");

            SampleLibrary2.SampleClass2 sampleClass2 = new SampleLibrary2.SampleClass2();
            sampleClass2.SampleMethod2(5);
            sampleClass2.SampleMethod2(50);
            sampleClass2.SampleMethod2(12);
        }
    }
}
