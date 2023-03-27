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
*   File Name: DecodeParser.cs                                                 
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
using DecodesParser.Service;
using DecodesParser.Model;
using TPSFLib.Utility;
using System.Data;

//Specify Logging Configuration
[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4Net.config", Watch = true)]

namespace DecodesParser
{
    class DecodesParser
    {
        //Get Logger Object
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private DecodesService decodesService = new DecodesService();


	    public static void Main(String[] args)
        {
            logger.Info("************ BEGIN: Decode Parser  ***********");
            
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
                                DecodesParser parser = new DecodesParser();
                                parser.ProcessDecodeFile(inputFile);

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
                logger.Info("************ END: Decode Parser  ***********");

            }
	    }

        private void ProcessDecodeFile(FileInfo input)
        {
            logger.Info("Parse file: " + input.Name);

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(DECODES));
            FileStream fileStream = File.OpenRead(input.FullName);
            DECODES decodes = (DECODES)xmlSerializer.Deserialize(fileStream);

            logger.Info("No of Records Mentioned in Records tag:" + decodes.RECORDS);

            List<BRSDecode> brDecodeList = new List<BRSDecode>();
            if (Convert.ToInt16(decodes.RECORDS) > 0)
            {
                logger.Info("Start Casting  " + decodes.DECODE.Length + " succesfully parsed positions from xml file to BRS Format.");

                foreach (DECODE decode in decodes.DECODE)
                {
                    String tagName = decode.TAG_NAME != null && decode.TAG_NAME.Text != null && decode.TAG_NAME.Text.Length > 0 ? decode.TAG_NAME.Text[0] : null;
                    foreach (DECODE_record decodeRec in decode.DECODE_set.DECODE_record)
                    {
                        brDecodeList.Add(setupBRSDecodeObject(decodeRec, tagName));
                    }
                }
                decodesService.loadBRSDecodes(brDecodeList);
            }
            fileStream.Close();
        }

        private BRSDecode setupBRSDecodeObject(DECODE_record decodeRecord, String tagName)
        {
            BRSDecode brDecode = new BRSDecode();
            brDecode.Tagname = tagName;
            brDecode.Meaning = decodeRecord.MEANING!=null && decodeRecord.MEANING.Text!=null && decodeRecord.MEANING.Text.Length > 0 ? decodeRecord.MEANING.Text[0] : null;
            brDecode.Cde = decodeRecord.CDE!=null && decodeRecord.CDE.Text!=null && decodeRecord.CDE.Text.Length > 0 ? decodeRecord.CDE.Text[0] : null;
            return brDecode;
        }

    }
}
