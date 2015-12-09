﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Microsoft Public License (Ms-PL) license. See LICENSE file in the project root for full license information.

namespace PCLCrypto
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Validation;
    using static PInvoke.BCrypt;
    using Platform = Windows.Security.Cryptography.Core;

    /// <summary>
    /// WinRT implementation of the <see cref="IAsymmetricKeyAlgorithmProvider"/> interface.
    /// </summary>
    internal class AsymmetricKeyAlgorithmProvider : IAsymmetricKeyAlgorithmProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AsymmetricKeyAlgorithmProvider"/> class.
        /// </summary>
        /// <param name="algorithm">The algorithm.</param>
        public AsymmetricKeyAlgorithmProvider(AsymmetricAlgorithm algorithm)
        {
            this.Algorithm = algorithm;
            this.AlgorithmHandle = BCryptOpenAlgorithmProvider(GetAlgorithmName(algorithm));
        }

        /// <summary>
        /// Gets the algorithm.
        /// </summary>
        public AsymmetricAlgorithm Algorithm { get; }

        /// <inheritdoc/>
        public IReadOnlyList<KeySizes> LegalKeySizes
        {
            get
            {
                // Not exposed by WinRT. We probably need to switch this to BCrypt.
                return this.algorithm.GetTypicalLegalAsymmetricKeySizes();
            }
        }

        /// <summary>
        /// Gets the BCrypt algorithm.
        /// </summary>
        internal SafeAlgorithmHandle AlgorithmHandle { get; }

        /// <inheritdoc/>
        public ICryptographicKey CreateKeyPair(int keySize)
        {
            Requires.Range(keySize > 0, "keySize");

            var key = this.platform.CreateKeyPair((uint)keySize);
            return new WinRTCryptographicKey(key, canExportPrivateKey: true);
        }

        /// <inheritdoc/>
        public ICryptographicKey ImportKeyPair(byte[] keyBlob, CryptographicPrivateKeyBlobType blobType)
        {
            Requires.NotNull(keyBlob, "keyBlob");

            var key = BCryptImportKeyPair(this.AlgorithmHandle, AsymmetricKeyBlobTypes.EccPrivate, keyBlob, BCryptImportKeyPairFlags.None);
            return new WinRTCryptographicKey(key, canExportPrivateKey: true);
        }

        /// <inheritdoc/>
        public ICryptographicKey ImportPublicKey(byte[] keyBlob, CryptographicPublicKeyBlobType blobType)
        {
            Requires.NotNull(keyBlob, "keyBlob");

            var key = BCryptImportKeyPair(this.AlgorithmHandle, GetPlatformKeyBlobType(blobType), keyBlob);
            return new WinRTCryptographicKey(key, canExportPrivateKey: false);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of mangaed and unmanaged resources held by this instance.
        /// </summary>
        /// <param name="disposing"><c>true</c> if actively being disposed of.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.AlgorithmHandle?.Dispose();
            }
        }

        /// <summary>
        /// Gets the platform-specific enum value for the given PCL enum value.
        /// </summary>
        /// <param name="blobType">The platform independent enum value for the blob type.</param>
        /// <returns>The platform-specific enum value for the equivalent blob type.</returns>
        internal static string GetPlatformKeyBlobType(CryptographicPublicKeyBlobType blobType)
        {
            switch (blobType)
            {
                case CryptographicPublicKeyBlobType.X509SubjectPublicKeyInfo:
                    return Platform.CryptographicPublicKeyBlobType.X509SubjectPublicKeyInfo;
                case CryptographicPublicKeyBlobType.Pkcs1RsaPublicKey:
                    return Platform.CryptographicPublicKeyBlobType.Pkcs1RsaPublicKey;
                case CryptographicPublicKeyBlobType.BCryptPublicKey:
                    return Platform.CryptographicPublicKeyBlobType.BCryptPublicKey;
                case CryptographicPublicKeyBlobType.Capi1PublicKey:
                    return Platform.CryptographicPublicKeyBlobType.Capi1PublicKey;
                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Gets the platform-specific enum value for the given PCL enum value.
        /// </summary>
        /// <param name="blobType">The platform independent enum value for the blob type.</param>
        /// <returns>The platform-specific enum value for the equivalent blob type.</returns>
        internal static Platform.CryptographicPrivateKeyBlobType GetPlatformKeyBlobType(CryptographicPrivateKeyBlobType blobType)
        {
            switch (blobType)
            {
                case CryptographicPrivateKeyBlobType.Pkcs8RawPrivateKeyInfo:
                    return Platform.CryptographicPrivateKeyBlobType.Pkcs8RawPrivateKeyInfo;
                case CryptographicPrivateKeyBlobType.Pkcs1RsaPrivateKey:
                    return Platform.CryptographicPrivateKeyBlobType.Pkcs1RsaPrivateKey;
                case CryptographicPrivateKeyBlobType.BCryptPrivateKey:
                    return Platform.CryptographicPrivateKeyBlobType.BCryptPrivateKey;
                case CryptographicPrivateKeyBlobType.Capi1PrivateKey:
                    return Platform.CryptographicPrivateKeyBlobType.Capi1PrivateKey;
                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Returns the string to pass to the platform APIs for a given algorithm.
        /// </summary>
        /// <param name="algorithm">The algorithm desired.</param>
        /// <returns>The platform-specific string to pass to OpenAlgorithm.</returns>
        private static string GetAlgorithmName(AsymmetricAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case AsymmetricAlgorithm.DsaSha1:
                case AsymmetricAlgorithm.DsaSha256:
                    return AlgorithmIdentifiers.BCRYPT_DSA_ALGORITHM;
                case AsymmetricAlgorithm.EcdsaP256Sha256:
                    return AlgorithmIdentifiers.BCRYPT_ECDSA_P256_ALGORITHM;
                case AsymmetricAlgorithm.EcdsaP384Sha384:
                    return AlgorithmIdentifiers.BCRYPT_ECDSA_P384_ALGORITHM;
                case AsymmetricAlgorithm.EcdsaP521Sha512:
                    return AlgorithmIdentifiers.BCRYPT_ECDSA_P521_ALGORITHM;
                case AsymmetricAlgorithm.RsaOaepSha1:
                case AsymmetricAlgorithm.RsaOaepSha256:
                case AsymmetricAlgorithm.RsaOaepSha384:
                case AsymmetricAlgorithm.RsaOaepSha512:
                case AsymmetricAlgorithm.RsaPkcs1:
                case AsymmetricAlgorithm.RsaSignPkcs1Sha1:
                case AsymmetricAlgorithm.RsaSignPkcs1Sha256:
                case AsymmetricAlgorithm.RsaSignPkcs1Sha384:
                case AsymmetricAlgorithm.RsaSignPkcs1Sha512:
                case AsymmetricAlgorithm.RsaSignPssSha1:
                case AsymmetricAlgorithm.RsaSignPssSha256:
                case AsymmetricAlgorithm.RsaSignPssSha384:
                case AsymmetricAlgorithm.RsaSignPssSha512:
                    return AlgorithmIdentifiers.BCRYPT_RSA_ALGORITHM;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
