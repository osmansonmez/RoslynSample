using System;

namespace SampleLibrary1
{
    public class SampleClass1 : ISampleClass1
    {
        public string SampleVariable = "SampleData";
        public void SampleMethod1(string param1, string param2)
        {
            SampleVariable = "SampleData2";
        }

        public void SampleMethod1(ParamClass param1)
        {

        }
    }

    public class ParamClass
    {
        public string Param1 { get; set; }

        public string Param2 { get; set; }

    }
}
