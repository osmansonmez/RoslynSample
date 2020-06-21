using System;

namespace SampleApp2
{
    class Program
    {
        static void Main(string[] args)
        {
            SampleLibrary2.SampleClass2 sampleClass2 = new SampleLibrary2.SampleClass2();
            sampleClass2.SampleMethod2(5);
            sampleClass2.SampleMethod2(25);
            sampleClass2.SampleMethod2(12);

            SampleLibrary3.SampleClass3 sampleClass3 = new SampleLibrary3.SampleClass3();
            sampleClass3.SampleMethod3(5, 25.5M);
            sampleClass3.SampleMethod3(5, 32.5M);
        }
    }
}
