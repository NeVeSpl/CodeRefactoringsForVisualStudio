using System;
using System.Collections.Generic;
using System.Text;

namespace CodeRefactoringsForVisualStudio.Tests.GenerateMapping.Data
{
    class Foo
    {
        public string A { get; set; }


        [|void Fuu(Foo source)|]
        {
             
        }
    }
}