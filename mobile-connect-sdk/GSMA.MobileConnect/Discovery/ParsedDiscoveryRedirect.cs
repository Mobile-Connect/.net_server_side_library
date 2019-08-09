﻿namespace GSMA.MobileConnect.Discovery
{
    /// <summary>
    /// Class to hold details parsed from the discovery redirect
    /// </summary>
    public class ParsedDiscoveryRedirect
    {
        private readonly string _selectedMCC;
        private readonly string _selectedMNC;
        private readonly string _encryptedMSISDN;
        private readonly string _loginHintToken;

        /// <summary>
        /// The Mobile Country Code of the selected operator
        /// </summary>
        public string SelectedMCC
        {
            get { return _selectedMCC; }
        }

        /// <summary>
        /// The Mobile Network Code of the selected operator
        /// </summary>
        public string SelectedMNC
        {
            get { return _selectedMNC; }
        }

        /// <summary>
        /// The encrypted MSISDN is specified
        /// </summary>
        public string EncryptedMSISDN
        {
            get { return _encryptedMSISDN; }
        }

        /// <summary>
        /// The encrypted MSISDN is specified
        /// </summary>
        public string LoginHintToken
        {
            get { return _loginHintToken; }
        }

        /// <summary>
        /// Returns true if data exists for MCC and MNC
        /// </summary>
        public bool HasMCCAndMNC
        {
            get { return !string.IsNullOrEmpty(SelectedMCC) && !string.IsNullOrEmpty(SelectedMNC); }
        }

        /// <summary>
        /// Creates a ParsedDiscoveryRedirect instance with the specified values
        /// </summary>
        /// <param name="selectedMCC">The selected mobile country code</param>
        /// <param name="selectedMNC">The selected mobile network code</param>
        /// <param name="encryptedMSISDN">The encrypted MSISDN or subscriber id</param>
        /// <param name="loginHintToken">subscriber id token</param>
        public ParsedDiscoveryRedirect(string selectedMCC, string selectedMNC, string encryptedMSISDN, string loginHintToken)
        {
            this._selectedMCC = selectedMCC;
            this._selectedMNC = selectedMNC;
            this._encryptedMSISDN = encryptedMSISDN;
            this._loginHintToken = loginHintToken;
        }
    }
}
