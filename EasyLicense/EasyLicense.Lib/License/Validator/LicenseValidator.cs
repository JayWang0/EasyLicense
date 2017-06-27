using System;
using System.IO;
using EasyLicense.Lib.License.Exception;

namespace EasyLicense.Lib.License.Validator
{
	/// <summary>
	///     License validator validates a license file that can be located on disk.
	/// </summary>
	public class LicenseValidator : AbstractLicenseValidator
	{
		private readonly string licensePath;
		private string inMemoryLicense;

		/// <summary>
		///     Creates a new instance of <seealso cref="LicenseValidator" /> .
		/// </summary>
		/// <param name="publicKey">public key</param>
		/// <param name="licensePath">path to license file</param>
		public LicenseValidator(string publicKey, string licensePath)
			: base(publicKey)
		{
			this.licensePath = licensePath;
		}

		/// <summary>
		///     Creates a new instance of <seealso cref="LicenseValidator" /> .
		/// </summary>
		/// <param name="publicKey">public key</param>
		/// <param name="licensePath">path to license file</param>
		/// <param name="licenseServerUrl">license server endpoint address</param>
		/// <param name="clientId">Id of the license holder</param>
		public LicenseValidator(string publicKey, string licensePath, string licenseServerUrl, Guid clientId)
			: base(publicKey, licenseServerUrl, clientId)
		{
			this.licensePath = licensePath;
		}

		/// <summary>
		///     Gets or Sets the license content
		/// </summary>
		protected override string License
		{
			get
			{
				if (string.IsNullOrEmpty(inMemoryLicense))
				{
					return File.ReadAllText(licensePath);
				}
				return inMemoryLicense;
			}

			set
			{
				try
				{
					File.WriteAllText(licensePath, value);
				}
				catch
				{
					inMemoryLicense = value;
					throw;
				}
			}
		}

		/// <summary>
		///     Validates loaded license
		/// </summary>
		public override void AssertValidLicense()
		{
			if (File.Exists(licensePath) == false)
				throw new LicenseFileNotFoundException();

			base.AssertValidLicense();
		}

		/// <summary>
		///     Removes existing license from the machine.
		/// </summary>
		public override void RemoveExistingLicense()
		{
			File.Delete(licensePath);
		}
	}
}