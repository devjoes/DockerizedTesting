using System;

namespace DockerizedTesting.RemoteDocker.Tests
{
    public class SignedMessageException : Exception
    {

        public SignedMessageException(string message, Exception ex, bool invalidSignature, bool invalidMessage, bool expiredMessage)
            :base(message, ex)
        {
            this.InvalidSignature = invalidSignature;
            this.InvalidMessage = invalidMessage;
            this.ExpiredMessage = expiredMessage;
        }

        /// <summary>
        /// Message could not be validated because the signature was invalid. Possibly encrypted with wrong public key.
        /// </summary>
        public bool InvalidSignature { get; }
        /// <summary>
        /// The signature was decrypted successfully but the message hash did not match.
        /// </summary>
        public bool InvalidMessage { get; }
        /// <summary>
        /// Message was valid but TimeSigned has expired. It has been at least ValidationConfig.MaxTimeDifference since signing
        /// </summary>
        public bool ExpiredMessage { get; }
    }
}