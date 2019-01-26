using System;
using System.Collections.Generic;
using System.Text;

namespace CodeRefactoringsForVisualStudio.Tests.InvertAssignmentDirection.Data
{
    class Foo
    {
        public int FiledMember;
        public int PropertyMember { get; set; }
    }


    class ShouldInvertAssignmentWithMemberAccessOnOneSide
    {
        void Foo()
        {
            var foo = new Foo();
            int test = 666;

            test = foo.PropertyMember;
        }
    }
}
