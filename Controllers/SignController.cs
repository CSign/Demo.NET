using CSign.Integration.Example.Client;
using CSign.Integration.Example.Models;
using CSign.Integration.Example.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Caching;
using System.Web.Mvc;

namespace CSign.Integration.Example.Controllers
{
    public class SignController : Controller
    {
        // GET: Sign
        public ActionResult Index()
        {
            return View();
        }



        [HttpPost]
        public ActionResult Upload(SignModel model)
        {
            Dictionary<string, byte[]>   sessionFiles = new Dictionary<string, byte[]>();
            HttpContext.Cache.Add(GetSessionFilesKey(model.SessionId), sessionFiles, null, DateTime.Now.AddHours(1), Cache.NoSlidingExpiration, CacheItemPriority.Default, null);
            PopulateSessionFiles(Request, sessionFiles);
            ///
            /// First of all when signing files with CSign integration, a transaction needs to be created. You should store the id of this transation somewhere
            /// 
            /// When starting the transation you should supply CSign with all the information that will be signed
            IntegrationServiceClient            csignClient             = new IntegrationServiceClient();
            TransactionStartRequest             transationStartRequest  = new TransactionStartRequest {
                    AuthorizationData           = CSignHelper.GetAuthorization(),
                    Title                       = model.Title,
                    Description                 = model.Description,
                    ProcurementId               = 0,                                                        ///< Old stuff used by some integrating partners. Nothing to see here, move along!
                    Files                       = GetListOfFileInfoForSignature(Request, model.SessionId).ToArray(),
                    OwnerRegistrationNumber     = "5567515019"                                              ///< This determines what company will show up on the signature receipt. 
                                                                                                            ///< In our real environment you should use the organizationID of your company
            };
            TransactionStartResponse transationStartResponse = csignClient.TransactionStart(transationStartRequest);

            ///
            /// When you want someone to sign it is time to call TransactionAddSignature.
            /// Here you should supply information about what individual (if any one in particular) and on behalf of what organisation
            TransactionAddSignatureRequest  transationAddSignatureRequest = new TransactionAddSignatureRequest {
                AuthorizationData   = CSignHelper.GetAuthorization(),
                Scenario            = (int) CSignHelper.ScenarioCode.AnyIndividual,
                SessionId           = model.SessionId.ToString(),
                RetUrl              = GetSignatureFinishedReturnUrl(Request,
                                                                    model.SessionId, 
                                                                    transationStartResponse.Transaction.TransactionId),
                                                                                                    ///<    Will be called by the users browser when signature is completed. 
                                                                                                    ///     NOTE: This is not sufficient to reliably know the signature has taken place and is valid. The call could be a spoof
                SignatureUser       = new SignatureUser {
                    IndividualParameter = new IndividualParameter {
                        Culture = "sv-SE",                                                          ///<    Determines what signature methods are available,BankID, MobiltBankID, Telia..
                        Email   = "info@csign.se"                                                   ///<    You need to add some email here. In case you have the email of the signing user that is good, otherwise just add some other valid email
                    }   
                },
                TransactionId       = transationStartResponse.Transaction.TransactionId,
               
            };
            TransactionAddSignatureResponse  transationAddSignatureResponse = csignClient.TransactionAddSignature(transationAddSignatureRequest);
            return View(transationAddSignatureResponse);
         }


        public ActionResult SignatureCompleted(int transactionID, Guid sessionGuid)
        {
            ///
            ///  Now we need to validate the signature and receive info about who signed it, (in case it is possible for anyone to sign)
            ///  by calling CSign API.
            IntegrationServiceClient            csignClient             = new IntegrationServiceClient();
            TransactionGetRequest               transactionGetRequest   = new TransactionGetRequest {
                AuthorizationData   = CSignHelper.GetAuthorization(),
                TransactionId       = transactionID,
            };

            TransactionGetResponse  transactionGetResponse = csignClient.TransactionGet(transactionGetRequest);
            /// 
            /// In case we are happy with the signature and do not want any further signatures, it is time to finalize the Transaction
            /// It would however be possible to add more signatures before calling Finalize
            TransactionFinalizeRequest finalizeRequest = new TransactionFinalizeRequest {
                AuthorizationData   = CSignHelper.GetAuthorization(),
                TransactionId       = transactionID,
            };
            TransactionFinalizeResponse finalizeResponse = csignClient.TransactionFinalize(finalizeRequest);
            ///
            /// When the transaction is finalized we can download the signature receipt
            GetReceiptResponse receiptResponse = csignClient.GetPDFReceipt(new GetReceiptRequest {
                AuthorizationData   = CSignHelper.GetAuthorization(),
                GetPdf              = true,
                SignatureObjectId   = finalizeRequest.TransactionId
            });

            Dictionary<string, byte[]> sessionfiles = (Dictionary<string, byte[]>) HttpContext.Cache[GetSessionFilesKey(sessionGuid)];
            sessionfiles.Add("SignatureReceipt.pdf", receiptResponse.Pdf);

            return View(new SignatureCompletedModel{ 
                TransactionGetResponse  = transactionGetResponse,
                FinalizedResponse       = finalizeResponse,
                SessionId               = sessionGuid
            });
        }

        public FileResult File(Guid sessionGuid, string name)
        {
            Dictionary<string, byte[]> sessionfiles = (Dictionary<string, byte[]>) HttpContext.Cache[GetSessionFilesKey(sessionGuid)];
            return File(sessionfiles[name], "name");
        }

        /// <summary>
        /// NOTE: Not really related to Csign but this is a way of storing files temporarily in this Demo. You should store signed files more permanently.
        /// </summary>
        /// <param name="request"></param>
        private void PopulateSessionFiles(HttpRequestBase request, Dictionary<string, byte[]> sessionFiles)
        {
            HttpFileCollectionBase  uploadedFiles   = request.Files;
            foreach (string fileIndex in uploadedFiles)
            {
                HttpPostedFileBase file = uploadedFiles[fileIndex];
                if (file.ContentLength == 0)
                    continue;

                MemoryStream target = new MemoryStream();
                file.InputStream.CopyTo(target);
                byte[] data = target.ToArray();

                sessionFiles[Path.GetFileName(file.FileName)] = data;
            }
        }

        private List<VirtualFile>   GetListOfFileInfoForSignature(HttpRequestBase request, Guid sessionGuid)
        {
            HttpFileCollectionBase  uploadedFiles   = request.Files;
            List<VirtualFile>       files           = new List<VirtualFile>();
            foreach (string fileIndex in uploadedFiles)
            {
                HttpPostedFileBase file = uploadedFiles[fileIndex];
                if (file.ContentLength == 0)
                    continue;

                MemoryStream target = new MemoryStream();
                file.InputStream.CopyTo(target);
                byte[] data = target.ToArray();

                VirtualFile fileInfo =  new VirtualFile {
                    FileName            = Path.GetFileName(file.FileName),
                    FileSize            = file.ContentLength,
                    SourceHash          = CSignHelper.GetChecksum(data),                                        ///< SHA256 checksum of the file
                    OriginalLocation    = GetFileUrl(request, sessionGuid, Path.GetFileName(file.FileName))     ///< This is an Url where the file is available for download. The link will be displayed in the CSign signature UI
                };
                files.Add(fileInfo);
            }
            return files;
        }

        private string GetUrl(HttpRequestBase request, Guid sessionGuid, string urlPattern)
        {
            string      currentHostName = request.Url.Host;
            string      protocol        = request.Url.ToString().ToLower().Contains("https") ? "https" : "http";
            string      nonDefaultPort  = (request.Url.Port == 80) ? "" : ":" + request.Url.Port.ToString();
            return  string.Format(urlPattern,
                                            protocol,
                                            currentHostName,
                                            nonDefaultPort,
                                            sessionGuid.ToString());
        }

        private string GetSessionFilesKey(Guid sessionGuid)
        {
            return  string.Format("SessionFiles_{0}", sessionGuid);
        }

        private string GetSignatureFinishedReturnUrl(HttpRequestBase request, Guid sessionGuid, int transactionID)
        {
            return  string.Format("{0}&transactionID={1}", 
                    GetUrl(request, sessionGuid, "{0}://{1}{2}/Sign/SignatureCompleted?sessionGuid={3}"), 
                    transactionID);
        }

        private string GetFileUrl(HttpRequestBase request, Guid sessionGuid, string fileName)
        {
            return  GetUrl(request, sessionGuid, "{0}://{1}{2}/Sign/File?sessionGuid={3}&name=" + fileName);
        }
    }
}