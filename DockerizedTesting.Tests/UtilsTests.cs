using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace DockerizedTesting.Tests
{
    public class UtilsTests
    {
        [Theory]
        [InlineData("dotnet")]
        [InlineData("dotnet.exe")]
        public void CommandExistsReturnsTrueWhenCommandExists(string cmd)
        {
            Assert.True(Utils.CommandExists(cmd));
        }

        [Fact]
        public void ThrowsOnNull()
        {
            Assert.Throws<ArgumentNullException>(() => Utils.CommandExists(null));
        }

        [Theory]
        [InlineData("DSFDF:@<>")]
        [InlineData("foo/bar")]
        public void ThrowsOnBadFileName(string cmd)
        {
            Assert.Throws<ArgumentException>(() => Utils.CommandExists(cmd));
        }

        [Fact]
        public void CommandExistsReturnsTrueOnError()
        {
            Assert.True(Utils.CommandExists("dotnet", null));
        }

        [Fact]
        public void CommandExistsReturnsFalseWhenCommandDoesntExist()
        {
            Assert.False(Utils.CommandExists("FBE74814-5586-4D0E-BB5A-B3EA69F9A321"));
        }
    }
}
