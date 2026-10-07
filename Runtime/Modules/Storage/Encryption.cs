using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Moimo.Modules
{
    public static class Encryption
    {
        private const string Key = "m0im0a99t0jktxnl";

        public static byte[] Encrypt(string value)
        {
            using (Aes _aes = Aes.Create())
            {
                byte[] _keyBytes = Encoding.UTF8.GetBytes(Key);
                Array.Resize(ref _keyBytes, _aes.KeySize / 8);

                _aes.Key = _keyBytes;
                _aes.GenerateIV();

                using (var _encryptor = _aes.CreateEncryptor(_aes.Key, _aes.IV))
                using (var _ms = new MemoryStream())
                {
                    _ms.Write(_aes.IV, 0, _aes.IV.Length);
                    using (var _cs = new CryptoStream(_ms, _encryptor, CryptoStreamMode.Write))
                    using (var _sw = new StreamWriter(_cs))
                    {
                        _sw.Write(value);
                    }
                    return _ms.ToArray();
                }
            }
        }

        public static string Decrypt(byte[] encrypt)
        {
            using (Aes _aes = Aes.Create())
            {
                byte[] _keyBytes = Encoding.UTF8.GetBytes(Key);
                Array.Resize(ref _keyBytes, _aes.KeySize / 8);

                _aes.Key = _keyBytes;

                byte[] _iv = new byte[_aes.BlockSize / 8];
                Array.Copy(encrypt, _iv, _iv.Length);

                using (var _decryptor = _aes.CreateDecryptor(_aes.Key, _iv))
                using (var _ms = new MemoryStream(encrypt, _iv.Length, encrypt.Length - _iv.Length))
                using (var _cs = new CryptoStream(_ms, _decryptor, CryptoStreamMode.Read))
                using (var _sr = new StreamReader(_cs))
                {
                    return _sr.ReadToEnd();
                }
            }
        }
    }
}
