using Microsoft.AspNetCore.Mvc;
using System.Linq;
using LoginWeb.Models;
using LoginWeb.Data;
using System.Security.Cryptography;
using System.Text;
using System;
using System.IO;
using Org.BouncyCastle.Crypto.Digests;


namespace LoginWeb.Controllers
{
    [ApiController]
    [Route("Device")]
    public class DeviceController : Controller
    {
        private readonly AppDbContext _context;

        public DeviceController(AppDbContext context)
        {
            _context = context;
        }

        // POST: /Device/Create
        [HttpPost("Create")]
        public async Task<IActionResult> Create()
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                string body = await reader.ReadToEndAsync();  

                var dto = Newtonsoft.Json.JsonConvert.DeserializeObject<RegisterDeviceDto>(body);
                if (dto == null)
                    return BadRequest(new { success = false, message = "Invalid JSON format." });


                // 🔹 Validate required fields
                if (string.IsNullOrEmpty(dto.Name) || string.IsNullOrEmpty(dto.Username) ||
                    string.IsNullOrEmpty(dto.PlaintextPassword) || string.IsNullOrEmpty(dto.IPAddress) ||
                    dto.Port == null || string.IsNullOrEmpty(dto.Status))
                {
                    return BadRequest(new { success = false, message = "All fields are required." });
                }

                // 🔹 Validate IP Address
                //if (!System.Net.IPAddress.TryParse(dto.IPAddress, out _))
                //{
                //    return BadRequest(new { success = false, message = "Invalid IP Address format." });
                //}

                // 🔹 Encrypt Password
                byte[] aesKey = EncryptionService.GenerateAESKey(dto.Username);
                var (IV, encryptedPassword) = EncryptionService.Encrypt(dto.PlaintextPassword, aesKey);

                // 🔹 Save to Database
                var newDevice = new Device
                {
                    Name = dto.Name,
                    Username = dto.Username,
                    IV = IV,
                    Password = encryptedPassword,
                    IPAddress = dto.IPAddress,
                    Port = dto.Port,
                    Status = dto.Status,

                    // Default values for extra fields
                    DeviceType = "Unknown",
                    CPUUsage = 0,
                    MemoryUsage = 0,
                    LastUpdated = DateTime.UtcNow,
                    BatteryLevel = 100,
                    OSVersion = "Unknown"
                };

                _context.Devices.Add(newDevice);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Device Registered Successfully", deviceId = newDevice.Id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex}");
                Console.WriteLine($"[INNER EXCEPTION] {ex.InnerException?.Message}");

                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal Server Error",
                    error = ex.InnerException?.Message ?? ex.Message
                });
            }

        }

        // POST: /Device/Edit/{id}
        [HttpPost ("Edit/{id}")]
        public IActionResult Edit(int id, [FromBody] DeviceUpdateModel updatedDevice)
        {
            if (ModelState.IsValid)
            {
                var device = _context.Devices.Find(id);
                if (device == null)
                {
                    return NotFound();
                }
                device.Name = updatedDevice.Name;
                device.IPAddress = updatedDevice.IPAddress;
                device.Status = updatedDevice.Status;

                _context.Devices.Update(device);
                _context.SaveChanges();
                var devices = _context.Devices.ToList();
                return Json(new { success = true, devices = devices });
            }
            return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)) });
        }
        [HttpPost ("Delete/{id}")]
        public IActionResult Delete(int id)
        {
            var device = _context.Devices.Find(id);
            if (device == null)
            {
                return NotFound();
            }

            _context.Devices.Remove(device);
            _context.SaveChanges();
            var devices = _context.Devices.ToList();
            return Json(new { success = true, devices = devices });
        }

        public class EncryptionService
        {
            private static string ComputeMD4(string input)
            {
                MD4Digest md4 = new MD4Digest();
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                md4.BlockUpdate(inputBytes, 0, inputBytes.Length);
                byte[] hashBytes = new byte[md4.GetDigestSize()];
                md4.DoFinal(hashBytes, 0);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }

            public static byte[] GenerateAESKey(string username)
            {
                string md4Hash = ComputeMD4(username);
                using (SHA256 sha256 = SHA256.Create())
                {
                    return sha256.ComputeHash(Encoding.UTF8.GetBytes(md4Hash)); 
                }
            }
            public static (string IV, string EncryptedData) Encrypt(string plaintext, byte[] key)
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.GenerateIV();
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                    {
                        byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
                        byte[] encryptedBytes = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

                        return (Convert.ToBase64String(aes.IV), Convert.ToBase64String(encryptedBytes));
                    }
                }
            }
            public static string Decrypt(string encryptedData, string iv, byte[] key)
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = Convert.FromBase64String(iv); 
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    byte[] encryptedBytes = Convert.FromBase64String(encryptedData);

                    using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    {
                        byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                        return Encoding.UTF8.GetString(decryptedBytes);
                    }
                }
            }
        }
    }
    public class RegisterDeviceDto
    {
        public string Name { get; set; }
        public string Username { get; set; }
        public string PlaintextPassword { get; set; }
        public string IPAddress { get; set; }
        public int? Port { get; set; }
        public string Status { get; set; }
    }
}
