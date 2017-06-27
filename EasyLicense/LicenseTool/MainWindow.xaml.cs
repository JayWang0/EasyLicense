using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
			if (!File.Exists("privateKey.xml"))
			{
				MessageBox.Show("Please create a license key first");
				return;
			}

			var privateKey = File.ReadAllText(@"privateKey.xml");
			var generator = new LicenseGenerator(privateKey);

			var dictionary = new Dictionary<string, string>();

			// generate the license
			var license = generator.Generate("EasyLicense", Guid.NewGuid(), DateTime.UtcNow.AddYears(1), dictionary,
				LicenseType.Standard);
			
			txtLicense.Text = license;
			File.WriteAllText("license.lic", license);
		}

		private static void ValidateLicense()
		{
			if (!File.Exists("publicKey.xml"))
			{
				MessageBox.Show("Please create a license key first");
				return;
			}
			
			var publicKey = File.ReadAllText(@"publicKey.xml");

			var validator = new LicenseValidator(publicKey, @"license.lic");

			try
			{
				validator.AssertValidLicense();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}

		private void btnGenerateLicenseKey_Click(object sender, RoutedEventArgs e)
		{
			// var assembly = AppDomain.CurrentDomain.BaseDirectory;

			if (File.Exists("privateKey.xml") || File.Exists("publicKey.xml"))
			{
				var result = MessageBox.Show("The key is existed, override it?", "Warning", MessageBoxButton.YesNo);
				if (result == MessageBoxResult.No)
				{
					return;
				}
			}

			var privateKey = "";
			var publicKey = "";
			LicenseGenerator.GenerateLicenseKey(out privateKey, out publicKey);

			File.WriteAllText("privateKey.xml", privateKey);
			File.WriteAllText("publicKey.xml", publicKey);

			MessageBox.Show("The Key is created, please backup it.");
		}
	}
}