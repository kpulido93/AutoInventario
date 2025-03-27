using System;
using System.Management;
using System.Security.Principal;
using System.IO;
using Microsoft.Win32;

namespace Autoinventario
{
    public static class SystemInfo
    {
        public static string GetHostname() => Environment.MachineName;

        public static string GetResponsibleUser() => WindowsIdentity.GetCurrent().Name;

        public static string GetDeviceType()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT PCSystemType FROM Win32_ComputerSystem");
                foreach (ManagementObject obj in searcher.Get())
                {
                    int type = Convert.ToInt32(obj["PCSystemType"]);
                    return type switch
                    {
                        1 => "Sobremesa",
                        2 => "Portatil",
                        3 => "Tablet",
                        _ => "Desconocido"
                    };
                }
            }
            catch { }
            return "Desconocido";
        }

        public static string GetOS()
        {
            var searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                return obj["Caption"].ToString();
            }
            return "Desconocido";
        }

        public static string GetAntivirus()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("root\\SecurityCenter2", "SELECT DisplayName FROM AntivirusProduct");
                foreach (ManagementObject obj in searcher.Get())
                {
                    return obj["DisplayName"].ToString();
                }
            }
            catch { }
            return "No instalado";
        }

        public static string GetEncryptionStatus()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("root\\CIMv2\\Security\\MicrosoftVolumeEncryption", "SELECT ProtectionStatus FROM Win32_EncryptableVolume");
                foreach (ManagementObject obj in searcher.Get())
                {
                    return Convert.ToInt32(obj["ProtectionStatus"]) == 1 ? "Encriptado" : "No Encriptado";
                }
            }
            catch { }
            return "No comprobado";
        }

        public static string GetRecoveryPassword()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("root\\CIMv2\\Security\\MicrosoftVolumeEncryption", "SELECT RecoveryPassword FROM Win32_EncryptableVolume");
                foreach (ManagementObject obj in searcher.Get())
                {
                    return obj["RecoveryPassword"].ToString();
                }
            }
            catch { }
            return "No disponible";
        }

        public static string GetSerialNumber()
        {
            var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_Bios");
            foreach (ManagementObject obj in searcher.Get())
            {
                return obj["SerialNumber"].ToString();
            }
            return "Desconocido";
        }

        public static string GetBrand()
        {
            var searcher = new ManagementObjectSearcher("SELECT Manufacturer FROM Win32_ComputerSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                return obj["Manufacturer"].ToString();
            }
            return "Desconocido";
        }

        public static string GetModel()
        {
            var searcher = new ManagementObjectSearcher("SELECT Model FROM Win32_ComputerSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                return obj["Model"].ToString();
            }
            return "Desconocido";
        }

        public static string GetProcessor()
        {
            var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
            foreach (ManagementObject obj in searcher.Get())
            {
                return obj["Name"].ToString();
            }
            return "Desconocido";
        }

        public static string GetStorage()
        {
            var searcher = new ManagementObjectSearcher("SELECT Size FROM Win32_DiskDrive");
            foreach (ManagementObject obj in searcher.Get())
            {
                double sizeGB = Convert.ToDouble(obj["Size"]) / 1e9;
                return Math.Round(sizeGB, 2) + " GB";
            }
            return "Desconocido";
        }

        public static string GetRAM()
        {
            var searcher = new ManagementObjectSearcher("SELECT Capacity FROM Win32_PhysicalMemory");
            double totalRAM = 0;
            foreach (ManagementObject obj in searcher.Get())
            {
                totalRAM += Convert.ToDouble(obj["Capacity"]);
            }
            return (totalRAM / 1e9) + " GB";
        }

        public static string GetDomain()
        {
            var searcher = new ManagementObjectSearcher("SELECT Domain FROM Win32_ComputerSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                return obj["Domain"].ToString();
            }
            return "Desconocido";
        }

        public static string GetWindowsLicense()
        {
            var searcher = new ManagementObjectSearcher("SELECT OA3xOriginalProductKey FROM SoftwareLicensingService");
            foreach (ManagementObject obj in searcher.Get())
            {
                return obj["OA3xOriginalProductKey"].ToString();
            }
            return "No disponible";
        }

        public static string GetPurchaseDate()
        {
            try
            {
                return Directory.GetCreationTime("C:\\Windows").ToString("yyyy-MM-dd HH:mm");
            }
            catch { }
            return "Desconocido";
        }

        public static string GetCurrentDateTime()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        }

        public static string GetOfficeLicense()
        {
            try
            {
                string[] officeVersions = { "16.0", "15.0", "14.0", "12.0" }; // Office 2016, 2013, 2010, 2007
                string basePath = @"SOFTWARE\Microsoft\Office\";

                foreach (var version in officeVersions)
                {
                    string path = basePath + version + @"\Registration";
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(path))
                    {
                        if (key != null)
                        {
                            foreach (var subKeyName in key.GetSubKeyNames())
                            {
                                using (RegistryKey subKey = key.OpenSubKey(subKeyName))
                                {
                                    object productKey = subKey?.GetValue("DigitalProductID");
                                    if (productKey != null)
                                    {
                                        return $"Office {version} detectado";
                                    }
                                }
                            }
                        }
                    }
                }

                return "No se encontró una licencia de Office";
            }
            catch (Exception ex)
            {
                return $"Error al obtener la licencia de Office: {ex.Message}";
            }
        }
    }
}
