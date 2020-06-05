using DockerizedTesting.Models;
using DockerizedTesting.RemoteDocker.Tests;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Security.Cryptography;
using System.Text;

namespace DockerizedTesting.RemoteDocker
{
    public interface IValidateMessage
    {
        void Validate<T>(SignedMessage<T> signedMessage);
    }
    public class MessageValidator : IValidateMessage
    {
        private TimeSpan timeDifferenceTolerance;
        private RSACryptoServiceProvider rsa;
        private SHA512 sha;

        public MessageValidator(IOptions<ValidationConfig> config)
        {
            if (config == null || config.Value == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            if (config.Value.PrivateKey == null)
            {
                throw new ArgumentException(nameof(config.Value.PrivateKey) + " is null", nameof(config));
            }
            if (config.Value.MaxTimeDifference.TotalMilliseconds < 1)
            {
                throw new ArgumentException(nameof(config.Value.MaxTimeDifference) + " < 1ms", nameof(config));
            }
            this.timeDifferenceTolerance = config.Value.MaxTimeDifference;
            this.rsa = new RSACryptoServiceProvider();
            this.sha = SHA512.Create();
            var privateKey = Utils.ReadPemKey(config.Value.PrivateKey);
            rsa.ImportRSAPrivateKey(privateKey, out _);
        }

        public DateTime Date { get; internal set; }

        public void Validate<T>(SignedMessage<T> signedMessage)
        {
            signedMessage.SetValid(false);
            bool isExpired = signedMessage.TimeSigned.Add(this.timeDifferenceTolerance) < DateTime.UtcNow;
            bool isFromTheFuture = signedMessage.TimeSigned > DateTime.UtcNow;

            var hash = this.decryptSignature(signedMessage.Signature);
            var correctHash = Convert.ToBase64String(GetHash(signedMessage, this.sha));
            if (hash != correctHash)
            {
                throw new SignedMessageException("Message could not be validated because the hash was different, the signature was valid. Message may have been modified.", null, false, true, true);
            }
            
            if (isExpired || isFromTheFuture)
            {
                throw new SignedMessageException((isFromTheFuture ? "Find Sarah Connor, message is from the future! o.O" : "Message has expired")
                    + $" Message signed @ ${signedMessage.TimeSigned}. Current time: ${DateTime.UtcNow}",
                    null, false, false, true);
            }
            signedMessage.SetValid(true);
        }

        private string decryptSignature(string signature)
        {
            try
            {
                return Convert.ToBase64String(this.rsa.Decrypt(Convert.FromBase64String(signature), false));
            }
            catch (Exception ex)
            {
                throw new SignedMessageException("Message could not be validated because the signature was invalid. Possibly encrypted with wrong public key.", ex, true, true, true);
            }
        }

        public static byte[] GetHash<T>(SignedMessage<T> envelope, SHA512 sha)
        {
            var settings = new JsonSerializerSettings
            {
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                Culture = System.Globalization.CultureInfo.InvariantCulture,
                Formatting = Formatting.None
            };
            string json = JsonConvert.SerializeObject(envelope.Message, settings);
            const string nonce = "C2734D5D-423D-4F05-B8AE-D0A3DDB8A55F";
            return sha.ComputeHash(Encoding.UTF8.GetBytes(json + envelope.TimeSigned.ToString() + nonce));
        }

        public static string GetSignature<T>(SignedMessage<T> envelope, SHA512 sha, RSACryptoServiceProvider rsa)
        {
            var hash = GetHash(envelope, sha);
            return Convert.ToBase64String(rsa.Encrypt(hash, false));
        }
    }
}
