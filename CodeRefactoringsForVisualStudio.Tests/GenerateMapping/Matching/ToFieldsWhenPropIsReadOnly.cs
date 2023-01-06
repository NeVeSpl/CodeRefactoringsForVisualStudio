using System;
using System.Collections.Generic;
using System.Text;

namespace CodeRefactoringsForVisualStudio.Tests.GenerateMapping.Data
{
    class Foo
    {
        private int a;
        private string _B;


        public int A
        {
            get { return a; }
        }
        public string B
        {
            get => _B;
        }


        [|void Fuu(Input other)|]
        {
             
        }
    }
    class Input
    {
        public int A = > 7;
        public string B => "";
    }
}