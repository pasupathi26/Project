using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Web;
using System.Web.Hosting;
using System.Xml;
using System.Security.Cryptography.X509Certificates;

namespace RMSServices
{
    public class OldNexege
    {
        static string SessionId = "";
        static string CsvFileName = "";
        static CookieContainer cookies;
        static HttpWebRequest wr;
        public static SuccessResponse OldNexgeUpload(string Ipaddress, string UserName, string Password, string ActionType, string Description, string ServerRatesheetName, string pulse, bool? status, int? gracePeriod, List<RMSServices.RatesheetV2.ngt_rateslist> RatesheetData)
        {
            SuccessResponse res = new SuccessResponse();
            // string RatesheetFilePath = "check";
            string[] rUpload = null;
            try
            {
                // string RatesheetFilePath = ConversiontoOldNexege(RatesheetData.Where(t => t.Effective_on == String.Format("{0:yyyy-MM-dd HH:mm:ss}", DateTime.Now)).ToList());

                // Update Purpose
                RatesheetData = RatesheetData.Where(t => Convert.ToDateTime(t.Effective_on) <= DateTime.Now).ToList();
                string RatesheetFilePath = ConversiontoOldNexege(RatesheetData);
                byte[] DataNexge = null;
                if (RatesheetData.Count != 0)
                {
                    FileStream fsdelete = new FileStream(RatesheetFilePath, FileMode.Open, FileAccess.Read);
                    DataNexge = new byte[fsdelete.Length];
                    fsdelete.Read(DataNexge, 0, DataNexge.Length);
                    fsdelete.Close();
                }

                UpdateError("OldNexge MappingRatesheet:- ConversiontoOldNexege", SessionId, "RatesheetLogs.txt", ServerRatesheetName, false);
                if (!RatesheetFilePath.Contains("Exception"))
                {

                    if (ActionType == "Add")
                    {


                        UpdateError("OldNexge MappingRatesheet Method Create ratesheet Initializing..", SessionId, "RatesheetLogs.txt", ServerRatesheetName, false);
                        rUpload = createRatesheet(ServerRatesheetName, Ipaddress, UserName, Description, gracePeriod, pulse, Password, RatesheetData, status);
                        if (rUpload[0] == "true")
                        {

                            res.status = "success";
                            res.Message = "Billing Plan Updated Successfully for Billing Plan " + ServerRatesheetName;
                        }
                        else
                        {
                            res.status = "failure";
                            res.Message = rUpload[1] + " for Billing Plan " + ServerRatesheetName;
                        }
                    }
                    else
                    {

                        if (RatesheetData.Count == 0)
                        {
                            res.status = "success";
                            res.Message = "Billing Plan Updated Successfully for Billing Plan " + ServerRatesheetName;
                        }
                        else
                        {
                            string SessionId = AuthicateoldLogin(Ipaddress, UserName, Password);

                            if (SessionId != "")
                            {
                                string[] UploadRatesheet = UploadOldNexegeRatesheet(RatesheetFilePath, Ipaddress, SessionId);
                                UpdateError("OldNexge MappingRatesheet Method UploadOldNexegeRatesheet", UploadRatesheet[0], "RatesheetLogs.txt", UploadRatesheet[1], false);
                                if (UploadRatesheet[0] != "false")
                                {

                                    rUpload = verifyRatesheet(RatesheetFilePath, Ipaddress);
                                    UpdateError("OldNexge MappingRatesheet Method Verify ratesheet", " Status: " + rUpload[0] + "  Error Code: " + rUpload[1], "RatesheetLogs.txt", RatesheetFilePath, false);
                                    if (rUpload[0] == "true")
                                    {
                                        if (ActionType == "Update")
                                        {

                                            rUpload = activateRatesheet(RatesheetFilePath, ServerRatesheetName, Ipaddress, DataNexge);
                                            if (rUpload[0] == "true")
                                            {
                                                res.status = "success";
                                                res.Message = "Billing Plan Updated Successfully for Billing Plan " + ServerRatesheetName;
                                            }
                                            else
                                            {
                                                res.status = "failure";
                                                res.Message = rUpload[1] + " for Billing Plan " + ServerRatesheetName;
                                            }
                                        }

                                        //else
                                        //{
                                        //    UpdateError("OldNexge MappingRatesheet Method Create ratesheet Initializing..", SessionId, "RatesheetLogs.txt", ServerRatesheetName, false);
                                        //    rUpload = createRatesheet(ServerRatesheetName, Ipaddress, UserName, Description, gracePeriod, pulse, DataNexge, Password);
                                        //    if (rUpload[0] == "true")
                                        //    {
                                        //        res.status = "success";
                                        //        res.Message = "Billing Plan Updated Successfully for Billing Plan " + ServerRatesheetName;
                                        //    }
                                        //    else
                                        //    {
                                        //        res.status = "failure";
                                        //        res.Message = rUpload[1] + " for Billing Plan " + ServerRatesheetName;
                                        //    }
                                        //}
                                    }
                                    else
                                    {
                                        res.status = "failure";
                                        res.Message = rUpload[1] + " for Billing Plan " + ServerRatesheetName;
                                    }
                                }
                                else
                                {
                                    res.status = "failure";
                                    res.Message = UploadRatesheet[1] + " for Billing Plan " + ServerRatesheetName;
                                }
                            }
                            else
                            {
                                res.status = "failure";
                                res.Message = "Not able to Login.Invalid Server Details Found for the Selected Server.";
                            }
                        }
                    }

                }
                else
                {
                    res.status = "failure";
                    res.Message = RatesheetFilePath;
                }
                return res;
            }
            catch (Exception ex)
            {
                res.Message = ex.ToString();
                res.status = "failure";
                return res;
            }
        }

        private static string[] activateRatesheet(string fileName, string ratesheetName, string ip, byte[] datadelete)
        {
            try
            {
                WebClient wc1 = new WebClient();
                wc1.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                byte[] postData = Encoding.ASCII.GetBytes("actionId=uploadRateSheet&xmlData=<?xml version=\"1.0\" encoding=\"UTF-8\"?><xmlData><fileName>" + fileName + "</fileName><billingPlan>" + ratesheetName + "</billingPlan></xmlData>");
                byte[] response = wc1.UploadData("http://" + ip + "/VerifyRateSheet.jsp", "POST", postData);

                UpdateError("OldNexge MappingRatesheet Method activateRatesheet", ratesheetName, "RatesheetLogs.txt", Encoding.ASCII.GetString(response), false);

                return resolveXml(Encoding.ASCII.GetString(response));

            }
            catch (Exception ex)
            {
                UpdateError("OldNexge UpdateRatesheet activateRatesheet Response Error:-", ex.Message, "RatesheetLogs.txt", "", false);

                return new string[] { "false", ex.Message, "" };
                // return new string[] { "true", "Ratesheet Updated Successfully", "Ratesheet Updated Successfully" };
            }
        }

        //// Pasupathi Crteated Old Nexge Update ratesheet proper

        public static SuccessResponse OldNexgeUpdateRatesheet(string Ipaddress, string UserName, string Password, string ActionType, string Description, string ServerRatesheetName, string pulse, bool? status, int? gracePeriod, List<RMSServices.RatesheetV2.ngt_rateslist> RatesheetData, bool companytype)
        {
            SuccessResponse res = new SuccessResponse();
            try
            {

                #region Old Nexge Update
                UpdateError("OldNexge MappingRatesheet:- OldNexgeUpdateRatesheet", SessionId, "RatesheetLogs.txt", ServerRatesheetName, false);


                string[] UploadRatesheet = UpdateOldnexgeRatesheet(RatesheetData, ServerRatesheetName, Ipaddress, UserName, Password, companytype);

                if (UploadRatesheet[0] == "true")
                    res.status = UploadRatesheet[0] == "true" ? "success" : "failure";

                #endregion

            }
            catch (Exception ex)
            {
                UpdateError("OldNexge MappingRatesheet:- OldNexgeUpdateRatesheet Error", ex.Message, "RatesheetLogs.txt", ServerRatesheetName, false);

                res.status = "failure";
            }
            return res;
        }

        public static SuccessResponse OldNexgeUpdateReplaceRatesheet(string Ipaddress, string UserName, string Password, string ActionType, string Description, string ServerRatesheetName, string pulse, bool? status, int? gracePeriod, List<RMSServices.RatesheetV2.ngt_rateslist> RatesheetData, bool companytype, string Ratesheetid, string RequestId, int serverid, string ratesheetnamerms)
        {
            SuccessResponse res = new SuccessResponse();
            try
            {

                #region Old Nexge Update
                UpdateError("OldNexge MappingRatesheet:- OldNexgeUpdateReplaceRatesheet", SessionId, "RatesheetLogs.txt", ServerRatesheetName, false);


                string[] UploadRatesheet = UpdateReplaceOldnexgeRatesheet(RatesheetData, ServerRatesheetName, Ipaddress, UserName, Password, companytype, Ratesheetid, RequestId, serverid, ratesheetnamerms);

                UpdateError("OldNexge MappingRatesheet:- OldNexgeUpdateReplaceRatesheet Method complete", SessionId, "RatesheetLogs.txt", UploadRatesheet[0], false);

                if (UploadRatesheet[0] == "true")
                    res.status = UploadRatesheet[0] == "true" ? "success" : "failure";

                #endregion

            }
            catch (Exception ex)
            {
                UpdateError("OldNexge MappingRatesheet:- OldNexgeUpdateReplaceRatesheet Error", ex.Message, "RatesheetLogs.txt", ServerRatesheetName, false);

                res.status = "failure";
            }
            return res;
        }

        public static SuccessResponse OldNexgeFutureUpdateReplaceRatesheet(string Ipaddress, string UserName, string Password, string ServerRatesheetName, List<RMSServices.RatesheetV2.ngt_rateslist> RatesheetData, bool companytype)
        {
            SuccessResponse res = new SuccessResponse();
            try
            {

                #region Old Nexge Update
                UpdateError("OldNexge MappingRatesheet:- OldNexgeFutureUpdateReplaceRatesheet", SessionId, "RatesheetLogs.txt", ServerRatesheetName, false);


                string[] UploadRatesheet = UpdateReplaceFutureOldnexgeRatesheet(RatesheetData, ServerRatesheetName, Ipaddress, UserName, Password, companytype);

                if (UploadRatesheet[0] == "true")
                    res.status = UploadRatesheet[0] == "true" ? "success" : "failure";

                #endregion

            }
            catch (Exception ex)
            {
                UpdateError("OldNexge MappingRatesheet:- OldNexgeFutureUpdateReplaceRatesheet Error", ex.Message, "RatesheetLogs.txt", ServerRatesheetName, false);

                res.status = "failure";
            }
            return res;
        }
        public static SuccessResponse OldNexgeFutureReplaceRatesheet(string Ipaddress, string UserName, string Password, string ServerRatesheetName, List<RMSServices.RatesheetV2.ngt_rateslist> RatesheetData, bool companytype)
        {
            SuccessResponse res = new SuccessResponse();
            try
            {

                #region Old Nexge Update
                UpdateError("OldNexge MappingRatesheet:- OldNexgeFutureReplaceRatesheet", SessionId, "RatesheetLogs.txt", ServerRatesheetName, false);


                string[] UploadRatesheet = ReplaceFutureOldnexgeRatesheet(RatesheetData, ServerRatesheetName, Ipaddress, UserName, Password, companytype);

                if (UploadRatesheet[0] == "true")
                    res.status = UploadRatesheet[0] == "true" ? "success" : "failure";

                #endregion

            }
            catch (Exception ex)
            {
                UpdateError("OldNexge MappingRatesheet:- OldNexgeFutureReplaceRatesheet Error", ex.Message, "RatesheetLogs.txt", ServerRatesheetName, false);

                res.status = "failure";
            }
            return res;
        }


        public static string GetLast(string source, int tail_length)
        {
            if (tail_length >= source.Length)
                return source;
            return source.Substring(source.Length - tail_length);
        }


        private static string[] RatesheetOnOldNexegeUpdate(string fileName, string filePath, string ip, string sessionID)
        {
            try
            {
                NameValueCollection nvc = new NameValueCollection();
                nvc.Add("actionId", "upload");
                nvc.Add("fileName", fileName);
                return resolveXml(HttpUploadFile("http://" + ip + "/admin/billingadmin/UploadPrefixFile.jsp?actionId=upload&fileName=" + fileName, filePath, "file", "text/csv", nvc, sessionID));
                //return resolvehtml(HttpUploadFile("http://" + ip + "/admin/billingadmin/UploadFile.jsp?actionId=upload&fileName=" + fileName, filePath, "file", "text/csv", nvc, sessionID));
            }
            catch (Exception ex)
            {
                UpdateError("OldNexge MappingRatesheet Method uploadRatesheetOnOldNexege Error:-", ex.Message, "RatesheetLogs.txt", fileName, false);
                return new string[] { "false", ex.Message, "" };
            }
        }


        private static string[] UpdateOldnexgeRatesheet(List<RMSServices.RatesheetV2.ngt_rateslist> RatesheetData, string ratesheetName, string ip, string owner, string password, bool companytype)
        {

            SessionId = AuthicateoldLogin(ip, owner, password);

            RestClient restClient1 = new RestClient();
            restClient1.CookieContainer = new CookieContainer();

            #region ViewAddressrule

            // Url1
            string URI = "http://" + ip + "/InterUserBillingServlet?actionId=viewAddressRule";
            RestRequest wc = new RestRequest(URI, Method.GET);
            wc.AddHeader("Accept", "text / html,application / xhtml + xml,application / xml; q = 0.9,*/*;q=0.8");
            wc.AddHeader("Accept-Encoding", "gzip, deflate");
            wc.AddHeader("Accept-Language", "en-US,en;q=0.5");
            wc.AddHeader("Cookie", "JSESSIONID=" + SessionId);
            // wc.AddHeader("Cookie", "JSESSIONID=FAB3207A939AB04F558744E954F8DD20");

            wc.AddHeader("Host", ip);
            wc.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:47.0) Gecko/20100101 Firefox/47.0");
            wc.AddCookie("JSESSIONID", SessionId);
            //  wc.AddCookie("JSESSIONID", "FAB3207A939AB04F558744E954F8DD20");
            wc.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["WcTimeout"]);
            restClient1.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["RcTimeout"]);

            ServicePointManager.ServerCertificateValidationCallback =
            delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            { return true; };

            IRestResponse response1 = restClient1.Execute(wc);
            string result = "";
            string pulserules = "";
            string ruleid = "";
            try
            {
                if (response1.Content.Contains("> " + ratesheetName + "_addressRule"))
                {
                    string[] arrr_CodecData = response1.Content.Split(new string[] { "> " + ratesheetName + "_addressRule" }, StringSplitOptions.None);
                    int index1 = arrr_CodecData[0].LastIndexOf('=');
                    int index = arrr_CodecData[0].Length;
                    int ncount = index - index1;
                    result = GetLast(arrr_CodecData[0], ncount);
                    result = result.Replace('=', ' ').Replace('"', ' ').Replace(" ", "");
                }
                else
                {
                    UpdateError("OldNexge UpdateRatesheet Response Success:-", response1.Content, "RatesheetLogs.txt", "  View AddressRule Error", false);

                    return new string[] { "false", "Ratesheet Updated Successfully", "Ratesheet Update Error" };
                }
                pulserules = result.Split(',')[0].ToString();
                ruleid = (Convert.ToInt32(pulserules) - 1).ToString();
            }
            catch (Exception ex)
            {
                UpdateError("OldNexge UpdateRatesheet ViewAddressrule Error:-", ex.Message, "RatesheetLogs.txt", "  View AddressRule Error", false);

                return new string[] { "false", "Ratesheet Updated Successfully", "Ratesheet Update Error" };
            }
            #endregion

            #region generateaddressrule

            string URIgenerate = "http://" + ip + "/admin/billingadmin/ViewAddressRulesBySearch.jsp?actionId=uploadedAddress&ruleid=" + pulserules + "& ruleName=" + ratesheetName + "_addressRule&subscriberId=admin";
            wc = new RestRequest(URIgenerate, Method.POST);
            wc.AddHeader("Accept", "text / html,application / xhtml + xml,application / xml; q = 0.9,*/*;q=0.8");
            wc.AddHeader("Accept-Encoding", "gzip, deflate");
            wc.AddHeader("Accept-Language", "en-US,en;q=0.5");
            wc.AddHeader("Cookie", "JSESSIONID=" + SessionId);
            wc.AddHeader("Referer", "http://" + ip + "/admin/billingadmin/ViewAddressRulesBySearch.jsp?ruleId=" + pulserules + "& ruleName=" + ratesheetName + "_addressRule & subscriberId=admin");
            wc.AddHeader("Host", ip);
            wc.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:47.0) Gecko/20100101 Firefox/47.0");
            wc.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            wc.AddCookie("JSESSIONID", SessionId);

            wc.AddParameter("actionId", "uploadedAddress");
            wc.AddParameter("ruleid", pulserules);
            wc.AddParameter("ruleName", ratesheetName + "_addressRule");
            wc.AddParameter("subscriberId", "admin");



            wc.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["WcTimeout"]);
            restClient1.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["RcTimeout"]);

            ServicePointManager.ServerCertificateValidationCallback =
            delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            { return true; };

            response1 = restClient1.Execute(wc);

            if (!response1.Content.Contains("AddressRule is Exported Successfully"))
                return new string[] { "false", "Ratesheet Generated Failure", "Ratesheet generate fail" };


            #endregion

            #region DownloadAddressrule

            string URIDownload = "http://" + ip + "/templateFiles/" + ratesheetName + "_addressRule.csv";
            wc = new RestRequest(URIDownload, Method.GET);
            wc.AddHeader("Accept", "text / html,application / xhtml + xml,application / xml; q = 0.9,*/*;q=0.8");
            wc.AddHeader("Accept-Encoding", "gzip, deflate");
            wc.AddHeader("Accept-Language", "en-US,en;q=0.5");
            wc.AddHeader("Cookie", "JSESSIONID=" + SessionId);
            // wc.AddHeader("Cookie", "JSESSIONID=FAB3207A939AB04F558744E954F8DD20");

            wc.AddHeader("Host", ip);
            wc.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:47.0) Gecko/20100101 Firefox/47.0");
            wc.AddCookie("JSESSIONID", SessionId);
            //  wc.AddCookie("JSESSIONID", "FAB3207A939AB04F558744E954F8DD20");
            wc.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["WcTimeout"]);
            restClient1.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["RcTimeout"]);

            ServicePointManager.ServerCertificateValidationCallback =
            delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            { return true; };


            response1 = restClient1.Execute(wc);

            if (response1.Content.Contains("The requested resource"))
                return new string[] { "false", "Template Generated Failure", "Template generate fail" };

            string[] arrr_RateData = response1.Content.Split(new string[] { "\n" }, StringSplitOptions.None);

            RateDetails objRateClass = null;
            List<RateDetails> listRateDetails = new List<RateDetails>();
            string _code = string.Empty;
            string _codeName = string.Empty;
            string _Rate = string.Empty;
            string _pulseid = string.Empty;
            string _Status = string.Empty;


            for (int i = 0; i < arrr_RateData.Length - 1; i++)
            {
                if (i == 0)
                    continue;

                try
                {
                    arrr_RateData[i] = arrr_RateData[i].Contains("Others,") ? arrr_RateData[i].Replace(", Others,", " Others") : arrr_RateData[i].Contains(", ") ? arrr_RateData[i].Replace(", ", " ") : arrr_RateData[i];
                    string[] arr_RateNew = arrr_RateData[i].Split(',');
                    if (arr_RateNew[0].Trim().ToString() != "")
                    {
                        _code = arr_RateNew[0].Trim().ToString();
                        _codeName = arr_RateNew[1].Replace('"', ' ').Replace(" ", "").Trim().ToString();
                        _Rate = arr_RateNew[2].Trim().ToString();
                        _pulseid = arr_RateNew[3].Trim().ToString();
                        _Status = arr_RateNew[4].Trim().ToString();

                        objRateClass = new RateDetails() { Code = _code, CodeName = _codeName, Rate = _Rate, Status = _Status, pulseid = _pulseid };
                        listRateDetails.Add(objRateClass);
                    }
                }
                catch (Exception ex)
                {
                    UpdateError("OldNexge Update DownloadAddressrule Error:", "  Error:" + ex.Message, "RatesheetLogs.txt", "Data Count: " + i.ToString(), false);

                    return new string[] { "false", "Template Generated Failure", "Template generate fail" };
                }
            }

            #endregion

            #region commentedpasu

            //         // Url2
            //         string URI1 = "http://" + ip + "/admin/billingadmin/CreateAddressRule.jsp?actionId=displayCreateAddressRules&ruleId=" + ruleid + "& ruleName=" + ratesheetName + "_addressRule&subscriberId=admin";
            //         wc = new RestRequest(URI1, Method.POST);
            //         wc.AddHeader("Accept", "text / html, application / xhtml + xml, application / xml; q = 0.9,*/*;q=0.8");
            //         wc.AddHeader("Accept-Encoding", "gzip, deflate");
            //         wc.AddHeader("Accept-Language", "en-US,en;q=0.5");

            //         wc.AddHeader("Cookie", "JSESSIONID=" + SessionId);
            //         wc.AddHeader("Cookie", "JSESSIONID=E05F2CCD3289D3DDE1AC1271DA800021");

            //         wc.AddHeader("Host", ip);
            //         wc.AddHeader("Referer", "http://" + ip + "/InterUserBillingServlet?actionId=updatePlan");
            //         wc.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:47.0) Gecko/20100101 Firefox/47.0");
            //         wc.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            //         wc.AddCookie("JSESSIONID", SessionId);

            //         wc.AddParameter("viewMyPlan", true);
            //         wc.AddParameter("actionId", "displayCreateAddressRule");
            //         wc.AddParameter("ruleId", rules);
            //         wc.AddParameter("typeCode", "address");


            //         wc.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["WcTimeout"]);
            //         restClient1.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["RcTimeout"]);

            //         ServicePointManager.ServerCertificateValidationCallback =
            //delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            //{ return true; };

            //         response1 = restClient1.Execute(wc);

            //         UpdateError("OldNexge AddressRuleId Record", response1.Content, "RatesheetLogs.txt", "", false);
            //         string addressruleid = "12263";
            //         string pulseruleid = "12262";

            //         if (response1.Content.Contains("click here </a> to login again"))
            //         {
            //             UpdateError("OldNexge Error Session Timeout Error:-", response1.Content, "RatesheetLogs.txt", "", false);
            //             //return resolveXml(Encoding.ASCII.GetString(response));
            //             return new string[] { "true", "Ratesheet Updated Successfully", "Ratesheet Update Success" };
            //         }

            //         if (response1.Content.Contains("addressRuleId"))
            //         {
            //             string obj = response1.Content.Replace("addressRuleId", "^");

            //             string[] s1 = obj.Split('^');
            //             string[] ss = s1[1].ToString().Split('"');

            //             addressruleid = ss[2].ToString();
            //             UpdateError("OldNexge AddressRuleid Record", addressruleid, "RatesheetLogs.txt", "", false);
            //         }

            //         if (response1.Content.Contains("pulseRuleId"))
            //         {

            //             string obj1 = response1.Content.Replace("pulseRuleId", "^");
            //             string[] s2 = obj1.Split('^');

            //             string[] ss1 = s2[1].ToString().Split('"');

            //             pulseruleid = ss1[2].ToString();
            //             UpdateError("OldNexge PulseRuleID Record", pulseruleid, "RatesheetLogs.txt", "", false);

            //         }

            #endregion

            #region CreateRatesheet

            var Regioncode = listRateDetails.Select(k => k.Code).ToList();

            List<RMSServices.RatesheetV2.ngt_rateslist> RatesheetData1 = RatesheetData.Where(t => Convert.ToDateTime(t.Effective_on) <= DateTime.Now & !Regioncode.Contains(t.ngt_regionName)).ToList();

            UpdateError("OldNexge UpdateRatesheet ADDRatesheet before Response", "  Data Count: " + RatesheetData.Count.ToString(), "RatesheetLogs.txt", "", false);

            // RatesheetData= RatesheetData.Where(Regioncode.Contains(e=>e.ngt_regionName))
            // string RatesheetFilePath = ConversiontoOldNexegeUpdate(RatesheetData, ruleid);



            //byte[] DataNexge = null;
            //if (RatesheetData.Count != 0)
            //{
            //    FileStream fsdelete = new FileStream(RatesheetFilePath, FileMode.Open, FileAccess.Read);
            //    DataNexge = new byte[fsdelete.Length];
            //    fsdelete.Read(DataNexge, 0, DataNexge.Length);
            //    fsdelete.Close();
            //}

            #endregion

            #region UploadFile

            //         string Ratesheetname = RatesheetFilePath.Split(new string[] { "ratesheetFiles" }, StringSplitOptions.None)[1].ToString();
            //         Ratesheetname = new string(Ratesheetname.Skip(1).ToArray());

            //         // Url2
            //         string URI2 = "http://" + ip + "/admin/billingadmin/UploadPrefixFile.jsp?actionId=upload&fileName=" + Ratesheetname;
            //         wc = new RestRequest(URI2, Method.POST);
            //         wc.AddHeader("Accept", "text / html, application / xhtml + xml, application / xml; q = 0.9,*/*;q=0.8");
            //         wc.AddHeader("Accept-Encoding", "gzip, deflate");
            //         wc.AddHeader("Accept-Language", "en-US,en;q=0.5");

            //         // wc.AddHeader("Cookie", "JSESSIONID=" + SessionId);
            //         wc.AddHeader("Cookie", "JSESSIONID=4FF7E0C372CD92C582FCA6345D1BA0C8");

            //         wc.AddHeader("Host", ip);
            //         wc.AddHeader("Referer", "http://" + ip + "/admin/billingadmin/UploadPrefixFile.jsp?actionId=upload&fileName=" + RatesheetFilePath);
            //         wc.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:47.0) Gecko/20100101 Firefox/47.0");
            //         wc.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            //         wc.AddCookie("JSESSIONID", "4FF7E0C372CD92C582FCA6345D1BA0C8");
            //         // wc.AddCookie("JSESSIONID", SessionId);
            //         wc.AddFile("fileName", DataNexge, ratesheetName);

            //         wc.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["WcTimeout"]);
            //         restClient1.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["RcTimeout"]);

            //         ServicePointManager.ServerCertificateValidationCallback =
            //delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            //{ return true; };

            //         response1 = restClient1.Execute(wc);
            //         if (!response1.Content.Contains("success();"))
            //         {
            //             UpdateError("OldNexge UpdateRatesheet Response Success:-", response1.Content, "RatesheetLogs.txt", "  Upload File Error", false);

            //             return new string[] { "false", "Ratesheet Updated Error", "Ratesheet Update Error" };
            //         }

            #endregion

            #region Uploadedratesheet

            //         // Url3
            //         string URI3 = "http://" + ip + "/admin/billingadmin/CreateAddressRule.jsp?actionId=uploaded&removeCache=no";

            //         wc = new RestRequest(URI3, Method.POST);
            //         wc.AddHeader("Accept", "text / html, application / xhtml + xml, application / xml; q = 0.9,*/*;q=0.8");
            //         wc.AddHeader("Accept-Encoding", "gzip, deflate");
            //         wc.AddHeader("Accept-Language", "en-US,en;q=0.5");

            //         //  wc.AddHeader("Cookie", "JSESSIONID=" + SessionId);
            //         wc.AddHeader("Cookie", "JSESSIONID=4FF7E0C372CD92C582FCA6345D1BA0C8");
            //         wc.AddHeader("Host", ip);
            //         wc.AddHeader("Referer", "http://" + ip + "/admin/billingadmin/CreateAddressRule.jsp?actionId=displayCreateAddressRules&ruleId=" + ruleid + "& ruleName=" + ratesheetName + "_addressRule&subscriberId=admin");
            //         wc.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:47.0) Gecko/20100101 Firefox/47.0");
            //         wc.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            //         // wc.AddCookie("JSESSIONID", SessionId);
            //         wc.AddCookie("JSESSIONID", "4FF7E0C372CD92C582FCA6345D1BA0C8");

            //         wc.AddParameter("ruleId", pulserules);
            //         wc.AddParameter("matchExp", "");
            //         wc.AddParameter("ruleValue", "");
            //         wc.AddParameter("typeCode", "destination");
            //         wc.AddParameter("pulseRules", ruleid);
            //         wc.AddParameter("rateSheetType", companytype == true ? "2" : "1");
            //         wc.AddParameter("permitType", false);
            //         wc.AddParameter("profitChecking", false);
            //         wc.AddParameter("countryNotExist", "");
            //         wc.AddParameter("seqNo", "");
            //         wc.AddParameter("addSeqNo", "");
            //         wc.AddParameter("flag", false);
            //         wc.AddParameter("ruleName", ratesheetName + "_addressRule");
            //         wc.AddParameter("ruleId", pulserules);
            //         wc.AddParameter("subscriberId", "admin");


            //         wc.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["WcTimeout"]);
            //         restClient1.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["RcTimeout"]);

            //         ServicePointManager.ServerCertificateValidationCallback =
            //delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            //{ return true; };

            //         response1 = restClient1.Execute(wc);

            //         if (response1.Content.Contains("File not found") || response1.Content.Contains("org.apache.jasper.JasperException: Exception in JSP:") || response1.Content.Contains("Please Upload a Correct Rate Sheet"))
            //         {
            //             UpdateError("OldNexge UpdateRatesheet Response Success:-", response1.Content, "RatesheetLogs.txt", "  After Upload File", false);

            //             return new string[] { "false", "Ratesheet Updated Error", "Ratesheet Update Error" };
            //         }

            #endregion

            #region Displaycreateaddressrule

            string URIgenerate1 = "http://" + ip + "/admin/billingadmin/CreateAddressRule.jsp?actionId=displayCreateAddressRules&ruleId=" + pulserules + "&ruleName=" + ratesheetName + "_addressRule & subscriberId=admin";
            wc = new RestRequest(URIgenerate1, Method.POST);
            wc.AddHeader("Accept", "text / html,application / xhtml + xml,application / xml; q = 0.9,*/*;q=0.8");
            wc.AddHeader("Accept-Encoding", "gzip, deflate");
            wc.AddHeader("Accept-Language", "en-US,en;q=0.5");
            wc.AddHeader("Cookie", "JSESSIONID=" + SessionId);
            wc.AddHeader("Referer", "http://" + ip + "/InterUserBillingServlet?actionId=createAddressRule");
            wc.AddHeader("Host", ip);
            wc.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:47.0) Gecko/20100101 Firefox/47.0");
            wc.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            wc.AddCookie("JSESSIONID", SessionId);

            wc.AddParameter("viewMyPlan", "true");
            wc.AddParameter("ruleId", pulserules + ",admin");
            wc.AddParameter("typeCode", "address");
            wc.AddParameter("actionId", "displayCreateAddressRule");



            wc.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["WcTimeout"]);
            restClient1.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["RcTimeout"]);

            ServicePointManager.ServerCertificateValidationCallback =
            delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            { return true; };

            response1 = restClient1.Execute(wc);
            #endregion

            #region ADDRatesheet

            foreach (var a in RatesheetData1)
            {
                string URIFOR = "";
                // Url1
                URIFOR = "http://" + ip + "/admin/billingadmin/CreateAddressRule.jsp?actionId=addAddressRule";

                wc = new RestRequest(URIFOR, Method.POST);
                wc.AddHeader("Accept", "text / html,application / xhtml + xml,application / xml; q = 0.9,*/*;q=0.8");
                wc.AddHeader("Accept-Encoding", "gzip, deflate");
                wc.AddHeader("Accept-Language", "en-US,en;q=0.5");
                wc.AddHeader("Cookie", "JSESSIONID=" + SessionId);
                // wc.AddHeader("Cookie", "JSESSIONID=4FF7E0C372CD92C582FCA6345D1BA0C8");
                wc.AddHeader("Referer", "http://" + ip + "/admin/billingadmin/Navigate.jsp");
                wc.AddHeader("Host", ip);
                wc.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:47.0) Gecko/20100101 Firefox/47.0");
                wc.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                wc.AddCookie("JSESSIONID", SessionId);
                //  wc.AddCookie("JSESSIONID", "4FF7E0C372CD92C582FCA6345D1BA0C8");

                wc.AddParameter("ruleId", pulserules);
                wc.AddParameter("matchExp", a.ngt_regionName);
                wc.AddParameter("countryName", a.Description);
                wc.AddParameter("ruleValue", a.Call_rate);
                wc.AddParameter("typeCode", "destination");
                wc.AddParameter("pulseRules", ruleid);
                wc.AddParameter("rateSheetType", companytype == true ? "2" : "1");
                wc.AddParameter("permitType", false);
                wc.AddParameter("profitChecking", false);
                wc.AddParameter("countryNotExist", "");
                wc.AddParameter("seqNo", "");
                wc.AddParameter("addSeqNo", "");
                wc.AddParameter("flag", false);
                wc.AddParameter("ruleName", ratesheetName + "_addressRule");
                wc.AddParameter("ruleId", pulserules);
                wc.AddParameter("subscriberId", "admin");

                wc.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["WcTimeout"]);
                restClient1.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["RcTimeout"]);

                ServicePointManager.ServerCertificateValidationCallback =
       delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
       { return true; };

                response1 = restClient1.Execute(wc);
            }
            UpdateError("OldNexge UpdateRatesheet ADDRatesheet Response", response1.Content, "RatesheetLogs.txt", "", false);
            #endregion

            #region UpdatedRatesheet
            if (RatesheetData.Count != 0)
            {
                // Url1
                string URI4 = "http://" + ip + "/InterUserBillingServlet?actionId=createAddressRule";

                wc = new RestRequest(URI4, Method.POST);
                wc.AddHeader("Accept", "text / html,application / xhtml + xml,application / xml; q = 0.9,*/*;q=0.8");
                wc.AddHeader("Accept-Encoding", "gzip, deflate");
                wc.AddHeader("Accept-Language", "en-US,en;q=0.5");
                wc.AddHeader("Cookie", "JSESSIONID=" + SessionId);
                //  wc.AddHeader("Cookie", "JSESSIONID=4FF7E0C372CD92C582FCA6345D1BA0C8");
                wc.AddHeader("Referer", "http://" + ip + "/admin/billingadmin/Navigate.jsp");
                wc.AddHeader("Host", ip);
                wc.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:47.0) Gecko/20100101 Firefox/47.0");
                wc.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                wc.AddCookie("JSESSIONID", SessionId);
                // wc.AddCookie("JSESSIONID", "4FF7E0C372CD92C582FCA6345D1BA0C8");

                wc.AddParameter("ruleId", pulserules);
                wc.AddParameter("matchExp", "");
                wc.AddParameter("ruleValue", "");
                wc.AddParameter("typeCode", "destination");
                wc.AddParameter("pulseRules", ruleid);
                wc.AddParameter("rateSheetType", companytype == true ? "2" : "1");
                wc.AddParameter("permitType", false);
                wc.AddParameter("profitChecking", false);
                wc.AddParameter("countryNotExist", "");
                wc.AddParameter("seqNo", "");
                wc.AddParameter("addSeqNo", "");
                wc.AddParameter("flag", false);
                wc.AddParameter("ruleName", ratesheetName + "_addressRule");
                wc.AddParameter("ruleId", pulserules);
                wc.AddParameter("subscriberId", "admin");

                wc.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["WcTimeout"]);
                restClient1.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["RcTimeout"]);

                ServicePointManager.ServerCertificateValidationCallback =
       delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
       { return true; };

                response1 = restClient1.Execute(wc);
            }
            else
            {
                UpdateError("OldNexge UpdateRatesheet Update Ratesheet Final Response", "Record Count: Zero", "RatesheetLogs.txt", "", false);
                return new string[] { "true", "Ratesheet Contains Zero Record.", "Ratesheet Update Success" };
            }

            #endregion

            #region commented  

            //         // Url3
            //         string URI3 = "http://" + ip + "/admin/billingadmin/UpdateBillingPlan.jsp?actionId=upload";
            //         wc = new RestRequest(URI3, Method.POST);

            //         wc.AddHeader("Accept", "text / html, application / xhtml + xml, application / xml; q = 0.9,*/*;q=0.8");
            //         wc.AddHeader("Accept-Encoding", "gzip, deflate");
            //         wc.AddHeader("Accept-Language", "en-US,en;q=0.5");

            //         wc.AddHeader("Cookie", "JSESSIONID=" + SessionId);
            //         wc.AddHeader("Cookie", "JSESSIONID=E05F2CCD3289D3DDE1AC1271DA800021");

            //         wc.AddHeader("Host", ip);
            //         wc.AddHeader("Referer", "http://" + ip + "/admin/billingadmin/UpdatePlan.jsp?bpCode=" + ratesheetName + "&planType=null&currencyType= &actionId = displayUpdatebillingPlan");
            //         wc.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:47.0) Gecko/20100101 Firefox/47.0");
            //         wc.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            //         wc.AddCookie("JSESSIONID", SessionId);

            //         wc.AddCookie("JSESSIONID", "E05F2CCD3289D3DDE1AC1271DA800021");
            //         wc.AddParameter("addressRuleId", addressruleid);
            //         wc.AddParameter("bpCode", ratesheetName);
            //         wc.AddParameter("bpDescription", ratesheetName);
            //         wc.AddParameter("bpName", ratesheetName);
            //         wc.AddParameter("connectionCharge", 0);
            //         wc.AddParameter("gracePeriod", 0);
            //         wc.AddParameter("minCallDuration", 0);
            //         wc.AddParameter("nextPulse", 1);
            //         wc.AddParameter("noOfStartPulses", 1);
            //         wc.AddParameter("pulseRuleId", pulseruleid);
            //         wc.AddParameter("rateSheetType", 2);
            //         wc.AddParameter("startPulse", 1);
            //         wc.AddParameter("subscriberId", "admin");
            //         wc.AddParameter("uploadedVal", true);


            //         wc.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["WcTimeout"]);
            //         restClient1.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["RcTimeout"]);

            //         ServicePointManager.ServerCertificateValidationCallback =
            //delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            //{ return true; };

            //         response1 = restClient1.Execute(wc);

            #endregion

            #region response

            UpdateError("OldNexge UpdateRatesheet Update Response", response1.Content, "RatesheetLogs.txt", "", false);

            if (response1.Content.Contains("Address Rule has been successfully added"))
            {
                UpdateError("OldNexge UpdateRatesheet Update Response Success:-", response1.Content, "RatesheetLogs.txt", "  Create Address Rule", false);

                return new string[] { "true", "Ratesheet Updated Successfully", "Ratesheet Update Success" };
            }
            else
            {
                UpdateError("OldNexge UpdateRatesheet Update Response Error:-", response1.Content, "RatesheetLogs.txt", "Create Address Rule Error", false);

                // return resolveXml(Encoding.ASCII.GetString(response));
                return new string[] { "false", "Ratesheet Error.", "Ratesheet Update Success" };
            }

            #endregion


        }

        private static string[] UpdateReplaceOldnexgeRatesheet(List<RMSServices.RatesheetV2.ngt_rateslist> RatesheetData, string ratesheetName, string ip, string owner, string password, bool companytype, string Ratesheetid, string RequestId, int serverid, string ratesheetnamerms)
        {
            string[] rUpload = null;
            SessionId = AuthicateoldLogin(ip, owner, password);

            RestClient restClient1 = new RestClient();
            restClient1.CookieContainer = new CookieContainer();

            #region ViewAddressrule

            // Url1
            string URI = "http://" + ip + "/InterUserBillingServlet?actionId=viewAddressRule";
            RestRequest wc = new RestRequest(URI, Method.GET);
            wc.AddHeader("Accept", "text / html,application / xhtml + xml,application / xml; q = 0.9,*/*;q=0.8");
            wc.AddHeader("Accept-Encoding", "gzip, deflate");
            wc.AddHeader("Accept-Language", "en-US,en;q=0.5");
            wc.AddHeader("Cookie", "JSESSIONID=" + SessionId);
            // wc.AddHeader("Cookie", "JSESSIONID=FAB3207A939AB04F558744E954F8DD20");

            wc.AddHeader("Host", ip);
            wc.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:47.0) Gecko/20100101 Firefox/47.0");
            wc.AddCookie("JSESSIONID", SessionId);
            //  wc.AddCookie("JSESSIONID", "FAB3207A939AB04F558744E954F8DD20");
            wc.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["WcTimeout"]);
            restClient1.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["RcTimeout"]);

            ServicePointManager.ServerCertificateValidationCallback =
            delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            { return true; };

            IRestResponse response1 = restClient1.Execute(wc);
            string result = "";
            string pulserules = "";
            string ruleid = "";
            try
            {
                if (response1.Content.Contains("> " + ratesheetName + "_addressRule"))
                {
                    string[] arrr_CodecData = response1.Content.Split(new string[] { "> " + ratesheetName + "_addressRule" }, StringSplitOptions.None);
                    int index1 = arrr_CodecData[0].LastIndexOf('=');
                    int index = arrr_CodecData[0].Length;
                    int ncount = index - index1;
                    result = GetLast(arrr_CodecData[0], ncount);
                    result = result.Replace('=', ' ').Replace('"', ' ').Replace(" ", "");
                }
                else
                {
                    UpdateError("OldNexge UpdateRatesheet Response Success:-", response1.Content, "RatesheetLogs.txt", "  View AddressRule Error", false);

                    return new string[] { "false", "Ratesheet Updated Successfully", "Ratesheet Update Error" };
                }
                pulserules = result.Split(',')[0].ToString();
                ruleid = (Convert.ToInt32(pulserules) - 1).ToString();
            }
            catch (Exception ex)
            {
                UpdateError("OldNexge UpdateRatesheet Response Success:-", response1.Content, "RatesheetLogs.txt", "  View AddressRule Error", false);

                return new string[] { "false", "Ratesheet Updated Successfully", "Ratesheet Update Error" };
            }
            UpdateError("OldNexge UpdateRatesheet Response Success:-", "", "RatesheetLogs.txt", "  View AddressRule Completed Successfully", false);

            #endregion

            #region generateaddressrule

            string URIgenerate = "http://" + ip + "/admin/billingadmin/ViewAddressRulesBySearch.jsp?actionId=uploadedAddress&ruleid=" + pulserules + "& ruleName=" + ratesheetName + "_addressRule&subscriberId=admin";
            wc = new RestRequest(URIgenerate, Method.POST);
            wc.AddHeader("Accept", "text / html,application / xhtml + xml,application / xml; q = 0.9,*/*;q=0.8");
            wc.AddHeader("Accept-Encoding", "gzip, deflate");
            wc.AddHeader("Accept-Language", "en-US,en;q=0.5");
            wc.AddHeader("Cookie", "JSESSIONID=" + SessionId);
            wc.AddHeader("Referer", "http://" + ip + "/admin/billingadmin/ViewAddressRulesBySearch.jsp?ruleId=" + pulserules + "& ruleName=" + ratesheetName + "_addressRule & subscriberId=admin");
            wc.AddHeader("Host", ip);
            wc.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:47.0) Gecko/20100101 Firefox/47.0");
            wc.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            wc.AddCookie("JSESSIONID", SessionId);

            wc.AddParameter("actionId", "uploadedAddress");
            wc.AddParameter("ruleid", pulserules);
            wc.AddParameter("ruleName", ratesheetName + "_addressRule");
            wc.AddParameter("subscriberId", "admin");



            wc.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["WcTimeout"]);
            restClient1.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["RcTimeout"]);

            ServicePointManager.ServerCertificateValidationCallback =
            delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            { return true; };

            response1 = restClient1.Execute(wc);

            if (!response1.Content.Contains("AddressRule is Exported Successfully"))
                return new string[] { "false", "Ratesheet Generated Failure", "Ratesheet generate fail" };


            UpdateError("OldNexge UpdateRatesheet Response Success:-", "", "RatesheetLogs.txt", "  Generate AddressRule Completed Successfully", false);
            #endregion

            #region DownloadAddressrule

            string URIDownload = "http://" + ip + "/templateFiles/" + ratesheetName + "_addressRule.csv";
            wc = new RestRequest(URIDownload, Method.GET);
            wc.AddHeader("Accept", "text / html,application / xhtml + xml,application / xml; q = 0.9,*/*;q=0.8");
            wc.AddHeader("Accept-Encoding", "gzip, deflate");
            wc.AddHeader("Accept-Language", "en-US,en;q=0.5");
            wc.AddHeader("Cookie", "JSESSIONID=" + SessionId);
            // wc.AddHeader("Cookie", "JSESSIONID=FAB3207A939AB04F558744E954F8DD20");

            wc.AddHeader("Host", ip);
            wc.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:47.0) Gecko/20100101 Firefox/47.0");
            wc.AddCookie("JSESSIONID", SessionId);
            //  wc.AddCookie("JSESSIONID", "FAB3207A939AB04F558744E954F8DD20");
            wc.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["WcTimeout"]);
            restClient1.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["RcTimeout"]);

            ServicePointManager.ServerCertificateValidationCallback =
            delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            { return true; };


            response1 = restClient1.Execute(wc);

            if (response1.Content.Contains("The requested resource"))
                return new string[] { "false", "Template Generated Failure", "Template generate fail" };

            string[] arrr_RateData = response1.Content.Split(new string[] { "\n" }, StringSplitOptions.None);

            RateDetails objRateClass = null;
            List<RateDetails> listRateDetails = new List<RateDetails>();
            string _code = string.Empty;
            string _codeName = string.Empty;
            string _Rate = string.Empty;
            string _pulseid = string.Empty;
            string _Status = string.Empty;


            for (int i = 0; i < arrr_RateData.Length - 1; i++)
            {
                if (i == 0)
                    continue;

                try
                {
                    arrr_RateData[i] = arrr_RateData[i].Contains("Others,") ? arrr_RateData[i].Replace(", Others,", " Others") : arrr_RateData[i].Contains(", ") ? arrr_RateData[i].Replace(", ", " ") : arrr_RateData[i];
                    string[] arr_RateNew = arrr_RateData[i].Split(',');
                    if (arr_RateNew[0].Trim().ToString() != "")
                    {
                        if (arr_RateNew.Length == 5)
                        {
                            _code = arr_RateNew[0].Trim().ToString();
                            _codeName = arr_RateNew[1].Replace('"', ' ').Replace(" ", "").Trim().ToString();
                            _Rate = arr_RateNew[2].Trim().ToString();
                            _pulseid = arr_RateNew[3].Trim().ToString();
                            _Status = arr_RateNew[4].Trim().ToString();
                        }
                        else if (arr_RateNew.Length == 6)
                        {
                            _code = arr_RateNew[0].Trim().ToString();
                            _codeName = arr_RateNew[1].Replace('"', ' ').Replace(" ", "").Trim().ToString() + " " + arr_RateNew[2].Replace('"', ' ').Replace(" ", "").Trim().ToString();
                            _Rate = arr_RateNew[3].Trim().ToString();
                            _pulseid = arr_RateNew[4].Trim().ToString();
                            _Status = arr_RateNew[5].Trim().ToString();
                        }
                        else if (arr_RateNew.Length == 7)
                        {
                            _code = arr_RateNew[0].Trim().ToString();
                            _codeName = arr_RateNew[1].Replace('"', ' ').Replace(" ", "").Trim().ToString() + " " + arr_RateNew[2].Replace('"', ' ').Replace(" ", "").Trim().ToString() + " " + arr_RateNew[3].Replace('"', ' ').Replace(" ", "").Trim().ToString();
                            _Rate = arr_RateNew[4].Trim().ToString();
                            _pulseid = arr_RateNew[5].Trim().ToString();
                            _Status = arr_RateNew[6].Trim().ToString();
                        }
                        else
                        {
                            _code = arr_RateNew[0].Trim().ToString();
                            _codeName = arr_RateNew[1].Replace('"', ' ').Replace(" ", "").Trim().ToString() + " " + arr_RateNew[2].Replace('"', ' ').Replace(" ", "").Trim().ToString() + " " + arr_RateNew[3].Replace('"', ' ').Replace(" ", "").Trim().ToString() + " " + arr_RateNew[4].Replace('"', ' ').Replace(" ", "").Trim().ToString();
                            _Rate = arr_RateNew[5].Trim().ToString();
                            _pulseid = arr_RateNew[6].Trim().ToString();
                            _Status = arr_RateNew[7].Trim().ToString();
                        }


                        objRateClass = new RateDetails() { Code = _code, CodeName = _codeName, Rate = _Rate, Status = _Status, pulseid = _pulseid };
                        listRateDetails.Add(objRateClass);
                    }
                }
                catch (Exception ex)
                {
                    UpdateError("OldNexge DownloadAddressrule Error:", "  Error:" + ex.Message, "RatesheetLogs.txt", "Data Count: " + i.ToString(), false);

                    return new string[] { "false", "Template Generated Failure", "Template generate fail" };
                }


            }
            UpdateError("OldNexge UpdateRatesheet Response Success:-", "", "RatesheetLogs.txt", "  Download AddressRule Completed Successfully", false);
            #endregion

            #region FutureUpdate

            try
            {

                List<List<RMSServices.RatesheetV2.ngt_rateslist>> EffectiveRateSheet = new List<List<RMSServices.RatesheetV2.ngt_rateslist>>();

                List<List<RMSServices.RatesheetV2.ngt_rateslist>> groups = RatesheetData.OrderByDescending(x => x.Effective_on).GroupBy(x => x.Effective_on).Select(grp => grp.ToList()).ToList();

                foreach (var group in groups)
                {
                    if (Convert.ToDateTime(group[0].Effective_on).Date > DateTime.UtcNow)
                        EffectiveRateSheet.Add(group);
                }

                if (EffectiveRateSheet.Count > 0)
                {
                    foreach (var group in EffectiveRateSheet)
                    {
                        string RatesheetFutureFilePath = ConversiontoOldNexegeFuture(group);

                        using (rmsEntities rms = new rmsEntities())
                        {
                            oldnexgeupdatepushjobqueue updateoldnexge = new oldnexgeupdatepushjobqueue();
                            updateoldnexge.JobStatus = "Pending";
                            updateoldnexge.RequestType = "Update";
                            updateoldnexge.FileNameCSV = RatesheetFutureFilePath;
                            updateoldnexge.RequestID = RequestId;
                            updateoldnexge.RatesheetName = ratesheetnamerms;
                            updateoldnexge.ServerId = serverid;
                            updateoldnexge.ServerRatesheetNmae = ratesheetName;
                            updateoldnexge.CompanyType = companytype == true ? 2 : 1;
                            updateoldnexge.EffectiveOn = Convert.ToDateTime(group[0].Effective_on);
                            updateoldnexge.RateSheetId = Ratesheetid;

                            rms.oldnexgeupdatepushjobqueues.Add(updateoldnexge);
                            rms.SaveChanges();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateError("OldNexge UpdateReplaceRatesheet Response Error:-", ex.Message, "RatesheetLogs.txt", "  Update Future Date", false);
            }
            UpdateError("OldNexge UpdateRatesheet Response Success:-", "", "RatesheetLogs.txt", "  Future Update Completed Successfully", false);
            #endregion

            #region CreateRatesheet

            //  var Regioncode = listRateDetails.Select(k => k.Code).ToList();

            RatesheetData = RatesheetData.Where(t => Convert.ToDateTime(t.Effective_on) <= DateTime.Now).ToList();

            UpdateError("OldNexge RatesheetData Data Success:-", "Count:" + RatesheetData.Count, "RatesheetLogs.txt", "Data Count Success", false);

            var RegioncodeList = RatesheetData.Select(k => k.ngt_regionName).ToList();

            UpdateError("OldNexge RegioncodeList Data Success:-", "Count:" + RegioncodeList.Count, "RatesheetLogs.txt", "RegioncodeList Count Success", false);

            List<RateDetails> Listdata = listRateDetails.Where(t => t.Status != "NULL" & !RegioncodeList.Contains(t.Code)).ToList();

            UpdateError("OldNexge Listdata Data Success:-", "Count:" + Listdata.Count, "RatesheetLogs.txt", "Listdata Count Success", false);

            try
            {
                foreach (var a in Listdata)
                {
                    RatesheetData.Add(new RMSServices.RatesheetV2.ngt_rateslist()
                    {
                        Id = 1,
                        Effective_on = String.Format("{0:yyyy-MM-dd HH:mm:ss}", DateTime.Now),
                        Call_rate = Convert.ToDecimal(a.Rate),
                        Grace_period = 1,
                        Minimal_time = 1,
                        Resolution = 1,
                        ngt_region_codeId = 1,
                        //shripal ngt_regionName = rate.ngt_region_code.Country_code,
                        ngt_regionName = a.Code,
                        Description = a.CodeName,
                        Connection_charge = 1,
                        Active = a.Status.ToUpper() == "TRUE" ? "B" : "A",
                        Priority = 1,
                        Trunck = "1",
                        BatchID = 1,
                        CreatedOn = String.Format("{0:yyyy-MM-dd HH:mm:ss}", DateTime.Now),
                        StartPulse = "1",
                        NextPulse = "1",
                        EndDate = String.Format("{0:yyyy-MM-dd HH:mm:ss}", DateTime.Now)
                    });
                }
            }
            catch (Exception ex)
            {
                UpdateError("OldNexge UpdateRatesheet ADDRatesheet before Response Error", "  Data Count: " + RatesheetData.Count, "RatesheetLogs.txt", " Error:" + ex.Message, false);
                return new string[] { "false", "Template Generated Failure", "Template generate fail" };
            }

            UpdateError("OldNexge UpdateRatesheet ADDRatesheet before Response", "  Data Count: " + RatesheetData.Count, "RatesheetLogs.txt", "", false);

            #endregion

            #region generateratesheet

            string RatesheetFilePath = ConversiontoOldNexege(RatesheetData);
            byte[] DataNexge = null;
            if (RatesheetData.Count != 0)
            {
                FileStream fsdelete = new FileStream(RatesheetFilePath, FileMode.Open, FileAccess.Read);
                DataNexge = new byte[fsdelete.Length];
                fsdelete.Read(DataNexge, 0, DataNexge.Length);
                fsdelete.Close();
            }

            #endregion

            #region UpdatePlan

            if (SessionId != "")
            {
                string[] UploadRatesheet = UploadOldNexegeRatesheet(RatesheetFilePath, ip, SessionId);
                UpdateError("OldNexge MappingRatesheet Method UploadReplaceOldNexegeRatesheet", UploadRatesheet[0], "RatesheetLogs.txt", UploadRatesheet[1], false);
                if (UploadRatesheet[0] != "false")
                {

                     rUpload = verifyRatesheet(RatesheetFilePath, ip);
                    UpdateError("OldNexge MappingRatesheet Method Verify ratesheet UploadReplaceOldNexegeRatesheet", " Status: " + rUpload[0] + "  Error Code: " + rUpload[1], "RatesheetLogs.txt", RatesheetFilePath, false);
                    if (rUpload[0] == "true")
                    {
                         rUpload = activateRatesheet(RatesheetFilePath, ratesheetName, ip, DataNexge);
                        if (rUpload[0] == "true")
                        {
                            return new string[] { "true", "Billing Plan Updated Successfully for Billing Plan " + ratesheetName, "Ratesheet Update Error" };
                        }
                        else
                        {
                            return new string[] { "false", rUpload[1] + " for Billing Plan " + ratesheetName, "Ratesheet Update Error" };

                        }
                    }
                    else
                    {
                        return new string[] { "false", rUpload[1] + " for Billing Plan " + ratesheetName, "Ratesheet Update Error" };

                    }
                }
                else
                {
                    return new string[] { "false", UploadRatesheet[1] + " for Billing Plan " + ratesheetName, "Ratesheet Update Error" };

                }
            }
            else
            {
                return new string[] { "false", "Not able to Login.Invalid Server Details Found for the Selected Server.", "Ratesheet Update Error" };

            }
            #endregion
        }

        private static string[] UpdateReplaceFutureOldnexgeRatesheet(List<RMSServices.RatesheetV2.ngt_rateslist> RatesheetData, string ratesheetName, string ip, string owner, string password, bool companytype)
        {
            string[] rUpload = null;
            SessionId = AuthicateoldLogin(ip, owner, password);

            RestClient restClient1 = new RestClient();
            restClient1.CookieContainer = new CookieContainer();

            #region ViewAddressrule

            // Url1
            string URI = "http://" + ip + "/InterUserBillingServlet?actionId=viewAddressRule";
            RestRequest wc = new RestRequest(URI, Method.GET);
            wc.AddHeader("Accept", "text / html,application / xhtml + xml,application / xml; q = 0.9,*/*;q=0.8");
            wc.AddHeader("Accept-Encoding", "gzip, deflate");
            wc.AddHeader("Accept-Language", "en-US,en;q=0.5");
            wc.AddHeader("Cookie", "JSESSIONID=" + SessionId);
            // wc.AddHeader("Cookie", "JSESSIONID=FAB3207A939AB04F558744E954F8DD20");

            wc.AddHeader("Host", ip);
            wc.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:47.0) Gecko/20100101 Firefox/47.0");
            wc.AddCookie("JSESSIONID", SessionId);
            //  wc.AddCookie("JSESSIONID", "FAB3207A939AB04F558744E954F8DD20");
            wc.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["WcTimeout"]);
            restClient1.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["RcTimeout"]);

            ServicePointManager.ServerCertificateValidationCallback =
            delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            { return true; };

            IRestResponse response1 = restClient1.Execute(wc);
            string result = "";
            if (response1.Content.Contains("> " + ratesheetName + "_addressRule"))
            {
                string[] arrr_CodecData = response1.Content.Split(new string[] { "> " + ratesheetName + "_addressRule" }, StringSplitOptions.None);
                int index1 = arrr_CodecData[0].LastIndexOf('=');
                int index = arrr_CodecData[0].Length;
                int ncount = index - index1;
                result = GetLast(arrr_CodecData[0], ncount);
                result = result.Replace('=', ' ').Replace('"', ' ').Replace(" ", "");
            }
            else
            {
                UpdateError("OldNexge UpdateRatesheet Response Success:-", response1.Content, "RatesheetLogs.txt", "  View AddressRule Error", false);

                return new string[] { "false", "Ratesheet Updated Successfully", "Ratesheet Update Error" };
            }
            string pulserules = result.Split(',')[0].ToString();
            string ruleid = (Convert.ToInt32(pulserules) - 1).ToString();

            #endregion

            #region generateaddressrule

            string URIgenerate = "http://" + ip + "/admin/billingadmin/ViewAddressRulesBySearch.jsp?actionId=uploadedAddress&ruleid=" + pulserules + "& ruleName=" + ratesheetName + "_addressRule&subscriberId=admin";
            wc = new RestRequest(URIgenerate, Method.POST);
            wc.AddHeader("Accept", "text / html,application / xhtml + xml,application / xml; q = 0.9,*/*;q=0.8");
            wc.AddHeader("Accept-Encoding", "gzip, deflate");
            wc.AddHeader("Accept-Language", "en-US,en;q=0.5");
            wc.AddHeader("Cookie", "JSESSIONID=" + SessionId);
            wc.AddHeader("Referer", "http://" + ip + "/admin/billingadmin/ViewAddressRulesBySearch.jsp?ruleId=" + pulserules + "& ruleName=" + ratesheetName + "_addressRule & subscriberId=admin");
            wc.AddHeader("Host", ip);
            wc.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:47.0) Gecko/20100101 Firefox/47.0");
            wc.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            wc.AddCookie("JSESSIONID", SessionId);

            wc.AddParameter("actionId", "uploadedAddress");
            wc.AddParameter("ruleid", pulserules);
            wc.AddParameter("ruleName", ratesheetName + "_addressRule");
            wc.AddParameter("subscriberId", "admin");



            wc.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["WcTimeout"]);
            restClient1.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["RcTimeout"]);

            ServicePointManager.ServerCertificateValidationCallback =
            delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            { return true; };

            response1 = restClient1.Execute(wc);

            if (!response1.Content.Contains("AddressRule is Exported Successfully"))
                return new string[] { "false", "Ratesheet Generated Failure", "Ratesheet generate fail" };


            #endregion

            #region DownloadAddressrule

            string URIDownload = "http://" + ip + "/templateFiles/" + ratesheetName + "_addressRule.csv";
            wc = new RestRequest(URIDownload, Method.GET);
            wc.AddHeader("Accept", "text / html,application / xhtml + xml,application / xml; q = 0.9,*/*;q=0.8");
            wc.AddHeader("Accept-Encoding", "gzip, deflate");
            wc.AddHeader("Accept-Language", "en-US,en;q=0.5");
            wc.AddHeader("Cookie", "JSESSIONID=" + SessionId);
            // wc.AddHeader("Cookie", "JSESSIONID=FAB3207A939AB04F558744E954F8DD20");

            wc.AddHeader("Host", ip);
            wc.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:47.0) Gecko/20100101 Firefox/47.0");
            wc.AddCookie("JSESSIONID", SessionId);
            //  wc.AddCookie("JSESSIONID", "FAB3207A939AB04F558744E954F8DD20");
            wc.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["WcTimeout"]);
            restClient1.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["RcTimeout"]);

            ServicePointManager.ServerCertificateValidationCallback =
            delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            { return true; };


            response1 = restClient1.Execute(wc);

            if (response1.Content.Contains("The requested resource"))
                return new string[] { "false", "Template Generated Failure", "Template generate fail" };

            string[] arrr_RateData = response1.Content.Split(new string[] { "\n" }, StringSplitOptions.None);

            RateDetails objRateClass = null;
            List<RateDetails> listRateDetails = new List<RateDetails>();
            string _code = string.Empty;
            string _codeName = string.Empty;
            string _Rate = string.Empty;
            string _pulseid = string.Empty;
            string _Status = string.Empty;


            for (int i = 0; i < arrr_RateData.Length - 1; i++)
            {
                if (i == 0)
                    continue;

                arrr_RateData[i] = arrr_RateData[i].Contains("Others,") ? arrr_RateData[i].Replace(", Others,", " Others") : arrr_RateData[i].Contains(", ") ? arrr_RateData[i].Replace(", ", " ") : arrr_RateData[i];
                string[] arr_RateNew = arrr_RateData[i].Split(',');
                if (arr_RateNew[0].Trim().ToString() != "")
                {
                    if (arr_RateNew.Length == 5)
                    {
                        _code = arr_RateNew[0].Trim().ToString();
                        _codeName = arr_RateNew[1].Replace('"', ' ').Replace(" ", "").Trim().ToString();
                        _Rate = arr_RateNew[2].Trim().ToString();
                        _pulseid = arr_RateNew[3].Trim().ToString();
                        _Status = arr_RateNew[4].Trim().ToString();
                    }
                    else if (arr_RateNew.Length == 6)
                    {
                        _code = arr_RateNew[0].Trim().ToString();
                        _codeName = arr_RateNew[1].Replace('"', ' ').Replace(" ", "").Trim().ToString() + " " + arr_RateNew[2].Replace('"', ' ').Replace(" ", "").Trim().ToString();
                        _Rate = arr_RateNew[3].Trim().ToString();
                        _pulseid = arr_RateNew[4].Trim().ToString();
                        _Status = arr_RateNew[5].Trim().ToString();
                    }
                    else if (arr_RateNew.Length == 7)
                    {
                        _code = arr_RateNew[0].Trim().ToString();
                        _codeName = arr_RateNew[1].Replace('"', ' ').Replace(" ", "").Trim().ToString() + " " + arr_RateNew[2].Replace('"', ' ').Replace(" ", "").Trim().ToString() + " " + arr_RateNew[3].Replace('"', ' ').Replace(" ", "").Trim().ToString();
                        _Rate = arr_RateNew[4].Trim().ToString();
                        _pulseid = arr_RateNew[5].Trim().ToString();
                        _Status = arr_RateNew[6].Trim().ToString();
                    }
                    else
                    {
                        _code = arr_RateNew[0].Trim().ToString();
                        _codeName = arr_RateNew[1].Replace('"', ' ').Replace(" ", "").Trim().ToString() + " " + arr_RateNew[2].Replace('"', ' ').Replace(" ", "").Trim().ToString() + " " + arr_RateNew[3].Replace('"', ' ').Replace(" ", "").Trim().ToString() + " " + arr_RateNew[4].Replace('"', ' ').Replace(" ", "").Trim().ToString();
                        _Rate = arr_RateNew[5].Trim().ToString();
                        _pulseid = arr_RateNew[6].Trim().ToString();
                        _Status = arr_RateNew[7].Trim().ToString();
                    }


                    objRateClass = new RateDetails() { Code = _code, CodeName = _codeName, Rate = _Rate, Status = _Status, pulseid = _pulseid };
                    listRateDetails.Add(objRateClass);
                }

            }

            #endregion

            #region CreateRatesheet        

            var RegioncodeList = RatesheetData.Select(k => k.ngt_regionName).ToList();

            List<RateDetails> Listdata = listRateDetails.Where(t => t.Status != "NULL" & !RegioncodeList.Contains(t.Code)).ToList();

            foreach (var a in Listdata)
            {
                RatesheetData.Add(new RMSServices.RatesheetV2.ngt_rateslist()
                {
                    Id = 1,
                    Effective_on = String.Format("{0:yyyy-MM-dd HH:mm:ss}", DateTime.Now),
                    Call_rate = Convert.ToDecimal(a.Rate),
                    Grace_period = 1,
                    Minimal_time = 1,
                    Resolution = 1,
                    ngt_region_codeId = 1,
                    //shripal ngt_regionName = rate.ngt_region_code.Country_code,
                    ngt_regionName = a.Code,
                    Description = a.CodeName,
                    Connection_charge = 1,
                    Active = a.Status.ToUpper() == "TRUE" ? "B" : "A",
                    Priority = 1,
                    Trunck = "1",
                    BatchID = 1,
                    CreatedOn = String.Format("{0:yyyy-MM-dd HH:mm:ss}", DateTime.Now),
                    StartPulse = "1",
                    NextPulse = "1",
                    EndDate = String.Format("{0:yyyy-MM-dd HH:mm:ss}", DateTime.Now)
                });
            }

            UpdateError("OldNexge UpdateRatesheet ADDRatesheet before Response", "  Data Count: " + RatesheetData.Count.ToString(), "RatesheetLogs.txt", "", false);

            #endregion

            #region generateratesheet

            string RatesheetFilePath = ConversiontoOldNexege(RatesheetData);
            byte[] DataNexge = null;
            if (RatesheetData.Count != 0)
            {
                FileStream fsdelete = new FileStream(RatesheetFilePath, FileMode.Open, FileAccess.Read);
                DataNexge = new byte[fsdelete.Length];
                fsdelete.Read(DataNexge, 0, DataNexge.Length);
                fsdelete.Close();
            }

            #endregion

            #region UpdatePlan

            if (SessionId != "")
            {
                string[] UploadRatesheet = UploadOldNexegeRatesheet(RatesheetFilePath, ip, SessionId);
                UpdateError("OldNexge MappingRatesheet Method UploadReplaceOldNexegeRatesheet", UploadRatesheet[0], "RatesheetLogs.txt", UploadRatesheet[1], false);
                if (UploadRatesheet[0] != "false")
                {

                    rUpload = verifyRatesheet(RatesheetFilePath, ip);
                    UpdateError("OldNexge MappingRatesheet Method Verify ratesheet UploadReplaceOldNexegeRatesheet", " Status: " + rUpload[0] + "  Error Code: " + rUpload[1], "RatesheetLogs.txt", RatesheetFilePath, false);
                    if (rUpload[0] == "true")
                    {
                        rUpload = activateRatesheet(RatesheetFilePath, ratesheetName, ip, DataNexge);
                        if (rUpload[0] == "true")
                        {
                            return new string[] { "true", "Billing Plan Updated Successfully for Billing Plan " + ratesheetName, "Ratesheet Update Error" };
                        }
                        else
                        {
                            return new string[] { "false", rUpload[1] + " for Billing Plan " + ratesheetName, "Ratesheet Update Error" };

                        }
                    }
                    else
                    {
                        return new string[] { "false", rUpload[1] + " for Billing Plan " + ratesheetName, "Ratesheet Update Error" };

                    }
                }
                else
                {
                    return new string[] { "false", UploadRatesheet[1] + " for Billing Plan " + ratesheetName, "Ratesheet Update Error" };

                }
            }
            else
            {
                return new string[] { "false", "Not able to Login.Invalid Server Details Found for the Selected Server.", "Ratesheet Update Error" };

            }
            #endregion
        }
        private static string[] ReplaceFutureOldnexgeRatesheet(List<RMSServices.RatesheetV2.ngt_rateslist> RatesheetData, string ratesheetName, string ip, string owner, string password, bool companytype)
        {
            string[] rUpload = null;
            SessionId = AuthicateoldLogin(ip, owner, password);

            RestClient restClient1 = new RestClient();
            restClient1.CookieContainer = new CookieContainer();

            #region generateratesheet

            string RatesheetFilePath = ConversiontoOldNexege(RatesheetData);
            byte[] DataNexge = null;
            if (RatesheetData.Count != 0)
            {
                FileStream fsdelete = new FileStream(RatesheetFilePath, FileMode.Open, FileAccess.Read);
                DataNexge = new byte[fsdelete.Length];
                fsdelete.Read(DataNexge, 0, DataNexge.Length);
                fsdelete.Close();
            }

            #endregion

            #region UpdatePlan

            if (SessionId != "")
            {
                string[] UploadRatesheet = UploadOldNexegeRatesheet(RatesheetFilePath, ip, SessionId);
                UpdateError("OldNexge MappingRatesheet Method UploadReplaceOldNexegeRatesheet", UploadRatesheet[0], "RatesheetLogs.txt", UploadRatesheet[1], false);
                if (UploadRatesheet[0] != "false")
                {

                    rUpload = verifyRatesheet(RatesheetFilePath, ip);
                    UpdateError("OldNexge MappingRatesheet Method Verify ratesheet UploadReplaceOldNexegeRatesheet", " Status: " + rUpload[0] + "  Error Code: " + rUpload[1], "RatesheetLogs.txt", RatesheetFilePath, false);
                    if (rUpload[0] == "true")
                    {
                        rUpload = activateRatesheet(RatesheetFilePath, ratesheetName, ip, DataNexge);
                        if (rUpload[0] == "true")
                        {
                            return new string[] { "true", "Billing Plan Updated Successfully for Billing Plan " + ratesheetName, "Ratesheet Update Error" };
                        }
                        else
                        {
                            return new string[] { "false", rUpload[1] + " for Billing Plan " + ratesheetName, "Ratesheet Update Error" };

                        }
                    }
                    else
                    {
                        return new string[] { "false", rUpload[1] + " for Billing Plan " + ratesheetName, "Ratesheet Update Error" };

                    }
                }
                else
                {
                    return new string[] { "false", UploadRatesheet[1] + " for Billing Plan " + ratesheetName, "Ratesheet Update Error" };

                }
            }
            else
            {
                return new string[] { "false", "Not able to Login.Invalid Server Details Found for the Selected Server.", "Ratesheet Update Error" };

            }
            #endregion
        }

        public class RateDetails
        {
            public string Code { get; set; }
            public string CodeName { get; set; }
            public string Rate { get; set; }
            public string pulseid { get; set; }
            public string Status { get; set; }


        }
        public static void CreateCSVFileNexge(DataTable dt, string strFilePath)
        {
            try
            {

                StreamWriter sw = new StreamWriter(strFilePath, false);

                int iColCount = dt.Columns.Count;

                for (int i = 0; i < iColCount; i++)
                {
                    sw.Write(dt.Columns[i]);

                    if (i < iColCount - 1)
                    {

                        sw.Write(",");

                    }

                }

                sw.Write(sw.NewLine);

                foreach (DataRow dr in dt.Rows)
                {

                    for (int i = 0; i < iColCount; i++)
                    {

                        if (!Convert.IsDBNull(dr[i]))
                        {

                            sw.Write(dr[i].ToString());

                        }

                        if (i < iColCount - 1)
                        {

                            sw.Write(",");

                        }

                    }
                    sw.Write(sw.NewLine);
                }

                sw.Close();
            }
            catch (Exception ex)
            {
                UpdateError("UpdateRatesheetServer Update after createCSV", ex.Message, "ParellelRatesheetLogsy.txt", "  " + ex.InnerException.Message, false);
            }
            // return dt;
        }

        private static string[] createRatesheet(string ratesheetName, string ip, string owner, string Description, int? Graceperiod, string pulse, string password, List<RMSServices.RatesheetV2.ngt_rateslist> RatesheetData, bool? status)
        {
            try
            {

                string OldNexgeSampleFilePath = ConversiontoDummyOldNexege();

                FileStream fsoldnexge = new FileStream(OldNexgeSampleFilePath, FileMode.Open, FileAccess.Read);
                byte[] DataOldNexge = new byte[fsoldnexge.Length];
                fsoldnexge.Read(DataOldNexge, 0, DataOldNexge.Length);
                fsoldnexge.Close();

                // Create Login For old nexge

                SessionId = AuthicateoldLogin(ip, owner, password);

                RestClient restClient1 = new RestClient();
                restClient1.CookieContainer = new CookieContainer();

                // Url1
                string URI = "http://" + ip + "/InterUserBillingServlet?actionId=createPlan";

                RestRequest wc = new RestRequest(URI, Method.GET);
                wc.AddHeader("Accept", "text / html,application / xhtml + xml,application / xml; q = 0.9,*/*;q=0.8");
                wc.AddHeader("Accept-Encoding", "gzip, deflate");
                wc.AddHeader("Accept-Language", "en-US,en;q=0.5");
                wc.AddHeader("Cookie", "JSESSIONID=" + SessionId);

                wc.AddHeader("Host", ip);
                wc.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:47.0) Gecko/20100101 Firefox/47.0");
                wc.AddCookie("JSESSIONID", SessionId);
                wc.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["WcTimeout"]);
                restClient1.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["RcTimeout"]);

                ServicePointManager.ServerCertificateValidationCallback =
                delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
               { return true; };

                IRestResponse response1 = restClient1.Execute(wc);
                //  UpdateError("OldNexge MappingRatesheet Error:-", response1.Content, "RatesheetLogs.txt", "", false);

                // Url2
                string URI1 = "http://" + ip + "/admin/billingadmin/UploadFile.jsp?actionId=upload&fileName=" + CsvFileName;
                wc = new RestRequest(URI1, Method.POST);
                wc.AddHeader("Accept", "text / html, application / xhtml + xml, application / xml; q = 0.9,*/*;q=0.8");
                wc.AddHeader("Accept-Encoding", "gzip, deflate");
                wc.AddHeader("Accept-Language", "en-US,en;q=0.5");

                wc.AddHeader("Cookie", "JSESSIONID=" + SessionId);
                // wc.AddHeader("Cookie", "JSESSIONID=E05F2CCD3289D3DDE1AC1271DA800021");

                wc.AddHeader("Host", ip);
                wc.AddHeader("Referer", "http://" + ip + "/admin/billingadmin/UploadFile.jsp");
                wc.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:47.0) Gecko/20100101 Firefox/47.0");
                wc.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                wc.AddCookie("JSESSIONID", SessionId);
                wc.AddFile("file", DataOldNexge, ratesheetName);

                wc.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["WcTimeout"]);
                restClient1.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["RcTimeout"]);

                ServicePointManager.ServerCertificateValidationCallback =
               delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
               { return true; };


                response1 = restClient1.Execute(wc);
                //  UpdateError("OldNexge MappingRatesheet Error:-", response1.Content, "RatesheetLogs.txt", "", false);

                //// Url3
                string URI2 = "http://" + ip + "/admin/billingadmin/CreateBillingPlan.jsp?actionId=upload";
                wc = new RestRequest(URI2, Method.POST);

                wc.AddHeader("Accept", "text / html, application / xhtml + xml, application / xml; q = 0.9,*/*;q=0.8");
                wc.AddHeader("Accept-Encoding", "gzip, deflate");
                wc.AddHeader("Accept-Language", "en-US,en;q=0.5");

                wc.AddHeader("Cookie", "JSESSIONID=" + SessionId);
                // wc.AddHeader("Cookie", "JSESSIONID=E05F2CCD3289D3DDE1AC1271DA800021");

                wc.AddHeader("Host", ip);
                wc.AddHeader("Referer", "http://" + ip + "/InterUserBillingServlet?actionId=createPlan");
                wc.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:47.0) Gecko/20100101 Firefox/47.0");
                wc.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                wc.AddCookie("JSESSIONID", SessionId);


                // wc.AddCookie("JSESSIONID", "E05F2CCD3289D3DDE1AC1271DA800021");

                wc.AddParameter("bpDescription", Description);
                wc.AddParameter("bpName", ratesheetName);
                wc.AddParameter("connectionCharge", 0);
                wc.AddParameter("gracePeriod", 0);
                wc.AddParameter("minCallDuration", 0);
                wc.AddParameter("nextPulse", 1);
                wc.AddParameter("noOfStartPulses", 1);
                wc.AddParameter("rateSheetType", 2);
                wc.AddParameter("startPulse", 1);
                wc.AddParameter("subscriberId", "admin");
                wc.AddParameter("uploadedVal", true);


                wc.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["WcTimeout"]);
                restClient1.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["RcTimeout"]);

                ServicePointManager.ServerCertificateValidationCallback =
               delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
               { return true; };

                response1 = restClient1.Execute(wc);
                SuccessResponse res1 = new SuccessResponse();
                if (response1.Content.Contains("Billing Plan has been successfully Created"))
                {

                    res1 = OldNexege.OldNexgeUpload(ip, owner, password, "Update", Description, ratesheetName, pulse, status, Graceperiod, RatesheetData);
                    if (res1.status == "success")
                        UpdateError("OldNexge UpdateAPI Response", res1.Message, "RatesheetLogs.txt", "", false);

                    UpdateError("OldNexge MappingRatesheet Response", response1.Content, "RatesheetLogs.txt", "Old Nexge Ratesheet Mapped Successfully", false);
                    return new string[] { "true", "Ratesheet Mapped Successfully", "Ratesheet Mapped Successfully" };
                }
                else if (response1.Content.Contains("Billing Plan already exists!!"))
                {
                    res1 = OldNexege.OldNexgeUpload(ip, owner, password, "Update", Description, ratesheetName, pulse, status, Graceperiod, RatesheetData);
                    if (res1.status == "success")
                        UpdateError("OldNexge UpdateAPI Response", res1.Message, "RatesheetLogs.txt", "", false);

                    UpdateError("OldNexge MappingRatesheet Response", response1.Content, "RatesheetLogs.txt", "Old Nexge Ratesheet Mapped Successfully", false);
                    return new string[] { "true", "Ratesheet Mapped Successfully", "Ratesheet Mapped Successfully" };
                }
                else if (response1.Content.Contains("No Details in the file"))
                {
                    UpdateError("OldNexge MappingRatesheet Response", response1.Content, "RatesheetLogs.txt", "Old Nexge Ratesheet Not Mapped Successfully", false);
                    return new string[] { "false", "Ratesheet Not mapped", "" };
                }
                else if (response1.Content.Contains("click here </a> to login again"))
                {
                    UpdateError("OldNexge MappingRatesheet SessionId Login Error:-", response1.Content, "RatesheetLogs.txt", "Old Nexge Ratesheet Not Mapped Successfully", false);
                    return new string[] { "false", "Ratesheet Not mapped", "" };
                }


                else
                {
                    UpdateError("OldNexge MappingRatesheet Error:-", response1.Content, "RatesheetLogs.txt", "Old Nexge Ratesheet Not Mapped Successfully", false);
                    return resolveXml(response1.Content.ToString());
                }

            }
            catch (Exception ex)
            {
                UpdateError("OldNexge MappingRatesheet Error:-", ex.Message, "RatesheetLogs.txt", ex.InnerException.Message, false);
                return new string[] { "false", ex.Message, "" };
            }
        }
        public static void UpdateError(string ErrorMessage, string ErrorDescription, string FileName, string Parametersdata, bool Savetodb)
        {
            try
            {
                string FilePath = System.Web.Hosting.HostingEnvironment.MapPath("~/logs/" + FileName);
                System.IO.StreamWriter file1 = File.AppendText(FilePath);
                file1.WriteLine("\r\n" + DateTime.Now.ToString() + " ");
                file1.WriteLine(ErrorMessage + " ");
                file1.WriteLine(ErrorDescription + " ");
                file1.WriteLine(Parametersdata + " ");
                file1.Close();
                if (Savetodb)
                {
                    //saving the same parameters also to db.

                }
            }
            catch (Exception ex)
            {

            }
        }
        private static string[] verifyRatesheet(string filename, string ip)
        {
            try
            {
                WebClient wc = new WebClient();
                wc.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                byte[] postData = Encoding.ASCII.GetBytes("actionId=verifyRateSheet&xmlData=<?xml version=\"1.0\" encoding=\"UTF-8\"?><xmlData><fileName>" + filename + "</fileName><rateSheetType>2</rateSheetType></xmlData>");
                byte[] response = wc.UploadData("http://" + ip + "/VerifyRateSheet.jsp", "POST", postData);

                return resolveXml(Encoding.ASCII.GetString(response));

            }
            catch (Exception ex)
            {
                return new string[] { "false", ex.Message, "" };
            }

        }


        // pasupathi commeneted Old Code for Nexge login 

        //private static string AuthicateoldLogin(string ipaddress, string user, string password)
        //{
        //    string cookie = "";
        //    //calling login api
        //    try
        //    {
        //        password = Common.Decrypt(password);
        //        string service = String.Format(ConfigurationManager.AppSettings["OldLoginUrl"].ToString(), ipaddress, user, password, ipaddress); ;
        //        cookies = new CookieContainer();
        //        wr = (HttpWebRequest)WebRequest.Create(service);
        //        wr.KeepAlive = true;
        //        wr.CookieContainer = cookies;
        //        WebResponse wresp = wr.GetResponse();
        //        HttpWebResponse response = (HttpWebResponse)wr.GetResponse();
        //        foreach (Cookie Value in response.Cookies)
        //        {
        //            cookie = Value.Value;
        //            break;
        //        }
        //        Stream stream2 = wresp.GetResponseStream();
        //        StreamReader reader2 = new StreamReader(stream2);
        //        string output = reader2.ReadToEnd();


        //        if (output.Contains("Invalid User"))
        //        {
        //            return "";
        //        }
        //        else
        //        {
        //            return cookie;
        //        }
        //    }
        //    catch (Exception ex)
        //    {

        //        return "";
        //    }
        //}

        // pasupathi Created for Old nexge login
        private static string AuthicateoldLogin(string ipaddress, string user, string password)
        {
            string cookie = "";
            //calling login api
            try
            {
                password = Common.Decrypt(password);
                string URI = "http://" + ipaddress + "/InternalUserServlet?actionId=userLogin";
                RestClient restClient1 = new RestClient();
                restClient1.CookieContainer = new System.Net.CookieContainer();

                RestRequest wc = new RestRequest(URI, Method.POST);
                wc.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["WcTimeout"]);

                wc.AddHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                wc.AddHeader("Accept-Encoding", "gzip, deflate");
                wc.AddHeader("Accept-Language", "en-US,en;q=0.5");
                wc.AddHeader("Host", ipaddress);
                wc.AddHeader("Referer", "http://" + ipaddress + "/telservnet/admin/LoginMain.jsp");
                wc.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 6.3; Win64; x64; rv:49.0) Gecko/20100101 Firefox/49.0");
                wc.AddHeader("Content-Type", "application/x-www-form-urlencoded");

                wc.AddParameter("userId", user);
                wc.AddParameter("password", password);
                wc.AddParameter("url", ipaddress);

                ServicePointManager.ServerCertificateValidationCallback =
        delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        { return true; };

                restClient1.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["RcTimeout"]);
                IRestResponse response1 = restClient1.Execute(wc);
                // UpdateError("OldNexge Login Response", response1.Content, "RatesheetLogs.txt", "", false);
                if (response1.Content.ToString().Contains("invalid user"))
                {
                    return "";
                }
                else
                {
                    cookie = response1.Cookies[0].Value;
                    SessionId = cookie;
                    return cookie;
                }
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        private static string AuthicateoldLoginmesoft(string ipaddress, string user, string password)
        {
            string cookie = "";
            //calling login api
            try
            {
                password = Common.Decrypt(password);
                string URI = "http://" + ipaddress + "/InternalUserServlet?actionId=userLogin";
                RestClient restClient1 = new RestClient();
                restClient1.CookieContainer = new System.Net.CookieContainer();

                RestRequest wc = new RestRequest(URI, Method.POST);
                wc.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["WcTimeout"]);

                wc.AddHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                wc.AddHeader("Accept-Encoding", "gzip, deflate");
                wc.AddHeader("Accept-Language", "en-US,en;q=0.5");
                wc.AddHeader("Host", ipaddress);
                wc.AddHeader("Referer", "http://" + ipaddress + "/admin/LoginMain.jsp");
                wc.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 6.3; Win64; x64; rv:49.0) Gecko/20100101 Firefox/49.0");
                wc.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                wc.AddHeader("Origin", "http://" + ipaddress);

                wc.AddParameter("userId", user);
                wc.AddParameter("password", password);
                wc.AddParameter("url", ipaddress);
                wc.AddParameter("x", 37);
                wc.AddParameter("y", 10);

                ServicePointManager.ServerCertificateValidationCallback =
        delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        { return true; };

                restClient1.Timeout = Convert.ToInt32(ConfigurationManager.AppSettings["RcTimeout"]);
                IRestResponse response1 = restClient1.Execute(wc);
                // UpdateError("OldNexge Login Response", response1.Content, "RatesheetLogs.txt", "", false);
                if (response1.Content.ToString().Contains("invalid user"))
                {
                    return "";
                }
                else
                {
                    cookie = response1.Cookies[0].Value;
                    SessionId = cookie;
                    return cookie;
                }
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        private static string[] UploadOldNexegeRatesheet(string filename, string IPadresss, string sessionid)
        {
            string[] rUpload = new string[2];
            try
            {
                //string filepath = System.Web.Hosting.HostingEnvironment.MapPath("~/Temp/" + filename);
                rUpload = uploadRatesheetOnOldNexege(filename, filename, IPadresss, sessionid);
                return rUpload;
                //if (rUpload[0] == "true")
                //{
                //    return true;
                //}
                //else
                //{
                //    return false;
                //}
            }
            catch (Exception ex)
            {
                rUpload[0] = "false";
                rUpload[1] = ex.Message;
                return rUpload;

            }
        }

        private static string[] uploadRatesheetOnOldNexege(string fileName, string filePath, string ip, string sessionID)
        {
            try
            {
                NameValueCollection nvc = new NameValueCollection();
                nvc.Add("actionId", "uploadRateSheetCSVFile");
                nvc.Add("fileName", fileName);
                return resolveXml(HttpUploadFile("http://" + ip + "/VerifyRateSheet.jsp?actionId=uploadRateSheetCSVFile&fileName=" + fileName, filePath, "file", "text/csv", nvc, sessionID));
                //return resolvehtml(HttpUploadFile("http://" + ip + "/admin/billingadmin/UploadFile.jsp?actionId=upload&fileName=" + fileName, filePath, "file", "text/csv", nvc,sessionID));
            }
            catch (Exception ex)
            {
                UpdateError("OldNexge MappingRatesheet Method uploadRatesheetOnOldNexege Error:-", ex.Message, "RatesheetLogs.txt", fileName, false);
                return new string[] { "false", ex.Message, "" };
            }
        }

        private static string[] resolvehtml(string str)
        {
            string[] rA = new string[2];
            try
            {

                rA[0] = "true";
                rA[1] = "CSV Uploaded SuccessFully";


            }
            catch (Exception ex)
            {
                rA[0] = "false";
                rA[1] = ex.Message;

            }
            return rA;

        }

        private static string[] resolveXml(string str)
        {
            try
            {
                string[] rA = new string[2];
                XmlDocument dom = new XmlDocument();
                dom.LoadXml(str.Trim());
                XmlElement root = dom.DocumentElement;
                XmlNodeList nodes = root.ChildNodes;
                XmlNode status = root.SelectSingleNode("status");
                if (status != null)
                {
                    if (status.InnerText == "true")
                    {
                        rA[0] = "true";
                        XmlNode cs_data = root.SelectSingleNode("cs-data");
                        if (cs_data != null)
                            rA[1] = cs_data.InnerText;
                    }
                    else
                    {
                        rA[0] = "false";
                        XmlNode cs_data = root.SelectSingleNode("cs-data");
                        if (cs_data != null)
                        {
                            for (int i = 0; i < cs_data.ChildNodes.Count; i++)
                            {
                                rA[1] += (cs_data.ChildNodes)[i].InnerText + "\r\n";
                            }
                        }
                    }
                }
                return rA;
            }
            catch (Exception ex)
            {
                return new string[] { "false", ex.Message, "" };
            }

        }

        private static string HttpUploadFile(string url, string file, string paramName, string contentType, NameValueCollection nvc, string sessionID)
        {

            string output = ""; CookieContainer cookies = new CookieContainer();
            //log.Debug(string.Format("Uploading {0} to {1}", file, url));
            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);
            wr.ContentType = "multipart/form-data; boundary=" + boundary;
            wr.Method = "POST";
            wr.KeepAlive = true;
            wr.Credentials = System.Net.CredentialCache.DefaultCredentials;
            cookies.Add(new Cookie() { Name = "JSESSIONID", Value = sessionID, Domain = "180.87.64.54" });
            wr.CookieContainer = cookies;


            Stream rs = wr.GetRequestStream();

            string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
            foreach (string key in nvc.Keys)
            {
                rs.Write(boundarybytes, 0, boundarybytes.Length);
                string formitem = string.Format(formdataTemplate, key, nvc[key]);
                byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
                rs.Write(formitembytes, 0, formitembytes.Length);
            }
            rs.Write(boundarybytes, 0, boundarybytes.Length);

            string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
            string header = string.Format(headerTemplate, paramName, file, contentType);
            byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
            rs.Write(headerbytes, 0, headerbytes.Length);



            FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
            byte[] buffer = new byte[4096];
            int bytesRead = 0;
            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
            {
                rs.Write(buffer, 0, bytesRead);
            }
            fileStream.Close();

            byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
            rs.Write(trailer, 0, trailer.Length);
            rs.Close();

            WebResponse wresp = null;
            try
            {
                wresp = wr.GetResponse();
                Stream stream2 = wresp.GetResponseStream();
                StreamReader reader2 = new StreamReader(stream2);
                output = reader2.ReadToEnd();

                //log.Debug(string.Format("File uploaded, server response is: {0}", reader2.ReadToEnd()));

            }
            catch (Exception ex)
            {
                //log.Error("Error uploading file", ex);
                output = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><response-data><actionid>uploadRateSheetCSVFile</actionid><service-provider>AS</service-provider><status>false</status><cs-data>Error uploading file : " + ex.Message + "</cs-data></response-data>";
                if (wresp != null)
                {
                    wresp.Close();
                    wresp = null;
                }
            }
            finally
            {
                wr = null;

            }
            return output;
        }

        // Pasupathi Commented for Old code

        //protected static string ConversiontoOldNexege(List<RMSServices.RatesheetV2.ngt_rateslist> RateSheet)
        //{
        //    List<OldNexegeRatesheet> rateList = new List<OldNexegeRatesheet>();
        //    try
        //    {
        //        string files = "~/ratesheetFiles/" + DateTime.Now.ToString("yyyyMMddHHmmssffff") + ".csv";
        //        string filename = HostingEnvironment.MapPath(files);
        //        foreach (var rate in RateSheet)
        //        {
        //            if (rate.Active != "D")
        //            {
        //                using (rmsEntities rmsContxt = new rmsEntities())
        //                {
        //                    foreach (var k in rmsContxt.specialcharconfigs.ToList())
        //                    {
        //                        rateList.Add(new OldNexegeRatesheet() { Blocked = rate.Active == "A" ? "False" : "True", Country_Name = rate.Description.Replace(k.Char, k.Replace), Country_Prefix = rate.ngt_regionName.Replace(k.Char, k.Replace), Rate = rate.Call_rate });
        //                    }
        //                }
        //            }
        //        }

        //        RMSDTR.CreateCSVFromGenericList(rateList, filename);
        //        return filename;
        //    }
        //    catch (Exception ex)
        //    {
        //        return "Exception " + ex.Message;
        //    }

        //}

        // Pasupathi Created New Method For Conversion Old nexge
        protected static string ConversiontoOldNexege(List<RMSServices.RatesheetV2.ngt_rateslist> RateSheet)
        {
            List<OldNexegeRatesheet> rateList = new List<OldNexegeRatesheet>();
            try
            {
                string OriginalName = DateTime.Now.ToString("yyyyMMddHHmmssffff") + ".csv";
                CsvFileName = OriginalName;
                string files = "~/ratesheetFiles/" + OriginalName;
                string filename = HostingEnvironment.MapPath(files);
                foreach (var rate in RateSheet)
                {
                    if (rate.Active != "D")
                    {
                        //using (rmsEntities rmsContxt = new rmsEntities())
                        //{
                        //    foreach (var k in rmsContxt.specialcharconfigs.ToList())
                        //    {
                        //        rate.Description.Replace(k.Char, k.Replace);
                        //        rate.ngt_regionName.Replace(k.Char, k.Replace);
                        //    }
                        //}
                        rateList.Add(new OldNexegeRatesheet() { Blocked = rate.Active == "A" ? "False" : "True", Country_Name = rate.Description, Country_Prefix = rate.ngt_regionName, Rate = rate.Call_rate });
                    }
                }

                RMSDTR.CreateCSVFromGenericList(rateList, filename);
                return filename;
            }
            catch (Exception ex)
            {
                return "Exception " + ex.Message;
            }

        }

        protected static string ConversiontoOldNexegeFuture(List<RMSServices.RatesheetV2.ngt_rateslist> RateSheet)
        {
            List<OldNexegeRatesheet> rateList = new List<OldNexegeRatesheet>();
            try
            {
                string OriginalName = DateTime.Now.ToString("yyyyMMddHHmmssffff") + ".csv";
                CsvFileName = OriginalName;
                string files = "~/FuturOldeNexgeRatesheet/" + OriginalName;
                string filename = HostingEnvironment.MapPath(files);
                foreach (var rate in RateSheet)
                {
                    if (rate.Active != "D")
                    {
                        //using (rmsEntities rmsContxt = new rmsEntities())
                        //{
                        //    foreach (var k in rmsContxt.specialcharconfigs.ToList())
                        //    {
                        //        rate.Description.Replace(k.Char, k.Replace);
                        //        rate.ngt_regionName.Replace(k.Char, k.Replace);
                        //    }
                        //}
                        rateList.Add(new OldNexegeRatesheet() { Blocked = rate.Active == "A" ? "False" : "True", Country_Name = rate.Description, Country_Prefix = rate.ngt_regionName, Rate = rate.Call_rate });
                    }
                }

                RMSDTR.CreateCSVFromGenericList(rateList, filename);
                return filename;
            }
            catch (Exception ex)
            {
                return "Exception " + ex.Message;
            }

        }

        protected static string ConversiontoOldNexegeUpdate(List<RMSServices.RatesheetV2.ngt_rateslist> RateSheet, string pulseruleid)
        {
            List<OldNexegeRatesheetUpdate> rateList = new List<OldNexegeRatesheetUpdate>();
            try
            {
                string OriginalName = DateTime.Now.ToString("yyyyMMddHHmmssffff") + ".csv";
                CsvFileName = OriginalName;
                string files = "~/ratesheetFiles/" + OriginalName;
                string filename = HostingEnvironment.MapPath(files);
                foreach (var rate in RateSheet)
                {
                    if (rate.Active != "D")
                    {
                        //using (rmsEntities rmsContxt = new rmsEntities())
                        //{
                        //    foreach (var k in rmsContxt.specialcharconfigs.ToList())
                        //    {
                        //        rate.Description.Replace(k.Char, k.Replace);
                        //        rate.ngt_regionName.Replace(k.Char, k.Replace);
                        //    }
                        //}
                        rateList.Add(new OldNexegeRatesheetUpdate() { Blocked = rate.Active == "A" ? "FALSE" : "TRUE", Country_Name = rate.Description, Country_Prefix = rate.ngt_regionName, Rate = rate.Call_rate, Pulse_Rule_Id = pulseruleid });
                    }
                }

                RMSDTR.CreateCSVFromGenericList(rateList, filename);
                return filename;
            }
            catch (Exception ex)
            {
                return "Exception " + ex.Message;
            }

        }

        protected static string ConversiontoDummyOldNexege()
        {
            List<OldNexegeRatesheet> rateList = new List<OldNexegeRatesheet>();
            try
            {
                string OriginalName = DateTime.Now.ToString("yyyyMMddHHmmssffff") + ".csv";
                CsvFileName = OriginalName;
                string files = "~/ratesheetFiles/" + OriginalName;
                string filename = HostingEnvironment.MapPath(files);

                rateList.Add(new OldNexegeRatesheet()
                {
                    Blocked = "False",
                    Country_Name = "Dummy Plan",
                    Country_Prefix = "0",
                    Rate = 20
                });


                RMSDTR.CreateCSVFromGenericList(rateList, filename);
                return filename;
            }
            catch (Exception ex)
            {
                return "Exception " + ex.Message;
            }

        }

        private class OldNexegeRatesheet
        {
            public string Country_Prefix { get; set; }
            public string Country_Name { get; set; }
            public decimal Rate { get; set; }
            public string Blocked { get; set; }
            public int Pulse_Rule_Id = 0;
        }
        private class OldNexegeRatesheetUpdate
        {
            public string Country_Prefix { get; set; }
            public string Country_Name { get; set; }
            public decimal Rate { get; set; }

            public string Pulse_Rule_Id { get; set; }
            public string Blocked { get; set; }


        }


    }
}
