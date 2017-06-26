﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1.X509;
using Proto;
using TestAuthority.Web.Actors.Message;
using TestAuthority.Web.X509;

namespace TestAuthority.Web.Actors
{
    public class SslCertificateActor : IActor
    {
        private const string DefaultPassword = "123123123";
        private const string SubjectName = "TestAuthority";
        private readonly ILogger<SslCertificateActor> logger;
        private readonly RootCertificateManager rootCertificateManager;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="rootCertificateManager">Root certificate manager.</param>
        public SslCertificateActor(ILogger<SslCertificateActor> logger, RootCertificateManager rootCertificateManager)
        {
            this.logger = logger;
            this.rootCertificateManager = rootCertificateManager;

            logger.LogDebug("Actor created @{actorName}", GetType().Name);
        }

        public Task ReceiveAsync(IContext context)
        {
            switch (context.Message)
            {
                case IssueSslCertificateRequest request:
                    byte[] rawData = IssueCertificate(request);
                    context.Respond(new IssueSslCertificateResponse { RawData = rawData, Filename = "cert.pfx" });
                    return Actor.Done;
            }
            return Actor.Done;
        }

        private byte[] IssueCertificate(IssueSslCertificateRequest request)
        {
            var hostnames = new List<string>();
            if (request.IncludeLocalhost)
            {
                hostnames.Add("localhost");
            }
            logger.LogInformation($"Issue certificate request for {request.SubjectCommonName}");
            hostnames.AddRange(request.Hostnames.Select(x => x.ToLowerInvariant()));

            byte[] certificate = new CertificateBuilder()
                .SetNotBefore(request.NotBefore)
                .SetNotAfter(request.NotAfter)
                .SetSubject(new X509NameWrapper().Add(X509Name.CN, request.SubjectCommonName))
                .AddSubjectAltNameExtension(hostnames, request.IpAddress)
                .SetExtendedKeyUsage(KeyPurposeID.IdKPServerAuth, KeyPurposeID.IdKPClientAuth)
                .GenerateCertificate(rootCertificateManager.GetRootCertificate(SubjectName), DefaultPassword);

            return certificate;
        }
    }
}
