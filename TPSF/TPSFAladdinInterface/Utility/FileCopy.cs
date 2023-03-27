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
*   File Name: FileCopy.cs                                                 
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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace EncryptionUtility.Utility
{
    class FileCopy
    {
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
                
        public void SetConfigToFile(string Passwd,string key, string vect,string clear_text)
        {
            try
            {
                String appHome = Environment.GetEnvironmentVariable("TPSF_DIR_HOME");
                String appInstance = Environment.GetEnvironmentVariable("TPSF_INSTANCE");
                String fileConfig = appHome + "\\data\\SITE\\" + appInstance + "\\parm\\tpsfbatch.ini";

                logger.Info("Application Home Directory:" + appHome);
                logger.Info("Application Instance:" + appInstance);
                logger.Info("Configuration File:" + fileConfig);

                if (!File.Exists(fileConfig))
                {
                    logger.Error("Error: Invalid config file " + fileConfig);
                    fileConfig = "C:\\TPSF-HOME\\data\\SITE\\TPSFD\\parm\\tpsfbatch.ini";
                    logger.Info("Load default config file" + fileConfig);
                }

                Properties config = new Properties(fileConfig);
                String keyVect =  vect + key;
                config.set("LOGPASSWORD", Passwd);
                config.set("KEY", keyVect);
                config.set("CLEAR_TEXT", clear_text);
                config.Save();
                logger.Info("New Password saved in Config file");
            }
            catch (Exception e)
            {
                logger.Error(e.Message);
                logger.Error(e.StackTrace);
                logger.Error(e.InnerException);
                throw (e);
            }
    }
}

}