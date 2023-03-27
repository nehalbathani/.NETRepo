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
*   File Name: ComplianceParser.cs                                                 
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
using ComplianceParser.Service;
using ComplianceParser.Model;
using TPSFLib.Utility;
using System.Data;

//Specify Logging Configuration
[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4Net.config", Watch = true)]

namespace ComplianceParser
{
    class ComplianceParser
    {
        //Get Logger Object
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private ComplianceService complianceService = new ComplianceService();


	    public static void Main(String[] args)
	    {
            logger.Info("************ BEGIN: Compliance Parser  ***********");
            
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
                                ComplianceParser parser = new ComplianceParser();
                                parser.ProcessComplienceFile(inputFile);

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
                logger.Info("************ END: Compliance Parser  ***********");
            
            }

	    }

        private void ProcessComplienceFile(FileInfo input)
        {

            logger.Info("Parse file: " + input.Name);

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(COMPLIANCES));
            FileStream fileStream = File.OpenRead(input.FullName);
            COMPLIANCES compliances = (COMPLIANCES)xmlSerializer.Deserialize(fileStream);

            logger.Info("No of Records Mentioned in Records tag:" + compliances.RECORDS);

            List<BRSComplience> brComplianceList = new List<BRSComplience>();
            if (Convert.ToInt16(compliances.RECORDS)>0)
            {
                logger.Info("Start Casting  " + compliances.COMPLIANCE.Length + " succesfully parsed positions from xml file to BRS Format.");

                foreach (COMPLIANCE compliance in compliances.COMPLIANCE)
                {
                    brComplianceList.Add(setupBRSComplianceObject(compliance));
                }

                complianceService.loadBRSCompliances(brComplianceList);
                
            }
            fileStream.Close();
        }

        private BRSComplience setupBRSComplianceObject(COMPLIANCE compliance)
        {
            String zeroValue="0";
            BRSComplience brCompl = new BRSComplience();

            brCompl.Accepted_Flag = compliance.ACCEPTED_FLAG != null && compliance.ACCEPTED_FLAG.Value != null  ? compliance.ACCEPTED_FLAG.Value : null;
            brCompl.AcceptedUntil = compliance.ACCEPTED_UNTIL != null && compliance.ACCEPTED_UNTIL.Value != null ? compliance.ACCEPTED_UNTIL.Value : null;
            brCompl.Age = compliance.AGE != null && compliance.AGE.Value != null ? compliance.AGE.Value : null;
            brCompl.CauseType = compliance.CAUSE_TYPE != null && compliance.CAUSE_TYPE.Value != null ? compliance.CAUSE_TYPE.Value : null;
            brCompl.Comments = compliance.COMMENTS != null && compliance.COMMENTS.Value != null ? compliance.COMMENTS.Value : null;
            brCompl.EntryTime = compliance.ENTRY_TIME != null && compliance.ENTRY_TIME.Value != null ? compliance.ENTRY_TIME.Value : null;
            brCompl.Message = compliance.MESSAGE != null && compliance.MESSAGE.Value != null ? compliance.MESSAGE.Value : null;
            brCompl.ModifiedBy = compliance.MODIFIED_BY != null && compliance.MODIFIED_BY.Value != null ? compliance.MODIFIED_BY.Value : null;
            brCompl.OrigViolationNum = compliance.ORIG_VIOLATION_NUM != null && compliance.ORIG_VIOLATION_NUM.Value != null ? compliance.ORIG_VIOLATION_NUM.Value : null;
            brCompl.Pm = compliance.PM != null && compliance.PM.Value != null ? compliance.PM.Value : null;
            brCompl.Portfolio = compliance.PORTFOLIO != null && compliance.PORTFOLIO.Value != null ? compliance.PORTFOLIO.Value : null;
            brCompl.PortfolioName = compliance.PORTFOLIO_NAME != null && compliance.PORTFOLIO_NAME.Value != null ? compliance.PORTFOLIO_NAME.Value : null;
            brCompl.Program = compliance.PROGRAM != null && compliance.PROGRAM.Value != null ? compliance.PROGRAM.Value : null;
            brCompl.Purpose = compliance.PURPOSE != null && compliance.PURPOSE.Value != null ? compliance.PURPOSE.Value : null;
            brCompl.RespPortfolio = compliance.RESP_PORTFOLIO != null && compliance.RESP_PORTFOLIO.Value != null ? compliance.RESP_PORTFOLIO.Value : null;
            brCompl.RespPortfolioName = compliance.RESP_PORTFOLIO_NAME != null && compliance.RESP_PORTFOLIO_NAME.Value != null ? compliance.RESP_PORTFOLIO_NAME.Value : null;
            brCompl.ReviewedBy = compliance.REVIEWED_BY != null && compliance.REVIEWED_BY.Value != null ? compliance.REVIEWED_BY.Value : null;
            brCompl.ReviewedTime = compliance.REVIEWED_TIME != null && compliance.REVIEWED_TIME.Value != null ? compliance.REVIEWED_TIME.Value : null;
            brCompl.Rule = compliance.RULE != null && compliance.RULE.Value != null ? compliance.RULE.Value : null;
            brCompl.Rule_level = compliance.RULE_LEVEL != null && compliance.RULE_LEVEL.Value != null ? compliance.RULE_LEVEL.Value : null;
            brCompl.ViolationDate = compliance.VIOLATION_DATE != null && compliance.VIOLATION_DATE.Value != null ? compliance.VIOLATION_DATE.Value : null;
            brCompl.ViolationNum = compliance.VIOLATION_NUM != null && compliance.VIOLATION_NUM.Value != null ? compliance.VIOLATION_NUM.Value : null;
            brCompl.ModifiedTime = compliance.MODIFIED_TIME != null && compliance.MODIFIED_TIME.Value != null ? compliance.MODIFIED_TIME.Value : null;
            brCompl.Offender = compliance.OFFENDER != null && compliance.OFFENDER.Value != null ? compliance.OFFENDER.Value : null;
            
            brCompl.CompDetailSet = compliance.COMP_DETAIL_set != null && compliance.COMP_DETAIL_set.SIZE != null ? compliance.COMP_DETAIL_set.SIZE : zeroValue;
            
            int setCounter = Convert.ToInt32(compliance.COMP_DETAIL_set != null && compliance.COMP_DETAIL_set.SIZE != null ? compliance.COMP_DETAIL_set.SIZE : zeroValue);
            if (setCounter > 0)
            {
                CompDetail[] BrCompDetailset = new CompDetail[setCounter];
                COMP_DETAIL[] compData = compliance.COMP_DETAIL_set.COMP_DETAIL;
                    
                for (int i = 0; i < setCounter; i++)
                {
                    if (compData != null) {
                        BrCompDetailset[i] = new CompDetail();
                        BrCompDetailset[i].CurFace = (compData[i].CUR_FACE != null && compData[i].CUR_FACE.Value != null) ? compData[i].CUR_FACE.Value : null;
                        BrCompDetailset[i].OrigFace = (compData[i].ORIG_FACE != null && compData[i].ORIG_FACE.Value != null) ? compData[i].ORIG_FACE.Value : null;
                        BrCompDetailset[i].PercentNav = (compData[i].PERCENT_NAV != null && compData[i].PERCENT_NAV.Value != null) ? compData[i].PERCENT_NAV.Value : null;
                        BrCompDetailset[i].Cusip = (compData[i].CUSIP != null && compData[i].CUSIP.Value != null) ? compData[i].CUSIP.Value : null;
                    }
                }
                brCompl.CompDetail = BrCompDetailset;
            }
            /*
            brCompl.Accepted_Flag = compliance.ACCEPTED_FLAG!=null && compliance.ACCEPTED_FLAG.Text!=null && compliance.ACCEPTED_FLAG.Text.Length > 0 ? compliance.ACCEPTED_FLAG.Text[0] : null;
            brCompl.AcceptedUntil = compliance.ACCEPTED_UNTIL != null && compliance.ACCEPTED_UNTIL.Text != null && compliance.ACCEPTED_UNTIL.Text.Length > 0 ? compliance.ACCEPTED_UNTIL.Text[0] : null;
            brCompl.Age = compliance.AGE != null && compliance.AGE.Text != null && compliance.AGE.Text.Length > 0 ? compliance.AGE.Text[0] : null;
            brCompl.CauseType = compliance.CAUSE_TYPE != null && compliance.CAUSE_TYPE.Text != null && compliance.CAUSE_TYPE.Text.Length > 0 ? compliance.CAUSE_TYPE.Text[0] : null;
            brCompl.Comments = compliance.COMMENTS != null && compliance.COMMENTS.Text != null && compliance.COMMENTS.Text.Length > 0 ? compliance.COMMENTS.Text[0] : null;
            brCompl.EntryTime = compliance.ENTRY_TIME != null && compliance.ENTRY_TIME.Text != null && compliance.ENTRY_TIME.Text.Length > 0 ? compliance.ENTRY_TIME.Text[0] : null;
            brCompl.Message = compliance.MESSAGE != null && compliance.MESSAGE.Text != null && compliance.MESSAGE.Text.Length > 0 ? compliance.MESSAGE.Text[0] : null;
            brCompl.ModifiedBy = compliance.MODIFIED_BY != null && compliance.MODIFIED_BY.Text != null && compliance.MODIFIED_BY.Text.Length > 0 ? compliance.MODIFIED_BY.Text[0] : null;
            brCompl.OrigViolationNum = compliance.ORIG_VIOLATION_NUM != null && compliance.ORIG_VIOLATION_NUM.Text != null && compliance.ORIG_VIOLATION_NUM.Text.Length > 0 ? compliance.ORIG_VIOLATION_NUM.Text[0] : null;
            brCompl.Pm = compliance.PM != null && compliance.PM.Text != null && compliance.PM.Text.Length > 0 ? compliance.PM.Text[0] : null;
            brCompl.Portfolio = compliance.PORTFOLIO != null && compliance.PORTFOLIO.Text != null && compliance.PORTFOLIO.Text.Length > 0 ? compliance.PORTFOLIO.Text[0] : null;
            brCompl.PortfolioName = compliance.PORTFOLIO_NAME != null && compliance.PORTFOLIO_NAME.Text != null && compliance.PORTFOLIO_NAME.Text.Length > 0 ? compliance.PORTFOLIO_NAME.Text[0] : null;
            brCompl.Program = compliance.PROGRAM != null && compliance.PROGRAM.Text != null && compliance.PROGRAM.Text.Length > 0 ? compliance.PROGRAM.Text[0] : null;
            brCompl.Purpose = compliance.PURPOSE != null && compliance.PURPOSE.Text != null && compliance.PURPOSE.Text.Length > 0 ? compliance.PURPOSE.Text[0] : null;
            brCompl.RespPortfolio = compliance.RESP_PORTFOLIO != null && compliance.RESP_PORTFOLIO.Text != null && compliance.RESP_PORTFOLIO.Text.Length > 0 ? compliance.RESP_PORTFOLIO.Text[0] : null;
            brCompl.RespPortfolioName = compliance.RESP_PORTFOLIO_NAME != null && compliance.RESP_PORTFOLIO_NAME.Text != null && compliance.RESP_PORTFOLIO_NAME.Text.Length > 0 ? compliance.RESP_PORTFOLIO_NAME.Text[0] : null;
            brCompl.ReviewedBy = compliance.REVIEWED_BY != null && compliance.REVIEWED_BY.Text != null && compliance.REVIEWED_BY.Text.Length > 0 ? compliance.REVIEWED_BY.Text[0] : null;
            brCompl.ReviewedTime = compliance.REVIEWED_TIME != null && compliance.REVIEWED_TIME.Text != null && compliance.REVIEWED_TIME.Text.Length > 0 ? compliance.REVIEWED_TIME.Text[0] : null;
            brCompl.Rule = compliance.RULE != null && compliance.RULE.Text != null && compliance.RULE.Text.Length > 0 ? compliance.RULE.Text[0] : null;
            brCompl.Rule_level = compliance.RULE_LEVEL != null && compliance.RULE_LEVEL.Text != null && compliance.RULE_LEVEL.Text.Length > 0 ? compliance.RULE_LEVEL.Text[0] : null;
            brCompl.ViolationDate = compliance.VIOLATION_DATE != null && compliance.VIOLATION_DATE.Text != null && compliance.VIOLATION_DATE.Text.Length > 0 ? compliance.VIOLATION_DATE.Text[0] : null;
            brCompl.ViolationNum = compliance.VIOLATION_NUM != null && compliance.VIOLATION_NUM.Text != null && compliance.VIOLATION_NUM.Text.Length > 0 ? compliance.VIOLATION_NUM.Text[0] : null;
            */
            return brCompl;
        }
    }
}
