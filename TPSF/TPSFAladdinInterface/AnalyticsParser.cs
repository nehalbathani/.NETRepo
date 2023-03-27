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
*   File Name: AnalyticsParser.cs                                                 
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
using AnalyticsParser.Service;
using AnalyticsParser.Model;
using TPSFLib.Utility;
using System.Data;

//Specify Logging Configuration
[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4Net.config", Watch = true)]

namespace AnalyticsParser
{
    class AnalyticsParser
    {
        //Get Logger Object
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private AnalyticsService analyticService = new AnalyticsService();


	    public static void Main(String[] args)
	    {
            logger.Info("************ BEGIN: Analytics Parser  ***********");
            
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
                            AnalyticsParser parser = new AnalyticsParser();
                            parser.ProcessAnalyticsFile(inputFile);

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
                logger.Info("************ END: Analytics Parser  ***********");
            
            }

	    }

        private void ProcessAnalyticsFile(FileInfo input)
        {

            logger.Info("Parse file: " + input.Name);

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(FUND));
            FileStream fileStream = File.OpenRead(input.FullName);
            FUNDINSTRUMENTS analytics = ((FUND)xmlSerializer.Deserialize(fileStream)).INSTRUMENTS;
            
            logger.Info("No of Records Mentioned in Records tag:" + analytics.RECORDS);

            List<BRSAnalytics> brsAnalyticsList = new List<BRSAnalytics>();
            if (Convert.ToInt16(analytics.RECORDS)>0)
            {
                logger.Info("Start Casting  " + analytics.INSTRUMENT.Length + " succesfully parsed positions from xml file to BRS Format.");

                foreach (FUNDINSTRUMENTSINSTRUMENT analytic in analytics.INSTRUMENT)
                {
                    brsAnalyticsList.Add(setupBRSAnalyticsObject(analytic));
                }

                analyticService.loadBRSAnalytics(brsAnalyticsList);
                
            }
            fileStream.Close();
        }

        private BRSAnalytics setupBRSAnalyticsObject(FUNDINSTRUMENTSINSTRUMENT analytics)
        {
            BRSAnalytics brAnaly = new BRSAnalytics();

            brAnaly.Abs_Model  = (analytics.ABS_MODEL!=null && analytics.ABS_MODEL.Trim()!="")?analytics.ABS_MODEL:null;
            brAnaly.Abs_Speed = (analytics.ABS_SPEED!=null && analytics.ABS_SPEED.Trim()!="")?analytics.ABS_SPEED:null;
            brAnaly.Acc_Int = (analytics.ACC_INT!=null && analytics.ACC_INT.Trim()!="")?analytics.ACC_INT:null;
            brAnaly.Acc_Int_Factor = (analytics.ACC_INT_FACTOR!=null && analytics.ACC_INT_FACTOR.Trim()!="")?analytics.ACC_INT_FACTOR:null;
            brAnaly.Alt_Oas = (analytics.ALT_OAS!=null && analytics.ALT_OAS.Trim()!="")?analytics.ALT_OAS:null;
            brAnaly.Barc_Sect_Path = (analytics.BARC_SECT_PATH!=null && analytics.BARC_SECT_PATH.Trim()!="")?analytics.BARC_SECT_PATH:null;
            brAnaly.Barc_Four_Pillar_Industry = (analytics.BARCLAYS_FOUR_PILLAR_INDUSTRY!=null && analytics.BARCLAYS_FOUR_PILLAR_INDUSTRY.Trim()!="")?analytics.BARCLAYS_FOUR_PILLAR_INDUSTRY:null;
            brAnaly.Barc_Four_Pillar_Sector= (analytics.BARCLAYS_FOUR_PILLAR_SECTOR!=null && analytics.BARCLAYS_FOUR_PILLAR_SECTOR.Trim()!="")?analytics.BARCLAYS_FOUR_PILLAR_SECTOR:null;
            brAnaly.Barc_Four_Pillar_Subindustry = (analytics.BARCLAYS_FOUR_PILLAR_SUBINDUSTRY!=null && analytics.BARCLAYS_FOUR_PILLAR_SUBINDUSTRY.Trim()!="")?analytics.BARCLAYS_FOUR_PILLAR_SUBINDUSTRY:null;
            brAnaly.Barc_Four_Pillar_Subsector = (analytics.BARCLAYS_FOUR_PILLAR_SUBSECTOR!=null && analytics.BARCLAYS_FOUR_PILLAR_SUBSECTOR.Trim()!="")?analytics.BARCLAYS_FOUR_PILLAR_SUBSECTOR:null;
            brAnaly.Bondtype = (analytics.BONDTYPE!=null && analytics.BONDTYPE.Trim()!="")?analytics.BONDTYPE:null;
            brAnaly.Cins = (analytics.CINS!=null && analytics.CINS.Trim()!="")?analytics.CINS:null;
            brAnaly.Clean_Mkt_Value = (analytics.CLEAN_MKT_VALUE!=null && analytics.CLEAN_MKT_VALUE.Trim()!="")?analytics.CLEAN_MKT_VALUE:null;
            brAnaly.Client_Id = (analytics.CLIENT_ID!=null && analytics.CLIENT_ID.Trim()!="")?analytics.CLIENT_ID:null;
            brAnaly.Country = (analytics.COUNTRY!=null && analytics.COUNTRY.Trim()!="")?analytics.COUNTRY:null;
            brAnaly.Coup_Freq  = (analytics.COUP_FREQ!=null && analytics.COUP_FREQ.Trim()!="")?analytics.COUP_FREQ:null;
            brAnaly.Coupon = (analytics.COUPON!=null && analytics.COUPON.Trim()!="")?analytics.COUPON:null;
            brAnaly.Cpn_type = (analytics.CPN_TYPE!=null && analytics.CPN_TYPE.Trim()!="")?analytics.CPN_TYPE:null;
            brAnaly.Cur_Bench  = (analytics.CUR_BENCH!=null && analytics.CUR_BENCH.Trim()!="")?analytics.CUR_BENCH:null;
            brAnaly.Cur_Face = (analytics.CUR_FACE!=null && analytics.CUR_FACE.Trim()!="")?analytics.CUR_FACE:null;
            brAnaly.Cur_Spd = (analytics.CUR_SPD!=null && analytics.CUR_SPD.Trim()!="")?analytics.CUR_SPD:null;
            brAnaly.Currency = (analytics.CURRENCY!=null && analytics.CURRENCY.Trim()!="")?analytics.CURRENCY:null;

            brAnaly.Current_Factor = (analytics.CURRENT_FACTOR!=null && analytics.CURRENT_FACTOR.Trim()!="")?analytics.CURRENT_FACTOR:null;
            brAnaly.Current_Factor_Eff_Dt = (analytics.CURRENT_FACTOR_EFF_DT!=null && analytics.CURRENT_FACTOR_EFF_DT.Trim()!="")?analytics.CURRENT_FACTOR_EFF_DT:null;
            brAnaly.Current_Yield = (analytics.CURRENT_YIELD!=null && analytics.CURRENT_YIELD.Trim()!="")?analytics.CURRENT_YIELD:null;
            brAnaly.Cusip = (analytics.CUSIP!=null && analytics.CUSIP.Trim()!="")?analytics.CUSIP:null;
            brAnaly.Dv01_Dollars = (analytics.DV01_DOLLARS!=null && analytics.DV01_DOLLARS.Trim()!="")?analytics.DV01_DOLLARS:null;
            brAnaly.Eff_Conv = (analytics.EFF_CONV!=null && analytics.EFF_CONV.Trim()!="")?analytics.EFF_CONV:null;
            brAnaly.Eff_Dur = (analytics.EFF_DUR!=null && analytics.EFF_DUR.Trim()!="")?analytics.EFF_DUR:null;
            brAnaly.Eff_Maturity = (analytics.EFF_MATURITY!=null && analytics.EFF_MATURITY.Trim()!="")?analytics.EFF_MATURITY:null;
            brAnaly.Flag_Convert = (analytics.FLAG_CONVERT!=null && analytics.FLAG_CONVERT.Trim()!="")?analytics.FLAG_CONVERT:null;
            brAnaly.Fundnum = (analytics.FUNDNUM!=null && analytics.FUNDNUM.Trim()!="")?analytics.FUNDNUM:null;
            brAnaly.Implied_Vol = (analytics.IMPLIED_VOL!=null && analytics.IMPLIED_VOL.Trim()!="")?analytics.IMPLIED_VOL:null;
            brAnaly.Infl = (analytics.INFL!=null && analytics.INFL.Trim()!="")?analytics.INFL:null;
            brAnaly.Internal_Rating = (analytics.INTERNAL_RATING!=null && analytics.INTERNAL_RATING.Trim()!="")?analytics.INTERNAL_RATING:null;
            brAnaly.Isin = (analytics.ISIN!=null && analytics.ISIN.Trim()!="")?analytics.ISIN:null;
            brAnaly.Issue_Date  = (analytics.ISSUE_DATE!=null && analytics.ISSUE_DATE.Trim()!="")?analytics.ISSUE_DATE:null;
            brAnaly.Kprd_10yr  = (analytics.KPRD_10YR!=null && analytics.KPRD_10YR.Trim()!="")?analytics.KPRD_10YR:null;
            brAnaly.Kprd_15yr = (analytics.KPRD_15YR!=null && analytics.KPRD_15YR.Trim()!="")?analytics.KPRD_15YR:null;
            brAnaly.Kprd_1yr = (analytics.KPRD_1YR!=null && analytics.KPRD_1YR.Trim()!="")?analytics.KPRD_1YR:null;
            brAnaly.Kprd_20yr = (analytics.KPRD_20YR!=null && analytics.KPRD_20YR.Trim()!="")?analytics.KPRD_20YR:null;
            brAnaly.Kprd_25yr = (analytics.KPRD_25YR!=null && analytics.KPRD_25YR.Trim()!="")?analytics.KPRD_25YR:null;
            brAnaly.Kprd_2yr = (analytics.KPRD_2YR!=null && analytics.KPRD_2YR.Trim()!="")?analytics.KPRD_2YR:null;
            brAnaly.Kprd_30yr = (analytics.KPRD_30YR!=null && analytics.KPRD_30YR.Trim()!="")?analytics.KPRD_30YR:null;

            brAnaly.Kprd_3mth = (analytics.KPRD_3MTH!=null && analytics.KPRD_3MTH.Trim()!="")?analytics.KPRD_3MTH:null;
            brAnaly.Kprd_3yr = (analytics.KPRD_3YR!=null && analytics.KPRD_3YR.Trim()!="")?analytics.KPRD_3YR:null;
            brAnaly.Kprd_5yr = (analytics.KPRD_5YR!=null && analytics.KPRD_5YR.Trim()!="")?analytics.KPRD_5YR:null;
            brAnaly.Kprd_7yr = (analytics.KPRD_7YR!=null && analytics.KPRD_7YR.Trim()!="")?analytics.KPRD_7YR:null;
            brAnaly.Krd_10yr = (analytics.KRD_10YR!=null && analytics.KRD_10YR.Trim()!="")?analytics.KRD_10YR:null;
            brAnaly.Krd_15yr = (analytics.KRD_15YR!=null && analytics.KRD_15YR.Trim()!="")?analytics.KRD_15YR:null;
            brAnaly.Krd_20yr = (analytics.KRD_20YR!=null && analytics.KRD_20YR.Trim()!="")?analytics.KRD_20YR:null;
            brAnaly.Krd_25yr = (analytics.KRD_25YR!=null && analytics.KRD_25YR.Trim()!="")?analytics.KRD_25YR:null;
            brAnaly.Krd_30yr = (analytics.KRD_30YR!=null && analytics.KRD_30YR.Trim()!="")?analytics.KRD_30YR:null;
            brAnaly.Krd_1yr = (analytics.KRD_1YR != null && analytics.KRD_1YR.Trim() != "") ? analytics.KRD_1YR : null;
            brAnaly.Krd_2yr = (analytics.KRD_2YR!=null && analytics.KRD_2YR.Trim()!="")?analytics.KRD_2YR:null;
            brAnaly.Krd_3yr = (analytics.KRD_3YR!=null && analytics.KRD_3YR.Trim()!="")?analytics.KRD_3YR:null;
            brAnaly.Krd_5yr = (analytics.KRD_5YR!=null && analytics.KRD_5YR.Trim()!="")?analytics.KRD_5YR:null;
            brAnaly.Krd_7yr = (analytics.KRD_7YR!=null && analytics.KRD_7YR.Trim()!="")?analytics.KRD_7YR:null;
            brAnaly.Krd_3mth = (analytics.KRD_3MTH!=null && analytics.KRD_3MTH.Trim()!="")?analytics.KRD_3MTH:null;
            brAnaly.Local_Acc_Int = (analytics.LOCAL_ACC_INT!=null && analytics.LOCAL_ACC_INT.Trim()!="")?analytics.LOCAL_ACC_INT:null;
            brAnaly.Local_Acc_Int_Factor = (analytics.LOCAL_ACC_INT_FACTOR!=null && analytics.LOCAL_ACC_INT_FACTOR.Trim()!="")?analytics.LOCAL_ACC_INT_FACTOR:null;
            brAnaly.Local_Dv01 = (analytics.LOCAL_DV01!=null && analytics.LOCAL_DV01.Trim()!="")?analytics.LOCAL_DV01:null;
            brAnaly.Local_Mkt_Value = (analytics.LOCAL_MKT_VALUE!=null && analytics.LOCAL_MKT_VALUE.Trim()!="")?analytics.LOCAL_MKT_VALUE:null;
            brAnaly.Local_Notion_Value = (analytics.LOCAL_NOTION_VALUE!=null && analytics.LOCAL_NOTION_VALUE.Trim()!="")?analytics.LOCAL_NOTION_VALUE:null;
            brAnaly.Maturity = (analytics.MATURITY!=null && analytics.MATURITY.Trim()!="")?analytics.MATURITY:null;
            brAnaly.Mkt_Notion = (analytics.MKT_NOTION!=null && analytics.MKT_NOTION.Trim()!="")?analytics.MKT_NOTION:null;

            brAnaly.Mkt_Value = (analytics.MKT_VALUE!=null && analytics.MKT_VALUE.Trim()!="")?analytics.MKT_VALUE:null;
            brAnaly.Mod_Dur = (analytics.MOD_DUR!=null && analytics.MOD_DUR.Trim()!="")?analytics.MOD_DUR:null;
            brAnaly.Mod_Dur_To_Worst = (analytics.MOD_DUR_TO_WORST!=null && analytics.MOD_DUR_TO_WORST.Trim()!="")?analytics.MOD_DUR_TO_WORST:null;
            brAnaly.MODEL_OAC = (analytics.MODEL_OAC!=null && analytics.MODEL_OAC.Trim()!="")?analytics.MODEL_OAC:null;
            brAnaly.MODEL_OAD = (analytics.MODEL_OAD!=null && analytics.MODEL_OAD.Trim()!="")?analytics.MODEL_OAD:null;
            brAnaly.Mtb_Dur = (analytics.MTB_DUR!=null && analytics.MTB_DUR.Trim()!="")?analytics.MTB_DUR:null;
            brAnaly.Nominal_Yield = (analytics.NOMINAL_YIELD!=null && analytics.NOMINAL_YIELD.Trim()!="")?analytics.NOMINAL_YIELD:null;

            brAnaly.Nvol_Dur = (analytics.NVOL_DUR!=null && analytics.NVOL_DUR.Trim()!="")?analytics.NVOL_DUR:null;
            brAnaly.Oad_Mult = (analytics.OAD_MULT!=null && analytics.OAD_MULT.Trim()!="")?analytics.OAD_MULT:null;
            brAnaly.Oas = (analytics.OAS!=null && analytics.OAS.Trim()!="")?analytics.OAS:null;
            brAnaly.Option_Delta = (analytics.OPTION_DELTA!=null && analytics.OPTION_DELTA.Trim()!="")?analytics.OPTION_DELTA:null;
            brAnaly.Option_Gamma = (analytics.OPTION_GAMMA!=null && analytics.OPTION_GAMMA.Trim()!="")?analytics.OPTION_GAMMA:null;
            brAnaly.Option_Theta = (analytics.OPTION_THETA!=null && analytics.OPTION_THETA.Trim()!="")?analytics.OPTION_THETA:null;
            brAnaly.Orig_Face = (analytics.ORIG_FACE!=null && analytics.ORIG_FACE.Trim()!="")?analytics.ORIG_FACE:null;

            brAnaly.Par_Oad = (analytics.PAR_OAD!=null && analytics.PAR_OAD.Trim()!="")?analytics.PAR_OAD:null;
            brAnaly.Pct_Of_Nav = (analytics.PCT_OF_NAV!=null && analytics.PCT_OF_NAV.Trim()!="")?analytics.PCT_OF_NAV:null;
            brAnaly.Pd_Cpr_12m = (analytics.PD_CPR_12M!=null && analytics.PD_CPR_12M.Trim()!="")?analytics.PD_CPR_12M:null;
            brAnaly.Pd_Cpr_1m = (analytics.PD_CPR_1M!=null && analytics.PD_CPR_1M.Trim()!="")?analytics.PD_CPR_1M:null;
            brAnaly.Pd_Cpr_3m = (analytics.PD_CPR_3M!=null && analytics.PD_CPR_3M.Trim()!="")?analytics.PD_CPR_3M:null;
            brAnaly.Pd_Cpr_6m = (analytics.PD_CPR_6M!=null && analytics.PD_CPR_6M.Trim()!="")?analytics.PD_CPR_6M:null;
            brAnaly.Pd_Cpr_Life =( analytics.PD_CPR_LIFE!=null && analytics.PD_CPR_LIFE.Trim()!="")?analytics.PD_CPR_LIFE:null;
            brAnaly.Pd_Cpr_Waleq = (analytics.PD_CPR_WALEQ!=null && analytics.PD_CPR_WALEQ.Trim()!="")?analytics.PD_CPR_WALEQ:null;
            brAnaly.Pd_Psa_Life = (analytics.PD_PSA_LIFE!=null && analytics.PD_PSA_LIFE.Trim()!="")?analytics.PD_PSA_LIFE:null;
            brAnaly.Pd_Psa_Waleq = (analytics.PD_PSA_WALEQ!=null && analytics.PD_PSA_WALEQ.Trim()!="")?analytics.PD_PSA_WALEQ:null;

            brAnaly.Pd_Wac= (analytics.PD_WAC!=null && analytics.PD_WAC.Trim()!="")?analytics.PD_WAC:null;
            brAnaly.Pd_Wala = (analytics.PD_WALA!=null && analytics.PD_WALA.Trim()!="")?analytics.PD_WALA:null;
            brAnaly.Pd_Wam = (analytics.PD_WAM!=null && analytics.PD_WAM.Trim()!="")?analytics.PD_WAM:null;
            brAnaly.Portf_Base_Currency = (analytics.PORTF_BASE_CURRENCY!=null && analytics.PORTF_BASE_CURRENCY.Trim()!="")?analytics.PORTF_BASE_CURRENCY:null;
            brAnaly.Portf_List = (analytics.PORTF_LIST!=null && analytics.PORTF_LIST.Trim()!="")?analytics.PORTF_LIST:null;
            brAnaly.Prep_Conv = (analytics.PREP_CONV!=null && analytics.PREP_CONV.Trim()!="")?analytics.PREP_CONV:null; 
            brAnaly.Prep_Dur = (analytics.PREP_DUR!=null && analytics.PREP_DUR.Trim()!="")?analytics.PREP_DUR:null;
            brAnaly.Put_Call = (analytics.PUT_CALL!=null && analytics.PUT_CALL.Trim()!="")?analytics.PUT_CALL:null;
            brAnaly.Pxs_Date = (analytics.PXS_DATE!=null && analytics.PXS_DATE.Trim()!="")?analytics.PXS_DATE:null;
            brAnaly.Pxs_Dec_Only = (analytics.PXS_DEC_ONLY!=null && analytics.PXS_DEC_ONLY.Trim()!="")?analytics.PXS_DEC_ONLY:null;
            brAnaly.Pxs_Purpose = (analytics.PXS_PURPOSE!=null && analytics.PXS_PURPOSE.Trim()!="")?analytics.PXS_PURPOSE:null;
            brAnaly.Pxs_Source = (analytics.PXS_SOURCE!=null && analytics.PXS_SOURCE.Trim()!="")?analytics.PXS_SOURCE:null;
            brAnaly.Real_Yield = (analytics.REAL_YIELD!=null && analytics.REAL_YIELD.Trim()!="")?analytics.REAL_YIELD:null;
            brAnaly.Ref_Security =( analytics.REFERENCE_SECURITY!=null && analytics.REFERENCE_SECURITY.Trim()!="")?analytics.REFERENCE_SECURITY:null;
            brAnaly.Restr_Asset = (analytics.RESTR_ASSET!=null && analytics.RESTR_ASSET.Trim()!="")?analytics.RESTR_ASSET:null;
            brAnaly.Risk_Country = (analytics.RISK_COUNTRY!=null && analytics.RISK_COUNTRY.Trim()!="")?analytics.RISK_COUNTRY:null;
            brAnaly.Risk_Date = (analytics.RISK_DATE!=null && analytics.RISK_DATE.Trim()!="")?analytics.RISK_DATE:null;
            brAnaly.Risk_Source = (analytics.RISK_SOURCE!=null && analytics.RISK_SOURCE.Trim()!="")?analytics.RISK_SOURCE:null;
            brAnaly.Sec_Desc2 = (analytics.SEC_DESC2!=null && analytics.SEC_DESC2.Trim()!="")?analytics.SEC_DESC2:null;
            brAnaly.Sedol = (analytics.SEDOL!=null && analytics.SEDOL.Trim()!="")?analytics.SEDOL:null;
            brAnaly.Settle_Flag = (analytics.SETTLE_FLAG!=null && analytics.SETTLE_FLAG.Trim()!="")?analytics.SETTLE_FLAG:null;

            brAnaly.Short_Std_Desc = (analytics.SHORT_STD_DESC!=null && analytics.SHORT_STD_DESC.Trim()!="")?analytics.SHORT_STD_DESC:null;
            brAnaly.Sm_Sec_Group = (analytics.SM_SEC_GROUP!=null && analytics.SM_SEC_GROUP.Trim()!="")?analytics.SM_SEC_GROUP:null;
            brAnaly.Sm_Sec_Type= (analytics.SM_SEC_TYPE!=null && analytics.SM_SEC_TYPE.Trim()!="")?analytics.SM_SEC_TYPE:null;
            brAnaly.Spread_Dur = (analytics.SPREAD_DUR!=null && analytics.SPREAD_DUR.Trim()!="")?analytics.SPREAD_DUR:null;
            brAnaly.Structure = (analytics.STRUCTURE!=null && analytics.STRUCTURE.Trim()!="")?analytics.STRUCTURE:null;
            brAnaly.Ticker = (analytics.TICKER!=null && analytics.TICKER.Trim()!="")?analytics.TICKER:null;
            brAnaly.Ult_Issuer_Name = (analytics.ULT_ISSUER_NAME!=null && analytics.ULT_ISSUER_NAME.Trim()!="")?analytics.ULT_ISSUER_NAME:null;
            brAnaly.Ult_Parent_Ticker= (analytics.ULTIMATE_PARENT_TICKER!=null && analytics.ULTIMATE_PARENT_TICKER.Trim()!="")?analytics.ULTIMATE_PARENT_TICKER:null;
            brAnaly.Wal = (analytics.WAL!=null && analytics.WAL.Trim()!="")?analytics.WAL:null;
            brAnaly.Wal_To_Mat= (analytics.WAL_TO_MAT!=null && analytics.WAL_TO_MAT.Trim()!="")?analytics.WAL_TO_MAT:null;
            brAnaly.Wal_To_Worst = (analytics.WAL_TO_WORST!=null && analytics.WAL_TO_WORST.Trim()!="")?analytics.WAL_TO_WORST:null;
            brAnaly.Yield_To_Mat = (analytics.YIELD_TO_MAT!=null && analytics.YIELD_TO_MAT.Trim()!="")?analytics.YIELD_TO_MAT:null;
            brAnaly.Zv_Maturity = (analytics.ZV_MATURITY!=null && analytics.ZV_MATURITY.Trim()!="")?analytics.ZV_MATURITY:null;
            brAnaly.Zv_Principal_Begin_Dt = (analytics.ZV_PRINCIPAL_BEGIN_DT!=null && analytics.ZV_PRINCIPAL_BEGIN_DT.Trim()!="")?analytics.ZV_PRINCIPAL_BEGIN_DT:null;
            brAnaly.Zv_Spread = (analytics.ZV_SPREAD!=null && analytics.ZV_SPREAD.Trim()!="")?analytics.ZV_SPREAD:null ;
            

            return brAnaly;
        }
    }
}
