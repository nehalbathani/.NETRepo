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
*   File Name: PortfolioParser.cs                                                 
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
using PortfolioParser.Service;
using PortfolioParser.Model;
using TPSFLib.Utility;
using System.Data;

//Specify Logging Configuration
[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4Net.config", Watch = true)]

namespace PortfolioParser
{
    class PortfolioParser
    {
        //Get Logger Object
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private PortfolioService portfolioService = new PortfolioService();


	    public static void Main(String[] args)
	    {
            logger.Info("************ BEGIN: Portfolio Parser  ***********");
            
            String feedCode = args[0];
            try
            {
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
                                PortfolioParser parser = new PortfolioParser();
                                parser.ProcessPortfolioFile(inputFile);

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
            catch (Exception e)
            {
                logger.Error(e.Message);
                logger.Error(e.StackTrace);
                logger.Error(e.InnerException);
            }
            finally
            {
                logger.Info("************ END: Portfolio Parser  ***********");

            }
	    }

        private void ProcessPortfolioFile(FileInfo input)
        {
            logger.Info("Parse file: " + input.Name);

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(FUND_GROUP));
            FileStream fileStream = File.OpenRead(input.FullName);
            FUND_GROUP portfolios = (FUND_GROUP)xmlSerializer.Deserialize(fileStream);

            logger.Info("No of Records Mentioned in Records tag:" + portfolios.RECORDS);

            List<BRSPortfolioBean> brPortfList = new List<BRSPortfolioBean>();
            if (Convert.ToInt16(portfolios.RECORDS) > 0)
            {
                logger.Info("Start Casting  " + portfolios.FUND_MEMBER.Length + " succesfully parsed positions from xml file to BRS Format.");

                foreach (FUND_MEMBER portf in portfolios.FUND_MEMBER)
                {
                    brPortfList.Add(setupBRSPortfolioObject(portf));
                }
                portfolioService.loadBRSPortfolios(brPortfList);
            }
            fileStream.Close();
        }

        private BRSPortfolioBean setupBRSPortfolioObject(FUND_MEMBER portf)
        {
            BRSPortfolioBean brPortf = new BRSPortfolioBean();
            brPortf.BaseCurrency = portf.BASE_CURRENCY != null && portf.BASE_CURRENCY.Value != null ? portf.BASE_CURRENCY.Value : null;
            brPortf.Country = portf.COUNTRY != null && portf.COUNTRY.Value != null ? portf.COUNTRY.Value : null;
            brPortf.Custodian = portf.CUSTODIAN != null && portf.CUSTODIAN.Value != null ? portf.CUSTODIAN.Value : null;
            brPortf.CustodyAcct = portf.CUSTODY_ACCT_NUM != null && portf.CUSTODY_ACCT_NUM.Value != null ? portf.CUSTODY_ACCT_NUM.Value : null;
            brPortf.DurationType = portf.DURATION_TYPE != null && portf.DURATION_TYPE.Value != null ? portf.DURATION_TYPE.Value : null;
            brPortf.Fund = portf.FUND != null && portf.FUND.Value != null ? portf.FUND.Value : null;
            brPortf.InceptDate = portf.INCEPT_DATE != null && portf.INCEPT_DATE.Value != null ? portf.INCEPT_DATE.Value : null;
            brPortf.InvestmentType = portf.INVESTMENT_TYPE != null && portf.INVESTMENT_TYPE.Value != null ? portf.INVESTMENT_TYPE.Value : null;
            brPortf.LegalStructure = portf.LEGAL_STRUCTURE != null && portf.LEGAL_STRUCTURE.Value != null ? portf.LEGAL_STRUCTURE.Value : null;
            brPortf.PmName = portf.PM_NAME != null && portf.PM_NAME.Value != null ? portf.PM_NAME.Value : null;
            brPortf.PortfolioCusip = portf.PORTFOLIO_CUSIP != null && portf.PORTFOLIO_CUSIP.Value != null ? portf.PORTFOLIO_CUSIP.Value : null;
            brPortf.PortfolioGroup = portf.PORTFOLIO_GROUP != null && portf.PORTFOLIO_GROUP.Value != null ? portf.PORTFOLIO_GROUP.Value : null;
            brPortf.PortfolioType = portf.PORTFOLIO_TYPE != null && portf.PORTFOLIO_TYPE.Value != null ? portf.PORTFOLIO_TYPE.Value : null;
            brPortf.PortfolioFullName = portf.PORTFOLIOS_FULL_NAME != null && portf.PORTFOLIOS_FULL_NAME.Value != null ? portf.PORTFOLIOS_FULL_NAME.Value : null;
            brPortf.PortfolioProtfName = portf.PORTFOLIOS_PORTFOLIO_NAME != null && portf.PORTFOLIOS_PORTFOLIO_NAME.Value != null ? portf.PORTFOLIOS_PORTFOLIO_NAME.Value : null;
            brPortf.RegulatoryStructure = portf.REGULATORY_STRUCTURE != null && portf.REGULATORY_STRUCTURE.Value != null ? portf.REGULATORY_STRUCTURE.Value : null;
            brPortf.State = portf.STATE != null && portf.STATE.Value != null ? portf.STATE.Value : null;
            brPortf.TaxId = portf.TAX_ID != null && portf.TAX_ID.Value != null ? portf.TAX_ID.Value : null;
            brPortf.TermDate = portf.TERM_DATE != null && portf.TERM_DATE.Value != null ? portf.TERM_DATE.Value : null;

            String zeroValue = "0";

            brPortf.AltFundSet = portf.ALT_FUND_ID_set != null && portf.ALT_FUND_ID_set.SIZE != null ? portf.ALT_FUND_ID_set.SIZE : zeroValue;

            int setCounter = Convert.ToInt32(portf.ALT_FUND_ID_set != null && portf.ALT_FUND_ID_set.SIZE != null ? portf.ALT_FUND_ID_set.SIZE : zeroValue);
            if (setCounter > 0)
            {
                AltFund[] BrAltFundList = new AltFund[setCounter];
                ALT_FUND_ID[] altFundData = portf.ALT_FUND_ID_set.ALT_FUND_ID;

                for (int i = 0; i < setCounter; i++)
                {
                    if (altFundData != null)
                    {
                        BrAltFundList[i] = new AltFund();
                        BrAltFundList[i].FundId = (altFundData[i].FUND_ID != null && altFundData[i].FUND_ID.Value != null) ? altFundData[i].FUND_ID.Value : null;
                        BrAltFundList[i].Loc = (altFundData[i].LOC != null && altFundData[i].LOC.Value != null) ? altFundData[i].LOC.Value : null;
                        BrAltFundList[i].LocType = (altFundData[i].LOC_TYPE != null && altFundData[i].LOC_TYPE.Value != null) ? altFundData[i].LOC_TYPE.Value : null;
                    }
                }
                brPortf.AltFund = BrAltFundList;
            }

            brPortf.IndexSet = portf.INDEX_set != null && portf.INDEX_set.SIZE != null ? portf.INDEX_set.SIZE : zeroValue;

            setCounter = Convert.ToInt32(portf.INDEX_set != null && portf.INDEX_set.SIZE != null ? portf.INDEX_set.SIZE : zeroValue);
            if (setCounter > 0)
            {
                Index[] BrIndexList = new Index[setCounter];
                INDEX[] indexData = portf.INDEX_set.INDEX;

                for (int i = 0; i < setCounter; i++)
                {
                    if (indexData != null)
                    {
                        BrIndexList[i] = new Index();
                        BrIndexList[i].Cusip = (indexData[i].CUSIP != null && indexData[i].CUSIP.Value != null) ? indexData[i].CUSIP.Value : null;
                        BrIndexList[i].Desc = (indexData[i].DESC != null && indexData[i].DESC.Value != null) ? indexData[i].DESC.Value : null;
                        BrIndexList[i].Name = (indexData[i].NAME != null && indexData[i].NAME.Value != null) ? indexData[i].NAME.Value : null;
                        BrIndexList[i].Priority = (indexData[i].PRIORITY != null && indexData[i].PRIORITY.Value != null) ? indexData[i].PRIORITY.Value : null;
                        BrIndexList[i].Type = (indexData[i].TYPE != null && indexData[i].TYPE.Value != null) ? indexData[i].TYPE.Value : null;
                    }
                }
                brPortf.Index = BrIndexList;
            }

            return brPortf;
        }

    }
}
