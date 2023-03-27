/*************************************************************************
*              Texas Education Agency                                       
**************************************************************************
* THIS DOCUMENT CONTAINS MATERIAL WHICH IS THE PROPERTY OF AND              
* CONFIDENTIAL TO THE TEXAS EDUCATION AGENCY. DISCLOSURE OUTSIDE            
* THE TEXAS EDUCATION AGENCY IS PROHIBITED, EXCEPT BY LICENSE OR            
* OTHER CONFIDENTIALITY AGREEMENT.                                       
*                                                                           
*      COPYRIGHT 2004 THE TEXAS EDUCATION AGENCY. ALL RIGHTS RESERVED.         
*           
*-------------------------------------------------------------------------
*                                                                           
*   File Name: CashForwardParser.cs                                                 
* Create Date: 10/01/2014                                                   
*     Purpose: 
*     Comments: None                                                         
*-------------------------------------------------------------------------
*       Change History                                                      
*-------------------------------------------------------------------------
*   Author(s): Nehal Bathani, Deepa Anand                                       
*     Version: 1.0                                                          
*        Date: 10/01/2014                                                   
*     Details: Initial creation of this class                        
*************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

using System.Reflection;
using CashForwardParser.Service;
using CashForwardParser.Model;
using TPSFLib.Utility;
using System.Data;

//Specify Logging Configuration
[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4Net.config", Watch = true)]

namespace CashForwardParser
{
    class CashForwardParser
    {
        //Get Logger Object
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private CashForwardService cashService = new CashForwardService();


	    public static void Main(String[] args)
	    {
            logger.Info("************ BEGIN: Cash Forward Parser  ***********");
            
            String feedCode = args[0];
            try{

                String appHome = Environment.GetEnvironmentVariable("TPSF_DIR_HOME");
                String appInstance = Environment.GetEnvironmentVariable("TPSF_INSTANCE");

                ConfigurationManager confMngr = new ConfigurationManager();
                Dictionary<String, String> ConfigurationMap = confMngr.getConfiguration(feedCode);

                String importDir = appHome + "\\data\\SITE\\" + appInstance + "\\" + ConfigurationMap["IMPORT_DIR"];
                String archiveDir = appHome + "\\data\\SITE\\" + appInstance + "\\" + ConfigurationMap["ARCHIVE_DIR"];
                String dataFilePattern = ConfigurationMap["FILE_PATTERN"];

                logger.Info("Application Home Directory:" + appHome);
                logger.Info("Application Instance:" + appInstance);
                logger.Info("Import Directory:" + importDir);
                logger.Info("Archive Directory:" + archiveDir);
                logger.Info("File Pattern:" + dataFilePattern);


             if (Directory.Exists(importDir))
            {
                String[] nuggetFiles = Directory.GetFiles(importDir, dataFilePattern);
                if (nuggetFiles.Length > 0)
                {
                    foreach (String nfile in nuggetFiles)
                    {
                        if (File.Exists(nfile))
                        {
                             FileInfo inputFile = new FileInfo(nfile);
                        
                            logger.Info("Parsing file:" + inputFile.FullName);
                            CashForwardParser parser = new CashForwardParser();
                            parser.ProcessCashFwdFile(inputFile);
                        
                            logger.Info("File to be archived: " + nfile);
                            //archive file
                            if (Directory.Exists(archiveDir))
                            {
                                File.Copy(nfile, archiveDir + inputFile.Name + "_" + DateTime.Now.ToString("yyyyMMddhhmmss"));
                            }
                            //Delete source file once its processed
                            File.Delete(inputFile.FullName);

                        }
                        else
                        {
                            logger.Error("*************  Error: Invalid file **********" + nfile);
                        }
                    }
                }
                 else
                 {
                     logger.Error("*************  Error: No valid files found **********");
                 }
            }
            else
            {
                logger.Error("Error: directory not found at " + importDir);
            }
           
            }
            catch(Exception e)
            {
                logger.Error(e.Message);
                logger.Error(e.StackTrace);
                logger.Error(e.InnerException);
            }
            finally
            {
                logger.Info("************ END: CashForward Parser  ***********");
            
            }

	    }

        private void ProcessCashFwdFile(FileInfo input)
        {

            logger.Info("Parse file: " + input.Name);

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(CASH_WF));
            FileStream fileStream = File.OpenRead(input.FullName);
            CASH_WF cashFrwd = (CASH_WF)xmlSerializer.Deserialize(fileStream);
            logger.Info("No of Records Mentioned in Records tag:" + cashFrwd.RECORDS);

            List<BRSCashForward> brCashFrwdList = new List<BRSCashForward>();
            if (Convert.ToInt16(cashFrwd.RECORDS) > 0)
            {
                logger.Info("Start Casting  " + cashFrwd.FUND_set.Length + " succesfully parsed Cash Forward from xml file to BRS Format.");

                foreach (FUND_set fundSet in cashFrwd.FUND_set)
                {
                    brCashFrwdList.Add(setupBRSCashFrwdObject(fundSet));
                }

                cashService.loadBRCashFwd(brCashFrwdList);
            }
            fileStream.Close();

        }

        private BRSCashForward setupBRSCashFrwdObject(FUND_set fundSet)
        {
            String zeroValue = "0";

            BRSCashForward brCashFrwd = new BRSCashForward();

            
            brCashFrwd.AggregateType = (fundSet.AGGREGATE_TYPE != null && fundSet.AGGREGATE_TYPE.Trim() !="")?fundSet.AGGREGATE_TYPE : null;
            brCashFrwd.Currency = (fundSet.CURRENCY != null && fundSet.CURRENCY.Trim() != "")? fundSet.CURRENCY : null;
            brCashFrwd.EndDate = (fundSet.END_DATE != null && fundSet.END_DATE.Trim() != "")? fundSet.END_DATE: null;
            brCashFrwd.Fund = (fundSet.FUND != null && fundSet.FUND.Trim() != "")? fundSet.FUND: null;
            brCashFrwd.FundName = (fundSet.FUND_NAME != null && fundSet.FUND_NAME.Trim()!= "")? fundSet.FUND_NAME: null;
            brCashFrwd.StartDate = (fundSet.START_DATE != null && fundSet.START_DATE.Trim() != "")? fundSet.START_DATE: null;
            
            brCashFrwd.SumDataSet = fundSet.SUM_DATA_set != null && fundSet.SUM_DATA_set.SIZE != null ? fundSet.SUM_DATA_set.SIZE : zeroValue;
            int setCounter = Convert.ToInt32(fundSet.SUM_DATA_set != null && fundSet.SUM_DATA_set.SIZE != null ? fundSet.SUM_DATA_set.SIZE : zeroValue);
            if (setCounter > 0)
            {
                SumData[] BrSumDataset = new SumData[setCounter];
                for (int i = 0; i < setCounter; i++)
                {
                    SUM_DATA sumData = fundSet.SUM_DATA_set.SUM_DATA;
                    if (sumData != null) { 
                        BrSumDataset[i] = new SumData();
                        BrSumDataset[i].CalcCommitedBal = (sumData.CALC_COMMITTED_BAL != null && sumData.CALC_COMMITTED_BAL.Trim() !="")? sumData.CALC_COMMITTED_BAL: null;
                        BrSumDataset[i].CalcSettleBal = (sumData.CALC_SETTLE_BAL != null && sumData.CALC_SETTLE_BAL.Trim() != "")? sumData.CALC_SETTLE_BAL: null;
                        BrSumDataset[i].CalcTradeBal = (sumData.CALC_TRADE_BAL != null && sumData.CALC_TRADE_BAL.Trim()!= "")? sumData.CALC_TRADE_BAL: null;
                        BrSumDataset[i].CommitedBalChange = (sumData.COMMITTED_BAL_CHANGE != null && sumData.COMMITTED_BAL_CHANGE.Trim() != "")? sumData.COMMITTED_BAL_CHANGE: null;
                        BrSumDataset[i].CommitedBalDiff = (sumData.COMMITTED_BAL_DIFF != null && sumData.COMMITTED_BAL_DIFF.Trim() != "")? sumData.COMMITTED_BAL_DIFF: null;
                        BrSumDataset[i].Currency = (sumData.CURRENCY != null && sumData.CURRENCY.Trim() != "") ? sumData.CURRENCY : null;
                        BrSumDataset[i].EndCommitedBal = (sumData.END_COMMITTED_BAL != null && sumData.END_COMMITTED_BAL.Trim() != "" )? sumData.END_COMMITTED_BAL: null;
                        BrSumDataset[i].EndSettleBal = (sumData.END_SETTLE_BAL != null && sumData.END_SETTLE_BAL.Trim() != "")? sumData.END_SETTLE_BAL: null;
                        BrSumDataset[i].EndTradeBal = (sumData.END_TRADE_BAL != null && sumData.END_TRADE_BAL.Trim() != "")? sumData.END_TRADE_BAL: null;
                        BrSumDataset[i].SettleBalChange = (sumData.SETTLE_BAL_CHANGE != null && sumData.SETTLE_BAL_CHANGE.Trim() != "")? sumData.SETTLE_BAL_CHANGE : null;
                        BrSumDataset[i].SettleBalDiff = (sumData.SETTLE_BAL_DIFF != null && sumData.SETTLE_BAL_DIFF.Trim()!= "")? sumData.SETTLE_BAL_DIFF: null;
                        BrSumDataset[i].StartCommitedBal = (sumData.START_COMMITTED_BAL != null && sumData.START_COMMITTED_BAL.Trim() != "")? sumData.START_COMMITTED_BAL: null;
                        BrSumDataset[i].StartSettleBal = (sumData.START_SETTLE_BAL != null && sumData.START_SETTLE_BAL.Trim()!="")? sumData.START_SETTLE_BAL: null;
                        BrSumDataset[i].StartTradeBal = (sumData.START_TRADE_BAL != null && sumData.START_TRADE_BAL.Trim() != "")? sumData.START_TRADE_BAL: null;
                        BrSumDataset[i].TradeBalChange = (sumData.TRADE_BAL_CHANGE != null && sumData.TRADE_BAL_CHANGE.Trim()!= "")? sumData.TRADE_BAL_CHANGE: null;
                        BrSumDataset[i].TradeBalDiff = (sumData.TRADE_BAL_DIFF != null && sumData.TRADE_BAL_DIFF.Trim() != "") ? sumData.TRADE_BAL_DIFF: null;
                   }
                }
                brCashFrwd.SumData = BrSumDataset;
            }


            brCashFrwd.TransDataSet = fundSet.TRANS_DATA_set != null && fundSet.TRANS_DATA_set.SIZE != null ? fundSet.TRANS_DATA_set.SIZE.ToString() : zeroValue;
            setCounter = Convert.ToInt32(fundSet.TRANS_DATA_set != null && fundSet.TRANS_DATA_set.SIZE != null ? fundSet.TRANS_DATA_set.SIZE.ToString() : zeroValue);
            if (setCounter > 0)
            {
                TransData[] BrTransDataset = new TransData[setCounter]; 
                TRANS_DATA[] transData = fundSet.TRANS_DATA_set.TRANS_DATA;
                    
                for (int i = 0; i < setCounter; i++)
                {
                    if (transData != null)
                    {
                        BrTransDataset[i] = new TransData();
                        BrTransDataset[i].AcctDesign = (transData[i].ACCT_DESIG != null && transData[i].ACCT_DESIG.Trim()!= "")? transData[i].ACCT_DESIG: null;
                        BrTransDataset[i].AssetID = (transData[i].ASSET_ID != null && transData[i].ASSET_ID.Trim() != "")? transData[i].ASSET_ID: null;
                        BrTransDataset[i].BaseNetMoney = (transData[i].BASE_NET_MONEY != null && transData[i].BASE_NET_MONEY.Trim() != "")? transData[i].BASE_NET_MONEY : null;
                        BrTransDataset[i].BookDate = (transData[i].BOOK_DATE != null && transData[i].BOOK_DATE.Trim() !="")? transData[i].BOOK_DATE: null;
                        BrTransDataset[i].Currency = (transData[i].CURRENCY != null && transData[i].CURRENCY.Trim() != "")? transData[i].CURRENCY: null;
                        BrTransDataset[i].CurrencyDesc = (transData[i].CURRENCY_DESC != null && transData[i].CURRENCY_DESC.Trim() !="")? transData[i].CURRENCY_DESC: null;
                        BrTransDataset[i].CurrFace = (transData[i].CURR_FACE != null && transData[i].CURR_FACE.Trim() != "")? transData[i].CURR_FACE: null;
                        BrTransDataset[i].DupNum = (transData[i].DUP_NUM != null && transData[i].DUP_NUM.Trim() != "")? transData[i].DUP_NUM: null;
                        BrTransDataset[i].Interest = (transData[i].INTEREST != null && transData[i].INTEREST.Trim() != "")? transData[i].INTEREST: null;
                        BrTransDataset[i].InvNum = (transData[i].INVNUM != null && transData[i].INVNUM.Trim() != "")? transData[i].INVNUM: null;
                        BrTransDataset[i].Isin= (transData[i].ISIN!= null && transData[i].ISIN.Trim() != "") ? transData[i].ISIN : null;
                        BrTransDataset[i].LotNum = (transData[i].LOTNUM != null && transData[i].LOTNUM.Trim() != "") ? transData[i].LOTNUM : null;
                        BrTransDataset[i].NetMoney = (transData[i].NET_MONEY != null && transData[i].NET_MONEY.Trim() != "") ? transData[i].NET_MONEY : null;
                        BrTransDataset[i].Principal = (transData[i].PRINCIPAL != null && transData[i].PRINCIPAL.Trim() != "") ? transData[i].PRINCIPAL : null;
                        BrTransDataset[i].SecDesc = (transData[i].SEC_DESC != null && transData[i].SEC_DESC.Trim() != "") ? transData[i].SEC_DESC : null;
                        BrTransDataset[i].SecGroup = (transData[i].SEC_GROUP != null && transData[i].SEC_GROUP.Trim() != "") ? transData[i].SEC_GROUP : null;
                        BrTransDataset[i].SecType = (transData[i].SEC_TYPE != null && transData[i].SEC_TYPE.Trim() != "") ? transData[i].SEC_TYPE : null;
                        BrTransDataset[i].Sedol = (transData[i].SEDOL != null && transData[i].SEDOL.Trim() != "") ? transData[i].SEDOL : null;
                        BrTransDataset[i].SeriesNum = (transData[i].SERIES_NUM != null && transData[i].SERIES_NUM.Trim() != "") ? transData[i].SERIES_NUM : null;
                        BrTransDataset[i].SettleDate = (transData[i].SETTLE_DATE != null && transData[i].SETTLE_DATE.Trim() != "") ? transData[i].SETTLE_DATE : null;
                        BrTransDataset[i].SpotRate = (transData[i].SPOT_RATE != null && transData[i].SPOT_RATE.Trim() != "") ? transData[i].SPOT_RATE : null;
                        BrTransDataset[i].Ticker = (transData[i].TICKER != null && transData[i].TICKER.Trim() != "") ? transData[i].TICKER : null;
                        BrTransDataset[i].TradeDate = (transData[i].TRADE_DATE != null && transData[i].TRADE_DATE.Trim() != "") ? transData[i].TRADE_DATE : null;
                        BrTransDataset[i].TranType1 = (transData[i].TRAN_TYPE1 != null && transData[i].TRAN_TYPE1.Trim() != "") ? transData[i].TRAN_TYPE1 : null;
                        BrTransDataset[i].TranType2 = (transData[i].TRAN_TYPE2 != null && transData[i].TRAN_TYPE2.Trim() != "") ? transData[i].TRAN_TYPE2 : null;
                        BrTransDataset[i].TranTypeDesc1 = (transData[i].TRAN_TYPE_DESC1 != null && transData[i].TRAN_TYPE_DESC1.Trim() != "") ? transData[i].TRAN_TYPE_DESC1 : null;
                        BrTransDataset[i].TranTypeDesc2 = (transData[i].TRAN_TYPE_DESC2 != null && transData[i].TRAN_TYPE_DESC2.Trim() != "") ? transData[i].TRAN_TYPE_DESC2 : null;

                    }
                }
                brCashFrwd.TransData = BrTransDataset;
            }
           
            return brCashFrwd;
        }
    }
}
