using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using DistSysACW.Models;
using Microsoft.AspNetCore.Authorization;
using CoreExtensions;
using DistSysACW.Singleton;
using System.Text;
using System.IO;

namespace DistSysACW.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ProtectedController : ControllerBase
    {
        protected readonly Models.UserContext _context;
        public ProtectedController(Models.UserContext context)
        {
            _context = context;
        }

        [ActionName("hello")]
        [HttpGet]
        public IActionResult Hello([FromHeader(Name = "ApiKey")] string ApiKey)
        {
            try
            {
                if (UserDatabaseAccess.UserCheckApiKey(ApiKey, _context))
                {
                    Models.User user = Models.UserDatabaseAccess.UserRead(ApiKey, _context);
                    return Ok("Hello " + user.UserName);
                }
                else
                {
                    return Ok(ApiKey + " Not in database");
                }
            }
            catch
            {
                return Ok("An error has occurred");
            }
        }

        [ActionName("SHA1")]
        [HttpGet]
        public IActionResult SHA1([FromHeader(Name = "ApiKey")] string ApiKey, [FromQuery] string message)
        {
            if (message == null || message == "") { return StatusCode(400, "Bad Request"); }
            else
            {
                try
                {
                    Models.User user = Models.UserDatabaseAccess.UserRead(ApiKey, _context);
                    byte[] ASCIIarray = System.Text.Encoding.ASCII.GetBytes(message);
                    byte[] SH1array;
                    SHA1 sha1Provider = new SHA1CryptoServiceProvider();
                    SH1array = sha1Provider.ComputeHash(ASCIIarray);

                    string hexString = Models.UserDatabaseAccess.ByteToHexString(SH1array);
                    return Ok(hexString);
                }
                catch { return BadRequest("Bad Request"); }
            }

        }

        [ActionName("SHA256")]
        [HttpGet]
        public IActionResult SHA256([FromHeader(Name = "ApiKey")] string ApiKey, [FromQuery] string message)
        {
            if (message == null || message == "") { return BadRequest("Bad Request"); }
            else
            {
                try
                {
                    Models.User user = Models.UserDatabaseAccess.UserRead(ApiKey, _context);
                    byte[] ASCIIarray = System.Text.Encoding.ASCII.GetBytes(message);
                    byte[] SHA256array;
                    SHA256 sha1Provider = new SHA256CryptoServiceProvider();
                    SHA256array = sha1Provider.ComputeHash(ASCIIarray);

                    string hexString = Models.UserDatabaseAccess.ByteToHexString(SHA256array);
                    return Ok(hexString);
                }
                catch { return BadRequest("Bad Request"); }
            }
        }

        [ActionName("GetPublicKey")]
        [HttpGet]
        public IActionResult GetPublicKey([FromHeader(Name = "ApiKey")] string ApiKey)
        {
            try
            {
                if (UserDatabaseAccess.UserCheckApiKey(ApiKey, _context))
                {
                    var response = RSACryptoExtensions.ToXmlStringCore22(RSACryptoServiceSingleton.getInstance().getRSA());
                    return Ok(response);
                }
            }
            catch
            {
                return Ok(null);
            }
            return Ok(null);
        }

        [ActionName("Sign")]
        [HttpGet]
        public IActionResult Sign([FromHeader(Name = "ApiKey")] string ApiKey, [FromQuery] string message)
        {
            try
            {
                if (UserDatabaseAccess.UserCheckApiKey(ApiKey, _context))
                {
                    var rsa = RSACryptoServiceSingleton.getInstance().getRSA();

                    byte[] msg = System.Text.Encoding.ASCII.GetBytes(message);

                    SHA1 sha1Provider = new SHA1CryptoServiceProvider();

                    var signature = rsa.SignData(msg, new SHA1CryptoServiceProvider());
                    var hexString = BitConverter.ToString(signature);
                    return Ok(hexString);
                }
            }
            catch
            {
                return Ok(null);
            }
            return Ok(null);
        }

        [ActionName("AddFifty")]
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult AddFifty([FromQuery] string encryptedInteger, [FromQuery] string encryptedSymKey, [FromQuery] string encryptedIV)
        {
            try
            {
                var rsa = RSACryptoServiceSingleton.getInstance().getRSA();

                encryptedInteger = encryptedInteger.Replace("-", "");
                byte[] ebyte_int = StringToByteArray(encryptedInteger);
                var dbyte_int = RSADecrypt(ebyte_int, rsa.ExportParameters(true), false);

                encryptedSymKey = encryptedSymKey.Replace("-", "");
                byte[] ebyte_key = StringToByteArray(encryptedSymKey);
                var dbyte_key = RSADecrypt(ebyte_key, rsa.ExportParameters(true), false);

                encryptedIV = encryptedIV.Replace("-", "");
                byte[] ebyte_IV = StringToByteArray(encryptedIV);
                var dbyte_IV = RSADecrypt(ebyte_IV, rsa.ExportParameters(true), false);

                string int_result = Encoding.UTF8.GetString(dbyte_int);
                int d_int = int.Parse(int_result) + 50;

                AesCryptoServiceProvider aes = new AesCryptoServiceProvider();

                ICryptoTransform encryptor = aes.CreateEncryptor(dbyte_key, dbyte_IV);

                byte[] array;

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter streamWriter = new StreamWriter((Stream)cryptoStream))
                        {
                            streamWriter.Write(d_int);
                        }
                        array = memoryStream.ToArray();
                    }
                }
                string hex_val = BitConverter.ToString(array);
                return Ok(hex_val);
            }
            catch
            {
                return StatusCode(400, "Bad Request");
            }
        }

        //Convert Hex String to Byte[]
        public static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length; byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        static public byte[] RSADecrypt(byte[] DataToDecrypt, RSAParameters RSAKeyInfo, bool DoOAEPPadding)
        {
            try
            {
                byte[] decryptedData;
                using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
                {
                    RSA.ImportParameters(RSAKeyInfo);


                    decryptedData = RSA.Decrypt(DataToDecrypt, DoOAEPPadding);
                }
                return decryptedData;
            }
            catch (CryptographicException e)
            {
                Console.WriteLine(e.ToString());

                return null;
            }

        }
    }
}