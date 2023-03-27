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
*   File Name: PriceListParser.cs                                                 
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
using PriceListParser.Service;
using PriceListParser.Model;
using TPSFLib.Utility;
using System.Data;

//Specify Logging Configuration
[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4Net.config", Watch = true)]

namespace PriceListParser
{
    class PriceListParser
    {
        //Get Logger Object
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private PriceListService priceService = new PriceListService();


        public static void Main(string[] args)
	    {
            logger.Info("************ BEGIN: PriceList Parser  ***********");
            
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
                                PriceListParser parser = new PriceListParser();
                                parser.ProcessPriceListFile(inputFile);

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
                logger.Info("************ END: PriceList Parser  ***********");

            }
	    }

        private void ProcessPriceListFile(FileInfo input)
        {

            logger.Info("Parse file: " + input.Name);

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(PRICE_LIST));
            FileStream fileStream = File.OpenRead(input.FullName);
            PRICE_LIST priceList = (PRICE_LIST)xmlSerializer.Deserialize(fileStream);

            logger.Info("No of Records Mentioned in Records tag:" + priceList.RECORDS);

            List<BRPrice> brPriceList = new List<BRPrice>();
            if (Convert.ToInt16(priceList.RECORDS) > 0)
            {
                logger.Info("Start Casting  " + priceList.SECURITY.Length + " succesfully parsed positions from xml file to BRS Format.");

                foreach (SECURITY price in priceList.SECURITY)
                {
                    brPriceList.Add(setupBRSPriceObject(price));
                }

                priceService.loadBRSPrices(brPriceList);
            }
            fileStream.Close();
        }

        private BRPrice setupBRSPriceObject(SECURITY price)
        {
            BRPrice brPrc = new BRPrice();

            brPrc.AccInt = price.ACC_INT != null && price.ACC_INT.Text != null && price.ACC_INT.Text.Length > 0 ? price.ACC_INT.Text[0] : null;
            brPrc.AccrInt = price.ACCR_INT != null && price.ACCR_INT.Text != null && price.ACCR_INT.Text.Length > 0 ? price.ACCR_INT.Text[0] : null;
            brPrc.AccrualDays = price.ACCRUAL_DAYS != null && price.ACCRUAL_DAYS.Text != null && price.ACCRUAL_DAYS.Text.Length > 0 ? price.ACCRUAL_DAYS.Text[0] : null;
            brPrc.AccrualRate = price.ACCRUAL_RATE != null && price.ACCRUAL_RATE.Text != null && price.ACCRUAL_RATE.Text.Length > 0 ? price.ACCRUAL_RATE.Text[0] : null;
            brPrc.Ai = price.AI != null && price.AI.Text != null && price.AI.Text.Length > 0 ? price.AI.Text[0] : null;
            brPrc.AiErn = price.AI_ERN != null && price.AI_ERN.Text != null && price.AI_ERN.Text.Length > 0 ? price.AI_ERN.Text[0] : null;
            brPrc.AiErnMd = price.AI_ERN_MD != null && price.AI_ERN_MD.Text != null && price.AI_ERN_MD.Text.Length > 0 ? price.AI_ERN_MD.Text[0] : null;
            brPrc.AiError = price.AI_ERROR != null && price.AI_ERROR.Text != null && price.AI_ERROR.Text.Length > 0 ? price.AI_ERROR.Text[0] : null;
            brPrc.AiMd = price.AI_MD != null && price.AI_MD.Text != null && price.AI_MD.Text.Length > 0 ? price.AI_MD.Text[0] : null;
            brPrc.Cins = price.CINS != null && price.CINS.Text != null && price.CINS.Text.Length > 0 ? price.CINS.Text[0] : null;
            brPrc.ClientId = price.CLIENT_ID != null && price.CLIENT_ID.Text != null && price.CLIENT_ID.Text.Length > 0 ? price.CLIENT_ID.Text[0] : null;
            brPrc.Country = price.COUNTRY != null && price.COUNTRY.Text != null && price.COUNTRY.Text.Length > 0 ? price.COUNTRY.Text[0] : null;
            brPrc.Currency = price.CURRENCY != null && price.CURRENCY.Text != null && price.CURRENCY.Text.Length > 0 ? price.CURRENCY.Text[0] : null;
            brPrc.CurrIaccrualDt = price.CURR_IACCRUAL_DT != null && price.CURR_IACCRUAL_DT.Text != null && price.CURR_IACCRUAL_DT.Text.Length > 0 ? price.CURR_IACCRUAL_DT.Text[0] : null;
            brPrc.CurveType = price.CURVE_TYPE != null && price.CURVE_TYPE.Text != null && price.CURVE_TYPE.Text.Length > 0 ? price.CURVE_TYPE.Text[0] : null;
            brPrc.Cusip = price.CUSIP != null && price.CUSIP.Text != null && price.CUSIP.Text.Length > 0 ? price.CUSIP.Text[0] : null;
            brPrc.DataIndicator = price.DATA_INDICATOR != null && price.DATA_INDICATOR.Text != null && price.DATA_INDICATOR.Text.Length > 0 ? price.DATA_INDICATOR.Text[0] : null;
            brPrc.Delay = price.DELAY != null && price.DELAY.Text != null && price.DELAY.Text.Length > 0 ? price.DELAY.Text[0] : null;
            brPrc.IndexName = price.INDEX_NAME != null && price.INDEX_NAME.Text != null && price.INDEX_NAME.Text.Length > 0 ? price.INDEX_NAME.Text[0] : null;
            brPrc.Isin = price.ISIN != null && price.ISIN.Text != null && price.ISIN.Text.Length > 0 ? price.ISIN.Text[0] : null;
            brPrc.Krd10y = price.KRD_10Y != null && price.KRD_10Y.Text != null && price.KRD_10Y.Text.Length > 0 ? price.KRD_10Y.Text[0] : null;
            brPrc.Krd15y = price.KRD_15Y != null && price.KRD_15Y.Text != null && price.KRD_15Y.Text.Length > 0 ? price.KRD_15Y.Text[0] : null;
            brPrc.Krd1y = price.KRD_1Y != null && price.KRD_1Y.Text != null && price.KRD_1Y.Text.Length > 0 ? price.KRD_1Y.Text[0] : null;
            brPrc.Krd20y = price.KRD_20Y != null && price.KRD_20Y.Text != null && price.KRD_20Y.Text.Length > 0 ? price.KRD_20Y.Text[0] : null;
            brPrc.Krd25y = price.KRD_25Y != null && price.KRD_25Y.Text != null && price.KRD_25Y.Text.Length > 0 ? price.KRD_25Y.Text[0] : null;
            brPrc.Krd2y = price.KRD_2Y != null && price.KRD_2Y.Text != null && price.KRD_2Y.Text.Length > 0 ? price.KRD_2Y.Text[0] : null;
            brPrc.Krd30y = price.KRD_30Y != null && price.KRD_30Y.Text != null && price.KRD_30Y.Text.Length > 0 ? price.KRD_30Y.Text[0] : null;
            brPrc.Krd3M = price.KRD_3M != null && price.KRD_3M.Text != null && price.KRD_3M.Text.Length > 0 ? price.KRD_3M.Text[0] : null;
            brPrc.Krd3y = price.KRD_3Y != null && price.KRD_3Y.Text != null && price.KRD_3Y.Text.Length > 0 ? price.KRD_3Y.Text[0] : null;
            brPrc.Krd5y = price.KRD_5Y != null && price.KRD_5Y.Text != null && price.KRD_5Y.Text.Length > 0 ? price.KRD_5Y.Text[0] : null;
            brPrc.Krd7y = price.KRD_7Y != null && price.KRD_7Y.Text != null && price.KRD_7Y.Text.Length > 0 ? price.KRD_7Y.Text[0] : null;
            brPrc.KrdBenchCusip = price.KRD_BENCH_CUSIP != null && price.KRD_BENCH_CUSIP.Text != null && price.KRD_BENCH_CUSIP.Text.Length > 0 ? price.KRD_BENCH_CUSIP.Text[0] : null;
            brPrc.KrdDt = price.KRD_DT != null && price.KRD_DT.Text != null && price.KRD_DT.Text.Length > 0 ? price.KRD_DT.Text[0] : null;
            brPrc.KrdSource = price.KRD_SOURCE != null && price.KRD_SOURCE.Text != null && price.KRD_SOURCE.Text.Length > 0 ? price.KRD_SOURCE.Text[0] : null;
            brPrc.Leg1Price = price.LEG1_PRICE != null && price.LEG1_PRICE.Text != null && price.LEG1_PRICE.Text.Length > 0 ? price.LEG1_PRICE.Text[0] : null;
            brPrc.Leg2Price = price.LEG2_PRICE != null && price.LEG2_PRICE.Text != null && price.LEG2_PRICE.Text.Length > 0 ? price.LEG2_PRICE.Text[0] : null;
            brPrc.MarkDt = price.MARK_DT != null && price.MARK_DT.Text != null && price.MARK_DT.Text.Length > 0 ? price.MARK_DT.Text[0] : null;
            brPrc.MarketVal = price.MARKET_VAL != null && price.MARKET_VAL.Text != null && price.MARKET_VAL.Text.Length > 0 ? price.MARKET_VAL.Text[0] : null;
            brPrc.Maturity = price.MATURITY != null && price.MATURITY.Text != null && price.MATURITY.Text.Length > 0 ? price.MATURITY.Text[0] : null;
            brPrc.ModDur = price.MOD_DUR != null && price.MOD_DUR.Text != null && price.MOD_DUR.Text.Length > 0 ? price.MOD_DUR.Text[0] : null;
            brPrc.ModelType = price.MODEL_TYPE != null && price.MODEL_TYPE.Text != null && price.MODEL_TYPE.Text.Length > 0 ? price.MODEL_TYPE.Text[0] : null;
            brPrc.NextActPayDt = price.NEXT_ACT_PAY_DT != null && price.NEXT_ACT_PAY_DT.Text != null && price.NEXT_ACT_PAY_DT.Text.Length > 0 ? price.NEXT_ACT_PAY_DT.Text[0] : null;
            brPrc.NextActResetDt = price.NEXT_ACT_RESET_DT != null && price.NEXT_ACT_RESET_DT.Text != null && price.NEXT_ACT_RESET_DT.Text.Length > 0 ? price.NEXT_ACT_RESET_DT.Text[0] : null;
            brPrc.NextIaccrualDt = price.NEXT_IACCRUAL_DT != null && price.NEXT_IACCRUAL_DT.Text != null && price.NEXT_IACCRUAL_DT.Text.Length > 0 ? price.NEXT_IACCRUAL_DT.Text[0] : null;
            brPrc.NextNomPayDt = price.NEXT_NOM_PAY_DT != null && price.NEXT_NOM_PAY_DT.Text != null && price.NEXT_NOM_PAY_DT.Text.Length > 0 ? price.NEXT_NOM_PAY_DT.Text[0] : null;
            brPrc.NextNomResetDt = price.NEXT_NOM_RESET_DT != null && price.NEXT_NOM_RESET_DT.Text != null && price.NEXT_NOM_RESET_DT.Text.Length > 0 ? price.NEXT_NOM_RESET_DT.Text[0] : null;
            brPrc.NextPayDownDt = price.NEXT_PAYDOWN_DT != null && price.NEXT_PAYDOWN_DT.Text != null && price.NEXT_PAYDOWN_DT.Text.Length > 0 ? price.NEXT_PAYDOWN_DT.Text[0] : null;
            brPrc.Oac = price.OAC != null && price.OAC.Text != null && price.OAC.Text.Length > 0 ? price.OAC.Text[0] : null;
            brPrc.Oad = price.OAD != null && price.OAD.Text != null && price.OAD.Text.Length > 0 ? price.OAD.Text[0] : null;
            brPrc.Oas = price.OAS != null && price.OAS.Text != null && price.OAS.Text.Length > 0 ? price.OAS.Text[0] : null;
            brPrc.OptionVal = price.OPTION_VAL != null && price.OPTION_VAL.Text != null && price.OPTION_VAL.Text.Length > 0 ? price.OPTION_VAL.Text[0] : null;
            brPrc.Pam = price.PAM != null && price.PAM.Text != null && price.PAM.Text.Length > 0 ? price.PAM.Text[0] : null;
            brPrc.Pm = price.PM != null && price.PM.Text != null && price.PM.Text.Length > 0 ? price.PM.Text[0] : null;
            brPrc.PrepayUnit = price.PREPAY_UNIT != null && price.PREPAY_UNIT.Text != null && price.PREPAY_UNIT.Text.Length > 0 ? price.PREPAY_UNIT.Text[0] : null;
            brPrc.Price = price.PRICE != null && price.PRICE.Text != null && price.PRICE.Text.Length > 0 ? price.PRICE.Text[0] : null;
            brPrc.PriceAsPct = price.PRICE_AS_PCT != null && price.PRICE_AS_PCT.Text != null && price.PRICE_AS_PCT.Text.Length > 0 ? price.PRICE_AS_PCT.Text[0] : null;
            brPrc.PriceMultiplier = price.PRICE_MULTIPLIER != null && price.PRICE_MULTIPLIER.Text != null && price.PRICE_MULTIPLIER.Text.Length > 0 ? price.PRICE_MULTIPLIER.Text[0] : null;
            brPrc.PricingCusip = price.PRICING_CUSIP != null && price.PRICING_CUSIP.Text != null && price.PRICING_CUSIP.Text.Length > 0 ? price.PRICING_CUSIP.Text[0] : null;
            brPrc.Purpose = price.PURPOSE != null && price.PURPOSE.Text != null && price.PURPOSE.Text.Length > 0 ? price.PURPOSE.Text[0] : null;
            brPrc.RiskDt = price.RISK_DT != null && price.RISK_DT.Text != null && price.RISK_DT.Text.Length > 0 ? price.RISK_DT.Text[0] : null;
            brPrc.RiskKey = price.RISK_KEY != null && price.RISK_KEY.Text != null && price.RISK_KEY.Text.Length > 0 ? price.RISK_KEY.Text[0] : null;
            brPrc.RiskPrice = price.RISK_PRICE != null && price.RISK_PRICE.Text != null && price.RISK_PRICE.Text.Length > 0 ? price.RISK_PRICE.Text[0] : null;
            brPrc.RiskPurpose = price.RISK_PURPOSE != null && price.RISK_PURPOSE.Text != null && price.RISK_PURPOSE.Text.Length > 0 ? price.RISK_PURPOSE.Text[0] : null;
            brPrc.Sedol = price.SEDOL != null && price.SEDOL.Text != null && price.SEDOL.Text.Length > 0 ? price.SEDOL.Text[0] : null;
            brPrc.SettleDt = price.SETTLE_DT != null && price.SETTLE_DT.Text != null && price.SETTLE_DT.Text.Length > 0 ? price.SETTLE_DT.Text[0] : null;
            brPrc.SmCoupFreq = price.SM_COUP_FREQ != null && price.SM_COUP_FREQ.Text != null && price.SM_COUP_FREQ.Text.Length > 0 ? price.SM_COUP_FREQ.Text[0] : null;
            brPrc.SmSecGroup = price.SM_SEC_GROUP != null && price.SM_SEC_GROUP.Text != null && price.SM_SEC_GROUP.Text.Length > 0 ? price.SM_SEC_GROUP.Text[0] : null;
            brPrc.SmSecType = price.SM_SEC_TYPE != null && price.SM_SEC_TYPE.Text != null && price.SM_SEC_TYPE.Text.Length > 0 ? price.SM_SEC_TYPE.Text[0] : null;
            brPrc.Source = price.SOURCE != null && price.SOURCE.Text != null && price.SOURCE.Text.Length > 0 ? price.SOURCE.Text[0] : null;
            brPrc.SpdDur = price.SPD_DUR != null && price.SPD_DUR.Text != null && price.SPD_DUR.Text.Length > 0 ? price.SPD_DUR.Text[0] : null;
            brPrc.Speed = price.SPEED != null && price.SPEED.Text != null && price.SPEED.Text.Length > 0 ? price.SPEED.Text[0] : null;
            brPrc.Spread = price.SPREAD != null && price.SPREAD.Text != null && price.SPREAD.Text.Length > 0 ? price.SPREAD.Text[0] : null;
            brPrc.StaticPrinBegin = price.STATIC_PRIN_BEGIN != null && price.STATIC_PRIN_BEGIN.Text != null && price.STATIC_PRIN_BEGIN.Text.Length > 0 ? price.STATIC_PRIN_BEGIN.Text[0] : null;
            brPrc.StaticYield = price.STATIC_YIELD != null && price.STATIC_YIELD.Text != null && price.STATIC_YIELD.Text.Length > 0 ? price.STATIC_YIELD.Text[0] : null;
            brPrc.StWal = price.ST_WAL != null && price.ST_WAL.Text != null && price.ST_WAL.Text.Length > 0 ? price.ST_WAL.Text[0] : null;
            brPrc.Ticker = price.TICKER != null && price.TICKER.Text != null && price.TICKER.Text.Length > 0 ? price.TICKER.Text[0] : null;
            brPrc.Uid = price.UID != null && price.UID.Text != null && price.UID.Text.Length > 0 ? price.UID.Text[0] : null;
            brPrc.Volatility = price.VOLATILITY != null && price.VOLATILITY.Text != null && price.VOLATILITY.Text.Length > 0 ? price.VOLATILITY.Text[0] : null;
            brPrc.WaleqCpr = price.WALEQ_CPR != null && price.WALEQ_CPR.Text != null && price.WALEQ_CPR.Text.Length > 0 ? price.WALEQ_CPR.Text[0] : null;
            brPrc.WaleqPsa = price.WALEQ_PSA != null && price.WALEQ_PSA.Text != null && price.WALEQ_PSA.Text.Length > 0 ? price.WALEQ_PSA.Text[0] : null;
            brPrc.Yield = price.YIELD != null && price.YIELD.Text != null && price.YIELD.Text.Length > 0 ? price.YIELD.Text[0] : null;
            brPrc.ZvMdur = price.ZV_MDUR != null && price.ZV_MDUR.Text != null && price.ZV_MDUR.Text.Length > 0 ? price.ZV_MDUR.Text[0] : null;
            brPrc.ZvYield = price.ZV_YIELD != null && price.ZV_YIELD.Text != null && price.ZV_YIELD.Text.Length > 0 ? price.ZV_YIELD.Text[0] : null;
          
            return brPrc;
        }
            
    }
}
