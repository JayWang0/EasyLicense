namespace EasyLicense.Lib.License.Validator
{
	internal sealed class TrailLicenseValidator : AbstractLicenseValidator
	{
		public TrailLicenseValidator(string publicKey)
			: base(publicKey)
		{
			// disable .
			DisableFutureChecks();

			LicenseAttributes.Clear();
		}

		protected override string License { get; set; }
	}
}