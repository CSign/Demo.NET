using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using CSign.Integration.Example.Client;

namespace CSign.Integration.Example.Models
{
    public class SignatureCompletedModel
    {
        public TransactionGetResponse               TransactionGetResponse      { get; set; }
        public TransactionFinalizeResponse          FinalizedResponse           { get; set; }
        public Guid                                 SessionId                   { get; set; }
    }
}