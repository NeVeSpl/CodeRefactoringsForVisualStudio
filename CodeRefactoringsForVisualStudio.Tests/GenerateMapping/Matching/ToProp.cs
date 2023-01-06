using System;
using System.Collections.Generic;
using System.Text;

namespace CodeRefactoringsForVisualStudio.Tests.GenerateMapping.Data
{
    class Foo
    {
        private int a;
        private double _b;

        public int A
        {
            get { return a; }
            set { a = value; }
        }
        public double B
        {
            get => _b;
            set => _b = value;
        }
        public string C { get; private set; }
        public string D { get; init set; }


        [|void Fuu(Input other)|]
        {
             
        }
    }
    class Input
    {
        public int A { get; set; }
        public double B { get; set; }
        public string C { get; set; }
        public string D { get; set; }
    }
}