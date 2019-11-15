using BlockBase.Utils.Crypto;
using Org.BouncyCastle.Security;
using System;
using System.IO;
using System.Security.Cryptography;

namespace BlockBase.DataProxy
{
    public class KeyManager
    {
        private const string PATH = "Keystore.txt";
        private static byte[] SaltSize = { 4, 7, 1, 5, 6, 3, 3, 9 };
        public byte[] MasterKey { get; set; }
        public byte[] MasterIV { get; set; }

        private byte[] GenerateKeyAndIV()
        {
            var masterkey = new byte[32];
            var iv = new byte[16];
            SecureRandom random = new SecureRandom();
            random.NextBytes(masterkey);
            random.NextBytes(iv);
            MasterKey = masterkey;
            MasterIV = iv;
            byte[] rv = new byte[masterkey.Length + iv.Length];
            Buffer.BlockCopy(masterkey, 0, rv, 0, masterkey.Length);
            Buffer.BlockCopy(iv, 0, rv, masterkey.Length, iv.Length);

            return rv;
        }

        public void Setup()
        {
            if (File.Exists(PATH))
            {
                var protectedKeyAndIV = File.ReadAllBytes(PATH);
                var unprotectedKeyAndIV = Unprotect(protectedKeyAndIV);
                var masterkey = new byte[32];
                var iv = new byte[16];

                if(unprotectedKeyAndIV == null)  return;
                
                Buffer.BlockCopy(unprotectedKeyAndIV, 0, masterkey, 0, masterkey.Length);
                Buffer.BlockCopy(unprotectedKeyAndIV, masterkey.Length, iv, 0, iv.Length);
                MasterKey = masterkey;
                MasterIV = iv;
            }
            else
            {
                var protectedKeyAndIV = Protect(GenerateKeyAndIV());
                if (protectedKeyAndIV == null) return;
                
                File.WriteAllBytes(PATH, protectedKeyAndIV);
            }
        }
        private byte[] Protect(byte[] data)
        {
            try
            {
                var password = ReadPassword();
                var keyGenerator = new Rfc2898DeriveBytes(password, SaltSize);
                return AES256.EncryptWithCBC(data,keyGenerator.GetBytes(32), keyGenerator.GetBytes(16));
            }
            catch (CryptographicException)
            {
                Console.WriteLine("WrongPassword.");
                return null;
            }
        }
        private byte[] Unprotect(byte[] data)
        {
            try
            {
                var password = ReadPassword();
                var keyGenerator = new Rfc2898DeriveBytes(password, SaltSize);
                return AES256.DecryptWithCBC(data, keyGenerator.GetBytes(32), keyGenerator.GetBytes(16));
            }
            catch (CryptographicException)
            {
                Console.WriteLine("WrongPassword.");
                return null;
            }
        }
        private string ReadPassword()
        {
            Console.WriteLine("Insert password:");
            string pass = "";
            do
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                // Backspace Should Not Work
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    pass += key.KeyChar;
                    Console.Write("*");
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
                    {
                        pass = pass.Substring(0, (pass.Length - 1));
                        Console.Write("\b \b");
                    }
                    else if (key.Key == ConsoleKey.Enter) break;
                }
            } while (true);
            
            return pass;
        }
    }
}
