using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using EasyLicense.Lib;
using EasyLicense.Lib.License;
using EasyLicense.Lib.License.Validator;

namespace EasyLicense.LicenseTool
{
	/// <summary>
	///     Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private void btnGenerateLicense_Click(object sender, RoutedEventArgs e)
		{
			GenerateLicense();

			ValidateLicense();
		}

		private void GenerateLicense()
		{
			var privateKey = File.ReadAllText(@"E:\EasyLicense\EasyLicense.Lib\Key\privateKey.xml");
			var generator = new LicenseGenerator(privateKey);

			var dictionary = new Dictionary<string, string>();

			// generate the license
			var license = generator.Generate("WQ", Guid.NewGuid(), DateTime.UtcNow.AddYears(1), dictionary,
				LicenseType.Standard);

			var encryptedLicense =
				new CryptoHelper("ABCDEFGHIJKLMNOP").Encrypt(license);

			txtLicense.Text = encryptedLicense;
			File.WriteAllText("license.lic", encryptedLicense);
		}

		private static void ValidateLicense()
		{
			var publicKey = File.ReadAllText(@"E:\EasyLicense\EasyLicense.Lib\Key\publicKey.xml");

			var validator = new LicenseValidator(publicKey, @"E:\EasyLicense\LicenseTool\bin\Debug\license.lic");

			try
			{
				validator.AssertValidLicense();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}
	}
}