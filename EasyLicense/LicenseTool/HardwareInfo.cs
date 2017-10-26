using System;
using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace EasyLicense.LicenseTool
{
	public class HardwareInfo
	{
		public HardwareInfo()
		{
		}

		private string CalculateMd5Hash(string input)
		{
			MD5 md5 = MD5.Create();

			byte[] inputBytes = Encoding.ASCII.GetBytes(input);
			byte[] hash = md5.ComputeHash(inputBytes);

			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < hash.Length; i++)
			{
				sb.Append(hash[i].ToString("X2"));
			}

			return sb.ToString();
		}
		public string GetHardwareString()
		{
			var key = MachineName + CpuId + MacAddress + DiskSerialNumber;
			var str = CalculateMd5Hash(key) + "ABCDEFG";

			return str.Substring(0, 6);
		}
		
		public string MachineName
		{
			get
			{
				try
				{
					return Environment.MachineName;
				}
				catch
				{
				}

				return "MachineName fail";
			}
		}

		public string CpuId
		{
			get
			{
				try
				{
					using (ManagementClass mc = new ManagementClass("Win32_Processor"))
					using (ManagementObjectCollection moc = mc.GetInstances())
					{
						foreach (ManagementObject mo in moc)
						{
							return mo.Properties["ProcessorId"].Value.ToString().Trim();
						}
					}
				}
				catch
				{
				}
				return "Get CpuId fail";
			}
		}

		public string DiskVolumeSerialNumber
		{
			get
			{
				try
				{
					using (ManagementClass mc = new ManagementClass("win32_logicaldisk"))
					using (ManagementObjectCollection moc = mc.GetInstances())
					{
						foreach (ManagementObject m in moc)
						{
							if (m["DeviceID"].ToString() == "C:")
							{
								return m["VolumeSerialNumber"].ToString().Trim();
							}
						}
					}

				}
				catch
				{
				}
				return "HardDiskId fail";
			}
		}

		public string MacAddress
		{
			get
			{
				try
				{
					using (ManagementClass mcMAC = new ManagementClass("Win32_NetworkAdapterConfiguration"))
					using (ManagementObjectCollection mocMAC = mcMAC.GetInstances())
					{
						foreach (ManagementObject m in mocMAC)
						{
							if ((bool)m["IPEnabled"])
							{
								return m["MacAddress"].ToString().Trim();
							}
						}
					}
				}
				catch
				{
				}

				return "MAC fail";
			}
		}

		public string DiskSerialNumber
		{
			get
			{
				try
				{
					using (ManagementClass mc = new ManagementClass("Win32_PhysicalMedia"))
					using (ManagementObjectCollection moc = mc.GetInstances())
					{
						foreach (ManagementObject mo in moc)
						{
							return mo.Properties["SerialNumber"].Value.ToString().Trim();
						}
					}
				}
				catch
				{
				}

				return "HardwareSerialNumber Fail";
			}
		}
	}
}
