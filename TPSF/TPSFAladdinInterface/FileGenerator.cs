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
*   File Name: FileGenerator.cs                                                 
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
using FileGenerator.Service;
using System.Data;

using FileGenerator.Model;
using TPSFLib.Utility;
using TPSFLib.Model;
using TPSFLib.DAO;
using TPSFLib.Service;


//Specify Logging Configuration
[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4Net.config", Watch = true)]

namespace FileGenerator
{
    class FileGenerator
    {
        //Get Logger Object
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private FileService fileService = new FileService();
        private MiscellaneousDAO miscDAO = new MiscellaneousDAO();
        private MiscellaneousService miscService = new MiscellaneousService();

        public static void Main(String[] args)
        {
            logger.Info("************ BEGIN: BondEdge LessTIPS File Generator  ***********");
            String feedCode = args[0];
            try
            {

                ConfigurationManager confMngr = new ConfigurationManager();
                Dictionary<String, String> ConfigurationMap = confMngr.getConfiguration(feedCode);

                String exportDir = ConfigurationMap["EXPORT_DIR"];
                String BondEdgeFileName = ConfigurationMap["FILENAME"];

                String appHome = Environment.GetEnvironmentVariable("TPSF_DIR_HOME");
                String appInstance = Environment.GetEnvironmentVariable("TPSF_INSTANCE");

                logger.Info("Application Home Directory:"+appHome);
                logger.Info("Application Instance:"+appInstance);
                logger.Info("Export Directory:"+exportDir);
                logger.Info("File Name:" + BondEdgeFileName);

                String BondEdgeFileNameFull = appHome + "\\data\\SITE\\" + appInstance + "\\" + exportDir + BondEdgeFileName;

                logger.Info("Exporting holdings data to file:" + BondEdgeFileNameFull);
                FileGenerator fileGenerator = new FileGenerator();
            
                fileGenerator.generateFile(BondEdgeFileNameFull, feedCode);

                fileGenerator.miscService.CopyFilesToTPSFOutbound(BondEdgeFileNameFull, ConfigurationMap);
            }
            catch (Exception e)
            {
                logger.Error(e.Message);
                logger.Error(e.StackTrace);
                logger.Error(e.InnerException);
            }
            finally
            {
                logger.Info("************ END: BondEdge LessTIPS File Generator ***********");
            }


        }


        private void generateFile(String BondEdgeFileNameFull, String feedCode)
        {
            miscDAO.CheckLoadStatusForFile(feedCode);
            
            List <LessTips> bondedgeList=fileService.getDataToExport();
            logger.Info("Fetched data from database for file. Record count:" + bondedgeList.Count);
            
                        
            List<FieldConfig> fieldConfigList = new List<FieldConfig>();
            FieldConfig field1 = new FieldConfig(1, "Cusip", true, 8);
            FieldConfig field2 = new FieldConfig(2, "SecDesc", false,28);
            FieldConfig field3 = new FieldConfig(3, "MoodysQuality",false, 4);
            FieldConfig field4 = new FieldConfig(4, "CouponRate", false, 6);
            FieldConfig field5 = new FieldConfig(5, "MaturityDate", false, 8);
            FieldConfig field6 = new FieldConfig(6, "MarketPrice", false, 7);
            FieldConfig field7 = new FieldConfig(7, "Yield", false, 6);
            FieldConfig field8 = new FieldConfig(8, "FormattedParValue",true, 6);
            FieldConfig field9 = new FieldConfig(9, "Sector", false, 4);
            FieldConfig field10 = new FieldConfig(10, "BondType", false, 1);
            FieldConfig field11 = new FieldConfig(11, "CallDate", false, 8);
            FieldConfig field12 = new FieldConfig(12, "CallPrice", false, 7);
            FieldConfig field13 = new FieldConfig(13, "AmtOutstanding", false, 5);
            FieldConfig field14 = new FieldConfig(14, "IntPayFreq", false, 1);
            FieldConfig field15 = new FieldConfig(15, "PutPrice", false, 7);
            FieldConfig field16 = new FieldConfig(16, "SinkFundRetire", false, 6);
            FieldConfig field17 = new FieldConfig(17, "PutDate", false, 8);
            FieldConfig field18 = new FieldConfig(18, "SinkBeginDate", false, 8);
            FieldConfig field19 = new FieldConfig(19, "SinkEndDate", false, 8);
            FieldConfig field20 = new FieldConfig(20, "PortfolioName",true, 20);
            fieldConfigList.Add(field1);
            fieldConfigList.Add(field2);
            fieldConfigList.Add(field3);
            fieldConfigList.Add(field4);
            fieldConfigList.Add(field5);
            fieldConfigList.Add(field6);
            fieldConfigList.Add(field7);
            fieldConfigList.Add(field8);
            fieldConfigList.Add(field9);
            fieldConfigList.Add(field10);
            fieldConfigList.Add(field11);
            fieldConfigList.Add(field12);
            fieldConfigList.Add(field13);
            fieldConfigList.Add(field14);
            fieldConfigList.Add(field15);
            fieldConfigList.Add(field16);
            fieldConfigList.Add(field17);
            fieldConfigList.Add(field18);
            fieldConfigList.Add(field19);
            fieldConfigList.Add(field20);

            miscService.exportListToFile<LessTips>(bondedgeList, BondEdgeFileNameFull, fieldConfigList);
            
        }
       
    }
}
