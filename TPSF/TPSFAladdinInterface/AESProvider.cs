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
*   File Name: AESProvider.cs                                                 
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
using System.Security.Cryptography;
using System.IO;

namespace EncryptionUtility
{
     static class AESProvider
    {

        public static string EncryptString(string plainText, string encryptionKey, string encryptionVector)
        {
            // Check arguments. 
            if (string.IsNullOrEmpty(plainText))
                throw new ArgumentNullException("Please provide plainText");
            if (string.IsNullOrEmpty(encryptionKey))
                throw new ArgumentNullException("Please provide EncryptionKey");
            if (string.IsNullOrEmpty(encryptionVector))
                throw new ArgumentNullException("Please provide EncryptionVector");

            byte[] encrypted;
            // Create an Aes object 
            // with the specified key and IV. 

            using (AesCryptoServiceProvider aesAlg = new AesCryptoServiceProvider())
            {
                aesAlg.Key = TruncateHash(encryptionKey, aesAlg.KeySize / 8);
                aesAlg.IV = TruncateHash(encryptionVector, aesAlg.BlockSize / 8);

                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption. 
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {

                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            // Return the encrypted bytes from the memory stream. 
            return Convert.ToBase64String(encrypted);
        }

        public static string DecryptString(string encryptedString, string encryptionKey, string encryptionVector)
        {
            // Check arguments. 
            if (string.IsNullOrEmpty(encryptedString))
                throw new ArgumentNullException("Please provide encryptedString");
            if (string.IsNullOrEmpty(encryptionKey))
                throw new ArgumentNullException("Please provide EncryptionKey");
            if (string.IsNullOrEmpty(encryptionVector))
                throw new ArgumentNullException("Please provide EncryptionVector");

            // Declare the string used to hold 
            // the decrypted text. 
            string plaintext = null;

            // Create an Aes object 
            // with the specified key and IV. 
            using (AesCryptoServiceProvider aesAlg = new AesCryptoServiceProvider())
            {
                aesAlg.Key = TruncateHash(encryptionKey, aesAlg.KeySize / 8);
                aesAlg.IV = TruncateHash(encryptionVector, aesAlg.BlockSize / 8);

                // Create a decrytor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption. 
                using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(encryptedString)))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            // Read the decrypted bytes from the decrypting stream 
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }

            }

            return plaintext;

        }

        public static void GenerateKeyAndVector(out string encryptionKey, out string encryptionVector)
        {
            using (AesCryptoServiceProvider aesAlg = new AesCryptoServiceProvider())
            {
                encryptionKey = Convert.ToBase64String(aesAlg.Key);
                encryptionVector = Convert.ToBase64String(aesAlg.IV);

            }
        }

        private static byte[] TruncateHash(string key, int length)
        {

            SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider();

            // Hash the key. 
            byte[] keyBytes = System.Text.Encoding.Unicode.GetBytes(key);
            byte[] hash = sha1.ComputeHash(keyBytes);

            // Truncate or pad the hash. 
            Array.Resize(ref hash, length);
            return hash;
        }

    }
}

