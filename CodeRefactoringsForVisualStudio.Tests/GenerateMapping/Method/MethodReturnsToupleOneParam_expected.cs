﻿using System;
using System.Collections.Generic;
using System.Text;

namespace CodeRefactoringsForVisualStudio.Tests.GenerateMapping.Data
{
    class Foo
    {
        public int A { get; set; }
        public string B { get; set; }


        (int a, string b) Fuu(Foo source)
        {
            var result = (source.A, source.B);
            return result;
        }
    }
}