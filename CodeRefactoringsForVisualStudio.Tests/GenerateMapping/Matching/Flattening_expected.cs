using System;
using System.Collections.Generic;
using System.Text;

namespace CodeRefactoringsForVisualStudio.Tests.GenerateMapping.Data
{
    class Foo
    {
        public int RedId
        {
            get;
            set;
        }
        public string BlueId
        {
            get;
            set;
        }


        void Fuu(Red red, Blue blue)
        {
            this.RedId = red.Id;
            this.BlueId = blue.Id;
        }
    }


    class Red
    {
        public int Id
        {
            get;
            set;
        }
    }
    class Blue
    {
        public string Id
        {
            get;
            set;
        }
    }
}