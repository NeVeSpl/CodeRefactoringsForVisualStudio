using System;
using System.Collections.Generic;
using System.Text;
using EncapsulateFieldForWPF;
using NUnit.Framework;

namespace CodeRefactoringsForVisualStudio.Tests.EncapsulateFieldForWPF
{
    public class PropertyNameGeneratorTests
    {
        [TestCase("mFoo", "Foo")]
        [TestCase("_Foo", "Foo")]
        [TestCase("iFoo", "Foo")]
        public void ShouldCreateValidPropertyNameFromPrefixedFieldName(string filedName, string expectedResult)
        {
            string result = PropertyNameGenerator.FromFieldName(filedName);

            Assert.AreEqual(expectedResult, result);
        }

        [TestCase("foo", "Foo")]
        [TestCase("alice", "Alice")]        
        public void ShouldCreateValidPropertyNameFromNotPrefixedFieldName(string filedName, string expectedResult)
        {
            string result = PropertyNameGenerator.FromFieldName(filedName);

            Assert.AreEqual(expectedResult, result);
        }

        [TestCase(null, "")]
        [TestCase("", "")]
        [TestCase(" ", "")]
        [TestCase(" ", "")]
        public void ShouldReturnEmptyStringForNullOrEmptyFieldName(string filedName, string expectedResult)
        {
            string result = PropertyNameGenerator.FromFieldName(filedName);

            Assert.AreEqual(expectedResult, result);
        }
    }
}
