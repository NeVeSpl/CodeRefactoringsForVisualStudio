using System;
using System.Collections.Generic;
using System.Text;

namespace CodeRefactoringsForVisualStudio.Tests.GenerateMapping.Data
{
    class Foo
    {
        private int a;
        private string _B;
        private double mC;


        [|void Fuu(int a, string b, double c)|]
        {
             
        }
    }
}