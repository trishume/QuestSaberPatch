// The contents of this file is modified from https://github.com/emulamer/Apkifier
// and is used under the MIT license at https://github.com/emulamer/Apkifier/blob/master/LICENSE

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.X509;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.X509.Store;
using System.Security.Cryptography;
using Org.BouncyCastle.Asn1.Cms;
using System.Text.RegularExpressions;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Utilities.IO.Pem;

using OpenSsl = Org.BouncyCastle.OpenSsl;

using Ionic.Zip;

namespace LibSaberPatch
{
    public static class Signer {
        private const string DebugCertificatePEM = @"-----BEGIN CERTIFICATE-----
MIICpjCCAY6gAwIBAgIITpLEzAv/BWIwDQYJKoZIhvcNAQELBQAwEjEQMA4GA1UE
AwwHVW5rbm93bjAgFw0wOTA2MDIwMDAwMDBaGA8yMDY5MDYwMjAwMDAwMFowEjEQ
MA4GA1UEAwwHVW5rbm93bjCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEB
AKTjqOckhu7QSfheDcFOtMmq3oYagrybDyIvUkQQfD5bN03dGq+3eD4N5OgZTip5
+W3WCWZCqQESwb2spb9Wx7QLYOeZb8FXlGIwo5d6nvRFHKm4Bomr37t0NcSK+JRD
a3/MOgPP5KQJ5L/z3RCZBKxn0zZBcrUrBLI/0z6kFFCmIo9b/TDQf8Si+mCeM8fu
dH32TTPVUk1mrhssOkykhsxCPbpHzZIj3TKGk04g2es1SlIEgQIldWswa4xkTjny
C7pi3hhpQuLKUpYO2GHhT5aq4J2rpZVScEzLiNckM9iyC+9MdWyG++hlrSb9GeAn
rwqiHN9BjYt8BtvpEDGahMcCAwEAATANBgkqhkiG9w0BAQsFAAOCAQEAAzWf0UuC
ZK7UWnyXltiAqmIHGduEVNaU8gQHvYlS7UiYWgieC2MhYcpojIWf78/n6TP46xUj
Zcs2WHw4M76ppp4Z0t32T4wKMV64rvxmxrT1rnrocpalHEW0L7o6npPwQdin58kY
ip+5dNleQmeFy8E/Plew3E3JiQKedfIR9xj3BNFr4cZHhuIk8bMXi8v4p7dr6A+4
cCYOowy93Oirb1z9RBQqaPQZkQWVH+LaRQ95CMu688hksVXUZz6ZcRzxtQsMmKj3
r/4yonSyufkTY2Sky0myL04/gbDCqLSi1CLo0ksFSRi7d9oChCtNewNoXByGq09X
09SW0xPRYHxoSA==
-----END CERTIFICATE-----
-----BEGIN RSA PRIVATE KEY-----
MIIEogIBAAKCAQEApOOo5ySG7tBJ+F4NwU60yarehhqCvJsPIi9SRBB8Pls3Td0a
r7d4Pg3k6BlOKnn5bdYJZkKpARLBvaylv1bHtAtg55lvwVeUYjCjl3qe9EUcqbgG
iavfu3Q1xIr4lENrf8w6A8/kpAnkv/PdEJkErGfTNkFytSsEsj/TPqQUUKYij1v9
MNB/xKL6YJ4zx+50ffZNM9VSTWauGyw6TKSGzEI9ukfNkiPdMoaTTiDZ6zVKUgSB
AiV1azBrjGROOfILumLeGGlC4spSlg7YYeFPlqrgnaullVJwTMuI1yQz2LIL70x1
bIb76GWtJv0Z4CevCqIc30GNi3wG2+kQMZqExwIDAQABAoIBAC0gYUlhJcyWFKh0
lS8iazgGG4B4IO+dQDcK3GjkWhx2ulwE9xjADZhuFQewZUQavbjhqxDhjX9NsthG
N9Z12ZHcy1iXFY7EeUemKB9836Pahk2sn51t/H1BALYZko6BJRqEuhvw+ZIrYv9l
rkqslirY/2UJ5GrQqyhdb2LlZOntHFDwYesZsKxj0v+IV4P9eRcwtEYr2M1AzZPb
qIEx1v+P6DpZsWDViUpzkfcjqYziViEeXIgkjeaQYkzCwn7h/iTT6WC/VUrMTGzB
mZ4wsDinrMTLQBYqaafmX7Ff172u8D7fIyjTLJjx9mbG3hWSJPfbPl1lDaw+tWrW
aoyZXgECgYEA7MHb8FM5xksv2UEpEDJY9IZbIHWNk+sGZv13bwP5AxMJYfWy0wpe
hGoqc49TSwG8EZwLhfvAtA0BXDXLI3+biulnO3JU6hgAeSzQyk3RQFjB08QM7CbQ
Uzb3T2pRgjRJdFgrRlUBtj19VC2sTR/zUqVSSxAniKGMRQ4zoIToINECgYEAskp2
3/EQDEj4u64Ggi5KaJtvKiITO9tGFt0svSuFCCgcM4sBg6EFX6JEbMfbepRZ5GTW
h9W8XLo9jIZBQYqLhuKJtJ+PlEBgAxK4JAEwgqmWTcHBXdYJicFVJMLsmQOK4HiJ
hNgsIaQTvyhl9/aNgJA96wjUb//pVsqSMNSa8hcCgYBxoUFMAMWz1BYs8UciDOgA
xBMsav7+RUiXWYNe9ssmnJZeO6wN+eYPK10ghWN2lmiLExe8wG1mfO9wMClE6lPe
wdLYBzGWANsJTWcQEXUiqvasCmYhWSeXKMRdiyt/kFTI0CBE6zudGbnzEtClW3ZO
7iWm/SPcQZyu7/f7TI6UYQKBgAGYzyXEV/t0L94meeJynbIAKme7NGbl2OPdiUgM
er2O9mmzxgiyyYSIxIog5CNd7swv5wgCbxR5ipGWpkD7B7LmlosqnrOaPAHrCgEw
jYmuES2THbNEdoNoWuXgZRQdxwGpsrmg4gxPFuowZ3FoIO5U3GkdhCGYrjNbzyFm
1hhzAoGAV6iFwnriGgTLQEz4Pjviqq05SrS2+6jvP6siB9I7GBPlSIQSBMPdyCKA
8hebhfmRmEjRzPxqxKAE3d68MIeZ3n5g0IFcPL+ps3u937qmsttKWgubjkBTr2Ot
hEJ9cirq8PX32lYS3Q5lHaFjlzNgVvijDQCFuxA4NOj+hDFfC/Q=
-----END RSA PRIVATE KEY-----";

        public static void Sign(string filename)
        {

            MemoryStream msManifestFile = new MemoryStream();
            MemoryStream msSignatureFileBody = new MemoryStream();

            //create the MF file header
            using (StreamWriter swManifest = GetSW(msManifestFile))
            {
                swManifest.WriteLine("Manifest-Version: 1.0");
                swManifest.WriteLine("Created-By: emulamer");
                swManifest.WriteLine();
            }

            SHA1 sha = SHA1Managed.Create();
            //so that we can do it in one pass, write the MF and SF line items at the same time to their respective streams
            using(ZipFile archive = ZipFile.Read(filename)) {
                foreach (ZipEntry ze in archive)
                {
                    WriteEntryHashes(ze, sha, msManifestFile, msSignatureFileBody);
                }

                //compute the hash on the entirety of the manifest file for the SF file
                msManifestFile.Seek(0, SeekOrigin.Begin);
                var manifestFileHash = sha.ComputeHash(msManifestFile);

                // var watch = System.Diagnostics.Stopwatch.StartNew();
                //delete all the META-INF stuff that exists already
                archive.Where(x => x.FileName.StartsWith("META-INF")).ToList().ForEach(x =>
                {
                    archive.RemoveEntry(x);
                });

                //write out the MF file
                msManifestFile.Seek(0, SeekOrigin.Begin);
                archive.AddEntry("META-INF/MANIFEST.MF", msManifestFile);

                //write the SF to memory then copy it out to the actual file- contents will be needed later to use for signing, don't want to hit the zip stream twice
                byte[] sigFileBytes = null;
                MemoryStream msSigFile = new MemoryStream();
                using (StreamWriter swSignatureFile = GetSW(msSigFile))
                {
                    swSignatureFile.WriteLine("Signature-Version: 1.0");
                    swSignatureFile.WriteLine($"SHA1-Digest-Manifest: {Convert.ToBase64String(manifestFileHash)}");
                    swSignatureFile.WriteLine("Created-By: emulamer");
                    swSignatureFile.WriteLine();
                }
                msSignatureFileBody.Seek(0, SeekOrigin.Begin);
                msSignatureFileBody.CopyTo(msSigFile);
                msSigFile.Seek(0, SeekOrigin.Begin);
                archive.AddEntry("META-INF/BS.SF", msSigFile);
                sigFileBytes = msSigFile.ToArray();

                //get the key block (all the hassle distilled into one line), then write it out to the RSA file
                byte[] keyBlock = SignIt(sigFileBytes);
                archive.AddEntry("META-INF/BS.RSA",
                    _name => new MemoryStream(keyBlock),
                    (_name, stream) => stream.Dispose());

                archive.Save();
                msSigFile.Dispose();
            }
            // watch.Stop();
            // Console.WriteLine("updating: " + watch.ElapsedMilliseconds);

            msManifestFile.Dispose();
            msManifestFile = null;
            msSignatureFileBody.Dispose();
            msSignatureFileBody = null;
        }

        /// <summary>
        /// Writes the MANIFEST.MF name and hash and the sigfile.SF hash for the sourceFile
        /// </summary>
        private static void WriteEntryHashes(
            ZipEntry sourceFile,
            SHA1 sha,
            Stream manifestFileStream,
            Stream signatureFileStream
        )
        {
            // These are going to be deleted soon so don't include them
            if(sourceFile.FileName.StartsWith("META-INF")) {
                return;
            }

            byte[] hash;
            using (MemoryStream stream = new MemoryStream()) {
                sourceFile.Extract(stream);
                stream.Seek(0, SeekOrigin.Begin);
                hash = sha.ComputeHash(stream);
            }

            using (MemoryStream msSection = new MemoryStream())
            {
                string hashOfMFSection = null;
                using (StreamWriter swSection = GetSW(msSection))
                {
                    swSection.WriteLine($"Name: {sourceFile.FileName}");
                    swSection.WriteLine($"SHA1-Digest: {Convert.ToBase64String(hash)}");
                    swSection.WriteLine("");

                }
                msSection.Seek(0, SeekOrigin.Begin);
                hashOfMFSection = Convert.ToBase64String(sha.ComputeHash(msSection));
                msSection.Seek(0, SeekOrigin.Begin);
                var actualString = UTF8Encoding.UTF8.GetString(msSection.ToArray());
                using (var swSFFile = GetSW(signatureFileStream))
                {
                    swSFFile.WriteLine($"Name: {sourceFile.FileName}");
                    swSFFile.WriteLine($"SHA1-Digest: {hashOfMFSection}");
                    swSFFile.WriteLine();
                }

                msSection.Seek(0, SeekOrigin.Begin);
                msSection.CopyTo(manifestFileStream);
            }
        }

        private static X509Certificate LoadCert(string pemData, out AsymmetricKeyParameter privateKey)
        {
            X509Certificate cert = null;
            privateKey = null;
            using (var reader = new StringReader(pemData))
            {
                var pemReader = new OpenSsl.PemReader(reader);
                object pemObject = null;
                while ((pemObject = pemReader.ReadObject()) != null)
                {
                    if (pemObject is X509Certificate)
                    {
                        cert = pemObject as X509Certificate;
                    }
                    else if (pemObject is AsymmetricCipherKeyPair)
                    {
                        privateKey = (pemObject as AsymmetricCipherKeyPair).Private;
                    }
                }
            }
            if (cert == null)
                throw new System.Security.SecurityException("Certificate could not be loaded from PEM data.");

            if (privateKey == null)
                throw new System.Security.SecurityException("Private Key could not be loaded from PEM data.");

            return cert;
        }
        /// <summary>
        /// Get a signature block that java will load a JAR with
        /// </summary>
        /// <param name="sfFileData">The data to sign</param>
        /// <returns>The signature block (including certificate) for the data passed in</returns>
        private static byte[] SignIt(byte[] sfFileData)
        {
            AsymmetricKeyParameter privateKey = null;

            var cert = LoadCert(DebugCertificatePEM, out privateKey);

            //create things needed to make the CmsSignedDataGenerator work
            var certStore = X509StoreFactory.Create("Certificate/Collection", new X509CollectionStoreParameters(new List<X509Certificate>() { cert }));
            CmsSignedDataGenerator dataGen = new CmsSignedDataGenerator();
            dataGen.AddCertificates(certStore);
            dataGen.AddSigner(privateKey, cert, CmsSignedDataGenerator.EncryptionRsa, CmsSignedDataGenerator.DigestSha256);

            //content is detached- i.e. not included in the signature block itself
            CmsProcessableByteArray detachedContent = new CmsProcessableByteArray(sfFileData);
            var signedContent = dataGen.Generate(detachedContent, false);

            //do lots of stuff to get things in the proper ASN.1 structure for java to parse it properly.  much trial and error.
            var signerInfos = signedContent.GetSignerInfos();
            var signer = signerInfos.GetSigners().Cast<SignerInformation>().First();
            SignerInfo signerInfo = signer.ToSignerInfo();
            Asn1EncodableVector digestAlgorithmsVector = new Asn1EncodableVector();
            digestAlgorithmsVector.Add(new AlgorithmIdentifier(new DerObjectIdentifier("2.16.840.1.101.3.4.2.1"), DerNull.Instance));
            ContentInfo encapContentInfo = new ContentInfo(new DerObjectIdentifier("1.2.840.113549.1.7.1"), null);
            Asn1EncodableVector asnVector = new Asn1EncodableVector();
            asnVector.Add(X509CertificateStructure.GetInstance(Asn1Object.FromByteArray(cert.GetEncoded())));
            Asn1EncodableVector signersVector = new Asn1EncodableVector();
            signersVector.Add(signerInfo.ToAsn1Object());
            SignedData signedData = new SignedData(new DerSet(digestAlgorithmsVector), encapContentInfo, new BerSet(asnVector), null, new DerSet(signersVector));
            ContentInfo contentInfo = new ContentInfo(new DerObjectIdentifier("1.2.840.113549.1.7.2"), signedData);
            return contentInfo.GetDerEncoded();
        }

        private static StreamWriter GetSW(Stream stream)
        {
            return new StreamWriter(stream, new UTF8Encoding(false), 1024, true);
        }
    }
}
