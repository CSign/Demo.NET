
using CSign.Integration.Example.Client;
using System;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;

namespace CSign.Integration.Example.Util
{
    public class CSignHelper
    {
        public static AuthorizationData GetAuthorization()
        {
            return new AuthorizationData
            {
                IntegrationId = int.Parse(ConfigurationManager.AppSettings["CSign_IntegrationId"]),
                IntegrationKey = Guid.Parse(ConfigurationManager.AppSettings["CSign_IntegrationKey"]),
            };
        }

        public static string GetChecksum(Byte[] fileData)
        {
            var     sha         = new SHA256Managed();
            byte[]  checksum    = sha.ComputeHash(fileData);
            return BitConverter.ToString(checksum).Replace("-", String.Empty);
        }

        public enum ScenarioCode
        { 
            AnyIndividual                               = 1000,
            SpecificIndividual                          = 1001,
            SignatoryOrPowerOfAttorneyHolder            = 2000,
            AnyOrganizationRepresentative               = 2001,
            AnyOrganizationRepresentativeNonSwedish     = 2002
        }
    }
}