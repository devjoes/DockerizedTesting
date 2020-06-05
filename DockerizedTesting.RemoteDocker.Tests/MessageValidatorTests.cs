using DockerizedTesting.Models;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using System;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace DockerizedTesting.RemoteDocker.Tests
{
    public class MessageValidatorTests
    {
        const string TestPrivateKey = 
            "-----BEGIN RSA PRIVATE KEY-----\n" +
            "MIIEowIBAAKCAQEA7N6ERrUYl1IGszUMCT6BTvSg1J3rJmA63dqquDnIBF6NAsOI\n" +
            "iBiKrarPdRkQ5P9XieGCWUxoqrYLU9b5DOLPiE1u4QkOKQ5OlqAgqyYF4D09DraP\n" +
            "/jmQHRr/fygBp2XTb9yAHCwq5ESpD7ViPr/G/O0C2Rx+LniW8zWjpvfq/RlgW2Vy\n" +
            "bJW1mn4XPrBDxYD1nBTzPZrvkRpZ80/CxP0llY+Jc6q0YTRLrqF23DdbNos7x0kn\n" +
            "N3vgZ+D9tRkb8liejK+/PuZc+T5w3a3Kf9eHNgeuC9vhGDKVCJMRw7j4Q/5vngPx\n" +
            "ruyDOaRi78CVZPL4Jw1DVkkQ1SflFV4tL3WpGQIDAQABAoIBAA76QwL1kIiA90t9\n" +
            "nzU2bpa/gSvwkF3s18wPd5wOi0c7c08pbgitBJfWpSeSXi8ctjxRthxPaI7i0/SJ\n" +
            "M1ZNQTItM0WnfO27qdx8Y5Ru4xA6zxGPGavJmAM/Ici4juI23hfEqZUeddfZP5du\n" +
            "sLenCL4Vfoib9J6boW/fhGmaY5F9WiXFOa2t8yyS2Os2k6p+McHHnF0tlNpamiY8\n" +
            "K+jheJb4Bdzil793N30xgdCzd5xOouy5RHfTSIZy0Qsaqt9gPhwaYgg3Jw2g54dG\n" +
            "a0xokB0+ULXhvJ7H0C6AOi1lKCY/h8zYAmYpdqrK/8IrCMnUO5XsBPLG+ktfJQ1Q\n" +
            "uuWzUAECgYEA+JApVVbafaDXNF/NY7YrfBEnBfyRmPJWzCzsBljDW076aGYSz8xp\n" +
            "7tN6PSBkw7QNG+9xIRbVluFDGKPrKrpza32gv24IXno0MpRFh8hjoCkkeYxLDZwF\n" +
            "NlixuktFpvdchTnfki/UFHx//q4SZThXLvfF7Ehfvty7ottNFBV1YSECgYEA8/TJ\n" +
            "fcUyw64pBJi9YncAs9xjqb7wgKHDRo6l09Uo5W4U1KRMfKuWiQ/41Mdbd+TyzhdW\n" +
            "d7pdH2lAhVGxCFHlWtI9kfDq44N9kcwBTmcnBVUjlB2PruG11eU6wGO8Hi9+dAFP\n" +
            "6P/YYaBgCII/zAsvtsQPUTK8CK6yaP5nkkeUMPkCgYEA2nq0qhtWn1gx8QpIWV21\n" +
            "aS8Wcu+m9p4EZyERMR7yUB7igcM+w8H0VwUxi+seRtrLLwPPF3ufsCg+NrlWKs+K\n" +
            "RjA9jCXmW0zk9pRXbVtZHl0rf18uVT8PYc6iIZKqHXGNtFjDSBacPomUY8KTzr6v\n" +
            "+0u0f9S5u+T/VM2YOZAHBaECgYBEZza4FTsOsx69MzanDUcdJ4aqaed1qeXfOtA0\n" +
            "fWhiLGOa3Ba2PkaPV/MldmCnVYLfVpvgJllGHXUB7M3+zzfIJ3ssGKlD8fKbluAm\n" +
            "47WFQUIgnclT9+XMe+HlYBG3RQnn7RZC9rntZdKHkD3jMJ/IV2EUG22t4Y4U8oCH\n" +
            "+5oJwQKBgC+Mo+XCMNHBx7pLneG6GEmh35k48Mzph+7F3Gu27/juvsHQRazRQyLw\n" +
            "XXqyAEibMqm8AAzoPCbS9o9tZGOCSp6kiA+immXaBpUMnUliEvVz4TqmIJqmr6v6\n" +
            "phHTwxnS8Kma2zS+eGlql7XLNNbmYVYSIfaOR+IsANpntsUwmgLz\n" +
            "-----END RSA PRIVATE KEY-----";

        const string TestPublicKey =
            "-----BEGIN PUBLIC KEY-----\n" +
            "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA7N6ERrUYl1IGszUMCT6B\n" +
            "TvSg1J3rJmA63dqquDnIBF6NAsOIiBiKrarPdRkQ5P9XieGCWUxoqrYLU9b5DOLP\n" +
            "iE1u4QkOKQ5OlqAgqyYF4D09DraP/jmQHRr/fygBp2XTb9yAHCwq5ESpD7ViPr/G\n" +
            "/O0C2Rx+LniW8zWjpvfq/RlgW2VybJW1mn4XPrBDxYD1nBTzPZrvkRpZ80/CxP0l\n" +
            "lY+Jc6q0YTRLrqF23DdbNos7x0knN3vgZ+D9tRkb8liejK+/PuZc+T5w3a3Kf9eH\n" +
            "NgeuC9vhGDKVCJMRw7j4Q/5vngPxruyDOaRi78CVZPL4Jw1DVkkQ1SflFV4tL3Wp\n" +
            "GQIDAQAB\n" +
            "-----END PUBLIC KEY-----";

        //const string TestWrongPublicKey =
        //    "-----BEGIN PUBLIC KEY-----\n" +
        //    "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA0JSGPzU+wU1Air0okudj\n" +
        //    "xTTd3ZR4KAdRgR8imvP3s1JQslwNDoEGokGV7H8yK08N14xmRxAc8zOFgxEWjxGZ\n" +
        //    "e4G6YQNAZ4PYB0U1H1Cgl0++Mbge4UJAlNqtzcAhN03rbTLTBa47vV0EgCBAh+ZT\n" +
        //    "Pr98R/aKGirW9LJJraVWMBRvLSK8EP9UJGjpN1GamUO15IxyameBIsmy97C4zkOj\n" +
        //    "nnAifbB5dj6hJuJEYatk2epxaBVviUfdZ9QHNw0e2NNoEuH62/E122lSu+V3q37E\n" +
        //    "qSCYTVth1g0eUHquP24pN/4Wp47op1ama0mMoN7zjGP8ERwfk8XGZchvPooV9JaK\n" +
        //    "DwIDAQAB\n" +
        //    "-----END PUBLIC KEY-----\n";


        [Fact]
        public void CtorThrowsWhenConfigIsInvalid()
        {
            Assert.Throws<ArgumentNullException>(() => new MessageValidator(null));
            Assert.Throws<ArgumentNullException>(() => new MessageValidator(new Mock<IOptions<ValidationConfig>>().Object));
            var options = new Mock<IOptions<ValidationConfig>>();
            options.Setup(o => o.Value).Returns(new ValidationConfig
            {
                PrivateKey = null
            });
            Assert.Throws<ArgumentException>(() => new MessageValidator(options.Object));
            options = new Mock<IOptions<ValidationConfig>>();
            options.Setup(o => o.Value).Returns(new ValidationConfig
            {
                PrivateKey = TestPrivateKey,
                MaxTimeDifference = TimeSpan.FromSeconds(0)
            });
            Assert.Throws<ArgumentException>(() => new MessageValidator(options.Object));
            options = new Mock<IOptions<ValidationConfig>>();
            options.Setup(o => o.Value).Returns(new ValidationConfig
            {
                PrivateKey = "Zm9vCg=="
            });
            Assert.Throws<CryptographicException>(() => new MessageValidator(options.Object));
        }

        [Theory]
        [InlineData(
            //"All your dockers are belong to us!" + TestWrongPublicKey
            "pk35j516Z7XOhcmbAAvcem8BjjI7uTeKcB2G2ZkizV38LkGLI2v4e3B9l/medS/JhhWrRxCyzmpMZCQBndoM289kcSQwKjhj0KQDomaeVf/0jFZn8lnz6x+317p5tufVxsUQKPdUFIx3cf9HCZaQD1uIs1/8+mwgpUKhd+ATrfnQUBA2eqMl+gzyPXQomRXP7dJcnL811sTXWVH11Hw1ODM7K2aZmzPcqp1tcLL8lh3azVHILFXvFIXeRXFmfIPrw2dYZiX7C0Ps1wPLNJOozvkFyk61EnM4sL5GvGEXiSy1oVNRs5nRpbQPAKpeHrU3Yk29UMLde0LotunRltZtHA==")]
        [InlineData("this is not encrypted")]
        [InlineData("thislooksencryptedbutisnt123==")]
        [InlineData(null)]
        public void ValidateThrowsWhenHashIsntEncryptedProperly(string signature)
        {
            var options = new Mock<IOptions<ValidationConfig>>();
            options.Setup(o => o.Value).Returns(new ValidationConfig
            {
                PrivateKey = TestPrivateKey
            });

            var validator = new MessageValidator(options.Object);
            var msg = new SignedMessage<Foo>()
            {
                TimeSigned = DateTime.UtcNow,
                Message = new Foo(),
                Signature = signature
            };

            var ex = Assert.Throws<SignedMessageException>(() => validator.Validate(msg));
            Assert.True(ex.InvalidMessage);
            Assert.True(ex.InvalidSignature);
            Assert.True(ex.ExpiredMessage);
            Assert.False(msg.Valid);
        }


        [Fact]
        public void ValidateThrowsWhenHashDoesNotMatch()
        {
            var options = new Mock<IOptions<ValidationConfig>>();
            options.Setup(o => o.Value).Returns(new ValidationConfig
            {
                PrivateKey = TestPrivateKey
            });

            var validator = new MessageValidator(options.Object);
            var msg = new SignedMessage<Foo>()
            {
                TimeSigned = new DateTime(2020, 1, 1),
                Message = new Foo { Bar = "bar" }
            };
            //var correct = "ITsf18KvfMYrJXiI9jF91CwNfL3PXRiuRjzaH7PnQj2BMBBcdOScWfL81gTtSWyrgMjbdGUj7bNSwgCz7qXAYBkocbsiSYuO0HdNFAOZnNjuolY7Gkf8oqoF1NHwf7TBko6sH3X9gmBVmXF7oxuLe9rfM4F52bSa9NhIdzomyn4MsOpW3GRwpNHWqkqH0Y1SyBUIoLIgw6VqoniOzSgTFPCdrnsf01MWOqyHhvMk0ezrjxZ9h7EhyJcslR5qRZiBuxPolK45MJPwIC7JokvCtAnK19VW08msvyxA0B3v9dJNeMwbzhzxFxeq++isKhCaQ8zM85r9c7Zz/KJqnZL9cQ==";
            //msg.Message.Bar = "baz";
            var incorrect = "NhGt6uoUsdcq0nEaMoSzSDt0LcYt4TJybDbp5fWapFW5LjJGVSTKpw0GEb6aM5+p86Gm9oItCOT2L4X4rCRAWDB+sx7fgfvvVYWjlkzJdgx9MLXqeCq02BLEozaEnM8d+f3Jxe1Rfx/Sve+1IsK4wlL9YnE4rrFBeY30VGa9bsX2kk0XOBuHEs6W7c7mNgEMZb3qc0pLn7I1I+d0XSpQstDLr9lWwTicQeR39tW5OZ04ONq9Rz/EbhSvpIjJzhTnXdtehPPPOjs/Uto31mbx2gZHicBRjCCzdT8Upv2JSzttp3rpg7GANY+zr/X4mTYPOaDQEHpe5yujMlNKlhoE2w==";
            msg.Signature = incorrect;

            var ex = Assert.Throws<SignedMessageException>(() => validator.Validate(msg));
            Assert.True(ex.InvalidMessage);
            Assert.True(ex.ExpiredMessage);
            Assert.False(ex.InvalidSignature);
            Assert.False(msg.Valid);
        }


        [Theory]
        [InlineData(-6)]
        [InlineData(2)]
        public void ValidateThrowsWhenMessageHasExpired(int addSecs)
        {
            var options = new Mock<IOptions<ValidationConfig>>();
            options.Setup(o => o.Value).Returns(new ValidationConfig
            {
                PrivateKey = TestPrivateKey
            });

            var validator = new MessageValidator(options.Object);
            var msg = new SignedMessage<Foo>()
            {
                TimeSigned = DateTime.UtcNow.AddSeconds(addSecs),
                Message = new Foo { Bar = "baz" }
            };
            msg.Signature = this.signMsg(msg, TestPublicKey);

            var ex = Assert.Throws<SignedMessageException>(() => validator.Validate(msg));
            Assert.True(ex.ExpiredMessage);
            Assert.False(ex.InvalidMessage);
            Assert.False(ex.InvalidSignature);
            Assert.False(msg.Valid);
        }

        [Fact]
        public void ValidateSucceedsWhenMessageIsValid()
        {
            var options = new Mock<IOptions<ValidationConfig>>();
            options.Setup(o => o.Value).Returns(new ValidationConfig
            {
                PrivateKey = TestPrivateKey
            });

            var validator = new MessageValidator(options.Object);
            var msg = new SignedMessage<Foo>()
            {
                TimeSigned = DateTime.UtcNow,
                Message = new Foo { Bar = "baz" }
            };
            msg.Signature = this.signMsg(msg, TestPublicKey);

            validator.Validate(msg);
            Assert.True(msg.Valid);
        }

        private string signMsg(SignedMessage<Foo> msg, string publicKey)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportSubjectPublicKeyInfo(Utils.ReadPemKey(publicKey), out _);
                using (var sha = SHA512.Create())
                {
                    return MessageValidator.GetSignature(msg, sha, rsa);
                }
            }
        }
    }


    public class Foo
    {
        public string Bar { get; set; } = "bar";
    }
}
