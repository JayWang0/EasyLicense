using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Threading;
using System.Xml;
using EasyLicense.Lib.License.Exception;

namespace EasyLicense.Lib.License.Validator
{
	/// <summary>
	///     Base license validator.
	/// </summary>
	public abstract class AbstractLicenseValidator
	{
		private readonly string licenseServerUrl;
		private readonly Timer nextLeaseTimer;
		private readonly string publicKey;

		private bool currentlyValidatingSubscriptionLicense;
		private bool disableFutureChecks;

		/// <summary>
		///     Creates a license validator with specfied public key.
		/// </summary>
		/// <param name="publicKey">public key</param>
		protected AbstractLicenseValidator(string publicKey)
		{
			LicenseAttributes = new Dictionary<string, string>();
			nextLeaseTimer = new Timer(LeaseLicenseAgain);
			this.publicKey = publicKey;
		}

		/// <summary>
		///     Creates a license validator using the client information and a service endpoint address
		///     to validate the license.
		/// </summary>
		/// <param name="publicKey"></param>
		/// <param name="licenseServerUrl"></param>
		/// <param name="clientId"></param>
		protected AbstractLicenseValidator(string publicKey, string licenseServerUrl, Guid clientId)
		{
			LicenseAttributes = new Dictionary<string, string>();
			nextLeaseTimer = new Timer(LeaseLicenseAgain);
			this.publicKey = publicKey;
			this.licenseServerUrl = licenseServerUrl;
		}

		/// <summary>
		///     Gets or Sets Floating license support
		/// </summary>
		public virtual bool DisableFloatingLicenses { get; set; }

		/// <summary>
		///     Gets the expiration date of the license
		/// </summary>
		public virtual DateTime ExpirationDate { get; private set; }

		/// <summary>
		///     Gets extra license information
		/// </summary>
		public virtual IDictionary<string, string> LicenseAttributes { get; }

		/// <summary>
		///     Gets the Type of the license
		/// </summary>
		public virtual LicenseType LicenseType { get; private set; }

		/// <summary>
		///     Gets the name of the license holder
		/// </summary>
		public virtual string Name { get; private set; }

		/// <summary>
		///     Gets or Sets the endpoint address of the subscription service
		/// </summary>
		public virtual string SubscriptionEndpoint { get; set; }

		/// <summary>
		///     Gets the Id of the license holder
		/// </summary>
		public Guid UserId { get; private set; }

		/// <summary>
		///     Gets or Sets the license content
		/// </summary>
		protected abstract string License { get; set; }

		/// <summary>
		///     Fired when license data is invalidated
		/// </summary>
		public event Action<InvalidationType> LicenseInvalidated;

		/// <summary>
		///     Validates loaded license
		/// </summary>
		public virtual void AssertValidLicense()
		{
			LicenseAttributes.Clear();
			if (HasExistingLicense())
				return;
			throw new LicenseNotFoundException();
		}

		/// <summary>
		///     Disables further license checks for the session.
		/// </summary>
		public void DisableFutureChecks()
		{
			disableFutureChecks = true;
			nextLeaseTimer.Dispose();
		}

		public virtual int GetLicenseAttribute(string attributeName)
		{
			if (LicenseAttributes.ContainsKey(attributeName))
				return Convert.ToInt32(LicenseAttributes[attributeName]);

			return -1;
		}

		/// <summary>
		///     Removes existing license from the machine.
		/// </summary>
		public virtual void RemoveExistingLicense()
		{
		}

		/// <summary>
		///     Loads license data from validated license file.
		/// </summary>
		/// <returns></returns>
		public bool TryLoadingLicenseValuesFromValidatedXml()
		{
			try
			{
				var doc = new XmlDocument();
				doc.LoadXml(License);

				if (TryGetValidDocument(publicKey, doc) == false)
					return false;

				if (doc.FirstChild == null)
					return false;

				if (doc.SelectSingleNode("/floating-license") != null)
				{
					var node = doc.SelectSingleNode("/floating-license/license-server-public-key/text()");
					if (node == null)
						throw new InvalidOperationException(
							"Invalid license file format, floating license without license server public key");
					return ValidateFloatingLicense(node.InnerText);
				}

				var result = ValidateXmlDocumentLicense(doc);
				if (result && disableFutureChecks == false)
					nextLeaseTimer.Change(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
				return result;
			}
			catch (RhinoLicensingException)
			{
				throw;
			}
			catch
			{
				return false;
			}
		}

		internal bool ValidateXmlDocumentLicense(XmlDocument doc)
		{
			var id = doc.SelectSingleNode("/license/@id");
			if (id == null)
				return false;

			UserId = new Guid(id.Value);

			var date = doc.SelectSingleNode("/license/@expiration");
			if (date == null)
				return false;

			ExpirationDate = DateTime.ParseExact(date.Value, "yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture);

			var licenseType = doc.SelectSingleNode("/license/@type");
			if (licenseType == null)
				return false;

			LicenseType = (LicenseType) Enum.Parse(typeof(LicenseType), licenseType.Value);

			var name = doc.SelectSingleNode("/license/name/text()");
			if (name == null)
				return false;

			Name = name.Value;

			var license = doc.SelectSingleNode("/license");
			foreach (XmlAttribute attrib in license.Attributes)
			{
				if (attrib.Name == "type" || attrib.Name == "expiration" || attrib.Name == "id")
					continue;

				LicenseAttributes[attrib.Name] = attrib.Value;
			}

			return true;
		}

		/// <summary>
		///     Loads the license file.
		/// </summary>
		/// <param name="newLicense"></param>
		/// <returns></returns>
		protected bool TryOverwritingWithNewLicense(string newLicense)
		{
			if (string.IsNullOrEmpty(newLicense))
				return false;
			try
			{
				var xmlDocument = new XmlDocument();
				xmlDocument.LoadXml(newLicense);
			}
			catch
			{
				return false;
			}
			License = newLicense;
			return true;
		}

		private bool HasExistingLicense()
		{
			try
			{
				if (TryLoadingLicenseValuesFromValidatedXml() == false)
					return false;

				bool result;
				if (LicenseType == LicenseType.Subscription)
					result = ValidateSubscription();
				else
					result = DateTime.UtcNow < ExpirationDate;

				if (result == false)
					throw new LicenseExpiredException("Expiration Date : " + ExpirationDate);

				return true;
			}
			catch (RhinoLicensingException)
			{
				throw;
			}
			catch
			{
				return false;
			}
		}

		private void LeaseLicenseAgain(object state)
		{
			if (HasExistingLicense())
				return;
			RaiseLicenseInvalidated();
		}

		private void RaiseLicenseInvalidated()
		{
			var licenseInvalidated = LicenseInvalidated;
			if (licenseInvalidated == null)
				throw new InvalidOperationException(
					"License was invalidated, but there is no one subscribe to the LicenseInvalidated event");
			licenseInvalidated(LicenseType == LicenseType.Floating
				? InvalidationType.CannotGetNewLicense
				: InvalidationType.TimeExpired);
		}

		private void TryGettingNewLeaseSubscription()
		{
		}

		private bool TryGetValidDocument(string licensePublicKey, XmlDocument doc)
		{
			var rsa = new RSACryptoServiceProvider();
			rsa.FromXmlString(licensePublicKey);

			var nsMgr = new XmlNamespaceManager(doc.NameTable);
			nsMgr.AddNamespace("sig", "http://www.w3.org/2000/09/xmldsig#");

			var signedXml = new SignedXml(doc);
			var sig = (XmlElement) doc.SelectSingleNode("//sig:Signature", nsMgr);
			if (sig == null)
				return false;
			signedXml.LoadXml(sig);

			return signedXml.CheckSignature(rsa);
		}

		private bool ValidateFloatingLicense(string publicKeyOfFloatingLicense)
		{
			if (DisableFloatingLicenses)
				return false;
			if (licenseServerUrl == null)
				throw new InvalidOperationException("Floating license encountered, but licenseServerUrl was not set");

			var success = false;

			// not support .
			return success;
		}

		private bool ValidateSubscription()
		{
			if ((ExpirationDate - DateTime.UtcNow).TotalDays > 4)
				return true;

			if (currentlyValidatingSubscriptionLicense)
				return DateTime.UtcNow < ExpirationDate;

			if (SubscriptionEndpoint == null)
				throw new InvalidOperationException(
					"Subscription endpoints are not supported for this license validator");

			try
			{
				TryGettingNewLeaseSubscription();
			}
			catch
			{
				throw;
			}

			return ValidateWithoutUsingSubscriptionLeasing();
		}

		private bool ValidateWithoutUsingSubscriptionLeasing()
		{
			currentlyValidatingSubscriptionLicense = true;
			try
			{
				return HasExistingLicense();
			}
			finally
			{
				currentlyValidatingSubscriptionLicense = false;
			}
		}
	}

	/// <summary>
	///     InvalidationType
	/// </summary>
	public enum InvalidationType
	{
		/// <summary>
		///     Can not create a new license
		/// </summary>
		CannotGetNewLicense,

		/// <summary>
		///     License is expired
		/// </summary>
		TimeExpired
	}
}