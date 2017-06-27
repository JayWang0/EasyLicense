using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using EasyLicense.Lib;
using EasyLicense.Lib.License.Validator;

namespace DemoProject
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private static CountManager countManager = new CountManager();

		public MainWindow()
		{
			InitializeComponent();

			countManager.Initialize(new Dictionary<string, int>()
			{
				{"ButtonClick", 3}
			});

			countManager.ExceedLimitation += CountManager_ExceedLimitation;
		}

		private void CountManager_ExceedLimitation(string obj)
		{
			if (Validation() == false)
			{
				MessageBox.Show("No license provided");
			}
		}

		private bool Validation()
		{
			var keyFile = "publicKey.xml";
			string publicKey = "";
			if (File.Exists(keyFile))
			{
				publicKey = File.ReadAllText(keyFile);
			}

			var licenseValidator = new LicenseValidator(publicKey, "license.lic");
			try
			{
				licenseValidator.AssertValidLicense();

				MessageBox.Show($"License is valid to {licenseValidator.Name}, date: {licenseValidator.ExpirationDate}");

				return true;
			}
			catch (Exception ex)
			{
				
			}

			return false;
		}


		private void Button_Click(object sender, RoutedEventArgs e)
		{
			countManager.IncreaseAndValidateCount("ButtonClick");
		}
	}
}
