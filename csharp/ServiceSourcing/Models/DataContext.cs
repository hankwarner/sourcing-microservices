using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;

namespace ServiceSourcing.Models
{
    public class DataContext : DbContext
    {
        private DbContextOptions _options;

        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
            this._options = options;
        }

        public static bool ValidateRemoteCert(object sender, X509Certificate serverCert, X509Chain chain, SslPolicyErrors sslpolicyerrors, string environment)
        {
            //TODO: setup installation of server-ca.pem, in CICD and in README
            var expectedCertPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Certs", environment, "server-ca.pem");
            var expectedCert = new X509Certificate2(expectedCertPath);
            
            var certVerified = expectedCert.Thumbprint.Equals(((X509Certificate2)serverCert).Thumbprint);
            return certVerified;
        }
        public static bool ValidateRemoteCertDev(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslpolicyerrors)
        {
            return ValidateRemoteCert(sender, certificate, chain, sslpolicyerrors, "Development");
        }
        public static bool ValidateRemoteCertProd(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslpolicyerrors)
        {
            return ValidateRemoteCert(sender, certificate, chain, sslpolicyerrors, "Production");
        }

        public static void ProvideClientCertificate(X509CertificateCollection certificates, string environment)
        {
            var clientCertWithKeyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Certs", environment, "client-certkey.pfx");
            var certWithKey = new X509Certificate2(clientCertWithKeyPath);
            certificates.Add(certWithKey);
        }
        public static void ProvideClientCertificateDev(X509CertificateCollection certificates)
        {
            ProvideClientCertificate(certificates, "development");
        }
        public static void ProvideClientCertificateProd(X509CertificateCollection certificates)
        {
            ProvideClientCertificate(certificates, "production");
        }
        
        public DbSet<AccountDetail> AccountDetails { get; set; }
        public DbSet<Address> Addresses { get; set; }
    }

    public class AccountDetail
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("email")]
        public string Email { get; set; }
        [Column("created")]
        public DateTime Created { get; set; }
        [Column("last_modified")]
        public DateTime LastModified { get; set; }
        [Column("addresses")]
        public List<Address> Addresses { get; set; }
    }

    public class Address
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("address_type")]
        public string AddressType { get; set; }
        [Column("first_name")]
        public string FirstName { get; set; }
        [Column("last_name")]
        public string LastName { get; set; }
        [Column("company_name")]
        public string CompanyName { get; set; }
        [Column("address_line")]
        public string AddressLine { get; set; }
        [Column("city")]
        public string City { get; set; }
        [Column("state")]
        public string State { get; set; }
        [Column("zip")]
        public string Zip { get; set; }
        [Column("phone_number")]
        public string PhoneNumber { get; set; }
        [Column("primary")]
        public bool Primary { get; set; }
        [Column("created")]
        public DateTime Created { get; set; }
        [Column("last_modified")]
        public DateTime LastModified { get; set; }
    }
}