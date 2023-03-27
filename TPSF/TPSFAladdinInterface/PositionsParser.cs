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
*   File Name: PositionsParser.cs                                                 
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
using PositionsParser.Service;
using PositionsParser.Model;
using TPSFLib.Utility;
using System.Data;

//Specify Logging Configuration
[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4Net.config", Watch = true)]

namespace PositionsParser
{
    class PositionsParser
    {
        //Get Logger Object
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private PositionService positionService = new PositionService();


	    public static void Main(String[] args)
        {
            logger.Info("************ BEGIN: Position Parser  ***********");
            
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
                                PositionsParser parser = new PositionsParser();
                                parser.ProcessPositionFile(inputFile);

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
                logger.Info("************ END: Position Parser  ***********");

            }
	    }

        private void ProcessPositionFile(FileInfo input)
        {

            logger.Info("Parse file: " + input.Name);

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(POSITIONS));
            FileStream fileStream = File.OpenRead(input.FullName);
            POSITIONS positions = (POSITIONS)xmlSerializer.Deserialize(fileStream);

            logger.Info("No of Records Mentioned in Records tag:" + positions.RECORDS);

            List<BRSPosition> brPositionsList = new List<BRSPosition>();
            if (Convert.ToInt16(positions.RECORDS) > 0)
            {
                logger.Info("Start Casting  " + positions.POSITION.Length + " succesfully parsed positions from xml file to BRS Format.");

                foreach (POSITION pos in positions.POSITION)
                {
                    brPositionsList.Add(setupBRSPositionObject(pos));
                }

                positionService.loadBRSPositions(brPositionsList);
            }
            fileStream.Close();
        }

        private BRSPosition setupBRSPositionObject(POSITION position)
        {
            BRSPosition brPos = new BRSPosition();

            brPos.AltId = position.ALT_ID != null && position.ALT_ID.Text != null && position.ALT_ID.Text.Length > 0 ? position.ALT_ID.Text[0] : null;
            brPos.BvGaap = position.BV_GAAP != null && position.BV_GAAP.Text != null && position.BV_GAAP.Text.Length>0?position.BV_GAAP.Text[0]:null;
            brPos.BvGaapDate = position.BV_GAAP_DATE != null && position.BV_GAAP_DATE.Text != null && position.BV_GAAP_DATE.Text.Length > 0 ? position.BV_GAAP_DATE.Text[0] : null;
            brPos.BvGaapPrice = position.BV_GAAP_PRICE != null && position.BV_GAAP_PRICE.Text != null && position.BV_GAAP_PRICE.Text.Length > 0 ? position.BV_GAAP_PRICE.Text[0] : null;
            brPos.BvGaapYield = position.BV_GAAP_YLD != null && position.BV_GAAP_YLD.Text != null && position.BV_GAAP_YLD.Text.Length > 0 ? position.BV_GAAP_YLD.Text[0] : null;
            brPos.BvStat = position.BV_STAT != null && position.BV_STAT.Text != null && position.BV_STAT.Text.Length > 0 ? position.BV_STAT.Text[0] : null;
            brPos.BvStatDate = position.BV_STAT_DATE != null && position.BV_STAT_DATE.Text != null && position.BV_STAT_DATE.Text.Length > 0 ? position.BV_STAT_DATE.Text[0] : null;
            brPos.BvStatPrice = position.BV_STAT_PRICE != null && position.BV_STAT_PRICE.Text != null && position.BV_STAT_PRICE.Text.Length > 0 ? position.BV_STAT_PRICE.Text[0] : null;
            brPos.BvStatYld = position.BV_STAT_YLD != null && position.BV_STAT_YLD.Text != null && position.BV_STAT_YLD.Text.Length > 0 ? position.BV_STAT_YLD.Text[0] : null;
            brPos.BvStmt = position.BV_STMT != null && position.BV_STMT.Text != null && position.BV_STMT.Text.Length > 0 ? position.BV_STMT.Text[0] : null;
            brPos.BvStmtDate = position.BV_STMT_DATE != null && position.BV_STMT_DATE.Text != null && position.BV_STMT_DATE.Text.Length > 0 ? position.BV_STMT_DATE.Text[0] : null;
            brPos.BvStmtPrice = position.BV_STMT_PRICE != null && position.BV_STMT_PRICE.Text != null && position.BV_STMT_PRICE.Text.Length > 0 ? position.BV_STMT_PRICE.Text[0] : null;
            brPos.BvStmtYld = position.BV_STMT_YLD != null && position.BV_STMT_YLD.Text != null && position.BV_STMT_YLD.Text.Length > 0 ? position.BV_STMT_YLD.Text[0] : null;
            brPos.BvTax = position.BV_TAX != null && position.BV_TAX.Text != null && position.BV_TAX.Text.Length > 0 ? position.BV_TAX.Text[0] : null;
            brPos.BvTaxDate = position.BV_TAX_DATE != null && position.BV_TAX_DATE.Text != null && position.BV_TAX_DATE.Text.Length > 0 ? position.BV_TAX_DATE.Text[0] : null;
            brPos.BvTaxPrice = position.BV_TAX_PRICE != null && position.BV_TAX_PRICE.Text != null && position.BV_TAX_PRICE.Text.Length > 0 ? position.BV_TAX_PRICE.Text[0] : null;
            brPos.BvTaxYld = position.BV_TAX_YLD != null && position.BV_TAX_YLD.Text != null && position.BV_TAX_YLD.Text.Length > 0 ? position.BV_TAX_YLD.Text[0] : null;
            brPos.ClientId = position.CLIENT_ID != null && position.CLIENT_ID.Text != null && position.CLIENT_ID.Text.Length > 0 ? position.CLIENT_ID.Text[0] : null;
            brPos.CounterpartyCode = position.COUNTERPARTY_CODE != null && position.COUNTERPARTY_CODE.Text != null && position.COUNTERPARTY_CODE.Text.Length > 0 ? position.COUNTERPARTY_CODE.Text[0] : null;
            brPos.CouponFix = position.COUPON_FIX != null && position.COUPON_FIX.Text != null && position.COUPON_FIX.Text.Length > 0 ? position.COUPON_FIX.Text[0] : null;
            brPos.Cusip = position.CUSIP != null && position.CUSIP.Text != null && position.CUSIP.Text.Length > 0 ? position.CUSIP.Text[0] : null;
            brPos.DescInstmt = position.DESC_INSTMT != null && position.DESC_INSTMT.Text != null && position.DESC_INSTMT.Text.Length > 0 ? position.DESC_INSTMT.Text[0] : null;
            brPos.DescInstmt2 = position.DESC_INSTMT2 != null && position.DESC_INSTMT2.Text != null && position.DESC_INSTMT2.Text.Length > 0 ? position.DESC_INSTMT2.Text[0] : null;
            brPos.Desk = position.DESK != null && position.DESK.Text != null && position.DESK.Text.Length > 0 ? position.DESK.Text[0] : null;
            brPos.Entity = position.ENTITY != null && position.ENTITY.Text != null && position.ENTITY.Text.Length > 0 ? position.ENTITY.Text[0] : null;
            brPos.ExSubBrokerId = position.EX_SUB_BROKER_ID != null && position.EX_SUB_BROKER_ID.Text != null && position.EX_SUB_BROKER_ID.Text.Length > 0 ? position.EX_SUB_BROKER_ID.Text[0] : null;
            brPos.ExtId = position.EXT_ID != null && position.EXT_ID.Text != null && position.EXT_ID.Text.Length > 0 ? position.EXT_ID.Text[0] : null;
            brPos.Fund = position.FUND != null && position.FUND.Text != null && position.FUND.Text.Length > 0 ? position.FUND.Text[0] : null;
            brPos.FxPayAmt = position.FX_PAY_AMT != null && position.FX_PAY_AMT.Text != null && position.FX_PAY_AMT.Text.Length > 0 ? position.FX_PAY_AMT.Text[0] : null;
            brPos.FxPayCurr = position.FX_PAY_CURR != null && position.FX_PAY_CURR.Text != null && position.FX_PAY_CURR.Text.Length > 0 ? position.FX_PAY_CURR.Text[0] : null;
            brPos.FxPrice = position.FX_PRICE != null && position.FX_PRICE.Text != null && position.FX_PRICE.Text.Length > 0 ? position.FX_PRICE.Text[0] : null;
            brPos.FxPriceSpot = position.FX_PRICE_SPOT != null && position.FX_PRICE_SPOT.Text != null && position.FX_PRICE_SPOT.Text.Length > 0 ? position.FX_PRICE_SPOT.Text[0] : null;
            brPos.FxRcvAmt = position.FX_RCV_AMT != null && position.FX_RCV_AMT.Text != null && position.FX_RCV_AMT.Text.Length > 0 ? position.FX_RCV_AMT.Text[0] : null;
            brPos.FxRcvCurr = position.FX_RCV_CURR != null && position.FX_RCV_CURR.Text != null && position.FX_RCV_CURR.Text.Length > 0 ? position.FX_RCV_CURR.Text[0] : null;
            brPos.InvNum = position.INVNUM != null && position.INVNUM.Text != null && position.INVNUM.Text.Length > 0 ? position.INVNUM.Text[0] : null;
            brPos.Isin = position.ISIN != null && position.ISIN.Text != null && position.ISIN.Text.Length > 0 ? position.ISIN.Text[0] : null;
            brPos.Manager = position.MANAGER != null && position.MANAGER.Text != null && position.MANAGER.Text.Length > 0 ? position.MANAGER.Text[0] : null;
            brPos.ManagerName = position.MANAGER_NAME != null && position.MANAGER_NAME.Text != null && position.MANAGER_NAME.Text.Length > 0 ? position.MANAGER_NAME.Text[0] : null;
            brPos.Maturity = position.MATURITY != null && position.MATURITY.Text != null && position.MATURITY.Text.Length > 0 ? position.MATURITY.Text[0] : null;
            brPos.MultiFundId = position.MULTI_FUND_ID != null && position.MULTI_FUND_ID.Text != null && position.MULTI_FUND_ID.Text.Length > 0 ? position.MULTI_FUND_ID.Text[0] : null;
            brPos.OrdNum = position.ORD_NUM != null && position.ORD_NUM.Text != null && position.ORD_NUM.Text.Length > 0 ? position.ORD_NUM.Text[0] : null;
            brPos.PortfoliosPortfolioName = position.PORTFOLIOS_PORTFOLIO_NAME != null && position.PORTFOLIOS_PORTFOLIO_NAME.Text != null && position.PORTFOLIOS_PORTFOLIO_NAME.Text.Length > 0 ? position.PORTFOLIOS_PORTFOLIO_NAME.Text[0] : null;
            brPos.PosCurrPar = position.POS_CUR_PAR != null && position.POS_CUR_PAR.Text != null && position.POS_CUR_PAR.Text.Length > 0 ? position.POS_CUR_PAR.Text[0] : null;
            brPos.PosDate = position.POS_DATE != null && position.POS_DATE.Text != null && position.POS_DATE.Text.Length > 0 ? position.POS_DATE.Text[0] : null;
            brPos.PosFace = position.POS_FACE != null && position.POS_FACE.Text != null && position.POS_FACE.Text.Length > 0 ? position.POS_FACE.Text[0] : null;
            brPos.PosSdFace = position.POS_SD_FACE != null && position.POS_SD_FACE.Text != null && position.POS_SD_FACE.Text.Length > 0 ? position.POS_SD_FACE.Text[0] : null;
            brPos.PosSdPar = position.POS_SD_PAR != null && position.POS_SD_PAR.Text != null && position.POS_SD_PAR.Text.Length > 0 ? position.POS_SD_PAR.Text[0] : null;
            brPos.Ppn = position.PPN != null && position.PPN.Text != null && position.PPN.Text.Length > 0 ? position.PPN.Text[0] : null;
            brPos.PricingCusip = position.PRICING_CUSIP != null && position.PRICING_CUSIP.Text != null && position.PRICING_CUSIP.Text.Length > 0 ? position.PRICING_CUSIP.Text[0] : null;
            brPos.Sedol = position.SEDOL != null && position.SEDOL.Text != null && position.SEDOL.Text.Length > 0 ? position.SEDOL.Text[0] : null;
            brPos.SmSecGroup = position.SM_SEC_GROUP != null && position.SM_SEC_GROUP.Text != null && position.SM_SEC_GROUP.Text.Length > 0 ? position.SM_SEC_GROUP.Text[0] : null;
            brPos.SmSecType = position.SM_SEC_TYPE != null && position.SM_SEC_TYPE.Text != null && position.SM_SEC_TYPE.Text.Length > 0 ? position.SM_SEC_TYPE.Text[0] : null;
            brPos.StrategyId = position.STRATEGY_ID != null && position.STRATEGY_ID.Text != null && position.STRATEGY_ID.Text.Length > 0 ? position.STRATEGY_ID.Text[0] : null;
            brPos.SubBrokerId = position.SUB_BROKER_ID != null && position.SUB_BROKER_ID.Text != null && position.SUB_BROKER_ID.Text.Length > 0 ? position.SUB_BROKER_ID.Text[0] : null;
            brPos.TbaCusip8 = position.TBA_CUSIP8 != null && position.TBA_CUSIP8.Text != null && position.TBA_CUSIP8.Text.Length > 0 ? position.TBA_CUSIP8.Text[0] : null;
            brPos.TbaCusip9 = position.TBA_CUSIP9 != null && position.TBA_CUSIP9.Text != null && position.TBA_CUSIP9.Text.Length > 0 ? position.TBA_CUSIP9.Text[0] : null;
            brPos.TouchCount = position.TOUCH_COUNT != null && position.TOUCH_COUNT.Text != null && position.TOUCH_COUNT.Text.Length > 0 ? position.TOUCH_COUNT.Text[0] : null;
            brPos.TranType1 = position.TRAN_TYPE1 != null && position.TRAN_TYPE1.Text != null && position.TRAN_TYPE1.Text.Length > 0 ? position.TRAN_TYPE1.Text[0] : null;
            brPos.TranType2 = position.TRAN_TYPE2 != null && position.TRAN_TYPE2.Text != null && position.TRAN_TYPE2.Text.Length > 0 ? position.TRAN_TYPE2.Text[0] : null;
            brPos.TrdCurrency = position.TRD_CURRENCY != null && position.TRD_CURRENCY.Text != null && position.TRD_CURRENCY.Text.Length > 0 ? position.TRD_CURRENCY.Text[0] : null;
            brPos.TrdPrice = position.TRD_PRICE != null && position.TRD_PRICE.Text != null && position.TRD_PRICE.Text.Length > 0 ? position.TRD_PRICE.Text[0] : null;
            brPos.TrdSettleDate = position.TRD_SETTLE_DATE != null && position.TRD_SETTLE_DATE.Text != null && position.TRD_SETTLE_DATE.Text.Length > 0 ? position.TRD_SETTLE_DATE.Text[0] : null;
            brPos.TrdTradeDate = position.TRD_TRADE_DATE != null && position.TRD_TRADE_DATE.Text != null && position.TRD_TRADE_DATE.Text.Length > 0 ? position.TRD_TRADE_DATE.Text[0] : null;
            brPos.TrdYield = position.TRD_YIELD != null && position.TRD_YIELD.Text != null && position.TRD_YIELD.Text.Length > 0 ? position.TRD_YIELD.Text[0] : null;
            brPos.Units = position.UNITS != null && position.UNITS.Text != null && position.UNITS.Text.Length > 0 ? position.UNITS.Text[0] : null;
            
            return brPos;
        }
            
    }
}
