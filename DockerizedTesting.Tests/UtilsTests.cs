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

        [Fact]
        public void ReadPemKeyReturnsCorrectKey()
        {
            string testData = "-----BEGIN RSA PRIVATE KEY-----\r\n" +
                " (well not really - its not actually a key - its just crap mostly)  \n" +
                "VGhlIHF1aWNrIGJyb3duIGZveCBqdW1wcyBvdmVyIHRoZSBsYXp5IGRvZy4gClRoZW4gMyBmb3hl"+
"cyBqdW1wZWQgaW4gdG8gYSBjaGlja2VuIFBFTSBhbmQgZ290IGVuY3J5cHRlZCBpbiB0byBqa2gy\r\n"+
"ajEiIiEhJSQlKClbXXxcPj8gYW5kIGxvdHMgb2Ygb3RoZXIgcmFuZG9tIHRlc3QgZGF0YSB3aXRo\n\n"+
"-----MIDDLISH OF RSA PRIVATE KEY??-----\r\n" +
"IGZ1bmt5ZCBjaGFyYWN0ZXJzLgpFdmVuIGNyYXp5IHVuaWNvZGUgc3ltYm9scyBsaWtlIHRoYXQg\t\r" +
"cG9vciBsYWQgWCDDhiBBLTEyLiBMb2FkcyBvZiB0ZXN0IGRhdGEhIEVub3VnaCB0byBtYWtlIHN1 \n"+
"cmUgaXQgc3BhbnMgYWNyb3NzIG11bHRpcGxlIGxpbmVzIHdoZW4gZW5jb2RlZC4=\n\n\n\n"+
"\r\n\n-----END RSA NON KEY-----";

            var b64 = Utils.ReadPemKey(testData);
            string expected =
"The quick brown fox jumps over the lazy dog. \n"+
"Then 3 foxes jumped in to a chicken PEM and got encrypted in to jkh2j1\"\"!!%$%()[]|\\>? and lots of other random test data with funkyd characters.\n"+
"Even crazy unicode symbols like that poor lad X Æ A-12. Loads of test data! Enough to make sure it spans across multiple lines when encoded.";
             Assert.Equal(expected, Encoding.UTF8.GetString(b64));
        }
    }
}
