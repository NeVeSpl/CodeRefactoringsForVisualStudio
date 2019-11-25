using System;
using System.Collections.Generic;
using System.Text;
using ConvertToFullWPFProperty;
using NUnit.Framework;

namespace CodeRefactoringsForVisualStudio.Tests.ConvertToFullWPFProperty
{
    public class FieldNameGeneratorTests
    {
        [TestCase("Foo", 'm', "mFoo")]
        [TestCase("foo", 'm', "mFoo")]
        [TestCase("fooBar", 'm', "mFooBar")]
        public void ShouldCreateValidFieldNameWithPrefixFromNameAndPrefix(string name, char prefix, string expectedResult)
        {
            string result = FieldNameGenerator.Generate(name, prefix);

            Assert.AreEqual(expectedResult, result);
        }

        [TestCase("foo", "foo")]
        [TestCase("Alice", "alice")]
        [TestCase("fooBar", "fooBar")]
        public void ShouldCreateValidFieldNameFromName(string name, string expectedResult)
        {
            string result = FieldNameGenerator.Generate(name);

            Assert.AreEqual(expectedResult, result);
        }

        [TestCase(null, "")]
        [TestCase("", "")]
        [TestCase(" ", "")]
        public void ShouldReturnEmptyStringForNullOrEmptyName(string name, string expectedResult)
        {
            string result = FieldNameGenerator.Generate(name);

            Assert.AreEqual(expectedResult, result);
        }
    }
}
